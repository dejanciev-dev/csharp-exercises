namespace CSharpExercises.Patterns;

/// <summary>
/// Mediator: participants talk only to the chat room, never to each other. Rules about who receives what (direct
/// messages, muting, join and leave notices) live in one place instead of being spread across every participant,
/// and n participants need n connections instead of n².
/// </summary>
public sealed class ChatRoom
{
    private readonly Dictionary<string, ChatParticipant> _members = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<(string Muter, string Muted)> _mutes = [];

    public ChatParticipant Join(string name)
    {
        if (_members.ContainsKey(name)) throw new InvalidOperationException($"'{name}' is already in the room.");
        var participant = new ChatParticipant(name, this);
        Broadcast("system", $"{name} joined");
        _members[name] = participant;
        return participant;
    }

    public void Leave(ChatParticipant participant)
    {
        if (_members.Remove(participant.Name)) Broadcast("system", $"{participant.Name} left");
    }

    internal void Send(ChatParticipant from, string text)
    {
        // "@name message" is a direct message.
        if (text.StartsWith('@') && text.IndexOf(' ') is var space and > 1)
        {
            var target = text[1..space];
            if (!_members.TryGetValue(target, out var recipient)) throw new InvalidOperationException($"No one called '{target}' is here.");
            Deliver(from.Name, recipient, $"(private) {text[(space + 1)..]}");
            return;
        }
        Broadcast(from.Name, text);
    }

    internal void Mute(ChatParticipant muter, string muted) => _mutes.Add((muter.Name, muted));

    private void Broadcast(string from, string text)
    {
        foreach (var member in _members.Values)
            if (!member.Name.Equals(from, StringComparison.OrdinalIgnoreCase)) Deliver(from, member, text);
    }

    private void Deliver(string from, ChatParticipant to, string text)
    {
        if (_mutes.Contains((to.Name, from))) return;
        to.Receive($"{from}: {text}");
    }
}

public sealed class ChatParticipant
{
    private readonly ChatRoom _room;
    private readonly List<string> _inbox = [];

    internal ChatParticipant(string name, ChatRoom room) => (Name, _room) = (name, room);

    public string Name { get; }
    public IReadOnlyList<string> Inbox => _inbox;

    public void Say(string text) => _room.Send(this, text);
    public void Mute(string name) => _room.Mute(this, name);
    internal void Receive(string message) => _inbox.Add(message);
}
