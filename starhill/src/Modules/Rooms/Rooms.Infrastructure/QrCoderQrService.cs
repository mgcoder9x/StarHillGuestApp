using QRCoder;
using Rooms.Application;

namespace Rooms.Infrastructure;

/// <summary>
/// Impl <see cref="IQrService"/> bằng QRCoder <see cref="PngByteQRCode"/> — PNG THUẦN MANAGED (không phụ thuộc
/// System.Drawing/SkiaSharp) → chạy đa nền tảng (kể cả CI Linux, máy không Docker). ECC Level Q (~25% phục hồi)
/// cân bằng độ bền QR dán vs mật độ; pixelsPerModule=20 đủ nét in nhãn. Đăng ký singleton thủ công trong
/// <c>AddRoomsInfrastructure</c> (Bedrock không auto-scan). Port nguyên vẹn từ resort-qr (SharedKernel→Bedrock).
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
