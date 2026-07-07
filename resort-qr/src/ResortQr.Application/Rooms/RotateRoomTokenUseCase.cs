using ResortQr.Application.Abstractions;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Rooms;

/// <summary>
/// Thu hồi token Active hiện hành + cấp token Active mới NGUYÊN TỬ (16 §4, Property B2): một SaveChanges
/// (revoke cũ + insert mới cùng transaction). Race 2 admin rotate → ux_qr_active phân xử → unique-violation
/// → qr_generation_failed (deterministic, không retry ẩn). Token cũ giữ lại (Revoked) — lịch sử (Req 7.4).
/// </summary>
public sealed class RotateRoomTokenUseCase : IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>, IScopedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ITokenGenerator _tokenGenerator;

    public RotateRoomTokenUseCase(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ITokenGenerator tokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<RotateRoomTokenResult>> ExecuteAsync(RotateRoomTokenInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var rooms = _unitOfWork.Repository<Room>();
        var tokens = _unitOfWork.Repository<RoomQrToken>();

        var room = await rooms.FindByIdAsync(input.RoomId, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Fail<RotateRoomTokenResult>(RoomsErrors.RoomNotFound);
        }

        if (room.Status != RoomStatus.Active)
        {
            return Result.Fail<RotateRoomTokenResult>(RoomsErrors.QrGenerationFailed);
        }

        // ≤1 do ux_qr_active. Load tracked → mutate persist ở SaveChanges.
        var current = await tokens
            .FirstOrDefaultAsync(t => t.RoomId == input.RoomId && t.Status == RoomQrTokenStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        var now = _clock.UtcNow;
        if (current is not null)
        {
            current.Status = RoomQrTokenStatus.Revoked;
            current.RevokedAt = now;
            current.RevokedByUserId = _currentUser.UserId;
            current.RevocationReason = input.Reason;
        }

        var token = await RoomTokenFactory.GenerateUniqueTokenAsync(tokens, _tokenGenerator, cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return Result.Fail<RotateRoomTokenResult>(RoomsErrors.QrGenerationFailed);
        }

        var preview = RoomTokenFactory.Mask(token);
        tokens.Add(new RoomQrToken
        {
            RoomId = input.RoomId,
            Token = token,
            TokenPreview = preview,
            Status = RoomQrTokenStatus.Active,
            Version = (current?.Version ?? 0) + 1,
            CreatedAt = now,
            CreatedByUserId = _currentUser.UserId,
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Fail<RotateRoomTokenResult>(RoomsErrors.QrGenerationFailed);
        }

        return Result.Ok(new RotateRoomTokenResult(token, preview));
    }
}
