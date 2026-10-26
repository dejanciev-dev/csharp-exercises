# C# Exercises

[![Build & Test](https://github.com/dejanciev-dev/csharp-exercises/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dejanciev-dev/csharp-exercises/actions/workflows/dotnet.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp)

A collection of C# exercises covering language fundamentals, data structures, LINQ, object-oriented design and classic algorithms. Every exercise is covered by xUnit tests.

## Exercises

| Topic | Exercise | Concepts |
|---|---|---|
| **Basics** | [FizzBuzz](src/CSharpExercises/Basics/FizzBuzz.cs) | tuple pattern matching, switch expressions |
| | [MathHelpers](src/CSharpExercises/Basics/MathHelpers.cs) | primes, factorial, Fibonacci (`yield return`), GCD |
| **Strings** | [StringExercises](src/CSharpExercises/Strings/StringExercises.cs) | reverse, palindrome, anagram, run-length encoding, word frequency |
| **Collections** | [Stack&lt;T&gt;](src/CSharpExercises/Collections/Stack.cs) | generics, a growable array from scratch |
| | [BracketValidator](src/CSharpExercises/Collections/BracketValidator.cs) | using a stack to check nesting |
| | [LruCache&lt;TKey, TValue&gt;](src/CSharpExercises/Collections/LruCache.cs) | O(1) cache with a dictionary and a linked list |
| | [SinglyLinkedList&lt;T&gt;](src/CSharpExercises/Collections/SinglyLinkedList.cs) | in-place reverse, fast/slow pointers, `IEnumerable<T>` |
| | [BinarySearchTree&lt;T&gt;](src/CSharpExercises/Collections/BinarySearchTree.cs) | insert, delete with in-order successor, iterative in-order traversal |
| | [MinHeap&lt;T&gt;](src/CSharpExercises/Collections/MinHeap.cs) | array-backed heap, sift up/down, priority queue via `IComparer<T>` |
| | [Graph&lt;T&gt;](src/CSharpExercises/Collections/Graph.cs) | adjacency list, BFS, DFS, shortest path, topological sort |
| **LINQ** | [LinqExercises](src/CSharpExercises/Linq/LinqExercises.cs) | `GroupBy`, `MaxBy`, `CountBy`, `Chunk`, records |
| | [MyLinq](src/CSharpExercises/Linq/MyLinq.cs) | LINQ operators rebuilt with `yield`: deferred vs. immediate, streaming vs. buffering, eager argument checks |
| | [RangeSequence](src/CSharpExercises/Linq/RangeSequence.cs) | `IEnumerable<T>` / `IEnumerator<T>` written by hand: what the compiler generates for `yield return` |
| **OOP** | [Shapes](src/CSharpExercises/Oop/Shapes.cs) | abstraction, inheritance, polymorphism, primary constructors |
| | [BankAccount](src/CSharpExercises/Oop/BankAccount.cs) | encapsulation, invariants, guard clauses |
| **Design patterns** | [Strategy](src/CSharpExercises/Patterns/Strategy.cs) | swappable, composable discount rules for a shopping cart |
| | [Observer](src/CSharpExercises/Patterns/Observer.cs) | stock price alerts with `IObservable<T>` / `IObserver<T>` |
| | [Factory](src/CSharpExercises/Patterns/ShapeFactory.cs) | create shapes from text, register new ones without changing the factory |
| | [Decorator](src/CSharpExercises/Patterns/Decorator.cs) | caching, logging and sale prices stacked around a price service |
| | [Builder](src/CSharpExercises/Patterns/EmailBuilder.cs) | fluent API producing an immutable, validated email |
| **Algorithms** | [Sorting](src/CSharpExercises/Algorithms/Sorting.cs) | bubble sort, merge sort, quick sort (generic) |
| | [Searching](src/CSharpExercises/Algorithms/Searching.cs) | binary search, Two Sum with a hash map |
| | [DynamicProgramming](src/CSharpExercises/Algorithms/DynamicProgramming.cs) | coin change (min coins & number of ways), longest common subsequence, edit distance |
| | [Dijkstra](src/CSharpExercises/Algorithms/Dijkstra.cs) | weighted shortest paths with a priority queue (reuses `MinHeap<T>`) |
| **Modern C#** | [Records](src/CSharpExercises/ModernCSharp/Records.cs) | immutable order with `with` expressions, value equality over collections |
| | [ListPatterns](src/CSharpExercises/ModernCSharp/ListPatterns.cs) | `[first, .., last]`, slice patterns, a text command parser |
| | [GenericMath](src/CSharpExercises/ModernCSharp/GenericMath.cs) | `INumber<T>`, `IBinaryInteger<T>`, a custom `Vector2D<T>` with operator interfaces |
| | [SpanExercises](src/CSharpExercises/ModernCSharp/SpanExercises.cs) | allocation-free parsing, `string.Create`, in-place rotation with `Span<T>` |
| **Async** | [ParallelDownloader](src/CSharpExercises/Async/ParallelDownloader.cs) | `Task.WhenAll`, throttling with `SemaphoreSlim`, racing with `Task.WhenAny` |
| | [Resilience](src/CSharpExercises/Async/Resilience.cs) | retry with exponential backoff, timeouts via `CancellationToken` |
| | [AsyncStreams](src/CSharpExercises/Async/AsyncStreams.cs) | `IAsyncEnumerable`, `[EnumeratorCancellation]`, producer/consumer with `Channel<T>` |

## Project structure

```
csharp-exercises/
├── src/CSharpExercises/                    # exercise implementations, one folder per topic
├── tests/CSharpExercises.Tests/            # xUnit tests for every exercise
└── benchmarks/CSharpExercises.Benchmarks/  # BenchmarkDotNet performance comparisons
```

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/dejanciev-dev/csharp-exercises.git
cd csharp-exercises
dotnet test
```

## Benchmarks

The [benchmarks](benchmarks/CSharpExercises.Benchmarks) project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to compare the hand-written sorts against `Array.Sort` and LINQ, and the `Span<T>` CSV parser against `string.Split`.

```bash
# quick run (a few minutes); drop --job short for full statistical precision
dotnet run -c Release --project benchmarks/CSharpExercises.Benchmarks -- --filter '*' --job short

# just one class
dotnet run -c Release --project benchmarks/CSharpExercises.Benchmarks -- --filter '*Csv*'
```

### Sorting 10,000 integers

| Method | Random | Already sorted | Reversed | Allocated |
|---|---:|---:|---:|---:|
| `Array.Sort` (baseline) | 0.67 ms | 0.07 ms | 0.18 ms | 39 KB |
| LINQ `Order()` | 0.64 ms | 0.08 ms | 0.18 ms | 39 KB |
| [QuickSort](src/CSharpExercises/Algorithms/Sorting.cs) | 0.94 ms | 0.15 ms | 0.16 ms | 39 KB |
| [MergeSort](src/CSharpExercises/Algorithms/Sorting.cs) | 2.32 ms | 1.18 ms | 1.56 ms | 2,133 KB |
| [BubbleSort](src/CSharpExercises/Algorithms/Sorting.cs) | 145.9 ms | **0.01 ms** | 93.8 ms | 39 KB |

What the numbers show:

- **Big-O wins at scale.** Bubble sort is O(n²): about 200× slower than `Array.Sort` on 10,000 random items, but only 10× slower on 100.
- **Best cases matter.** Bubble sort stops after one pass when nothing swaps, so on already-sorted input it is the fastest of all.
- **Allocations cost time.** Merge sort creates new arrays at every level of recursion (2 MB vs 39 KB), which is the main reason it trails quick sort.
- **Benchmarking finds bugs.** The first version of `QuickSort` always picked the last element as pivot. On sorted input that is its worst case, and it crashed with a stack overflow at 10,000 items. It now uses a middle pivot with Hoare partitioning and recurses only into the smaller half.

### Summing a CSV line

| Method | 10 fields | 1,000 fields | Allocated (1,000 fields) |
|---|---:|---:|---:|
| `string.Split` + `int.Parse` | 438 ns | 61.4 µs | 70 KB |
| [`SumCsv`](src/CSharpExercises/ModernCSharp/SpanExercises.cs) with `ReadOnlySpan<char>` | 239 ns | 33.4 µs | **0 B** |

Slicing spans instead of creating substrings makes parsing about 45% faster and allocation-free.

<sub>Measured with BenchmarkDotNet 0.15.8 `--job short` on .NET 10, Intel Core i7-6700HQ, Windows 10. Short runs are indicative; absolute numbers will differ on your machine.</sub>

## Roadmap

Ideas for upcoming exercises:

- [x] **Async**: parallel downloads with `Task.WhenAll`, cancellation tokens, `IAsyncEnumerable`
- [x] **Data structures**: linked list, binary search tree, min-heap / priority queue, graph with BFS & DFS
- [x] **Algorithms**: dynamic programming (coin change, longest common subsequence), Dijkstra
- [x] **Design patterns**: strategy, observer, factory, decorator, builder
- [x] **Modern C#**: records with `with` expressions, list patterns, generic math (`INumber<T>`), `Span<T>`
- [x] **Benchmarks**: comparing the sorting algorithms with BenchmarkDotNet
- [x] **LINQ internals**: rebuilding `Where`, `Select`, `OrderBy`, `GroupBy` and friends with `yield`
- [ ] **Dependency injection**: constructor injection, lifetimes, `Microsoft.Extensions.DependencyInjection`
- [ ] **Testing techniques**: test doubles and mocks, test data builders, testing time with `TimeProvider`
- [ ] **Putting it together**: a small console app or web API built on these exercises

## Author

**Dejan Ciev** · [GitHub @dejanciev-dev](https://github.com/dejanciev-dev)

If you find this useful, consider giving the repo a ⭐
