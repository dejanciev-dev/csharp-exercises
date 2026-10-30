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
| | [LogParser](src/CSharpExercises/Strings/LogParser.cs) | `[GeneratedRegex]` with named groups, key=value extraction, redacting emails and card numbers |
| | [TemplateEngine](src/CSharpExercises/Strings/TemplateEngine.cs) | `{{ name \| filter }}` placeholders with `Regex.Replace` and a `MatchEvaluator`, fail-loud on missing values |
| | [UnicodeText](src/CSharpExercises/Strings/UnicodeText.cs) | grapheme-aware length, reverse and truncate, slugs via `NormalizationForm.FormD`, accent-insensitive compare |
| **Collections** | [Stack&lt;T&gt;](src/CSharpExercises/Collections/Stack.cs) | generics, a growable array from scratch |
| | [BracketValidator](src/CSharpExercises/Collections/BracketValidator.cs) | using a stack to check nesting |
| | [LruCache&lt;TKey, TValue&gt;](src/CSharpExercises/Collections/LruCache.cs) | O(1) cache with a dictionary and a linked list |
| | [SinglyLinkedList&lt;T&gt;](src/CSharpExercises/Collections/SinglyLinkedList.cs) | in-place reverse, fast/slow pointers, `IEnumerable<T>` |
| | [BinarySearchTree&lt;T&gt;](src/CSharpExercises/Collections/BinarySearchTree.cs) | insert, delete with in-order successor, iterative in-order traversal |
| | [MinHeap&lt;T&gt;](src/CSharpExercises/Collections/MinHeap.cs) | array-backed heap, sift up/down, priority queue via `IComparer<T>` |
| | [Graph&lt;T&gt;](src/CSharpExercises/Collections/Graph.cs) | adjacency list, BFS, DFS, shortest path, topological sort |
| | [Trie](src/CSharpExercises/Collections/Trie.cs) | prefix tree for autocomplete, ranked suggestions, removal with pruning, iterative traversal |
| | [UnionFind&lt;T&gt;](src/CSharpExercises/Collections/UnionFind.cs) | disjoint sets with union by size and path compression |
| | [RingBuffer&lt;T&gt;](src/CSharpExercises/Collections/RingBuffer.cs) | fixed-size circular buffer, overwrite-oldest, moving average, fail-fast enumeration |
| | [Deque&lt;T&gt;](src/CSharpExercises/Collections/Deque.cs) | double-ended queue on a growable circular array, O(1) at both ends and by index |
| | [SkipList&lt;T&gt;](src/CSharpExercises/Collections/SkipList.cs) | sorted linked list with random "express lanes", O(log n) expected, range queries |
| | [LfuCache&lt;TKey, TValue&gt;](src/CSharpExercises/Collections/LfuCache.cs) | O(1) least-frequently-used eviction with per-frequency lists, ties broken by recency |
| | [BiMap&lt;TKey, TValue&gt;](src/CSharpExercises/Collections/BiMap.cs) | lookups in both directions, keeping two dictionaries consistent |
| | [IntervalSet](src/CSharpExercises/Collections/IntervalSet.cs) | merged half-open ranges: adding merges, removing splits, finding free slots |
| | [AvlTree&lt;T&gt;](src/CSharpExercises/Collections/AvlTree.cs) | self-balancing BST with rotations, subtree sizes for k-th smallest and rank |
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
| | [Command](src/CSharpExercises/Patterns/Command.cs) | undo/redo history, commands that remember state, macros undone as one step |
| | [Chain of responsibility](src/CSharpExercises/Patterns/ChainOfResponsibility.cs) | expense approvals passed along until someone decides |
| | [Visitor](src/CSharpExercises/Patterns/Visitor.cs) | HTML, Markdown, word count and table of contents over a document tree via double dispatch |
| | [Composite](src/CSharpExercises/Patterns/Composite.cs) | files and folders behind one abstraction, sizes, search and a `tree`-style listing |
| | [Adapter](src/CSharpExercises/Patterns/Adapter.cs) | wrapping a legacy Fahrenheit driver with error codes into an async Celsius interface |
| | [Proxy](src/CSharpExercises/Patterns/Proxy.cs) | lazy (virtual) and protection proxies, a logging proxy generated with `DispatchProxy` |
| | [Template method](src/CSharpExercises/Patterns/TemplateMethod.cs) | fixed export steps with CSV and Markdown variants and optional hooks |
| | [Mediator](src/CSharpExercises/Patterns/Mediator.cs) | a chat room routing broadcasts, direct messages and mutes |
| **Dependency injection** | [MiniContainer](src/CSharpExercises/DependencyInjection/MiniContainer.cs) | a DI container from scratch: reflection-based constructor injection, lifetimes, scopes, cycle and captive-dependency detection |
| | [Messaging](src/CSharpExercises/DependencyInjection/Messaging.cs) | `Microsoft.Extensions.DependencyInjection`: `AddXxx` extension, keyed services, `TimeProvider`, scope validation |
| **Testing techniques** | [RenewalService](src/CSharpExercises/Testing/RenewalService.cs) | code designed for testing: dependencies as interfaces, `TimeProvider` for the clock |
| | [Test doubles](tests/CSharpExercises.Tests/Testing/TestDoubles.cs) | hand-written dummy, stub, spy and fake, compared with [NSubstitute mocks](tests/CSharpExercises.Tests/Testing/RenewalServiceMockTests.cs) |
| | [SubscriptionBuilder](tests/CSharpExercises.Tests/Testing/SubscriptionBuilder.cs) | test data builder with defaults and an implicit conversion |
| | [TimeBased](src/CSharpExercises/Testing/TimeBased.cs) | rate limiter and debouncer tested instantly with `FakeTimeProvider` |
| **Algorithms** | [Sorting](src/CSharpExercises/Algorithms/Sorting.cs) | bubble sort, merge sort, quick sort (generic) |
| | [Searching](src/CSharpExercises/Algorithms/Searching.cs) | binary search, Two Sum with a hash map |
| | [DynamicProgramming](src/CSharpExercises/Algorithms/DynamicProgramming.cs) | coin change (min coins & number of ways), longest common subsequence, edit distance |
| | [Dijkstra](src/CSharpExercises/Algorithms/Dijkstra.cs) | weighted shortest paths with a priority queue (reuses `MinHeap<T>`) |
| | [MinimumSpanningTree](src/CSharpExercises/Algorithms/MinimumSpanningTree.cs) | Kruskal's algorithm (reuses `UnionFind<T>`) |
| | [Backtracking](src/CSharpExercises/Algorithms/Backtracking.cs) | permutations without duplicates, subsets, combination sum with pruning, N-Queens |
| | [Sudoku](src/CSharpExercises/Algorithms/Sudoku.cs) | backtracking with bit-mask candidates and fewest-options-first |
| | [StringSearch](src/CSharpExercises/Algorithms/StringSearch.cs) | Knuth–Morris–Pratt substring search in O(n + m) |
| | [Knapsack](src/CSharpExercises/Algorithms/Knapsack.cs) | 0/1 knapsack with item reconstruction, a one-row version, why greedy fails, subset sum |
| | [LongestIncreasingSubsequence](src/CSharpExercises/Algorithms/LongestIncreasingSubsequence.cs) | patience sorting in O(n log n), nesting envelopes |
| | [MaximumSubarray](src/CSharpExercises/Algorithms/MaximumSubarray.cs) | Kadane's algorithm, circular arrays, best single trade |
| | [WordBreak](src/CSharpExercises/Algorithms/WordBreak.cs) | splitting unspaced text into words: memoised search, all splits, fewest words |
| | [MatrixChain](src/CSharpExercises/Algorithms/MatrixChain.cs) | interval DP for the cheapest multiplication order |
| | [IntervalScheduling](src/CSharpExercises/Algorithms/IntervalScheduling.cs) | greedy by end time, room assignment with a priority queue, weighted scheduling with DP |
| **Caching** | [TtlCache&lt;TKey, TValue&gt;](src/CSharpExercises/Caching/TtlCache.cs) | absolute and sliding expiration on `TimeProvider`, lazy expiry, compaction, hit/miss counts |
| | [SingleFlight&lt;TKey, TValue&gt;](src/CSharpExercises/Caching/SingleFlight.cs) | stampede protection: concurrent requests for a key share one load, per-caller cancellation |
| | [CachedLookup&lt;TKey, TValue&gt;](src/CSharpExercises/Caching/CachedLookup.cs) | cache-aside with negative caching and invalidation |
| **Immutable data** | [PersistentStack&lt;T&gt; / PersistentQueue&lt;T&gt;](src/CSharpExercises/Immutable/PersistentStack.cs) | structural sharing, O(1) versions, a queue from two stacks |
| | [UndoableEditor](src/CSharpExercises/Immutable/UndoableEditor.cs) | undo/redo with snapshots of an `ImmutableList<T>` document, bounded history |
| | [ReadOnlyExposure](src/CSharpExercises/Immutable/ReadOnlyExposure.cs) | `IReadOnlyList` vs `ReadOnlyCollection` vs `ImmutableArray`, `FrozenDictionary` |
| **I/O & binary data** | [ReadingsFile](src/CSharpExercises/IO/ReadingsFile.cs) | a versioned binary format: magic number, `BinaryWriter`/`BinaryReader`, CRC-32 trailer, reading old versions, distrusting counts |
| | [Crc32](src/CSharpExercises/IO/Crc32.cs) | table-driven checksum, computed incrementally |
| | [PacketHeader](src/CSharpExercises/IO/PacketHeader.cs) | big-endian network headers with `BinaryPrimitives` on spans, validating untrusted input |
| | [CountingStream](src/CSharpExercises/IO/CountingStream.cs) | a `Stream` decorator, measuring gzip size without buffering, constant-memory CSV processing |
| **Numerics** | [FloatingPoint](src/CSharpExercises/Numerics/FloatingPoint.cs) | why `0.1 + 0.2 != 0.3`, tolerant comparison, Kahan summation, ULP distance |
| | [Rational](src/CSharpExercises/Numerics/Rational.cs) | exact fractions on `BigInteger` with operator interfaces, a valid `default` |
| | [NumberTheory](src/CSharpExercises/Numerics/NumberTheory.cs) | modular exponentiation with `UInt128`, modular inverse, deterministic Miller–Rabin, fast-doubling Fibonacci |
| **Hashing** | [StableHash](src/CSharpExercises/Hashing/StableHash.cs) | why `string.GetHashCode()` is randomised, FNV-1a, a SplitMix64 finalizer |
| | [OpenAddressingMap&lt;TKey, TValue&gt;](src/CSharpExercises/Hashing/OpenAddressingMap.cs) | a hash map from scratch: linear probing, tombstones, power-of-two sizing, load factor |
| | [BloomFilter](src/CSharpExercises/Hashing/BloomFilter.cs) | probabilistic set membership, sizing formulas, double hashing |
| | [ConsistentHashRing&lt;TNode&gt;](src/CSharpExercises/Hashing/ConsistentHashRing.cs) | sharding with virtual nodes: adding a server moves ~1/n of the keys instead of most of them |
| **Modern C#** | [Records](src/CSharpExercises/ModernCSharp/Records.cs) | immutable order with `with` expressions, value equality over collections |
| | [ListPatterns](src/CSharpExercises/ModernCSharp/ListPatterns.cs) | `[first, .., last]`, slice patterns, a text command parser |
| | [GenericMath](src/CSharpExercises/ModernCSharp/GenericMath.cs) | `INumber<T>`, `IBinaryInteger<T>`, a custom `Vector2D<T>` with operator interfaces |
| | [SpanExercises](src/CSharpExercises/ModernCSharp/SpanExercises.cs) | allocation-free parsing, `string.Create`, in-place rotation with `Span<T>` |
| **Async** | [ParallelDownloader](src/CSharpExercises/Async/ParallelDownloader.cs) | `Task.WhenAll`, throttling with `SemaphoreSlim`, racing with `Task.WhenAny` |
| | [Resilience](src/CSharpExercises/Async/Resilience.cs) | retry with exponential backoff, timeouts via `CancellationToken` |
| | [AsyncStreams](src/CSharpExercises/Async/AsyncStreams.cs) | `IAsyncEnumerable`, `[EnumeratorCancellation]`, producer/consumer with `Channel<T>` |
| | [AsyncCoordination](src/CSharpExercises/Async/AsyncCoordination.cs) | `AsyncLock` across awaits, `AsyncManualResetEvent` with `RunContinuationsAsynchronously`, `AsyncLazy<T>` with retry |
| **Concurrency** | [Counters](src/CSharpExercises/Concurrency/Counters.cs) | lost updates from `++`, `lock` with `System.Threading.Lock`, `Interlocked`, a lock-free max with compare-and-swap |
| | [BoundedBuffer&lt;T&gt;](src/CSharpExercises/Concurrency/BoundedBuffer.cs) | blocking producer/consumer queue with `Monitor.Wait` / `PulseAll` |
| | [OnceCache&lt;TKey, TValue&gt;](src/CSharpExercises/Concurrency/OnceCache.cs) | `ConcurrentDictionary` + `Lazy<T>` so the factory runs once per key, retry after a failed build |
| | [LockFreeStack&lt;T&gt;](src/CSharpExercises/Concurrency/LockFreeStack.cs) | Treiber stack with compare-and-swap, `SpinWait` back-off, why .NET has no ABA problem here |
| **Domain modelling** | [PrefixedIds](src/CSharpExercises/Domain/PrefixedIds.cs) | strongly typed `cus_…` / `ord_…` ids with static abstract members, time-ordered `Guid.CreateVersion7` |
| | [ValueObjects](src/CSharpExercises/Domain/ValueObjects.cs) | always-valid `EmailAddress` via `Result<T>`, `DateRange` with overlap, intersection and merging |
| | [Allocation](src/CSharpExercises/Domain/Allocation.cs) | splitting money without losing cents: largest-remainder method, by ratios, refunds |
| | [Specification&lt;T&gt;](src/CSharpExercises/Domain/Specification.cs) | composable business rules as expression trees (`&`, `\|`, `!`), parameter rebinding, usable with `IQueryable` |
| **Error handling** | [Result&lt;T&gt;](src/CSharpExercises/ErrorHandling/Result.cs) | errors as values: `Map`, `Bind`, `Ensure`, `Match`, LINQ query syntax, combining independent results |
| | [SignupValidator](src/CSharpExercises/ErrorHandling/SignupValidator.cs) | form validation that fails fast vs. reports every error, cross-field rules |
| | [ExceptionHandling](src/CSharpExercises/ErrorHandling/ExceptionHandling.cs) | every failure from `Task.WhenAll`, cleanup steps that all run, `ExceptionDispatchInfo` to keep stack traces |
| **Reflection** | [PropertyAccessor](src/CSharpExercises/Reflection/PropertyAccessor.cs) | expression trees compiled into fast getters and setters instead of `PropertyInfo.GetValue` |
| | [AttributeValidator](src/CSharpExercises/Reflection/AttributeValidator.cs) | custom attributes (`[Required]`, `[Range]`, `[MaxLength]`) read once per type and cached |
| | [CommandRegistry](src/CSharpExercises/Reflection/CommandRegistry.cs) | plugin discovery: scan assemblies for `[Command]` classes, create them with `Activator` |
| **Serialization** | [Money](src/CSharpExercises/Serialization/Money.cs) | custom `JsonConverter<T>` writing a value object as `"12.50 EUR"`, errors with the JSON path |
| | [OrderEvents](src/CSharpExercises/Serialization/OrderEvents.cs) | polymorphic JSON with `[JsonDerivedType]`, source-generated `JsonSerializerContext`, strict unknown-member handling, event replay |
| **Resource management** | [Disposables](src/CSharpExercises/Resources/Disposables.cs) | idempotent `Dispose`, `ObjectDisposedException`, disposing in reverse order, `IAsyncDisposable` with `await using` |
| | [Pooling](src/CSharpExercises/Resources/Pooling.cs) | an object pool with allocation-free struct leases, renting buffers from `ArrayPool<T>.Shared` |
| **Delegates & events** | [Functional](src/CSharpExercises/Delegates/Functional.cs) | composing functions, currying, partial application, memoizing recursive functions |
| | [TemperatureSensor](src/CSharpExercises/Delegates/TemperatureSensor.cs) | the standard event pattern, isolating handlers that throw, unsubscribing lambdas with an `IDisposable` token |
| | [MiddlewarePipeline](src/CSharpExercises/Delegates/MiddlewarePipeline.cs) | ASP.NET Core-style middleware built by composing `next => handler` delegates, short-circuiting |
| **State machines** | [StateMachine&lt;TState, TTrigger&gt;](src/CSharpExercises/StateMachines/StateMachine.cs) | table-driven transitions with a fluent API, guards with reasons, entry/exit actions, history |
| | [DocumentWorkflow](src/CSharpExercises/StateMachines/DocumentWorkflow.cs) | draft → review → approved → published, approval quorum, rules that read like requirements |
| | [VendingMachine](src/CSharpExercises/StateMachines/VendingMachine.cs) | a pure `(state, event) => (state, effects)` function with pattern matching, replayable and immutable |
| **Dates & times** | [CalendarMath](src/CSharpExercises/DatesAndTimes/CalendarMath.cs) | `DateOnly`, age with 29 February birthdays, nth weekday of a month, Easter, business days with holidays |
| | [TimeZones](src/CSharpExercises/DatesAndTimes/TimeZones.cs) | skipped and repeated local times around DST, "same time tomorrow", shared working hours, a weekly schedule |
| **Parsing** | [Lexer](src/CSharpExercises/Parsing/Lexer.cs) | turning text into tokens, syntax errors with positions |
| | [Parser](src/CSharpExercises/Parsing/Parser.cs) | recursive descent: precedence and associativity from the grammar, a syntax tree of records |
| | [ExpressionTools](src/CSharpExercises/Parsing/ExpressionTools.cs) | evaluating with variables and functions, constant folding, printing with minimal parentheses |
| **Generics** | [NotificationBus](src/CSharpExercises/Generics/NotificationBus.cs) | contravariant handlers (`in T`), dispatch on the runtime type, covariance and the array covariance trap |
| | [EnumInfo&lt;TEnum&gt;](src/CSharpExercises/Generics/EnumInfo.cs) | a static generic class as a per-type cache, `struct, Enum` constraint, `IParsable<T>` with static abstract members |
| **Compression & encoding** | [Varint](src/CSharpExercises/Compression/Varint.cs) | LEB128 variable-length integers, ZigZag for negatives, delta-encoding sorted timestamps, overflow-safe decoding |
| | [Huffman](src/CSharpExercises/Compression/Huffman.cs) | optimal prefix codes with `PriorityQueue`, canonical codes so the header stores only lengths, bit-level reading and writing |
| | [Base32](src/CSharpExercises/Compression/Base32.cs) | Crockford's alphabet for codes humans type, forgiving decoding, a mod-37 check symbol that catches typos and swaps |
| **Diffing** | [MyersDiff](src/CSharpExercises/Diffing/MyersDiff.cs) | the shortest edit script in O((N+M)·D), the algorithm behind `git diff`, walking the path back through saved frontiers |
| | [UnifiedDiff](src/CSharpExercises/Diffing/UnifiedDiff.cs) | `diff -u` output: hunks with context, merging nearby changes, `@@ -l,s +l,s @@` headers, parsing a diff back |
| | [Patch](src/CSharpExercises/Diffing/Patch.cs) | applying hunks to a file that changed since the diff was made: nearest-match search, offsets, conflicts |
| **Geometry** | [Vec / Segments](src/CSharpExercises/Geometry/Vec.cs) | dot and cross products, orientation, segment intersection without division, point-to-segment distance |
| | [ConvexHull](src/CSharpExercises/Geometry/ConvexHull.cs) | Andrew's monotone chain in O(n log n), diameter |
| | [Polygon](src/CSharpExercises/Geometry/Polygon.cs) | shoelace area, winding order, centroid, ray-casting containment, convexity |
| | [ClosestPair](src/CSharpExercises/Geometry/ClosestPair.cs) | divide and conquer with a strip check, verified against brute force |
| | [GeoDistance](src/CSharpExercises/Geometry/GeoDistance.cs) | haversine distance, initial bearing, bounding boxes for "within r km" |
| | [RectangleUnion](src/CSharpExercises/Geometry/RectangleUnion.cs) | area of overlapping rectangles with a sweep line |
| **Bit manipulation** | [BitTricks](src/CSharpExercises/Bits/BitTricks.cs) | power-of-two tests, Kernighan popcount, lowest set bit, bit reversal, Gosper's hack, subset enumeration |
| | [BitSet](src/CSharpExercises/Bits/BitSet.cs) | 64 flags per `ulong`, set operations, enumerating with trailing-zero counts, a bit-packed sieve |
| | [GrayCode](src/CSharpExercises/Bits/GrayCode.cs) | sequences where neighbours differ by one bit, encoding and decoding |
| | [MortonCode](src/CSharpExercises/Bits/MortonCode.cs) | Z-order curves by interleaving bits, a cheap spatial index |
| | [TravellingSalesman](src/CSharpExercises/Bits/TravellingSalesman.cs) | Held–Karp dynamic programming over bit-mask subsets |
| **Range queries** | [PrefixSums](src/CSharpExercises/RangeQueries/PrefixSums.cs) | O(1) range sums in 1D and 2D, counting subarrays with a given sum |
| | [FenwickTree](src/CSharpExercises/RangeQueries/FenwickTree.cs) | binary indexed tree with O(n) build, counting inversions |
| | [SegmentTree&lt;T&gt;](src/CSharpExercises/RangeQueries/SegmentTree.cs) | bottom-up, iterative, any associative operation, order-preserving for non-commutative ones |
| | [SparseTable](src/CSharpExercises/RangeQueries/SparseTable.cs) | O(1) range minimum on static data with overlapping power-of-two windows |
| | [DifferenceArray](src/CSharpExercises/RangeQueries/DifferenceArray.cs) | batched range updates, peak overlap of bookings |
| | [SlidingWindow](src/CSharpExercises/RangeQueries/SlidingWindow.cs) | window maximums with a monotonic deque, longest substring without repeats, shortest window with a sum |
| **Graph algorithms** | [StronglyConnectedComponents](src/CSharpExercises/Graphs/StronglyConnectedComponents.cs) | Tarjan's algorithm in one DFS, condensation |
| | [BellmanFord](src/CSharpExercises/Graphs/BellmanFord.cs) | negative weights, finding the negative cycle, currency arbitrage with -log(rate) |
| | [FloydWarshall](src/CSharpExercises/Graphs/FloydWarshall.cs) | all-pairs shortest paths, next-hop path reconstruction, graph centre |
| | [Bipartite](src/CSharpExercises/Graphs/Bipartite.cs) | two-colouring with BFS, maximum matching with augmenting paths |
| | [MaxFlow](src/CSharpExercises/Graphs/MaxFlow.cs) | Edmonds–Karp with residual capacities, the minimum cut |
| | [GridPathfinding](src/CSharpExercises/Graphs/GridPathfinding.cs) | A* with a Manhattan heuristic and weighted cells, compared with Dijkstra |
| | [CriticalConnections](src/CSharpExercises/Graphs/CriticalConnections.cs) | bridges and articulation points with low-link values |
| **Text algorithms** | [RabinKarp](src/CSharpExercises/TextAlgorithms/RabinKarp.cs) | rolling hashes mod a Mersenne prime, longest repeated substring by binary search |
| | [ZFunction](src/CSharpExercises/TextAlgorithms/ZFunction.cs) | prefix-match lengths in O(n), pattern search, smallest period |
| | [Manacher](src/CSharpExercises/TextAlgorithms/Manacher.cs) | longest palindromic substring in O(n) |
| | [SuffixArray](src/CSharpExercises/TextAlgorithms/SuffixArray.cs) | prefix doubling, Kasai's LCP array, substring search, distinct substrings |
| | [AhoCorasick](src/CSharpExercises/TextAlgorithms/AhoCorasick.cs) | many patterns in one pass with failure links, a word censor |
| | [Soundex](src/CSharpExercises/TextAlgorithms/Soundex.cs) | phonetic codes so "Robert" matches "Rupert" |
| | [WordWrap](src/CSharpExercises/TextAlgorithms/WordWrap.cs) | greedy line breaking vs. minimum raggedness with DP |
| **Randomness** | [Xoshiro256](src/CSharpExercises/Randomness/Xoshiro256.cs) | a seeded PRNG, SplitMix64 seeding, unbiased bounded integers with Lemire's method |
| | [Shuffling](src/CSharpExercises/Randomness/Shuffling.cs) | Fisher–Yates vs. the biased naive shuffle, sampling without replacement |
| | [ReservoirSampler&lt;T&gt;](src/CSharpExercises/Randomness/ReservoirSampler.cs) | uniform samples from a stream of unknown length, weighted reservoir sampling |
| | [AliasTable&lt;T&gt;](src/CSharpExercises/Randomness/AliasTable.cs) | Vose's alias method: O(1) weighted draws |
| | [MonteCarlo](src/CSharpExercises/Randomness/MonteCarlo.cs) | estimating π, the birthday paradox, Monty Hall |
| **Statistics** | [RunningStats](src/CSharpExercises/Statistics/RunningStats.cs) | Welford's one-pass variance, merging partial results, why E[x²] - E[x]² fails |
| | [StreamingMedian](src/CSharpExercises/Statistics/StreamingMedian.cs) | median of a stream with two heaps |
| | [Percentiles](src/CSharpExercises/Statistics/Percentiles.cs) | nearest-rank vs. interpolated percentiles, quickselect |
| | [HyperLogLog](src/CSharpExercises/Statistics/HyperLogLog.cs) | counting distinct items in 4 KB, merging sketches |
| | [CountMinSketch](src/CSharpExercises/Statistics/CountMinSketch.cs) | approximate counts that never underestimate, heavy hitters |
| | [LinearRegression](src/CSharpExercises/Statistics/LinearRegression.cs) | least squares fit, R², correlation |
| | [MovingAverages](src/CSharpExercises/Statistics/MovingAverages.cs) | simple and exponential moving averages, half-life decay for irregular samples |
| **Linear algebra** | [Matrix](src/CSharpExercises/LinearAlgebra/Matrix.cs) | immutable row-major matrix, cache-friendly multiplication order |
| | [GaussianElimination](src/CSharpExercises/LinearAlgebra/GaussianElimination.cs) | solving systems with partial pivoting, determinant, Gauss–Jordan inverse |
| | [MatrixPower](src/CSharpExercises/LinearAlgebra/MatrixPower.cs) | exponentiation by squaring for Fibonacci, Tribonacci and counting walks |
| | [GridTransforms](src/CSharpExercises/LinearAlgebra/GridTransforms.cs) | in-place rotation, spiral order, searching a sorted matrix |
| | [Transform2D](src/CSharpExercises/LinearAlgebra/Transform2D.cs) | affine transforms in homogeneous coordinates, composition, inverse, rotating around a point |
| | [PageRank](src/CSharpExercises/LinearAlgebra/PageRank.cs) | power iteration with damping and dead ends |
| **Reliability** | [TokenBucket](src/CSharpExercises/Reliability/TokenBucket.cs) | burst-friendly rate limiting with lazy refill on `TimeProvider`, Retry-After |
| | [CircuitBreaker](src/CSharpExercises/Reliability/CircuitBreaker.cs) | closed → open → half-open with a single trial call, state change events |
| | [Bulkhead](src/CSharpExercises/Reliability/Bulkhead.cs) | capping concurrency plus a queue, rejecting the rest immediately |
| | [LoadBalancer](src/CSharpExercises/Reliability/LoadBalancer.cs) | smooth weighted round-robin, health checks, power of two choices |
| | [TimingWheel](src/CSharpExercises/Reliability/TimingWheel.cs) | O(1) timeouts in hashed slots with rotation counts |
| | [CronSchedule](src/CSharpExercises/Reliability/CronSchedule.cs) | five-field cron parsing with lists, ranges and steps, the day-of-month/day-of-week rule |
| **Codecs** | [Base64Codec](src/CSharpExercises/Codecs/Base64Codec.cs) | 6-bit groups and padding by hand, URL-safe variant, strict decoding |
| | [HexDump](src/CSharpExercises/Codecs/HexDump.cs) | hex with nibble arithmetic, an `xxd`-style dump |
| | [PercentEncoding](src/CSharpExercises/Codecs/PercentEncoding.cs) | RFC 3986 escaping of UTF-8 bytes, `+` for spaces in forms, query strings |
| | [Utf8Codec](src/CSharpExercises/Codecs/Utf8Codec.cs) | UTF-8 by hand, rejecting overlong forms and surrogates, truncating on character boundaries |
| | [RomanNumerals](src/CSharpExercises/Codecs/RomanNumerals.cs) | greedy encoding, parsing that rejects non-canonical numerals |
| | [NumberToWords](src/CSharpExercises/Codecs/NumberToWords.cs) | spelling numbers in English, cheque amounts, ordinals |
| **File formats** | [Csv](src/CSharpExercises/Formats/Csv.cs) | RFC 4180 state machine: quotes, escaped quotes, embedded newlines, headers |
| | [IniFile](src/CSharpExercises/Formats/IniFile.cs) | sections, comments, quoted values, `${section.key}` references, typed reads with `IParsable<T>` |
| | [MiniJson](src/CSharpExercises/Formats/MiniJson.cs) | a complete JSON parser and serializer with positioned errors and a nesting limit |
| | [SemanticVersion](src/CSharpExercises/Formats/SemanticVersion.cs) | semver precedence (pre-releases, numeric identifiers), caret ranges |
| | [CommandLine](src/CSharpExercises/Formats/CommandLine.cs) | `--name value`, `--name=value`, bundled flags, `--`, shell-style splitting |
| | [HumanReadable](src/CSharpExercises/Formats/HumanReadable.cs) | `1.5 GiB` vs `1.5 GB`, `2h30m` durations, both directions |
| **Simulations** | [GameOfLife](src/CSharpExercises/Simulations/GameOfLife.cs) | Conway's Life on an unbounded board stored as a set of live cells |
| | [ElementaryAutomaton](src/CSharpExercises/Simulations/ElementaryAutomaton.cs) | Wolfram rules 30, 90 and 110 |
| | [TicTacToe](src/CSharpExercises/Simulations/TicTacToe.cs) | negamax with alpha-beta pruning, perfect play ends in a draw |
| | [Maze](src/CSharpExercises/Simulations/Maze.cs) | recursive backtracker with an explicit stack, BFS solving, ASCII rendering |
| | [FloodFill](src/CSharpExercises/Simulations/FloodFill.cs) | iterative paint bucket, counting islands, capturing surrounded regions |
| | [QueueSimulation](src/CSharpExercises/Simulations/QueueSimulation.cs) | discrete-event simulation of tellers, compared with M/M/1 queueing theory |
| **Functional programming** | [Option&lt;T&gt;](src/CSharpExercises/FunctionalProgramming/Option.cs) | an optional value instead of null, `Map`/`Bind`/`Match`, LINQ query syntax |
| | [Trampoline](src/CSharpExercises/FunctionalProgramming/Trampoline.cs) | deep and mutual recursion in constant stack space |
| | [Lens](src/CSharpExercises/FunctionalProgramming/Lens.cs) | composable getters and immutable setters for nested records |
| | [Zipper&lt;T&gt;](src/CSharpExercises/FunctionalProgramming/Zipper.cs) | an immutable list with an O(1) cursor |
| | [Monoids](src/CSharpExercises/FunctionalProgramming/Monoids.cs) | `IMonoid<T>` with static abstract members, parallel folds, Kadane as a monoid |
| | [ParserCombinators](src/CSharpExercises/FunctionalProgramming/ParserCombinators.cs) | small parsers combined with `Or`, `Many` and LINQ into a calculator grammar |
| **Checksums** | [Luhn](src/CSharpExercises/Checksums/Luhn.cs) | payment card check digits, card brands, the 09 ↔ 90 blind spot |
| | [Iban](src/CSharpExercises/Checksums/Iban.cs) | mod 97 by long division, country lengths, helpful error messages |
| | [Isbn](src/CSharpExercises/Checksums/Isbn.cs) | ISBN-10 (mod 11) and ISBN-13 (EAN), converting between them |
| | [Damm](src/CSharpExercises/Checksums/Damm.cs) | a quasigroup table that catches every single error and adjacent swap |
| **Finance** | [Amortization](src/CSharpExercises/Finance/Amortization.cs) | annuity payments, a cent-exact repayment schedule, the effect of paying extra |
| | [CashFlows](src/CSharpExercises/Finance/CashFlows.cs) | NPV, IRR with Newton's method and a bisection fallback, payback period, compounding |
| **Combinatorics** | [Binomial](src/CSharpExercises/Combinatorics/Binomial.cs) | exact binomials without factorials, Pascal's triangle, Lucas' theorem mod a prime |
| | [Permutations](src/CSharpExercises/Combinatorics/Permutations.cs) | next permutation in place, rank and unrank via factorial base, derangements |
| | [Combinations](src/CSharpExercises/Combinatorics/Combinations.cs) | lazy k-combinations, cartesian products as an odometer, listing change-making ways |
| | [Partitions](src/CSharpExercises/Combinatorics/Partitions.cs) | integer partitions, Stirling numbers of the second kind, Bell numbers |
| | [Catalan](src/CSharpExercises/Combinatorics/Catalan.cs) | balanced brackets and binary tree shapes |
| | [InclusionExclusion](src/CSharpExercises/Combinatorics/InclusionExclusion.cs) | counting divisible numbers over bit-mask subsets, surjections, Euler's totient |
| **Fuzzy matching** | [JaroWinkler](src/CSharpExercises/Fuzzy/JaroWinkler.cs) | name similarity with transpositions and a prefix bonus |
| | [DamerauLevenshtein](src/CSharpExercises/Fuzzy/DamerauLevenshtein.cs) | edit distance counting swaps, a banded version with early exit |
| | [NGramSimilarity](src/CSharpExercises/Fuzzy/NGramSimilarity.cs) | Jaccard and Dice over n-grams, a trigram index |
| | [BkTree](src/CSharpExercises/Fuzzy/BkTree.cs) | fuzzy dictionary lookup pruned by the triangle inequality |
| | [SpellChecker](src/CSharpExercises/Fuzzy/SpellChecker.cs) | Norvig's corrector: candidate edits ranked by word frequency |
| | [MinHash](src/CSharpExercises/Fuzzy/MinHash.cs) | estimating Jaccard similarity of documents from small signatures |
| **Search engine** | [Tokenizer](src/CSharpExercises/Search/Tokenizer.cs) | lower-casing, accent folding, stop words, a light stemmer, positions |
| | [InvertedIndex](src/CSharpExercises/Search/InvertedIndex.cs) | posting lists, merge intersection, NOT, phrase queries with positions |
| | [Ranking](src/CSharpExercises/Search/Ranking.cs) | TF-IDF and BM25 with term saturation and length normalisation |
| | [QueryParser](src/CSharpExercises/Search/QueryParser.cs) | `a AND (b OR "c d") -e` parsed into a tree and evaluated |
| | [Snippets](src/CSharpExercises/Search/Snippets.cs) | best matching window with highlighted terms |
| | [Autocomplete](src/CSharpExercises/Search/Autocomplete.cs) | prefix ranges by binary search, top-k by popularity |
| **Operating systems** | [CpuScheduling](src/CSharpExercises/OperatingSystems/CpuScheduling.cs) | FCFS, shortest job first, round-robin, Gantt charts and waiting times |
| | [PageReplacement](src/CSharpExercises/OperatingSystems/PageReplacement.cs) | FIFO, LRU, optimal and clock, Belady's anomaly |
| | [BankersAlgorithm](src/CSharpExercises/OperatingSystems/BankersAlgorithm.cs) | deadlock avoidance with safe sequences |
| | [DiskScheduling](src/CSharpExercises/OperatingSystems/DiskScheduling.cs) | FCFS, shortest seek, SCAN (elevator), C-SCAN |
| | [FreeListAllocator](src/CSharpExercises/OperatingSystems/FreeListAllocator.cs) | first, best and worst fit, splitting, coalescing, fragmentation |
| | [BuddyAllocator](src/CSharpExercises/OperatingSystems/BuddyAllocator.cs) | power-of-two blocks, buddies found by flipping one bit |
| **Networking** | [Ipv4Network](src/CSharpExercises/Networking/Ipv4Network.cs) | CIDR parsing, membership, subnetting, summarising a range into blocks |
| | [RoutingTable](src/CSharpExercises/Networking/RoutingTable.cs) | longest prefix match in a binary trie |
| | [InternetChecksum](src/CSharpExercises/Networking/InternetChecksum.cs) | RFC 1071 ones'-complement checksum, incremental updates, an IPv4 header |
| | [HttpMessageParser](src/CSharpExercises/Networking/HttpMessageParser.cs) | incremental HTTP/1.1 parsing, chunked bodies, limits, smuggling checks |
| | [Backoff](src/CSharpExercises/Networking/Backoff.cs) | exponential backoff with full, equal and decorrelated jitter, measured spikes |
| | [UriTemplate](src/CSharpExercises/Networking/UriTemplate.cs) | RFC 6570 expansion with operators and escaping |
| **Distributed systems** | [LamportClock](src/CSharpExercises/Distributed/LamportClock.cs) | logical timestamps and a causally consistent total order |
| | [VectorClock](src/CSharpExercises/Distributed/VectorClock.cs) | detecting concurrent writes, keeping siblings in a replica |
| | [Crdts](src/CSharpExercises/Distributed/Crdts.cs) | G-counter, PN-counter, LWW register, observed-remove set |
| | [MerkleTree](src/CSharpExercises/Distributed/MerkleTree.cs) | finding differing blocks in O(log n) comparisons, inclusion proofs |
| | [SnowflakeId](src/CSharpExercises/Distributed/SnowflakeId.cs) | time-sorted 64-bit ids without coordination, clock-skew protection |
| | [TwoPhaseCommit](src/CSharpExercises/Distributed/TwoPhaseCommit.cs) | prepare and vote, commit or abort everyone, timeouts as no votes |
| **Event sourcing** | [EventStore](src/CSharpExercises/EventSourcing/EventStore.cs) | append-only streams, optimistic concurrency, global positions |
| | [InventoryItem](src/CSharpExercises/EventSourcing/InventoryItem.cs) | an aggregate rebuilt from events, decisions separate from state changes |
| | [Projections](src/CSharpExercises/EventSourcing/Projections.cs) | read models that catch up idempotently, a daily report |
| | [Snapshots](src/CSharpExercises/EventSourcing/Snapshots.cs) | loading from a snapshot plus the events after it |
| | [Saga](src/CSharpExercises/EventSourcing/Saga.cs) | multi-step transactions with compensation in reverse order |
| | [Upcasting](src/CSharpExercises/EventSourcing/Upcasting.cs) | converting old event versions step by step when reading |
| **Spatial indexes** | [QuadTree&lt;T&gt;](src/CSharpExercises/Spatial/QuadTree.cs) | adaptive subdivision, range and radius queries |
| | [KdTree&lt;T&gt;](src/CSharpExercises/Spatial/KdTree.cs) | nearest and k-nearest neighbours with pruning |
| | [GeoHash](src/CSharpExercises/Spatial/GeoHash.cs) | interleaved lat/long bits as base-32 strings, neighbouring cells |
| | [SpatialHashGrid](src/CSharpExercises/Spatial/SpatialHashGrid.cs) | collision broad phase in near-linear time |
| | [IntervalTree&lt;T&gt;](src/CSharpExercises/Spatial/IntervalTree.cs) | overlap queries with subtree max-end pruning |
| **Image processing** | [Colors](src/CSharpExercises/Imaging/Colors.cs) | hex, RGB ↔ HSL, mixing, WCAG contrast ratio and readable text colour |
| | [Convolution](src/CSharpExercises/Imaging/Convolution.cs) | blur, sharpen, Sobel edges, a summed-area-table box blur |
| | [Dithering](src/CSharpExercises/Imaging/Dithering.cs) | threshold vs Floyd–Steinberg vs ordered (Bayer) dithering |
| | [Histogram](src/CSharpExercises/Imaging/Histogram.cs) | equalisation, contrast stretch, Otsu's threshold |
| | [Resampling](src/CSharpExercises/Imaging/Resampling.cs) | nearest neighbour and bilinear resizing, fitting thumbnails |
| | [Netpbm](src/CSharpExercises/Imaging/Netpbm.cs) | reading and writing PGM images (ASCII and binary) |
| **Signal processing** | [Fourier](src/CSharpExercises/Signals/Fourier.cs) | DFT vs Cooley–Tukey FFT, dominant frequencies, polynomial multiplication |
| | [Dtmf](src/CSharpExercises/Signals/Dtmf.cs) | generating and decoding phone tones with the Goertzel algorithm |
| | [WavFile](src/CSharpExercises/Signals/WavFile.cs) | writing and reading RIFF/WAVE PCM, skipping unknown chunks |
| | [Filters](src/CSharpExercises/Signals/Filters.cs) | one-pole low-pass and high-pass, median filter for spikes |
| | [PitchDetection](src/CSharpExercises/Signals/PitchDetection.cs) | autocorrelation pitch detection, nearest note and cents |
| **Interpreters** | [ShuntingYard](src/CSharpExercises/Interpreters/ShuntingYard.cs) | infix to RPN with associativity, unary minus and functions |
| | [StackMachine](src/CSharpExercises/Interpreters/StackMachine.cs) | a bytecode VM with calls and jumps, a two-pass assembler |
| | [TinyRegex](src/CSharpExercises/Interpreters/TinyRegex.cs) | Thompson NFA matching without catastrophic backtracking |
| | [TinyLisp](src/CSharpExercises/Interpreters/TinyLisp.cs) | reader, eval, closures, `let`, and tail calls in a loop |
| | [Brainfuck](src/CSharpExercises/Interpreters/Brainfuck.cs) | bracket pre-pairing and run folding |
| **Numerical methods** | [RootFinding](src/CSharpExercises/NumericalMethods/RootFinding.cs) | bisection, Newton and secant, convergence speed compared |
| | [Integration](src/CSharpExercises/NumericalMethods/Integration.cs) | trapezoid, Simpson, adaptive Simpson, Monte Carlo |
| | [Interpolation](src/CSharpExercises/NumericalMethods/Interpolation.cs) | linear, Lagrange (and Runge's phenomenon), natural cubic splines |
| | [GradientDescent](src/CSharpExercises/NumericalMethods/GradientDescent.cs) | numerical gradients, momentum, fitting a line, the Rosenbrock valley |
| | [OdeSolvers](src/CSharpExercises/NumericalMethods/OdeSolvers.cs) | Euler, RK4, semi-implicit Euler conserving a spring's energy |
| | [SimulatedAnnealing](src/CSharpExercises/NumericalMethods/SimulatedAnnealing.cs) | 2-opt moves with a cooling schedule on a 100-city tour |
| **Security** | [PasswordHasher](src/CSharpExercises/Security/PasswordHasher.cs) | salted PBKDF2, constant-time comparison, rehashing on cost upgrades |
| | [SignedToken](src/CSharpExercises/Security/SignedToken.cs) | HS256 JWTs by hand, rejecting `alg: none`, tampering and expiry |
| | [Totp](src/CSharpExercises/Security/Totp.cs) | RFC 6238 codes, drift window, replay protection, provisioning URIs |
| | [PasswordStrength](src/CSharpExercises/Security/PasswordStrength.cs) | entropy estimates discounted for common words, repeats and sequences |
| | [HtmlSanitizer](src/CSharpExercises/Security/HtmlSanitizer.cs) | allowlisted tags and attributes, safe URL schemes |
| **Commerce** | [VatCalculator](src/CSharpExercises/Commerce/VatCalculator.cs) | rounding per line vs per rate, extracting VAT from gross prices |
| | [TieredPricing](src/CSharpExercises/Commerce/TieredPricing.cs) | volume vs graduated tiers, price cliffs |
| | [Proration](src/CSharpExercises/Commerce/Proration.cs) | plan changes mid-cycle by actual days, partial first periods |
| | [InventoryReservations](src/CSharpExercises/Commerce/InventoryReservations.cs) | holding stock with expiry, confirming and releasing |
| | [CurrencyConverter](src/CSharpExercises/Commerce/CurrencyConverter.cs) | triangulated rates, minor units per currency, spreads |

## Project structure

```
csharp-exercises/
├── src/CSharpExercises/                    # exercise implementations, one folder per topic
├── src/CSharpExercises.Api/                # ASP.NET Core minimal API built on the exercises
├── tests/CSharpExercises.Tests/            # xUnit unit and integration tests
└── benchmarks/CSharpExercises.Benchmarks/  # BenchmarkDotNet performance comparisons
```

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/dejanciev-dev/csharp-exercises.git
cd csharp-exercises
dotnet test
```

## Web API

[CSharpExercises.Api](src/CSharpExercises.Api) puts the exercises together in an ASP.NET Core minimal API:

| Endpoint | Uses |
|---|---|
| `POST /api/sort` | bubble / merge / quick sort |
| `POST /api/routes/shortest` | Dijkstra (with the hand-written `MinHeap<T>`) |
| `GET /api/coins` | dynamic programming coin change |
| `GET /api/text/edit-distance`, `/lcs`, `/palindrome` | dynamic programming and string exercises |
| `GET` / `POST /api/shapes` | shape factory (injected via DI) |

Along the way it shows input validation with `ProblemDetails` responses, limits that stop expensive algorithms from being abused (bubble sort is capped at 5,000 numbers), rate limiting with the `SlidingWindowRateLimiter` from the testing exercises (`429` + `Retry-After`), options bound from `appsettings.json`, and an OpenAPI document.

```bash
dotnet run --project src/CSharpExercises.Api
curl -X POST http://localhost:5028/api/sort -H "Content-Type: application/json" \
     -d '{"algorithm":"quick","numbers":[5,2,9,1]}'
# {"algorithm":"quick","count":4,"sorted":[1,2,5,9]}
```

More sample requests are in [CSharpExercises.Api.http](src/CSharpExercises.Api/CSharpExercises.Api.http), and the OpenAPI document is at `/openapi/v1.json` in Development. [Integration tests](tests/CSharpExercises.Tests/Api/ApiTests.cs) run the whole app in memory with `WebApplicationFactory`, swapping in a fake clock to test rate limiting without waiting.

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
- [x] **Dependency injection**: constructor injection, lifetimes, `Microsoft.Extensions.DependencyInjection`
- [x] **Testing techniques**: test doubles and mocks, test data builders, testing time with `TimeProvider`
- [x] **Putting it together**: a small console app or web API built on these exercises
- [x] **Concurrency**: race conditions, `lock` vs `Interlocked`, a blocking queue with `Monitor`, run-once caching
- [x] **Error handling**: a `Result<T>` type, validation that collects every error, exceptions that are easy to lose
- [x] **Reflection**: custom attributes, expression trees, discovering plugins in assemblies
- [x] **Serialization**: `System.Text.Json` custom converters, polymorphism and source generation
- [x] **Resource management**: `IDisposable` and `IAsyncDisposable` done right, object and array pooling
- [x] **Delegates & events**: function composition and memoization, event pitfalls, a middleware pipeline
- [x] **Parsing**: a tokenizer and recursive-descent parser for arithmetic, evaluating and simplifying the syntax tree
- [x] **Generics**: variance, static generic caches, constraints with static abstract interface members
- [x] **More data structures & algorithms**: trie, union-find, ring buffer, Kruskal, backtracking, a Sudoku solver
- [x] **State machines**: table-driven with guards, a document workflow, a pure event-driven vending machine
- [x] **Dates & times**: `DateOnly` calendar math, business days, DST-safe time zone conversion, recurring schedules
- [x] **Text processing**: source-generated regex, a template engine, Unicode grapheme clusters, KMP search
- [x] **Hashing**: stable hashes, a hash map from scratch, Bloom filters, consistent hashing
- [x] **Caching**: expiration with `TimeProvider`, stampede protection, cache-aside with negative caching
- [x] **Immutable data**: persistent collections with structural sharing, snapshot-based undo, what "read-only" really means
- [x] **I/O & binary data**: a versioned file format with checksums, endianness, stream decorators, streaming processing
- [x] **Numerics**: floating-point pitfalls, exact rationals, modular arithmetic and primality testing
- [x] **Domain modelling**: strongly typed ids, value objects, money allocation, the specification pattern
- [x] **Async coordination**: async locks and events, async lazy initialisation, a lock-free stack
- [x] **Compression & encoding**: varints with ZigZag and deltas, Huffman coding, human-friendly Base32 with check symbols
- [x] **Diffing**: the Myers diff algorithm, unified diff output, applying patches that have drifted
- [x] **Geometry & bits**: convex hulls, polygons, closest pair, haversine, bit tricks, Gray and Morton codes, Held–Karp
- [x] **Range queries**: prefix sums, Fenwick and segment trees, sparse tables, sliding windows
- [x] **Graph & text algorithms**: SCCs, Bellman–Ford, Floyd–Warshall, max flow, A*, suffix arrays, Aho–Corasick, Manacher
- [x] **More DP & randomness**: knapsack, LIS, Kadane, interval scheduling, Fisher–Yates, reservoir sampling, alias tables
- [x] **Statistics & linear algebra**: Welford, HyperLogLog, count-min, regression, Gaussian elimination, PageRank
- [x] **Reliability**: token bucket, circuit breaker, bulkhead, load balancing, timing wheels, cron
- [x] **Codecs & formats**: Base64, UTF-8, percent-encoding, CSV, INI, JSON, semver, command lines
- [x] **Simulations**: Game of Life, cellular automata, minimax, mazes, flood fill, queueing
- [x] **Functional programming**: Option, trampolines, lenses, zippers, monoids, parser combinators
- [x] **More patterns & collections**: command, visitor, composite, proxy, mediator; deque, skip list, LFU, AVL tree
- [x] **Checksums & finance**: Luhn, IBAN, ISBN, Damm, loan amortization, NPV and IRR
- [x] **Combinatorics & fuzzy matching**: binomials, permutations, partitions, Catalan; Jaro–Winkler, BK-trees, MinHash
- [x] **Search engine**: tokenizing and stemming, inverted index, BM25, boolean queries, snippets, autocomplete
- [x] **Operating systems & networking**: scheduling, paging, allocators; CIDR, routing, checksums, HTTP parsing
- [x] **Distributed systems & event sourcing**: logical clocks, CRDTs, Merkle trees, 2PC; event store, projections, sagas
- [x] **Spatial, imaging & signals**: quadtrees, k-d trees, geohash; filters, dithering; FFT, DTMF, pitch detection
- [x] **Interpreters & numerical methods**: a VM, a regex engine, a Lisp; root finding, integration, ODEs, annealing
- [x] **Security & commerce**: password hashing, JWT, TOTP, sanitizing; VAT, tiered pricing, proration, FX

## Author

**Dejan Ciev** · [GitHub @dejanciev-dev](https://github.com/dejanciev-dev)

If you find this useful, consider giving the repo a ⭐
