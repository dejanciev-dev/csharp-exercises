using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace CSharpExercises.Formats;

/// <summary>
/// Newline-delimited JSON (JSON Lines): one JSON document per line. Unlike one big JSON array, it can be appended to,
/// streamed and processed line by line in constant memory, and one corrupt line doesn't spoil the rest. Used for logs,
/// data exports and streaming APIs. Reading is an async stream; bad lines can be skipped and reported.
/// </summary>
public static class NdJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync<T>(Stream stream, IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\n" };
        await foreach (var item in items.WithCancellation(cancellationToken))
            await writer.WriteLineAsync(JsonSerializer.Serialize(item, Options)); // compact JSON never contains a raw newline
    }

    /// <param name="onError">Called with the line number and error for unparsable lines; null makes them throw.</param>
    public static async IAsyncEnumerable<T> ReadAsync<T>(Stream stream, Action<int, string>? onError = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            T? item;
            try
            {
                item = JsonSerializer.Deserialize<T>(line, Options);
            }
            catch (JsonException e) when (onError is not null)
            {
                onError(lineNumber, e.Message);
                continue;
            }
            if (item is not null) yield return item;
        }
    }

    public static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
