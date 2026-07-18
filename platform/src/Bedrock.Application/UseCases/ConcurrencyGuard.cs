using Bedrock.Domain.Results;

namespace Bedrock.Application.UseCases;

/// <summary>
/// Checks the version read by a client before applying a mutable write. Persistence still performs the final
/// compare-and-swap at SaveChanges; this guard also catches stale edits that arrive after an earlier write committed.
/// </summary>
public static class ConcurrencyGuard
{
    public static void EnsureExpectedRowVersion(uint? actualRowVersion, uint? expectedRowVersion)
    {
        if (actualRowVersion != expectedRowVersion)
        {
            throw new ConcurrencyConflictException();
        }
    }
}
