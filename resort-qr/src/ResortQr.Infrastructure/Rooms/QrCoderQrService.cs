using ResortQr.Application.Rooms;
using QRCoder;

namespace ResortQr.Infrastructure.Rooms;

/// <summary>
/// Impl <see cref="IQrService"/> bằng QRCoder <see cref="PngByteQRCode"/> — PNG THUẦN MANAGED (không phụ thuộc
/// System.Drawing/SkiaSharp) → chạy đa nền tảng. ECC Level Q (~25% phục hồi) cân bằng độ bền QR dán vs mật độ;
/// pixelsPerModule=20 đủ nét in nhãn. Nằm ngoài namespace Persistence → auto-đăng ký (ISingletonService).
/// </summary>
public sealed class QrCoderQrService : IQrService
{
    private const int PixelsPerModule = 20;

    public byte[] RenderPng(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(PixelsPerModule);
    }
}
