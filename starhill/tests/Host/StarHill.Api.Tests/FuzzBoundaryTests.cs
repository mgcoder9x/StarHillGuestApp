using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Bedrock.Api;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Api;
using Concierge.Application;
using Faq.Api;
using Faq.Application;
using GuestAccess.Contracts;
using Housekeeping.Api;
using Housekeeping.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResortConfig.Contracts.Queries;
using Rooms.Api;
using Rooms.Application;
using Rules.Api;
using Rules.Application;
using StarHill.Api.Tests.Authorization;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// C2 (R2, military-grade-hardening) — Fuzz biên HTTP với bất biến "không 5xx, không rò". Kiến trúc DD-11: đặt
/// TRONG project test này để tái dùng pattern + Fake* use case + <see cref="Authorization.JwtTestTokens"/> của các
/// AuthTests (map endpoint module THẬT + fake use case/resolver, KHÔNG DB/Docker → cô lập đúng biên HTTP, đạt
/// R2.9/R2.10). Malformed input bị chặn ở model-binding/validation/ProblemDetails TRƯỚC use case; well-formed rơi
/// vào fake (2xx, không chạm DB). Bất biến kiểm: KHÔNG 5xx, 4xx phải problem+json, KHÔNG rò nội bộ, admin có JWT
/// đúng role KHÔNG bị 401. Slice này phủ Rooms(admin) + Rules + Faq + Housekeeping + Concierge (admin+guest).
/// </summary>
public sealed class FuzzBoundaryTests
{
    private const int PayloadsPerEndpoint = 500; // R2.1
    private const string GuestCookieName = "__Host-starhill_guest";

    // Route thật (Glossary). Role=null → guest/anonymous; "admin"/"staff" → gắn JWT đúng policy.
    private sealed record Endpoint(string Method, string Path, bool HasBody, string? Role, bool Guest);

    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static IReadOnlyList<Endpoint> Endpoints() =>
    [
        // ── Rooms admin (RequireAdmin) ──
        new("POST", "/v1/rooms", true, StarHillPolicies.RoleAdmin, false),
        new("PUT", $"/v1/rooms/{Id}", true, StarHillPolicies.RoleAdmin, false),
        new("DELETE", $"/v1/rooms/{Id}", false, StarHillPolicies.RoleAdmin, false),
        new("POST", $"/v1/rooms/{Id}/rotate-token", true, StarHillPolicies.RoleAdmin, false),
        // ── Rules admin (RequireStaff) ──
        new("POST", "/v1/rules/sections", true, StarHillPolicies.RoleStaff, false),
        new("PUT", $"/v1/rules/sections/{Id}", true, StarHillPolicies.RoleStaff, false),
        new("PUT", $"/v1/rules/sections/{Id}/translations/en", true, StarHillPolicies.RoleStaff, false),
        new("POST", "/v1/rules/publish", true, StarHillPolicies.RoleStaff, false),
        // ── Rules guest (AllowAnonymous) ──
        new("GET", $"/v1/guest/rules?roomId={Id}", false, null, true),
        new("POST", "/v1/guest/rules/acknowledge", true, null, true),
        // ── Faq admin (RequireStaff) ──
        new("POST", "/v1/faq/categories", true, StarHillPolicies.RoleStaff, false),
        new("POST", "/v1/faq/items", true, StarHillPolicies.RoleStaff, false),
        new("PUT", $"/v1/faq/items/{Id}", true, StarHillPolicies.RoleStaff, false),
        new("POST", "/v1/faq/reorder/categories", true, StarHillPolicies.RoleStaff, false),
        // ── Faq guest ──
        new("GET", $"/v1/guest/faq?roomId={Id}", false, null, true),
        // ── Housekeeping admin (RequireStaff) ──
        new("GET", "/v1/housekeeping", false, StarHillPolicies.RoleStaff, false),
        new("POST", $"/v1/housekeeping/{Id}/status", true, StarHillPolicies.RoleStaff, false),
        new("POST", "/v1/housekeeping/complete-by-token", true, StarHillPolicies.RoleStaff, false),
        // ── Housekeeping guest ──
        new("POST", "/v1/guest/housekeeping", true, null, true),
        new("GET", $"/v1/guest/housekeeping?roomId={Id}", false, null, true),
        // ── Concierge admin (RequireStaff) ──
        new("GET", "/v1/conversations", false, StarHillPolicies.RoleStaff, false),
        new("POST", $"/v1/conversations/{Id}/reply", true, StarHillPolicies.RoleStaff, false),
        new("POST", "/v1/notes", true, StarHillPolicies.RoleStaff, false),
        // ── Concierge guest ──
        new("POST", "/v1/guest/messages", true, null, true),
        new("GET", $"/v1/guest/conversation?roomId={Id}", false, null, true),
    ];

    private static async Task<IHost> StartAsync()
    {
        var resolver = new FakeCurrentGuestContextResolver
        {
            NextResult = Result.Success(new CurrentGuestContext(
                Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), FakeResortSettingsQuery.ResortId)),
        };

        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                // Pipeline THẬT (DD-11 fix): AddBedrockApi + UseBedrockApi mang ExceptionHandlingMiddleware →
                // problem+json cho lỗi binding/validation (R2.5). AddBedrockApi gọi AddBedrockAuthCore(config) nên
                // truyền JwtTestTokens.BuildConfig() để JWT test-token được chấp nhận (khớp Kid/Issuer/Audience).
                services.AddBedrockApi(JwtTestTokens.BuildConfig());
                services.AddStarHillAuthorization();
                services.ConfigureHttpJsonOptions(o =>
                    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

                // Endpoint module đăng ký qua IEndpointModule → UseBedrockApi tự map (như Host thật).
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, RoomsEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, RulesAdminEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, RulesGuestEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, FaqAdminEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, FaqGuestEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, HousekeepingAdminEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, HousekeepingGuestEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, ConciergeAdminEndpointModule>();
                services.AddSingleton<Bedrock.Api.Endpoints.IEndpointModule, ConciergeGuestEndpointModule>();

                services.AddScoped<IResortSettingsQuery, FakeResortSettingsQuery>();
                services.AddSingleton<ICurrentGuestContextResolver>(resolver);

                // Rooms.
                services.AddScoped<IUseCase<CreateRoomInput, CreateRoomResult>, FakeCreateRoom>();
                services.AddScoped<IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>, FakeRotateToken>();
                services.AddScoped<IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>, FakeRenderQrPng>();
                services.AddScoped<ICommandUseCase<UpdateRoomInput>, FakeUpdateRoom>();
                services.AddScoped<ICommandUseCase<ChangeRoomStatusInput>, FakeChangeStatus>();
                services.AddScoped<ICommandUseCase<Guid>, FakeDeleteRoom>();
                services.AddScoped<IRoomQueries, FakeRoomQueries>();

                // Rules.
                services.AddScoped<IUseCase<CreateRuleSectionInput, CreateRuleSectionResult>, FakeCreateRuleSection>();
                services.AddScoped<ICommandUseCase<UpdateRuleSectionInput>, FakeUpdateRuleSection>();
                services.AddScoped<ICommandUseCase<DeleteRuleSectionInput>, FakeDeleteRuleSection>();
                services.AddScoped<IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>, FakeUpsertRuleTranslation>();
                services.AddScoped<IUseCase<PublishRulesInput, PublishRulesResult>, FakePublishRules>();
                services.AddScoped<IUseCase<GetDraftPreviewInput, GetDraftPreviewResult>, FakeGetDraftPreview>();
                services.AddScoped<IUseCase<GetPublicationHistoryInput, GetPublicationHistoryResult>, FakeGetPublicationHistory>();
                services.AddScoped<IUseCase<GetRuleAdminDraftInput, GetRuleAdminDraftResult>, FakeGetRuleAdminDraft>();
                services.AddSingleton<IUseCase<GetCurrentRulesInput, GetCurrentRulesResult>>(new FakeGetCurrentRules());
                services.AddSingleton<IUseCase<AcknowledgeRulesInput, AcknowledgeRulesResult>>(new FakeAcknowledgeRules());

                // Faq.
                services.AddScoped<IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>, FakeCreateFaqCategory>();
                services.AddScoped<ICommandUseCase<UpdateFaqCategoryInput>, FakeUpdateFaqCategory>();
                services.AddScoped<ICommandUseCase<DeleteFaqCategoryInput>, FakeDeleteFaqCategory>();
                services.AddScoped<IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult>, FakeUpsertFaqCategoryTranslation>();
                services.AddScoped<IUseCase<CreateFaqItemInput, CreateFaqItemResult>, FakeCreateFaqItem>();
                services.AddScoped<ICommandUseCase<UpdateFaqItemInput>, FakeUpdateFaqItem>();
                services.AddScoped<ICommandUseCase<DeleteFaqItemInput>, FakeDeleteFaqItem>();
                services.AddScoped<IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>, FakeUpsertFaqItemTranslation>();
                services.AddScoped<ICommandUseCase<ReorderFaqCategoriesInput>, FakeReorderFaqCategories>();
                services.AddScoped<ICommandUseCase<ReorderFaqItemsInput>, FakeReorderFaqItems>();
                services.AddScoped<IUseCase<GetFaqAdminTreeInput, GetFaqAdminTreeResult>, FakeGetFaqAdminTree>();
                services.AddScoped<IUseCase<GetGuestFaqTreeInput, GetGuestFaqTreeResult>, FakeGetGuestFaqTree>();

                // Housekeeping.
                services.AddScoped<IHousekeepingReader, FakeHousekeepingReader>();
                services.AddScoped<IUseCase<RequestHousekeepingInput, RequestHousekeepingResult>, FakeRequestHousekeeping>();
                services.AddScoped<IUseCase<GetRoomHousekeepingStatusInput, GetRoomHousekeepingStatusResult>, FakeGetRoomHousekeepingStatus>();
                services.AddScoped<IUseCase<SetHousekeepingStatusInput, HousekeepingTicketResult>, FakeSetHousekeepingStatus>();
                services.AddScoped<IUseCase<CompleteHousekeepingByRoomInput, HousekeepingTicketResult>, FakeCompleteHousekeepingByRoom>();
                services.AddScoped<IUseCase<CompleteHousekeepingByTokenInput, HousekeepingTicketResult>, FakeCompleteHousekeepingByToken>();
                services.AddScoped<IUseCase<CreateHousekeepingByStaffInput, RequestHousekeepingResult>, FakeCreateHousekeepingByStaff>();

                // Concierge.
                services.AddScoped<IConciergeReader, FakeConciergeReader>();
                services.AddScoped<IUseCase<SendGuestMessageInput, SendGuestMessageResult>, FakeSendGuestMessage>();
                services.AddScoped<IUseCase<GetGuestConversationInput, GetGuestConversationResult>, FakeGetGuestConversation>();
                services.AddScoped<IUseCase<ReplyConversationInput, ReplyConversationResult>, FakeReplyConversation>();
                services.AddScoped<ICommandUseCase<MarkConversationReadInput>, FakeMarkConversationRead>();
                services.AddScoped<ICommandUseCase<CloseConversationInput>, FakeCloseConversation>();
                services.AddScoped<IUseCase<CreateInternalNoteInput, CreateInternalNoteResult>, FakeCreateInternalNote>();
                services.AddScoped<ICommandUseCase<UpdateInternalNoteInput>, FakeUpdateInternalNote>();
                services.AddScoped<ICommandUseCase<DeleteInternalNoteInput>, FakeDeleteInternalNote>();
            });
            webHost.Configure(app => app.UseBedrockApi());
        });

        return await builder.StartAsync();
    }

    private static readonly string[] LeakMarkers =
        ["Exception", "StackTrace", "at Bedrock.", "at StarHill.", "Password=", "Username="];

    [Fact]
    public async Task Fuzzing_the_http_boundary_never_yields_5xx_or_leaks()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var total = 0;

        foreach (var ep in Endpoints())
        {
            for (var i = 0; i < PayloadsPerEndpoint; i++)
            {
                var seed = unchecked((ep.Method.GetHashCode(StringComparison.Ordinal) * 397) ^ (ep.Path.GetHashCode(StringComparison.Ordinal) * 31) ^ i);
                var rng = new Random(seed);
                var (request, description) = BuildRequest(ep, rng, i);
                using var response = await client.SendAsync(request);
                total++;

                await AssertInvariantsAsync(ep, response, seed, description);
            }
        }

        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(180), $"Fuzz vượt 180s: {sw.Elapsed}. (đã gửi {total} payload)");
        Assert.True(total >= Endpoints().Count * PayloadsPerEndpoint);
    }

    private static async Task AssertInvariantsAsync(Endpoint ep, HttpResponseMessage response, int seed, string description)
    {
        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync();

        // R2.6: không bao giờ 5xx.
        Assert.False(
            status is >= 500 and <= 599,
            $"5xx tại {ep.Method} {ep.Path} (seed={seed}, class={description}): {status}\n{Trim(body)}");

        // R2.12: admin có JWT đúng role KHÔNG được 401.
        if (ep.Role is not null)
        {
            Assert.False(
                status == 401,
                $"401 tại admin endpoint {ep.Method} {ep.Path} dù JWT role='{ep.Role}' (seed={seed}).");
        }

        // R2.5: 4xx phải là application/problem+json (trừ 404 route-không-khớp — router trả rỗng, không phải
        // endpoint xử lý dữ liệu vào; xem phân tích trong báo cáo). Kiểm tường minh + message giàu chẩn đoán.
        if (status is >= 400 and <= 499 && !(status == 404 && body.Length == 0))
        {
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "(none)";
            Assert.True(
                contentType.StartsWith("application/problem+json", StringComparison.Ordinal),
                $"4xx KHÔNG problem+json tại {ep.Method} {ep.Path} (seed={seed}, class={description}): "
                + $"status={status}, content-type={contentType}\nbody={Trim(body)}");
        }

        // R2.7: không rò nội bộ.
        foreach (var marker in LeakMarkers)
        {
            Assert.False(
                body.Contains(marker, StringComparison.Ordinal),
                $"Rò '{marker}' tại {ep.Method} {ep.Path} (seed={seed}, class={description}).");
        }
    }

    private static string Trim(string body) => body.Length > 500 ? body[..500] : body;

    // Sinh request biến dạng tất định. class index quyết định lớp biến dạng (R2.2 body / R2.3 GET).
    private static (HttpRequestMessage Request, string Description) BuildRequest(Endpoint ep, Random rng, int index)
    {
        var request = new HttpRequestMessage(new HttpMethod(ep.Method), new Uri(BuildPath(ep, index, out var pathClass), UriKind.Relative));

        // Auth: admin gắn JWT đúng role (R2.11); guest gắn cookie (rác ở lớp cookie-malformed).
        if (ep.Role is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue(ep.Role));
        }

        string description;
        if (ep.HasBody)
        {
            var bodyClass = index % 6; // R2.2: 6 lớp.
            var (json, contentType, desc) = bodyClass switch
            {
                0 => ("{ this is : not json ", "application/json", "malformed-json"),
                1 => ("{\"sortOrder\":\"NaN\",\"isActive\":\"yes\",\"roomId\":12345,\"minReadSeconds\":\"x\"}", "application/json", "wrong-types"),
                2 => ("{}", "application/json", "missing-required"),
                3 => ("{\"__fuzz_unknown__\":\"" + Rand(rng, 8) + "\"}", "application/json", "extra-field"),
                4 => ("{\"body\":\"" + new string('A', 5000) + "\",\"changeNote\":\"" + new string('B', 5000) + "\"}", "application/json", "overlong-string"),
                _ => ("{\"blob\":\"" + new string('C', 2048) + "\"}", "application/json", "oversized-body>1KiB"),
            };
            request.Content = new StringContent(json, Encoding.UTF8, contentType);
            description = $"body:{desc}";
        }
        else
        {
            description = $"get:{pathClass}";
        }

        // Guest (non-cookie-malformed class): gắn cookie hợp lệ hình thức để đi qua resolver-success.
        if (ep.Guest)
        {
            var cookie = pathClass == 3 ? "!!!bad cookie value!!!" : "device-key-" + Rand(rng, 12);
            request.Headers.Add("Cookie", $"{GuestCookieName}={cookie}");
        }

        return (request, description);
    }

    // GET: 4 lớp biến dạng (R2.3). Trả path đã méo + class index để BuildRequest biết lớp cookie.
    private static string BuildPath(Endpoint ep, int index, out int getClass)
    {
        getClass = ep.HasBody ? -1 : index % 4;
        if (ep.HasBody || getClass < 0)
        {
            return ep.Path;
        }

        var basePath = ep.Path;
        return getClass switch
        {
            0 => AppendQuery(basePath, "roomId=not-a-guid&page=abc"),      // query sai kiểu
            1 => AppendQuery(basePath, "note=" + new string('Z', 4000)),   // query quá dài
            2 => ReplaceIdWithGarbage(basePath),                            // path {..} sai định dạng
            _ => basePath,                                                  // cookie-malformed (xử lý ở BuildRequest)
        };
    }

    private static string AppendQuery(string path, string query) =>
        path.Contains('?', StringComparison.Ordinal) ? $"{path}&{query}" : $"{path}?{query}";

    private static string ReplaceIdWithGarbage(string path) =>
        path.Contains(Id.ToString(), StringComparison.Ordinal)
            ? path.Replace(Id.ToString(), "not-a-guid-@@@", StringComparison.Ordinal)
            : AppendQuery(path, "x=1");

    private static string Rand(Random rng, int len)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var sb = new StringBuilder(len);
        for (var i = 0; i < len; i++)
        {
            sb.Append(chars[rng.Next(chars.Length)]);
        }

        return sb.ToString();
    }
}
