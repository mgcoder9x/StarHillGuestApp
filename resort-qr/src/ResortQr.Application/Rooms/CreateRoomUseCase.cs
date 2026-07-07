using FluentValidation;
using ResortQr.Application.Abstractions;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Rooms;

/// <summary>
/// Tạo phòng + cấp token Active NGUYÊN TỬ (16 §3): một SaveChanges = một transaction (room+token all-or-nothing).
/// Trùng số phòng đang sống (ux_room_number) → validation_error. Bất biến: đúng 1 token Active/phòng, Version=1.
/// </summary>
public sealed class CreateRoomUseCase : IUseCase<CreateRoomInput, CreateRoomResult>, IScopedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ITokenGenerator _tokenGenerator;

    public CreateRoomUseCase(
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

    public async Task<Result<CreateRoomResult>> ExecuteAsync(CreateRoomInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var resorts = _unitOfWork.Repository<Resort>();
        var rooms = _unitOfWork.Repository<Room>();
        var tokens = _unitOfWork.Repository<RoomQrToken>();

        // Single-resort (24): mỗi deployment 1 Resort. Chưa seed → lỗi cấu hình. TK-040: shared-DB lấy từ claim.
        var resort = await resorts.FirstOrDefaultAsync(_ => true, cancellationToken).ConfigureAwait(false);
        if (resort is null)
        {
            return Result.Fail<CreateRoomResult>(RoomsErrors.QrGenerationFailed);
        }

        var token = await RoomTokenFactory.GenerateUniqueTokenAsync(tokens, _tokenGenerator, cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return Result.Fail<CreateRoomResult>(RoomsErrors.QrGenerationFailed);
        }

        var now = _clock.UtcNow;
        var room = new Room
        {
            ResortId = resort.Id,
            RoomNumber = input.RoomNumber,
            Building = input.Building,
            Floor = input.Floor,
            Status = RoomStatus.Active,
        };
        rooms.Add(room);

        var preview = RoomTokenFactory.Mask(token);
        tokens.Add(new RoomQrToken
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
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // Thực tế chỉ ux_room_number khả dĩ (token đã 256-bit + pre-check). → trùng số phòng.
            return Result.Fail<CreateRoomResult>(RoomsErrors.RoomNumberTaken);
        }

        return Result.Ok(new CreateRoomResult(room.Id, token, preview));
    }
}

/// <summary>Validate input tạo phòng (Req 16.1) — decorator chặn trước khi vào use case.</summary>
public sealed class CreateRoomValidator : AbstractValidator<CreateRoomInput>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.RoomNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).InclusiveBetween(-10, 200).When(x => x.Floor.HasValue);
    }
}
