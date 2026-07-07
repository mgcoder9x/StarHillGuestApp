using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Rooms;

/// <summary>Đổi trạng thái phòng (Active/Inactive/Maintenance — 16 §2). Không tồn tại → not_found.</summary>
public sealed class ChangeRoomStatusUseCase : ICommandUseCase<ChangeRoomStatusInput>, IScopedService
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangeRoomStatusUseCase(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result> ExecuteAsync(ChangeRoomStatusInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var rooms = _unitOfWork.Repository<Room>();
        var room = await rooms.FindByIdAsync(input.RoomId, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Fail(RoomsErrors.RoomNotFound);
        }

        room.Status = input.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
