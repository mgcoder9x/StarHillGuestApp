using Xunit;

namespace Identity.IntegrationTests;

/// <summary>
/// Serialize các lớp integration của Identity (mỗi lớp tự spin Postgres qua Testcontainers). Đặt CHUNG một
/// collection ⇒ xUnit chạy TUẦN TỰ (không 2 Postgres song song) — cùng lý do chống flaky như N-053/N-060.
/// Tên KHÔNG kết thúc bằng "Collection" (CA1711, bài học N-053/N-060).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IdentityIntegrationDefinition
{
    public const string Name = "identity-integration";
}
