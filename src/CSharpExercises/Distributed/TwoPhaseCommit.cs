namespace CSharpExercises.Distributed;

public enum Vote { Yes, No }
public enum TransactionOutcome { Committed, Aborted }

public interface IParticipant
{
    string Name { get; }
    Task<Vote> PrepareAsync(Guid transaction, CancellationToken cancellationToken);
    Task CommitAsync(Guid transaction);
    Task RollbackAsync(Guid transaction);
}

/// <summary>
/// Two-phase commit makes several resources agree on one outcome. Phase 1: ask everyone to prepare (make the change
/// durable but not visible) and vote. Phase 2: commit only if every vote was yes; any no, error or timeout aborts
/// everyone. The coordinator writes its decision to a log first so it can finish the job after a crash. The weakness:
/// prepared participants are stuck waiting if the coordinator dies, which is why many systems prefer sagas.
/// </summary>
public sealed class TwoPhaseCommitCoordinator(TimeSpan prepareTimeout)
{
    private readonly List<string> _log = [];

    public IReadOnlyList<string> Log => _log;

    public async Task<TransactionOutcome> ExecuteAsync(IReadOnlyList<IParticipant> participants)
    {
        var transaction = Guid.NewGuid();
        _log.Add("begin");

        using var timeout = new CancellationTokenSource(prepareTimeout);
        var votes = await Task.WhenAll(participants.Select(async p =>
        {
            try
            {
                return await p.PrepareAsync(transaction, timeout.Token).WaitAsync(timeout.Token);
            }
            catch (Exception)
            {
                return Vote.No; // a crash or timeout counts as a no
            }
        }));

        var outcome = votes.All(v => v == Vote.Yes) ? TransactionOutcome.Committed : TransactionOutcome.Aborted;
        _log.Add(outcome == TransactionOutcome.Committed ? "commit" : "abort"); // the decision point

        // Phase 2 must reach everyone; a real coordinator retries until it does.
        await Task.WhenAll(participants.Select(p => outcome == TransactionOutcome.Committed ? p.CommitAsync(transaction) : p.RollbackAsync(transaction)));
        _log.Add("end");
        return outcome;
    }
}

/// <summary>An in-memory participant for demonstrations: holds staged changes until told the outcome.</summary>
public sealed class AccountParticipant(string name, decimal balance, decimal change, TimeSpan? prepareDelay = null) : IParticipant
{
    private decimal? _staged;

    public string Name { get; } = name;
    public decimal Balance { get; private set; } = balance;
    public bool HasPendingChange => _staged is not null;

    public async Task<Vote> PrepareAsync(Guid transaction, CancellationToken cancellationToken)
    {
        if (prepareDelay is { } delay) await Task.Delay(delay, cancellationToken);
        if (Balance + change < 0) return Vote.No; // would overdraw
        _staged = Balance + change;
        return Vote.Yes;
    }

    public Task CommitAsync(Guid transaction)
    {
        if (_staged is { } value) Balance = value;
        _staged = null;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(Guid transaction)
    {
        _staged = null;
        return Task.CompletedTask;
    }
}
