using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Common;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Rooms;

/// <summary>
/// Xóa MỀM phòng (16 §2): Repository.Remove → convention chuyển IsDeleted=true (giữ token/visit/lịch sử,
/// không vỡ FK — 04 §7.3). Không tồn tại → not_found. Input là RoomId.
/// </summary>
public sealed class DeleteRoomUseCase : ICommandUseCase<Guid>, IScopedService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoomUseCase(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result> ExecuteAsync(Guid input, CancellationToken cancellationToken = default)
    {
        var rooms = _unitOfWork.Repository<Room>();
        var room = await rooms.FindByIdAsync(input, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Fail(RoomsErrors.RoomNotFound);
        }

        rooms.Remove(room);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
