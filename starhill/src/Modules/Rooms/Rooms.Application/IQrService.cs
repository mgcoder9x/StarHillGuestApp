namespace Rooms.Application;

/// <summary>
/// Render QR PNG từ URL (thiết kế §5). Port ở Application; impl (QRCoder) ở Infrastructure. Sync (CPU-bound, ms).
/// Port thuần: KHÔNG kế thừa marker DI Bedrock (khác resort-qr <c>: ISingletonService</c>) — đăng ký thủ công
/// singleton trong <c>AddRoomsInfrastructure</c> (mirror cách Bedrock/Identity đăng ký, không auto-scan).
/// </summary>
public interface IQrService
{
    /// <summary>Sinh ảnh PNG (bytes) mã hoá <paramref name="url"/>.</summary>
    byte[] RenderPng(string url);
}
