namespace CSharpExercises.EventSourcing;

public sealed record SagaStep(string Name, Func<CancellationToken, Task> Execute, Func<Task> Compensate);

public sealed record SagaResult(bool Succeeded, IReadOnlyList<string> Log, string? FailedStep = null);

/// <summary>
/// A saga runs a business transaction that spans several services without a distributed lock or two-phase commit:
/// each step commits locally, and if a later step fails, the completed ones are undone by compensating actions in
/// reverse order (refund the payment, release the reservation). The system is briefly inconsistent but always ends
/// up either fully done or fully compensated.
/// </summary>
public sealed class Saga(IEnumerable<SagaStep> steps)
{
    private readonly IReadOnlyList<SagaStep> _steps = [.. steps];

    public async Task<SagaResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var log = new List<string>();
        var completed = new Stack<SagaStep>();

        foreach (var step in _steps)
        {
            try
            {
                await step.Execute(cancellationToken);
                completed.Push(step);
                log.Add($"done: {step.Name}");
            }
            catch (Exception e)
            {
                log.Add($"failed: {step.Name} ({e.Message})");
                while (completed.TryPop(out var done))
                {
                    try
                    {
                        await done.Compensate();
                        log.Add($"compensated: {done.Name}");
                    }
                    catch (Exception ce)
                    {
                        // A compensation must not stop the others; real systems retry it or alert a human.
                        log.Add($"compensation failed: {done.Name} ({ce.Message})");
                    }
                }
                return new SagaResult(false, log, step.Name);
            }
        }
        return new SagaResult(true, log);
    }
}
