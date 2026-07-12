using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Thu hồi token Active hiện hành + cấp token Active mới NGUYÊN TỬ (thiết kế §4, Property 2): một SaveChanges
/// (revoke cũ + insert mới cùng transaction). Race 2 admin rotate → <c>ux_qr_active</c> phân xử →
/// <see cref="UniqueConstraintViolationException"/> → <see cref="RoomsErrors.QrGenerationFailed"/> (deterministic,
/// KHÔNG retry ẩn). Token cũ giữ lại (Revoked) — lịch sử (Req 7.4). Inject <see cref="IRepository{T}"/> trực tiếp (DV-002).
/// </summary>
public sealed class RotateRoomTokenUseCase : IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>
{
    private readonly IRepository<Room> _rooms;
    private readonly IRepository<RoomQrToken> _tokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ITokenGenerator _tokenGenerator;

    public RotateRoomTokenUseCase(
        IRepository<Room> rooms,
        IRepository<RoomQrToken> tokens,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ITokenGenerator tokenGenerator)
    {
        _rooms = rooms;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<RotateRoomTokenResult>> ExecuteAsync(RotateRoomTokenInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var room = await _rooms.FindByIdAsync(input.RoomId, ct).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Failure<RotateRoomTokenResult>(RoomsErrors.RoomNotFound);
        }

        if (room.Status != RoomStatus.Active)
        {
            return Result.Failure<RotateRoomTokenResult>(RoomsErrors.QrGenerationFailed);
        }

        // ≤1 do ux_qr_active. Load tracked → mutate persist ở SaveChanges.
        var current = await _tokens
            .FirstOrDefaultAsync(t => t.RoomId == input.RoomId && t.Status == RoomQrTokenStatus.Active, ct)
            .ConfigureAwait(false);

        var now = _clock.UtcNow;
        if (current is not null)
        {
            current.Status = RoomQrTokenStatus.Revoked;
            current.RevokedAt = now;
            current.RevokedByUserId = _currentUser.UserId;
            current.RevocationReason = input.Reason;
        }

        var token = await RoomTokenFactory.GenerateUniqueTokenAsync(_tokens, _tokenGenerator, ct).ConfigureAwait(false);
        if (token is null)
        {
            return Result.Failure<RotateRoomTokenResult>(RoomsErrors.QrGenerationFailed);
        }

        var preview = RoomTokenFactory.Mask(token);
        _tokens.Add(new RoomQrToken
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
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<RotateRoomTokenResult>(RoomsErrors.QrGenerationFailed);
        }

        return Result.Success(new RotateRoomTokenResult(token, preview));
    }
}
