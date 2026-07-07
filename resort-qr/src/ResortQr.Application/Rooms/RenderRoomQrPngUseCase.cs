using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Rooms;

/// <summary>
/// Render QR PNG cho token Active của phòng (16 §6). URL = {GuestWebBaseUrl}/r/{token} — chỉ chứa URL,
/// KHÔNG số phòng trần (Req 1.6). baseUrl phải là https tuyệt đối hợp lệ (Req 15.6) — không hardcode host.
/// </summary>
public sealed class RenderRoomQrPngUseCase : IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>, IScopedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQrService _qrService;

    public RenderRoomQrPngUseCase(IUnitOfWork unitOfWork, IQrService qrService)
    {
        _unitOfWork = unitOfWork;
        _qrService = qrService;
    }

    public async Task<Result<RenderRoomQrPngResult>> ExecuteAsync(RenderRoomQrPngInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var room = await _unitOfWork.Repository<Room>().FindByIdAsync(input.RoomId, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Fail<RenderRoomQrPngResult>(RoomsErrors.RoomNotFound);
        }

        if (room.Status != RoomStatus.Active)
        {
            return Result.Fail<RenderRoomQrPngResult>(RoomsErrors.QrGenerationFailed);
        }

        var settings = await _unitOfWork.Repository<ResortSettings>()
            .FirstOrDefaultAsync(s => s.ResortId == room.ResortId, cancellationToken)
            .ConfigureAwait(false);

        var baseUrl = settings?.GuestWebBaseUrl;
        if (!IsValidHttpsBaseUrl(baseUrl))
        {
            return Result.Fail<RenderRoomQrPngResult>(RoomsErrors.InvalidConfiguration);
        }

        var token = await _unitOfWork.Repository<RoomQrToken>()
            .FirstOrDefaultAsync(t => t.RoomId == input.RoomId && t.Status == RoomQrTokenStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        if (token is null)
        {
            return Result.Fail<RenderRoomQrPngResult>(RoomsErrors.QrGenerationFailed);
        }

        var url = $"{baseUrl!.TrimEnd('/')}/r/{token.Token}";
        var png = _qrService.RenderPng(url);
        return Result.Ok(new RenderRoomQrPngResult(png));
    }

    private static bool IsValidHttpsBaseUrl(string? baseUrl) =>
        !string.IsNullOrWhiteSpace(baseUrl)
        && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
}
