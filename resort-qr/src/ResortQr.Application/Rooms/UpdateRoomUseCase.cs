using FluentValidation;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Rooms;

/// <summary>Sửa thông tin phòng (16 §2). Trùng số phòng đang sống → validation_error; không tồn tại → not_found.</summary>
public sealed class UpdateRoomUseCase : ICommandUseCase<UpdateRoomInput>, IScopedService
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoomUseCase(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result> ExecuteAsync(UpdateRoomInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var rooms = _unitOfWork.Repository<Room>();
        var room = await rooms.FindByIdAsync(input.RoomId, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Fail(RoomsErrors.RoomNotFound);
        }

        room.RoomNumber = input.RoomNumber;
        room.Building = input.Building;
        room.Floor = input.Floor;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Fail(RoomsErrors.RoomNumberTaken);
        }

        return Result.Ok();
    }
}

/// <summary>Validate sửa phòng (Req 16.1).</summary>
public sealed class UpdateRoomValidator : AbstractValidator<UpdateRoomInput>
{
    public UpdateRoomValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.RoomNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).InclusiveBetween(-10, 200).When(x => x.Floor.HasValue);
    }
}
