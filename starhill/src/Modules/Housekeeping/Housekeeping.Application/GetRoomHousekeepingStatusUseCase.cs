using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Housekeeping.Application;

/// <summary>
/// Khách xem trạng thái ticket dọn phòng hiện hành của phòng (Req 6.7 — đã tiếp nhận/đang làm/đã xong). Read-only →
/// <see cref="IUseCase{TInput,TOutput}"/> không transaction. Trả ticket MỚI NHẤT (mọi trạng thái) qua read-model
/// <see cref="IHousekeepingReader"/> — <c>null</c> nếu phòng chưa từng có ticket. KHÔNG rule-gate (đọc trạng thái của
/// chính mình là phụ trợ; tạo mới mới chịu gate). Endpoint resolve context để lấy roomId hợp lệ.
/// </summary>
public sealed class GetRoomHousekeepingStatusUseCase
    : IUseCase<GetRoomHousekeepingStatusInput, GetRoomHousekeepingStatusResult>
{
    private readonly IHousekeepingReader _reader;

    public GetRoomHousekeepingStatusUseCase(IHousekeepingReader reader) => _reader = reader;

    public async Task<Result<GetRoomHousekeepingStatusResult>> ExecuteAsync(
        GetRoomHousekeepingStatusInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ticket = await _reader.GetCurrentTicketByRoomAsync(input.RoomId, ct).ConfigureAwait(false);
        return Result.Success(new GetRoomHousekeepingStatusResult(ticket));
    }
}
