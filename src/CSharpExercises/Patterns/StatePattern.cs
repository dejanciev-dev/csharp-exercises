namespace CSharpExercises.Patterns;

/// <summary>
/// State pattern: an object whose behaviour depends on its state delegates to a state object, and changes behaviour by
/// swapping that object. Each state class handles only the operations that make sense for it, so there's no switch on a
/// status field repeated in every method. Shown with a TCP-like connection. (StateMachines/ has the table-driven
/// alternative; this is the object-oriented one.)
/// </summary>
public sealed class Connection
{
    private ConnectionState _state = new Closed();

    public string State => _state.GetType().Name;
    public List<string> Log { get; } = [];

    public void Open() => _state = _state.Open(this);
    public void Send(string data) => _state = _state.Send(this, data);
    public void Acknowledge() => _state = _state.Acknowledge(this);
    public void Close() => _state = _state.Close(this);

    private abstract class ConnectionState
    {
        public virtual ConnectionState Open(Connection c) => Invalid(nameof(Open));
        public virtual ConnectionState Send(Connection c, string data) => Invalid(nameof(Send));
        public virtual ConnectionState Acknowledge(Connection c) => Invalid(nameof(Acknowledge));
        public virtual ConnectionState Close(Connection c) => new Closed(); // closing is allowed from anywhere

        private ConnectionState Invalid(string operation) => throw new InvalidOperationException($"Can't {operation} while {GetType().Name}.");
    }

    private sealed class Closed : ConnectionState
    {
        public override ConnectionState Open(Connection c)
        {
            c.Log.Add("SYN sent");
            return new Opening();
        }
        public override ConnectionState Close(Connection c) => this; // already closed: harmless
    }

    private sealed class Opening : ConnectionState
    {
        public override ConnectionState Acknowledge(Connection c)
        {
            c.Log.Add("connected");
            return new Established();
        }
    }

    private sealed class Established : ConnectionState
    {
        public override ConnectionState Send(Connection c, string data)
        {
            c.Log.Add($"sent {data}");
            return this;
        }
        public override ConnectionState Close(Connection c)
        {
            c.Log.Add("FIN sent");
            return new Closed();
        }
    }
}
