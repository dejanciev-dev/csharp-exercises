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
| **OOP** | [Shapes](src/CSharpExercises/Oop/Shapes.cs) | abstraction, inheritance, polymorphism, primary constructors |
| | [BankAccount](src/CSharpExercises/Oop/BankAccount.cs) | encapsulation, invariants, guard clauses |
| **Algorithms** | [Sorting](src/CSharpExercises/Algorithms/Sorting.cs) | bubble sort, merge sort, quick sort (generic) |
| | [Searching](src/CSharpExercises/Algorithms/Searching.cs) | binary search, Two Sum with a hash map |
| | [DynamicProgramming](src/CSharpExercises/Algorithms/DynamicProgramming.cs) | coin change (min coins & number of ways), longest common subsequence, edit distance |
| | [Dijkstra](src/CSharpExercises/Algorithms/Dijkstra.cs) | weighted shortest paths with a priority queue (reuses `MinHeap<T>`) |
| **Async** | [ParallelDownloader](src/CSharpExercises/Async/ParallelDownloader.cs) | `Task.WhenAll`, throttling with `SemaphoreSlim`, racing with `Task.WhenAny` |
| | [Resilience](src/CSharpExercises/Async/Resilience.cs) | retry with exponential backoff, timeouts via `CancellationToken` |
| | [AsyncStreams](src/CSharpExercises/Async/AsyncStreams.cs) | `IAsyncEnumerable`, `[EnumeratorCancellation]`, producer/consumer with `Channel<T>` |

## Project structure

```
csharp-exercises/
├── src/CSharpExercises/          # exercise implementations, one folder per topic
└── tests/CSharpExercises.Tests/  # xUnit tests for every exercise
```

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/dejanciev-dev/csharp-exercises.git
cd csharp-exercises
dotnet test
```

## Roadmap

Ideas for upcoming exercises:

- [x] **Async**: parallel downloads with `Task.WhenAll`, cancellation tokens, `IAsyncEnumerable`
- [x] **Data structures**: linked list, binary search tree, min-heap / priority queue, graph with BFS & DFS
- [x] **Algorithms**: dynamic programming (coin change, longest common subsequence), Dijkstra
- [ ] **Design patterns**: strategy, observer, factory, decorator, builder
- [ ] **Modern C#**: records with `with` expressions, list patterns, generic math (`INumber<T>`), `Span<T>`
- [ ] **Benchmarks**: comparing the sorting algorithms with BenchmarkDotNet

## Author

**Dejan Ciev** · [GitHub @dejanciev-dev](https://github.com/dejanciev-dev)

If you find this useful, consider giving the repo a ⭐
