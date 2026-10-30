namespace CSharpExercises.FunctionalProgramming;

/// <summary>
/// A lens is a getter and an immutable setter for one part of a structure. Updating a deeply nested immutable record
/// by hand means a pyramid of <c>with</c> expressions; lenses compose so <c>person.Address.City</c> becomes one lens
/// that can get, set or modify the city in a single call.
/// </summary>
public sealed class Lens<TWhole, TPart>(Func<TWhole, TPart> get, Func<TWhole, TPart, TWhole> set)
{
    public TPart Get(TWhole whole) => get(whole);

    public TWhole Set(TWhole whole, TPart part) => set(whole, part);

    public TWhole Modify(TWhole whole, Func<TPart, TPart> change) => set(whole, change(get(whole)));

    /// <summary>Focuses further: a lens from the whole to a part of the part.</summary>
    public Lens<TWhole, TInner> Then<TInner>(Lens<TPart, TInner> inner) =>
        new(whole => inner.Get(get(whole)),
            (whole, value) => set(whole, inner.Set(get(whole), value)));
}

public static class Lens
{
    public static Lens<TWhole, TPart> Of<TWhole, TPart>(Func<TWhole, TPart> get, Func<TWhole, TPart, TWhole> set) => new(get, set);

    /// <summary>A lens onto one element of an immutable list.</summary>
    public static Lens<IReadOnlyList<T>, T> At<T>(int index) => new(
        list => list[index],
        (list, value) => [.. list.Take(index), value, .. list.Skip(index + 1)]);
}
