using ResortQr.Application.Abstractions;
using ResortQr.Application.Abstractions.Security;
using ResortQr.Domain.Identity;
using ResortQr.Domain.Resorts;
using Microsoft.EntityFrameworkCore;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// Seed khởi tạo idempotent: resort + ResortSettings mặc định + ResortLanguage (en default, vi/ko/zh)
/// + tài khoản admin (băm Argon2id). Chạy khi <c>Database:MigrateOnStartup=true</c>. Chạy lại KHÔNG nhân đôi.
/// </summary>
public sealed class ResortSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;

    public ResortSeeder(AppDbContext db, IPasswordHasher passwordHasher, IDateTimeProvider clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task SeedAsync(string adminEmail, string adminPassword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminPassword);

        var resort = await _db.Resorts.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (resort is null)
        {
            resort = new Resort
            {
                Name = "Star Hill Resort",
                Timezone = "Asia/Ho_Chi_Minh",
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
        await SeedAdminAsync(resort.Id, adminEmail, adminPassword, cancellationToken).ConfigureAwait(false);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedLanguagesAsync(Guid resortId, CancellationToken cancellationToken)
    {
        (string Code, string Name, bool IsDefault)[] languages =
        [
            ("en", "English", true),
            ("vi", "Tiếng Việt", false),
            ("ko", "한국어", false),
            ("zh", "中文", false),
        ];

        var existing = await _db.ResortLanguages
            .Where(l => l.ResortId == resortId)
            .Select(l => l.Code)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var sortOrder = 0;
        foreach (var (code, name, isDefault) in languages)
        {
            if (!existing.Contains(code))
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

    private async Task SeedAdminAsync(Guid resortId, string adminEmail, string adminPassword, CancellationToken cancellationToken)
    {
        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
        var exists = await _db.AppUsers
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        _db.AppUsers.Add(new AppUser
        {
            ResortId = resortId,
            Email = normalizedEmail,
            DisplayName = "Administrator",
            PasswordHash = _passwordHasher.Hash(adminPassword),
            Role = UserRole.Admin,
            IsActive = true,
        });
    }
}
