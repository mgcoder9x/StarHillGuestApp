using Bedrock.Infrastructure.Cryptography;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 9.1 — Argon2id password hasher. Dùng mức policy tối thiểu (m=8 MiB, t=1) cho test; đúng đắn thuật toán
/// (PHC roundtrip, verify hằng-thời-gian, salt ngẫu nhiên, fail-safe khi hash hỏng) không phụ thuộc cost.
/// </summary>
public sealed class PasswordHasherTests
{
    private static Argon2idPasswordHasher CreateHasher() =>
        new(new PasswordHashingOptions { MemoryKib = 8192, Iterations = 1, DegreeOfParallelism = 1 });

    [Fact]
    public void Hash_then_verify_correct_password_succeeds()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_wrong_password_fails()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("right-password");

        Assert.False(hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Verify_malformed_hash_returns_false_not_throw()
    {
        var hasher = CreateHasher();

        Assert.False(hasher.Verify("any", "not-a-valid-phc-string"));
        Assert.False(hasher.Verify("any", string.Empty));
    }

    [Fact]
    public void Hash_uses_argon2id_phc_format()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("pw");

        Assert.StartsWith("$argon2id$v=19$", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_hashes_of_same_password_differ_due_to_random_salt()
    {
        var hasher = CreateHasher();

        Assert.NotEqual(hasher.Hash("same"), hasher.Hash("same"));
    }

    [Fact]
    public void Verify_rejects_tampered_hash_with_out_of_bounds_params() // A-17: chống DoS — từ chối TRƯỚC khi cấp phát
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("pw"); // "$argon2id$v=19$m=8192,t=1,p=1$..$.."

        // Kẻ tấn công/DB hỏng đặt memory vượt trần policy (MaxMemoryKib = 1 GiB) → nếu tin mù sẽ cấp phát ~2GB.
        var tampered = hash.Replace("m=8192,", "m=2000000,", StringComparison.Ordinal);

        // IsWithinBounds từ chối tham số vượt policy → Verify trả false, KHÔNG gọi Compute (không cấp phát khủng).
        Assert.False(hasher.Verify("pw", tampered));
    }

    [Fact]
    public void Verify_rejects_unsupported_version() // A-17: enforce Argon2 v=19 (0x13)
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("pw");

        var wrongVersion = hash.Replace("$v=19$", "$v=18$", StringComparison.Ordinal);

        Assert.False(hasher.Verify("pw", wrongVersion)); // version khác 19 → parse từ chối → false (không tính).
    }

    [Fact]
    public void Oversized_password_is_bounded() // A-17: chặn input password khổng lồ (DoS qua cost hash)
    {
        var hasher = CreateHasher();
        var huge = new string('a', 4097); // 4097 byte UTF-8 > trần 4096.

        Assert.Throws<ArgumentException>(() => hasher.Hash(huge)); // Hash chặn tường minh.
        Assert.False(hasher.Verify(huge, hasher.Hash("pw")));      // Verify trả false (không tính) với password quá dài.
    }
}
