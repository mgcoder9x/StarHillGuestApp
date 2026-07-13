using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Rooms.Contracts;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Xóa MỀM phòng (thiết kế §4): <see cref="IRepository{T}.Remove"/> → interceptor chuyển IsDeleted=true (giữ
/// token/lịch sử, không vỡ FK nội-module). Không tồn tại/đã xóa mềm → <see cref="RoomsErrors.RoomNotFound"/>.
/// Input là RoomId. Inject <see cref="IRepository{T}"/> trực tiếp (DV-002).
/// </summary>
public sealed class DeleteRoomUseCase : ICommandUseCase<Guid>
{
    // KEYED (P0-1): khai module key → resolve đúng Unit of Work Rooms (chống last-registration-wins).
    public string PersistenceKey => RoomsModule.PersistenceKey;

    private readonly IRepository<Room> _rooms;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoomUseCase(IRepository<Room> rooms, IUnitOfWork unitOfWork)
    {
        _rooms = rooms;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid input, CancellationToken ct = default)
    {
        var room = await _rooms.FindByIdAsync(input, ct).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Failure(RoomsErrors.RoomNotFound);
        }

        _rooms.Remove(room);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
