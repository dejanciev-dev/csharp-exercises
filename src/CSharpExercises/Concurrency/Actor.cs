using System.Threading.Channels;

namespace CSharpExercises.Concurrency;

/// <summary>
/// The actor model: state is owned by one actor and changed only by processing messages from its mailbox one at a
/// time, so the state needs no locks at all. Callers post messages (fire and forget) or ask and await a reply.
/// The mailbox is a <see cref="Channel{T}"/> with a single reader loop.
/// </summary>
public abstract class Actor<TMessage> : IAsyncDisposable
{
    private readonly Channel<(TMessage Message, TaskCompletionSource<object?>? Reply)> _mailbox =
        Channel.CreateUnbounded<(TMessage, TaskCompletionSource<object?>?)>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Task _loop;

    protected Actor() => _loop = Task.Run(ProcessAsync);

    public void Post(TMessage message)
    {
        if (!_mailbox.Writer.TryWrite((message, null))) throw new InvalidOperationException("The actor has stopped.");
    }

    public async Task<TReply> AskAsync<TReply>(TMessage message)
    {
        var reply = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_mailbox.Writer.TryWrite((message, reply))) throw new InvalidOperationException("The actor has stopped.");
        return (TReply)(await reply.Task)!;
    }

    /// <summary>Handles one message. Never runs concurrently with itself, so it may touch the actor's fields freely.</summary>
    protected abstract object? Handle(TMessage message);

    /// <summary>Stops accepting messages, finishes the ones already queued, then completes.</summary>
    public async ValueTask DisposeAsync()
    {
        _mailbox.Writer.TryComplete();
        await _loop;
    }

    private async Task ProcessAsync()
    {
        await foreach (var (message, reply) in _mailbox.Reader.ReadAllAsync())
        {
            try
            {
                var result = Handle(message);
                reply?.SetResult(result);
            }
            catch (Exception e)
            {
                reply?.SetException(e); // a failing message doesn't kill the actor
            }
        }
    }
}

public abstract record AccountMessage
{
    public sealed record Deposit(decimal Amount) : AccountMessage;
    public sealed record Withdraw(decimal Amount) : AccountMessage;
    public sealed record GetBalance : AccountMessage;
}

/// <summary>A bank account as an actor: thousands of concurrent deposits, no lock, no lost updates.</summary>
public sealed class AccountActor : Actor<AccountMessage>
{
    private decimal _balance;
    private int _processed;

    public int Processed => Volatile.Read(ref _processed);

    protected override object? Handle(AccountMessage message)
    {
        Interlocked.Increment(ref _processed);
        switch (message)
        {
            case AccountMessage.Deposit d:
                _balance += d.Amount;
                return _balance;
            case AccountMessage.Withdraw w when w.Amount > _balance:
                throw new InvalidOperationException($"Insufficient funds: balance {_balance}, requested {w.Amount}.");
            case AccountMessage.Withdraw w:
                _balance -= w.Amount;
                return _balance;
            case AccountMessage.GetBalance:
                return _balance;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }
}
