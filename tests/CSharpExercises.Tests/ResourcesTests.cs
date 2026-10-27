using System.Text;
using CSharpExercises.Resources;

namespace CSharpExercises.Tests;

public class ResourcesTests
{
    private sealed class Recorder(string name, List<string> log, Exception? error = null) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            log.Add(name);
            if (error is not null) throw error;
        }
    }

    [Fact]
    public void TempFile_IsDeletedOnDispose()
    {
        string path;
        using (var file = new TempFile("hello"))
        {
            path = file.Path;
            Assert.Equal("hello", file.ReadAll());
        }

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void TempFile_DisposeTwiceIsSafe_ButUseAfterDisposeThrows()
    {
        var file = new TempFile();

        file.Dispose();
        file.Dispose();

        Assert.Throws<ObjectDisposedException>(() => file.Path);
    }

    [Fact]
    public void Composite_DisposesInReverseOrder()
    {
        List<string> log = [];
        using (var composite = new CompositeDisposable())
        {
            composite.Add(new Recorder("connection", log));
            composite.Add(new Recorder("transaction", log));
            composite.Add(new Recorder("command", log));
        }

        Assert.Equal(["command", "transaction", "connection"], log);
    }

    [Fact]
    public void Composite_DisposesEverythingEvenWhenOneThrows()
    {
        List<string> log = [];
        var composite = new CompositeDisposable();
        composite.Add(new Recorder("first", log));
        composite.Add(new Recorder("broken", log, new IOException("flush failed")));
        composite.Add(new Recorder("last", log));

        Assert.Throws<IOException>(composite.Dispose);
        Assert.Equal(["last", "broken", "first"], log);
    }

    [Fact]
    public void Composite_DisposesOnlyOnce()
    {
        var recorder = new Recorder("item", []);
        var composite = new CompositeDisposable();
        composite.Add(recorder);

        composite.Dispose();
        composite.Dispose();

        Assert.Equal(1, recorder.DisposeCount);
        Assert.Equal(0, composite.Count);
    }

    [Fact]
    public void Composite_AddAfterDispose_DisposesImmediately()
    {
        var composite = new CompositeDisposable();
        composite.Dispose();

        var late = composite.Add(new Recorder("late", []));

        Assert.Equal(1, late.DisposeCount);
    }

    [Fact]
    public async Task BatchWriter_WritesFullBatchesAndFlushesTheRestOnAwaitUsing()
    {
        var output = new StringWriter();

        await using (var writer = new BatchWriter(output, batchSize: 2))
        {
            await writer.WriteLineAsync("a");
            Assert.Equal("", output.ToString());

            await writer.WriteLineAsync("b");
            await writer.WriteLineAsync("c");
            Assert.Equal($"a{Environment.NewLine}b{Environment.NewLine}", output.ToString());
        }

        Assert.Equal($"a{Environment.NewLine}b{Environment.NewLine}c{Environment.NewLine}", output.ToString());
    }

    [Fact]
    public async Task BatchWriter_SyncDisposeAlsoFlushes()
    {
        var output = new StringWriter();
        var writer = new BatchWriter(output, batchSize: 10);
        await writer.WriteLineAsync("only line");

        writer.Dispose();

        Assert.Equal($"only line{Environment.NewLine}", output.ToString());
        Assert.Equal(1, writer.BatchesWritten);
    }

    [Fact]
    public async Task BatchWriter_WriteAfterDisposeThrows()
    {
        var writer = new BatchWriter(new StringWriter(), batchSize: 1);
        await writer.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await writer.WriteLineAsync("too late"));
    }

    [Fact]
    public void Pool_ReusesReturnedObjects()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder(), sb => sb.Clear());

        for (var i = 0; i < 100; i++)
        {
            using var lease = pool.Rent();
            lease.Value.Append("request ").Append(i);
        }

        Assert.Equal(1, pool.Created);
    }

    [Fact]
    public void Pool_ResetsObjectsBeforeReuse()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder(), sb => sb.Clear());

        using (var first = pool.Rent()) first.Value.Append("secret");
        using var second = pool.Rent();

        Assert.Equal("", second.Value.ToString());
    }

    [Fact]
    public void Pool_CreatesNewObjectsWhileOthersAreRentedOut()
    {
        var pool = new ObjectPool<List<int>>(() => [], maxRetained: 2);

        var leases = Enumerable.Range(0, 5).Select(_ => pool.Rent()).ToList();
        Assert.Equal(5, leases.Select(l => l.Value).Distinct().Count());

        leases.ForEach(l => l.Dispose());

        Assert.Equal(5, pool.Created);
        Assert.Equal(2, pool.Available); // the other three are left to the GC
    }

    [Fact]
    public void Pool_DisposingTheSameLeaseTwiceReturnsItOnce()
    {
        var pool = new ObjectPool<object>(() => new object());
        var lease = pool.Rent();

        lease.Dispose();
        lease.Dispose();

        Assert.Equal(1, pool.Available);
    }

    [Fact]
    public async Task StreamCopier_CopiesEveryByteThroughASmallBuffer()
    {
        var data = new byte[1_000_003];
        new Random(42).NextBytes(data);
        using var destination = new MemoryStream();

        var copied = await StreamCopier.CopyAsync(new MemoryStream(data), destination, bufferSize: 4_096);

        Assert.Equal(data.Length, copied);
        Assert.Equal(data, destination.ToArray());
    }

    [Fact]
    public async Task StreamCopier_StopsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            StreamCopier.CopyAsync(new MemoryStream(new byte[100]), Stream.Null, cancellationToken: cts.Token));
    }
}
