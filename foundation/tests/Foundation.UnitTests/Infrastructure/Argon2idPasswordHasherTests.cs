using Foundation.Application.Abstractions.Security;
using Foundation.Infrastructure.Security;
using Xunit;

namespace Foundation.UnitTests.Infrastructure;

public sealed class Argon2idPasswordHasherTests
{
    // Tham số NHỎ để test nhanh (không phải cấu hình prod).
    private static PasswordHashingOptions FastOptions(int iterations = 1) => new()
    {
        MemoryKib = 1024,
        Iterations = iterations,
        DegreeOfParallelism = 1,
        SaltSize = 16,
        HashSize = 32,
    };

    private readonly Argon2idPasswordHasher _sut = new(FastOptions());

    [Fact]
    public void Hash_should_produce_phc_argon2id_string()
    {
        var hash = _sut.Hash("correct horse battery staple");

        Assert.StartsWith("$argon2id$v=19$m=1024,t=1,p=1$", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Hash_should_not_contain_plaintext()
    {
        const string password = "SuperSecret123!";
        var hash = _sut.Hash(password);
        Assert.DoesNotContain(password, hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Same_password_should_produce_different_hashes_due_to_random_salt()
    {
        const string password = "same-password";
        Assert.NotEqual(_sut.Hash(password), _sut.Hash(password));
    }

    [Fact]
    public void Verify_correct_password_should_be_success()
    {
        const string password = "p@ssw0rd";
        var hash = _sut.Hash(password);

        Assert.Equal(PasswordVerificationResult.Success, _sut.Verify(hash, password));
    }

    [Fact]
    public void Verify_wrong_password_should_be_failed()
    {
        var hash = _sut.Hash("real-password");

        Assert.Equal(PasswordVerificationResult.Failed, _sut.Verify(hash, "wrong-password"));
    }

    [Theory]
    [InlineData("not-a-phc-string")]
    [InlineData("$argon2id$v=19$m=1024,t=1$onlytwoparams$x$y")]
    [InlineData("")]
    public void Verify_malformed_hash_should_be_failed_without_throwing(string malformed)
    {
        Assert.Equal(PasswordVerificationResult.Failed, _sut.Verify(malformed, "whatever"));
    }

    [Fact]
    public void Verify_with_upgraded_parameters_should_signal_rehash()
    {
        const string password = "upgrade-me";
        var oldHasher = new Argon2idPasswordHasher(FastOptions(iterations: 1));
        var oldHash = oldHasher.Hash(password);

        var newHasher = new Argon2idPasswordHasher(FastOptions(iterations: 2));

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, newHasher.Verify(oldHash, password));
    }
}
