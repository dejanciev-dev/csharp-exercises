using System.IO.Compression;

namespace CSharpExercises.IO;

/// <summary>
/// A decorator over any <see cref="Stream"/> that counts the bytes going through it. Streams compose:
/// <c>new GZipStream(new CountingStream(Stream.Null), ...)</c> measures how big compressed data would be
/// without keeping any of it in memory.
/// </summary>
public sealed class CountingStream(Stream inner, bool leaveOpen = false) : Stream
{
    public long BytesRead { get; private set; }
    public long BytesWritten { get; private set; }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var read = inner.Read(buffer);
        BytesRead += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);
        BytesRead += read;
        return read;
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        inner.Write(buffer);
        BytesWritten += buffer.Length;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await inner.WriteAsync(buffer, cancellationToken);
        BytesWritten += buffer.Length;
    }

    public override void Flush() => inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !leaveOpen) inner.Dispose();
        base.Dispose(disposing);
    }
}

public static class StreamTools
{
    /// <summary>
    /// The size <paramref name="source"/> would have after gzip compression, computed by streaming through
    /// a compressor into <see cref="Stream.Null"/>: memory use stays constant however big the input is.
    /// </summary>
    public static async Task<long> CompressedSizeAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var counter = new CountingStream(Stream.Null);
        await using (var gzip = new GZipStream(counter, CompressionLevel.Optimal, leaveOpen: true))
        {
            await source.CopyToAsync(gzip, cancellationToken);
        } // disposing the GZipStream flushes its last block into the counter

        return counter.BytesWritten;
    }

    /// <summary>
    /// Sums one column of a CSV stream line by line. Only one line is in memory at a time, so a 50 GB file works as
    /// well as a 50 KB one; <c>File.ReadAllLines</c> would try to load all of it.
    /// </summary>
    public static async Task<(long Rows, double Sum)> SumColumnAsync(Stream csv, int column, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csv, leaveOpen: true);
        await reader.ReadLineAsync(cancellationToken); // header

        long rows = 0;
        double sum = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0) continue;

            var fields = line.Split(',');
            if (column >= fields.Length) throw new InvalidDataException($"Row {rows + 2} has only {fields.Length} columns.");

            sum += double.Parse(fields[column], System.Globalization.CultureInfo.InvariantCulture);
            rows++;
        }
        return (rows, sum);
    }
}
