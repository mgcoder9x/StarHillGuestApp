namespace Bedrock.Application.Ports.Time;

/// <summary>
/// Nguồn thời gian trừu tượng (thay <c>DateTimeOffset.UtcNow</c> rải rác) → use case tất định, test được.
/// Đổi tên từ <c>IDateTimeProvider</c> (F12). Port thuần: KHÔNG kế thừa DI marker — lifetime quyết định
/// ở lúc đăng ký implementation (impl <c>SystemClock</c> khai <c>ISingletonService</c> ở Infrastructure).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
