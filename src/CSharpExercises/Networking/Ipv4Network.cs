using System.Net;
using System.Net.Sockets;

namespace CSharpExercises.Networking;

/// <summary>
/// IPv4 subnets in CIDR notation ("192.168.1.0/24"): the prefix length says how many leading bits are the network
/// part. With the address as a 32-bit integer, membership is one mask-and-compare, and subnetting or summarising
/// address ranges is just bit arithmetic.
/// </summary>
public readonly record struct Ipv4Network(uint Network, int PrefixLength)
{
    public static Ipv4Network Parse(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var prefix) || prefix is < 0 or > 32)
            throw new FormatException($"'{cidr}' is not CIDR notation.");
        var address = ToUInt32(parts[0]);
        var network = new Ipv4Network(address & MaskFor(prefix), prefix);
        if (network.Network != address) throw new FormatException($"'{cidr}' has host bits set; did you mean {network}?");
        return network;
    }

    public uint Mask => MaskFor(PrefixLength);
    public uint Broadcast => Network | ~Mask;
    public long AddressCount => 1L << (32 - PrefixLength);

    /// <summary>Usable hosts: minus the network and broadcast addresses, except for /31 point-to-point and /32 single hosts.</summary>
    public long UsableHosts => PrefixLength >= 31 ? AddressCount : AddressCount - 2;

    public bool Contains(string address) => (ToUInt32(address) & Mask) == Network;

    public bool Contains(Ipv4Network other) => other.PrefixLength >= PrefixLength && (other.Network & Mask) == Network;

    /// <summary>Splits into 2^(newPrefix - prefix) equal subnets.</summary>
    public IEnumerable<Ipv4Network> Subnets(int newPrefix)
    {
        if (newPrefix < PrefixLength || newPrefix > 32) throw new ArgumentOutOfRangeException(nameof(newPrefix));
        var step = 1L << (32 - newPrefix);
        for (long n = Network; n <= Broadcast; n += step) yield return new Ipv4Network((uint)n, newPrefix);
    }

    /// <summary>The smallest set of CIDR blocks covering exactly the range first..last, as firewall rules need.</summary>
    public static IReadOnlyList<Ipv4Network> Summarise(string first, string last)
    {
        long start = ToUInt32(first), end = ToUInt32(last);
        var blocks = new List<Ipv4Network>();
        while (start <= end)
        {
            // The biggest block that starts aligned at 'start' and doesn't run past 'end'.
            var size = start == 0 ? 1L << 32 : start & -start;
            while (start + size - 1 > end) size >>= 1;
            blocks.Add(new Ipv4Network((uint)start, 32 - (int)Math.Log2(size)));
            start += size;
        }
        return blocks;
    }

    public static uint ToUInt32(string address)
    {
        if (!IPAddress.TryParse(address, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork || address.Count(c => c == '.') != 3)
            throw new FormatException($"'{address}' is not an IPv4 address.");
        var bytes = ip.GetAddressBytes();
        return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
    }

    public static string ToAddress(uint value) => $"{value >> 24}.{value >> 16 & 255}.{value >> 8 & 255}.{value & 255}";

    public override string ToString() => $"{ToAddress(Network)}/{PrefixLength}";

    private static uint MaskFor(int prefix) => prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
}
