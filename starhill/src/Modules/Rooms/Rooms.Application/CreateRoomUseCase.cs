using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using ResortConfig.Contracts.Queries;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Tạo phòng + cấp token Active NGUYÊN TỬ (thiết kế §4): một SaveChanges = một transaction (room+token
/// all-or-nothing). Bất biến: đúng 1 token Active/phòng, Version=1. Bắt <see cref="UniqueConstraintViolationException"/>
/// (base QR-AD-010): <c>ux_room_number</c> (trùng số phòng đang sống) → <see cref="RoomsErrors.RoomNumberTaken"/>;
/// ràng buộc khác (vd đua token) → <see cref="RoomsErrors.QrGenerationFailed"/>. Inject <see cref="IRepository{T}"/>
/// TRỰC TIẾP (DV-002 — IUnitOfWork Bedrock KHÔNG có Repository accessor).
/// <para>
/// P1(a) — THẨM ĐỊNH tham chiếu resort: KHÔNG có FK chéo-schema sang ResortConfig (QR-DV-003) nên toàn vẹn tham
/// chiếu phải làm ở seam ứng dụng — kiểm <c>IResortExistenceQuery.ExistsAsync(input.ResortId)</c> TRƯỚC khi tạo
/// (validator chỉ chặn ResortId rỗng, KHÔNG kiểm tồn tại). Đặt trong use case (không phải validator) vì đây là
/// kiểm STATEFUL cần DB — validator phải thuần/không I/O. Resort không tồn tại → <see cref="RoomsErrors.ResortNotFound"/>,
/// dừng SỚM (không sinh token/không ghi). Kiểm tồn tại đứng TRƯỚC token-gen để không tốn công + không side-effect.
/// </para>
/// </summary>
public sealed class CreateRoomUseCase : IUseCase<CreateRoomInput, CreateRoomResult>
{
    private readonly IRepository<Room> _rooms;
    private readonly IRepository<RoomQrToken> _tokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IResortExistenceQuery _resorts;

    public CreateRoomUseCase(
        IRepository<Room> rooms,
        IRepository<RoomQrToken> tokens,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ITokenGenerator tokenGenerator,
        IResortExistenceQuery resorts)
    {
        _rooms = rooms;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _tokenGenerator = tokenGenerator;
        _resorts = resorts;
    }

    public async Task<Result<CreateRoomResult>> ExecuteAsync(CreateRoomInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // P1(a): resort tham chiếu phải tồn tại (thay FK chéo-schema). Dừng sớm nếu không → tránh phòng mồ côi.
        if (!await _resorts.ExistsAsync(input.ResortId, ct).ConfigureAwait(false))
        {
            return Result.Failure<CreateRoomResult>(RoomsErrors.ResortNotFound);
        }

        var token = await RoomTokenFactory.GenerateUniqueTokenAsync(_tokens, _tokenGenerator, ct).ConfigureAwait(false);
        if (token is null)
        {
            return Result.Failure<CreateRoomResult>(RoomsErrors.QrGenerationFailed);
        }

        var now = _clock.UtcNow;
        var room = new Room
        {
            ResortId = input.ResortId,
            RoomNumber = input.RoomNumber,
            Building = input.Building,
            Floor = input.Floor,
            Status = RoomStatus.Active,
        };
        _rooms.Add(room);

        var preview = RoomTokenFactory.Mask(token);
        _tokens.Add(new RoomQrToken
        {
            RoomId = room.Id,
            Token = token,
            TokenPreview = preview,
            Status = RoomQrTokenStatus.Active,
            Version = 1,
            CreatedAt = now,
            CreatedByUserId = _currentUser.UserId,
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException ex)
        {
            // ux_room_number → trùng số phòng; khác (đua token hiếm khi lọt pre-check) → qr_generation_failed.
            return ex.ConstraintName == "ux_room_number"
                ? Result.Failure<CreateRoomResult>(RoomsErrors.RoomNumberTaken)
                : Result.Failure<CreateRoomResult>(RoomsErrors.QrGenerationFailed);
        }

        return Result.Success(new CreateRoomResult(room.Id, token, preview));
    }
}

/// <summary>Validate input tạo phòng (Req 16.1) — ValidationUseCaseDecorator chặn trước khi vào use case.</summary>
public sealed class CreateRoomValidator : AbstractValidator<CreateRoomInput>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.ResortId).NotEmpty();
        RuleFor(x => x.RoomNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).InclusiveBetween(-10, 200).When(x => x.Floor.HasValue);
    }
}
