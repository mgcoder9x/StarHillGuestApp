using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;
using ResortConfig.Domain;

namespace ResortConfig.Infrastructure.Persistence;

/// <summary>
/// Seed khởi tạo IDEMPOTENT cho module ResortConfig (Req 12.4): 1 Resort + ResortSettings mặc định +
/// ResortLanguage (en default, vi/ko/zh). Chạy lại KHÔNG nhân đôi. Gọi từ Host sau migrate (gated cờ dev/compose).
/// KHÔNG seed tài khoản admin — đó là trách nhiệm module Identity (data ownership per-module). Port từ resort-qr
/// <c>ResortSeeder</c> (bỏ phần Identity). Seeder runtime thay HasData (QR-AD-008 — tránh xung đột xmin/audit).
/// </summary>
public sealed class ResortConfigSeeder
{
    private static readonly (string Code, string Name, bool IsDefault)[] Languages =
    [
        ("en", "English", true),
        ("vi", "Tiếng Việt", false),
        ("ko", "한국어", false),
        ("zh", "中文", false),
    ];

    private readonly ResortConfigDbContext _db;
    private readonly IClock _clock;

    public ResortConfigSeeder(ResortConfigDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Tên resort mặc định khi seed lần đầu (có thể chỉnh sau qua admin settings — slice B.3).</summary>
    public async Task SeedAsync(
        string resortName = "Star Hill Resort",
        string timezone = "Asia/Ho_Chi_Minh",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resortName);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezone);

        var resort = await _db.Resorts.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (resort is null)
        {
            resort = new Resort
            {
                Name = resortName,
                Timezone = timezone,
                CreatedAt = _clock.UtcNow,
            };
            _db.Resorts.Add(resort);
        }

        var hasSettings = await _db.ResortSettingsSet
            .AnyAsync(s => s.ResortId == resort.Id, cancellationToken).ConfigureAwait(false);
        if (!hasSettings)
        {
            _db.ResortSettingsSet.Add(new ResortSettings { ResortId = resort.Id });
        }

        await SeedLanguagesAsync(resort.Id, cancellationToken).ConfigureAwait(false);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedLanguagesAsync(Guid resortId, CancellationToken cancellationToken)
    {
        var existing = await _db.ResortLanguages
            .Where(l => l.ResortId == resortId)
            .Select(l => l.Code)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var sortOrder = 0;
        foreach (var (code, name, isDefault) in Languages)
        {
            if (!existing.Contains(code, StringComparer.OrdinalIgnoreCase))
            {
                _db.ResortLanguages.Add(new ResortLanguage
                {
                    ResortId = resortId,
                    Code = code,
                    DisplayName = name,
                    IsEnabled = true,
                    IsDefault = isDefault,
                    SortOrder = sortOrder,
                });
            }

            sortOrder++;
        }
    }
}
