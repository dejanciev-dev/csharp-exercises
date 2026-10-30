namespace CSharpExercises.Commerce;

public sealed record Reservation(Guid Id, string Sku, int Quantity, DateTimeOffset ExpiresAt);

/// <summary>
/// Holding stock for a checkout: a reservation takes units out of "available" without selling them, and expires if
/// the customer abandons the cart, so stock isn't locked forever. Confirming turns it into a sale. Expired reservations
/// are released lazily on the next read, using <see cref="TimeProvider"/> so tests can travel in time.
/// </summary>
public sealed class InventoryReservations(TimeProvider time, TimeSpan holdFor)
{
    private readonly Dictionary<string, int> _onHand = new();
    private readonly Dictionary<Guid, Reservation> _reservations = new();
    private readonly Lock _lock = new();

    public void Receive(string sku, int quantity)
    {
        lock (_lock) _onHand[sku] = _onHand.GetValueOrDefault(sku) + quantity;
    }

    public int Available(string sku)
    {
        lock (_lock)
        {
            ExpireOld();
            return _onHand.GetValueOrDefault(sku) - _reservations.Values.Where(r => r.Sku == sku).Sum(r => r.Quantity);
        }
    }

    public Reservation? TryReserve(string sku, int quantity)
    {
        lock (_lock)
        {
            if (Available(sku) < quantity) return null;
            var reservation = new Reservation(Guid.NewGuid(), sku, quantity, time.GetUtcNow() + holdFor);
            _reservations[reservation.Id] = reservation;
            return reservation;
        }
    }

    /// <summary>Completes the sale; fails if the reservation already expired (the stock may have gone to someone else).</summary>
    public bool Confirm(Guid reservationId)
    {
        lock (_lock)
        {
            ExpireOld();
            if (!_reservations.Remove(reservationId, out var r)) return false;
            _onHand[r.Sku] -= r.Quantity;
            return true;
        }
    }

    public void Release(Guid reservationId)
    {
        lock (_lock) _reservations.Remove(reservationId);
    }

    public int OnHand(string sku)
    {
        lock (_lock) return _onHand.GetValueOrDefault(sku);
    }

    private void ExpireOld()
    {
        var now = time.GetUtcNow();
        foreach (var expired in _reservations.Values.Where(r => r.ExpiresAt <= now).ToList()) _reservations.Remove(expired.Id);
    }
}
