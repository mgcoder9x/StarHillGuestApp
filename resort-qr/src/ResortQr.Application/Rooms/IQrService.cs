using ResortQr.SharedKernel.DependencyInjection;

namespace ResortQr.Application.Rooms;

/// <summary>
/// Render QR PNG từ URL (02 §5). Port ở Application; impl (QRCoder) ở Infrastructure. Sync (CPU-bound, ms).
/// </summary>
public interface IQrService : ISingletonService
{
    /// <summary>Sinh ảnh PNG (bytes) mã hoá <paramref name="url"/>.</summary>
    byte[] RenderPng(string url);
}
