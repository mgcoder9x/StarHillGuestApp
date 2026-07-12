using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Queries;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Render QR PNG cho token Active của phòng (thiết kế §4/§6). URL = <c>{GuestWebBaseUrl}/r/{token}</c> — CHỈ chứa
/// URL, KHÔNG số phòng trần (Req 1.6, CP1). <c>GuestWebBaseUrl</c> phải là https tuyệt đối hợp lệ (Req 15.6) —
/// không hardcode host. QR-DV-003: đọc base-url qua <see cref="IResortSettingsQuery"/> (Contracts, cross-module)
/// thay vì repository ResortSettings (khác module/schema — không FK/ref chéo-schema). Inject <see cref="IRepository{T}"/>
/// trực tiếp (DV-002). Thứ tự kiểm: phòng tồn tại → phòng Active → cấu hình https hợp lệ → có token Active → render.
/// </summary>
public sealed class RenderRoomQrPngUseCase : IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>
{
    private readonly IRepository<Room> _rooms;
    private readonly IRepository<RoomQrToken> _tokens;
    private readonly IResortSettingsQuery _settingsQuery;
    private readonly IQrService _qrService;

    public RenderRoomQrPngUseCase(
        IRepository<Room> rooms,
        IRepository<RoomQrToken> tokens,
        IResortSettingsQuery settingsQuery,
        IQrService qrService)
    {
        _rooms = rooms;
        _tokens = tokens;
        _settingsQuery = settingsQuery;
        _qrService = qrService;
    }

    public async Task<Result<RenderRoomQrPngResult>> ExecuteAsync(RenderRoomQrPngInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var room = await _rooms.FindByIdAsync(input.RoomId, ct).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Failure<RenderRoomQrPngResult>(RoomsErrors.RoomNotFound);
        }

        if (room.Status != RoomStatus.Active)
        {
            return Result.Failure<RenderRoomQrPngResult>(RoomsErrors.QrGenerationFailed);
        }

        // Cross-module đọc cấu hình qua Contracts (single-resort → một bản ghi settings). Null/không-https → lỗi cấu hình.
        var settings = await _settingsQuery.GetAsync(ct).ConfigureAwait(false);
        var baseUrl = settings?.GuestWebBaseUrl;
        if (!IsValidHttpsBaseUrl(baseUrl))
        {
            return Result.Failure<RenderRoomQrPngResult>(RoomsErrors.InvalidConfiguration);
        }

        var token = await _tokens
            .FirstOrDefaultAsync(t => t.RoomId == input.RoomId && t.Status == RoomQrTokenStatus.Active, ct)
            .ConfigureAwait(false);
        if (token is null)
        {
            return Result.Failure<RenderRoomQrPngResult>(RoomsErrors.QrGenerationFailed);
        }

        var url = $"{baseUrl!.TrimEnd('/')}/r/{token.Token}";
        var png = _qrService.RenderPng(url);
        return Result.Success(new RenderRoomQrPngResult(png));
    }

    private static bool IsValidHttpsBaseUrl(string? baseUrl) =>
        !string.IsNullOrWhiteSpace(baseUrl)
        && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
}
