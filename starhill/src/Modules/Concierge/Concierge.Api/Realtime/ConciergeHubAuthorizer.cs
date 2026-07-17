using System.Security.Claims;
using Concierge.Application;
using GuestAccess.Contracts;
using StarHill.Authorization;

namespace Concierge.Api.Realtime;

/// <summary>Kết quả phân giải quyền join của KHÁCH: <see cref="ContextValid"/> = cookie/phiên hợp lệ;
/// <see cref="ConversationId"/> = hội thoại của chính khách (null nếu khách chưa mở hội thoại nào).</summary>
public sealed record GuestJoinResolution(bool ContextValid, Guid? ConversationId);

/// <summary>
/// Logic auth-on-join của <see cref="ChatHub"/> TÁCH khỏi hub (thuần, test được KHÔNG cần SignalR). Bản chất bảo mật
/// (Req 5.5/5.6): khách CHỈ được join hội thoại CỦA CHÍNH MÌNH — server BỎ QUA conversationId client gửi, tự resolve
/// ngữ cảnh từ cookie phiên (<see cref="ICurrentGuestContextResolver"/>) rồi tra hội thoại của visit đó
/// (<see cref="IConciergeReader.GetGuestConversationByVisitAsync"/>). Nhân viên nhận diện bằng claim <c>role</c>
/// (staff/admin) — KHÔNG resolve cookie khách.
/// </summary>
public sealed class ConciergeHubAuthorizer
{
    private readonly ICurrentGuestContextResolver _resolver;
    private readonly IConciergeReader _reader;

    public ConciergeHubAuthorizer(ICurrentGuestContextResolver resolver, IConciergeReader reader)
    {
        _resolver = resolver;
        _reader = reader;
    }

    /// <summary>Nhân viên? Kiểm claim <c>role</c> JWT-native (base <c>RoleClaimType="role"</c>, <c>MapInboundClaims=false</c>)
    /// = staff HOẶC admin (superset). KHÔNG dùng <c>IsInRole</c> (phụ thuộc RoleClaimType của principal) — kiểm claim tường minh.</summary>
    public static bool IsStaff(ClaimsPrincipal? user) =>
        user is not null && user.Claims.Any(c =>
            string.Equals(c.Type, "role", StringComparison.Ordinal) &&
            (string.Equals(c.Value, StarHillPolicies.RoleStaff, StringComparison.Ordinal) ||
             string.Equals(c.Value, StarHillPolicies.RoleAdmin, StringComparison.Ordinal)));

    /// <summary>Phân giải hội thoại mà KHÁCH được phép join: resolve ngữ cảnh từ cookie + roomId (BỎ QUA id client gửi),
    /// tra hội thoại của visit. Cookie/phiên hỏng → <c>ContextValid=false</c>; hợp lệ nhưng chưa có hội thoại → <c>ConversationId=null</c>.</summary>
    public async Task<GuestJoinResolution> ResolveGuestConversationAsync(
        string? sessionCookie, Guid roomId, CancellationToken ct = default)
    {
        var context = await _resolver.ResolveAsync(sessionCookie, roomId, ct).ConfigureAwait(false);
        if (context.IsFailure)
        {
            return new GuestJoinResolution(ContextValid: false, ConversationId: null);
        }

        var conversation = await _reader
            .GetGuestConversationByVisitAsync(context.Value.GuestVisitId, ct)
            .ConfigureAwait(false);
        return new GuestJoinResolution(ContextValid: true, ConversationId: conversation?.ConversationId);
    }
}
