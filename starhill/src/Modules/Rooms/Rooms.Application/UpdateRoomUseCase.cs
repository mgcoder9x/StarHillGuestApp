using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using Rooms.Contracts;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Sửa thông tin phòng (thiết kế §4). Trùng số phòng đang sống (<c>ux_room_number</c>) → <see cref="RoomsErrors.RoomNumberTaken"/>;
/// không tồn tại/đã xóa mềm → <see cref="RoomsErrors.RoomNotFound"/>. Inject <see cref="IRepository{T}"/> trực tiếp (DV-002).
/// </summary>
public sealed class UpdateRoomUseCase : ICommandUseCase<UpdateRoomInput>
{
    // KEYED (P0-1): use case ghi khai module key → TransactionCommandUseCaseDecorator resolve ĐÚNG Unit of Work
    // của Rooms (không còn last-registration-wins). Cùng key/scope với IUnitOfWork inject bên dưới → nhất quán.
    public string PersistenceKey => RoomsModule.PersistenceKey;

    private readonly IRepository<Room> _rooms;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoomUseCase(IRepository<Room> rooms, IUnitOfWork unitOfWork)
    {
        _rooms = rooms;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(UpdateRoomInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var room = await _rooms.FindByIdAsync(input.RoomId, ct).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Failure(RoomsErrors.RoomNotFound);
        }

        room.RoomNumber = input.RoomNumber;
        room.Building = input.Building;
        room.Floor = input.Floor;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure(RoomsErrors.RoomNumberTaken);
        }

        return Result.Success();
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
