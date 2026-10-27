namespace CSharpExercises.StateMachines;

/// <summary>
/// A table-driven state machine: which triggers are allowed in which state is data, configured up front, instead of
/// <c>if</c>/<c>switch</c> checks spread through the code. A small version of libraries such as Stateless.
/// </summary>
public sealed class StateMachine<TState, TTrigger>(TState initial)
    where TState : notnull
    where TTrigger : notnull
{
    internal sealed record Transition(TState Destination, Func<bool> Guard, string GuardDescription);

    private readonly Dictionary<TState, StateConfiguration> _states = new();
    private readonly List<(TState From, TTrigger Trigger, TState To)> _history = [];

    public TState State { get; private set; } = initial;

    public IReadOnlyList<(TState From, TTrigger Trigger, TState To)> History => _history;

    /// <summary>Raised after every transition, with the state left, the trigger and the state entered.</summary>
    public event Action<TState, TTrigger, TState>? Transitioned;

    public StateConfiguration Configure(TState state)
    {
        if (!_states.TryGetValue(state, out var configuration)) _states[state] = configuration = new StateConfiguration(this);
        return configuration;
    }

    /// <summary>True if <paramref name="trigger"/> is configured for the current state and its guard passes.</summary>
    public bool CanFire(TTrigger trigger) => FindTransition(trigger, out _) is not null;

    /// <summary>Triggers that would succeed right now: useful for enabling buttons in a UI.</summary>
    public IReadOnlyList<TTrigger> PermittedTriggers =>
        _states.TryGetValue(State, out var configuration)
            ? [.. configuration.Transitions.Where(t => t.Value.Any(x => x.Guard())).Select(t => t.Key)]
            : [];

    public void Fire(TTrigger trigger)
    {
        var transition = FindTransition(trigger, out var failedGuard) ?? throw new InvalidOperationException(
            string.IsNullOrEmpty(failedGuard)
                ? $"'{trigger}' is not allowed in state '{State}'."
                : $"'{trigger}' is not allowed in state '{State}': {failedGuard}.");

        var from = State;
        // A transition to the same state is internal: the state is not left, so exit and entry actions do not run.
        var changesState = !EqualityComparer<TState>.Default.Equals(from, transition.Destination);
        if (changesState) _states[from].Exit?.Invoke();

        State = transition.Destination;
        _history.Add((from, trigger, State));

        if (changesState && _states.TryGetValue(State, out var entered)) entered.Entry?.Invoke();
        Transitioned?.Invoke(from, trigger, State);
    }

    private Transition? FindTransition(TTrigger trigger, out string? failedGuard)
    {
        failedGuard = null;
        if (!_states.TryGetValue(State, out var configuration) || !configuration.Transitions.TryGetValue(trigger, out var candidates))
            return null;

        // Several guarded transitions can share a trigger; the first whose guard passes wins.
        foreach (var candidate in candidates)
        {
            if (candidate.Guard()) return candidate;
            failedGuard ??= candidate.GuardDescription;
        }
        return null;
    }

    public sealed class StateConfiguration(StateMachine<TState, TTrigger> machine)
    {
        internal Dictionary<TTrigger, List<Transition>> Transitions { get; } = new();
        internal Action? Entry { get; private set; }
        internal Action? Exit { get; private set; }

        public StateConfiguration Permit(TTrigger trigger, TState destination) =>
            PermitIf(trigger, destination, () => true, "");

        /// <summary>Allows <paramref name="trigger"/> only while <paramref name="guard"/> returns true.</summary>
        public StateConfiguration PermitIf(TTrigger trigger, TState destination, Func<bool> guard, string description)
        {
            if (!Transitions.TryGetValue(trigger, out var list)) Transitions[trigger] = list = [];
            list.Add(new Transition(destination, guard, description));
            return this;
        }

        public StateConfiguration OnEntry(Action action)
        {
            Entry += action;
            return this;
        }

        public StateConfiguration OnExit(Action action)
        {
            Exit += action;
            return this;
        }

        /// <summary>Continue the fluent chain with another state.</summary>
        public StateConfiguration Configure(TState state) => machine.Configure(state);
    }
}
