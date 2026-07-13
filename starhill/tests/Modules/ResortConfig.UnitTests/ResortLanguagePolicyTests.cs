using ResortConfig.Domain;
using Xunit;

namespace ResortConfig.UnitTests;

/// <summary>
/// GUARD P1(b) — bất biến "đúng MỘT ngôn ngữ mặc định + default đang bật" của tập ngôn ngữ một resort, và thao tác
/// đặt-mặc-định NGUYÊN TỬ (<see cref="ResortLanguagePolicy"/>). Thuần, KHÔNG DB → chạy mọi máy (không cần Docker).
/// Chốt: DB index <c>ux_lang_default</c> chỉ chặn "&gt;1" và Npgsql-only → bất biến "đúng-một + enabled" do miền giữ.
/// </summary>
public sealed class ResortLanguagePolicyTests
{
    private static readonly Guid Rid = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ResortLanguage Lang(string code, bool isDefault, bool isEnabled = true) => new()
    {
        ResortId = Rid,
        Code = code,
        DisplayName = code.ToUpperInvariant(),
        IsEnabled = isEnabled,
        IsDefault = isDefault,
    };

    private static ResortLanguage[] SeedSet() =>
    [
        Lang("en", isDefault: true),
        Lang("vi", isDefault: false),
        Lang("ko", isDefault: false),
        Lang("zh", isDefault: false),
    ];

    // ---- HasExactlyOneDefault / SatisfiesInvariant ----

    [Fact]
    public void HasExactlyOneDefault_true_for_single_default()
    {
        Assert.True(ResortLanguagePolicy.HasExactlyOneDefault(SeedSet()));
    }

    [Fact]
    public void HasExactlyOneDefault_false_for_zero_default()
    {
        var set = new[] { Lang("en", false), Lang("vi", false) };
        Assert.False(ResortLanguagePolicy.HasExactlyOneDefault(set));
    }

    [Fact]
    public void HasExactlyOneDefault_false_for_two_defaults()
    {
        var set = new[] { Lang("en", true), Lang("vi", true) };
        Assert.False(ResortLanguagePolicy.HasExactlyOneDefault(set));
    }

    [Fact]
    public void SatisfiesInvariant_false_when_default_is_disabled()
    {
        var set = new[] { Lang("en", isDefault: true, isEnabled: false), Lang("vi", false) };
        Assert.False(ResortLanguagePolicy.SatisfiesInvariant(set)); // default bị tắt = vô nghĩa fallback.
    }

    [Fact]
    public void SatisfiesInvariant_true_for_enabled_single_default()
    {
        Assert.True(ResortLanguagePolicy.SatisfiesInvariant(SeedSet()));
    }

    // ---- TrySetDefault (atomic transition) ----

    [Fact]
    public void TrySetDefault_switches_default_atomically_and_keeps_exactly_one()
    {
        var set = SeedSet();

        var ok = ResortLanguagePolicy.TrySetDefault(set, "vi");

        Assert.True(ok);
        Assert.True(ResortLanguagePolicy.HasExactlyOneDefault(set));
        Assert.True(set.Single(l => l.Code == "vi").IsDefault);
        Assert.False(set.Single(l => l.Code == "en").IsDefault); // default cũ được gỡ trong CÙNG lượt.
    }

    [Fact]
    public void TrySetDefault_is_case_insensitive()
    {
        var set = SeedSet();
        Assert.True(ResortLanguagePolicy.TrySetDefault(set, "VI"));
        Assert.True(set.Single(l => l.Code == "vi").IsDefault);
    }

    [Fact]
    public void TrySetDefault_idempotent_when_target_already_default()
    {
        var set = SeedSet();
        Assert.True(ResortLanguagePolicy.TrySetDefault(set, "en"));
        Assert.True(ResortLanguagePolicy.HasExactlyOneDefault(set));
        Assert.True(set.Single(l => l.Code == "en").IsDefault);
    }

    [Fact]
    public void TrySetDefault_unknown_code_returns_false_and_leaves_invariant_unchanged()
    {
        var set = SeedSet();

        var ok = ResortLanguagePolicy.TrySetDefault(set, "fr");

        Assert.False(ok);
        Assert.True(set.Single(l => l.Code == "en").IsDefault); // không đổi.
        Assert.True(ResortLanguagePolicy.HasExactlyOneDefault(set));
    }

    [Fact]
    public void TrySetDefault_disabled_target_returns_false_and_does_not_change_default()
    {
        var set = new[] { Lang("en", isDefault: true), Lang("vi", isDefault: false, isEnabled: false) };

        var ok = ResortLanguagePolicy.TrySetDefault(set, "vi");

        Assert.False(ok); // không được đặt ngôn ngữ tắt làm mặc định.
        Assert.True(set.Single(l => l.Code == "en").IsDefault);
        Assert.False(set.Single(l => l.Code == "vi").IsDefault);
    }
}
