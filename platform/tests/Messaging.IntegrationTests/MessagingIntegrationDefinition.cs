using Xunit;

namespace Messaging.IntegrationTests;

/// <summary>
/// Serialize các lớp e2e messaging (mỗi lớp tự spin Postgres + RabbitMQ qua Testcontainers). Đặt CHUNG một
/// collection ⇒ xUnit chạy TUẦN TỰ (collection là đơn vị song song hoá) — tránh 4 container chạy đồng thời gây
/// tranh tài nguyên Docker (flaky). KHÔNG dùng fixture chung: mỗi lớp cần bộ container/exchange riêng cho cô lập
/// dữ liệu; chỉ cần loại bỏ song song. Cùng tinh thần N-053 (serialize integration nặng).
/// </summary>
[CollectionDefinition(Name)]
public sealed class MessagingIntegrationDefinition
{
    public const string Name = "messaging-integration";
}
