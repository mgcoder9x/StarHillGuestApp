using ResortConfig.Contracts.Queries;

namespace Rooms.IntegrationTests;

/// <summary>
/// Stub <see cref="IResortExistenceQuery"/> cho test Rooms (QR-AD-016): Rooms test KHÔNG có DB ResortConfig, nên
/// resort tồn tại được STUB (cô lập module — impl EF thật đã test ở ResortConfig). Mặc định trả <c>true</c> để
/// CreateRoom happy-path chạy; đặt <c>false</c> khi muốn kiểm nhánh resort-không-tồn-tại (đã phủ ở Rooms.UnitTests).
/// </summary>
internal sealed class TestResortExistenceQuery(bool exists = true) : IResortExistenceQuery
{
    public Task<bool> ExistsAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(exists);
}
