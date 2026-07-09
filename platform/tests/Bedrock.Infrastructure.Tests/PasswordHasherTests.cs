using Bedrock.Infrastructure.Cryptography;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 9.1 — Argon2id password hasher. Dùng tham số NHỎ (m=1 MiB, t=1) cho test nhanh; đúng đắn thuật toán
/// (PHC roundtrip, verify hằng-thời-gian, salt ngẫu nhiên, fail-safe khi hash hỏng) không phụ thuộc cost.
/// </summary>
public sealed class PasswordHasherTests
{
    private static Argon2idPasswordHasher CreateHasher() =>
        new(new PasswordHashingOptions { MemoryKib = 1024, Iterations = 1, DegreeOfParallelism = 1 });

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
}
