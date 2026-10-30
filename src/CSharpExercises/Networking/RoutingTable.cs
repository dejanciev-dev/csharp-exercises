namespace CSharpExercises.Networking;

/// <summary>
/// A router forwards each packet by the most specific route that matches its destination: longest prefix match.
/// Routes live in a binary trie keyed by address bits, so a lookup walks at most 32 nodes and remembers the last
/// route it passed, no matter how many routes there are.
/// </summary>
public sealed class RoutingTable<TNextHop>
{
    private sealed class Node
    {
        public readonly Node?[] Children = new Node?[2];
        public bool HasRoute;
        public TNextHop Route = default!;
    }

    private readonly Node _root = new();

    public int Count { get; private set; }

    public void Add(string cidr, TNextHop nextHop)
    {
        var network = Ipv4Network.Parse(cidr);
        var node = _root;
        for (var i = 0; i < network.PrefixLength; i++)
        {
            var bit = (int)(network.Network >> (31 - i) & 1);
            node = node.Children[bit] ??= new Node();
        }
        if (!node.HasRoute) Count++;
        node.HasRoute = true;
        node.Route = nextHop;
    }

    public bool Remove(string cidr)
    {
        var network = Ipv4Network.Parse(cidr);
        var node = _root;
        for (var i = 0; i < network.PrefixLength && node is not null; i++)
            node = node.Children[network.Network >> (31 - i) & 1];
        if (node is not { HasRoute: true }) return false;
        node.HasRoute = false;
        node.Route = default!;
        Count--;
        return true;
    }

    public bool TryLookup(string address, out TNextHop nextHop)
    {
        var value = Ipv4Network.ToUInt32(address);
        var node = _root;
        var found = node.HasRoute; // a 0.0.0.0/0 default route lives at the root
        nextHop = node.Route;

        for (var i = 0; i < 32; i++)
        {
            node = node.Children[value >> (31 - i) & 1];
            if (node is null) break;
            if (node.HasRoute)
            {
                found = true;
                nextHop = node.Route; // a longer match overrides the shorter one
            }
        }
        return found;
    }
}
