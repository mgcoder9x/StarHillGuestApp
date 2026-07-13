using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Rooms.Contracts;
using Rooms.Domain;

namespace Rooms.Application;

/// <summary>
/// Đổi trạng thái phòng (Active/Inactive/Maintenance — thiết kế §4). Không tồn tại/đã xóa mềm →
/// <see cref="RoomsErrors.RoomNotFound"/>. Inject <see cref="IRepository{T}"/> trực tiếp (DV-002).
/// </summary>
public sealed class ChangeRoomStatusUseCase : ICommandUseCase<ChangeRoomStatusInput>
{
    // KEYED (P0-1): khai module key → resolve đúng Unit of Work Rooms (chống last-registration-wins).
    public string PersistenceKey => RoomsModule.PersistenceKey;

    private readonly IRepository<Room> _rooms;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeRoomStatusUseCase(IRepository<Room> rooms, IUnitOfWork unitOfWork)
    {
        _rooms = rooms;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(ChangeRoomStatusInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var room = await _rooms.FindByIdAsync(input.RoomId, ct).ConfigureAwait(false);
        if (room is null)
        {
            return Result.Failure(RoomsErrors.RoomNotFound);
        }

        room.Status = input.Status;
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
