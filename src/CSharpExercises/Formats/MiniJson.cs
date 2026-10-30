using System.Globalization;
using System.Text;

namespace CSharpExercises.Formats;

/// <summary>JSON values as a small closed hierarchy of records.</summary>
public abstract record JsonValue
{
    public sealed record Null : JsonValue { public static readonly Null Instance = new(); }
    public sealed record Bool(bool Value) : JsonValue;
    public sealed record Number(double Value) : JsonValue;
    public sealed record String(string Value) : JsonValue;
    public sealed record Array(IReadOnlyList<JsonValue> Items) : JsonValue;
    public sealed record Object(IReadOnlyDictionary<string, JsonValue> Members) : JsonValue;

    public JsonValue this[string key] => this is Object o ? o.Members[key] : throw new InvalidOperationException("Not an object.");
    public JsonValue this[int index] => this is Array a ? a.Items[index] : throw new InvalidOperationException("Not an array.");
}

/// <summary>
/// A complete JSON parser (RFC 8259) in one recursive-descent class, and a serializer back to compact text. The
/// grammar is small enough that each rule is one method; errors report the position, and nesting is limited so
/// hostile input can't overflow the stack.
/// </summary>
public sealed class MiniJson
{
    private const int MaxDepth = 64;
    private readonly string _text;
    private int _pos;
    private int _depth;

    private MiniJson(string text) => _text = text;

    public static JsonValue Parse(string text)
    {
        var parser = new MiniJson(text);
        var value = parser.ParseValue();
        parser.SkipWhitespace();
        if (parser._pos != text.Length) throw parser.Error("Unexpected content after the value");
        return value;
    }

    public static string Serialize(JsonValue value) => value switch
    {
        JsonValue.Null => "null",
        JsonValue.Bool b => b.Value ? "true" : "false",
        JsonValue.Number n => n.Value.ToString("R", CultureInfo.InvariantCulture),
        JsonValue.String s => Quote(s.Value),
        JsonValue.Array a => $"[{string.Join(',', a.Items.Select(Serialize))}]",
        JsonValue.Object o => $"{{{string.Join(',', o.Members.Select(m => $"{Quote(m.Key)}:{Serialize(m.Value)}"))}}}",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private JsonValue ParseValue()
    {
        SkipWhitespace();
        if (_pos >= _text.Length) throw Error("Unexpected end of input");
        return _text[_pos] switch
        {
            '{' => Nested(ParseObject),
            '[' => Nested(ParseArray),
            '"' => new JsonValue.String(ParseString()),
            't' => Literal("true", new JsonValue.Bool(true)),
            'f' => Literal("false", new JsonValue.Bool(false)),
            'n' => Literal("null", JsonValue.Null.Instance),
            '-' or (>= '0' and <= '9') => ParseNumber(),
            var c => throw Error($"Unexpected character '{c}'"),
        };
    }

    private JsonValue Nested(Func<JsonValue> parse)
    {
        if (++_depth > MaxDepth) throw Error($"Nesting deeper than {MaxDepth}");
        var value = parse();
        _depth--;
        return value;
    }

    private JsonValue.Object ParseObject()
    {
        _pos++; // {
        var members = new Dictionary<string, JsonValue>();
        SkipWhitespace();
        if (TryConsume('}')) return new(members);

        do
        {
            SkipWhitespace();
            if (Peek() != '"') throw Error("Expected a property name");
            var key = ParseString();
            SkipWhitespace();
            Expect(':');
            if (!members.TryAdd(key, ParseValue())) throw Error($"Duplicate property '{key}'");
            SkipWhitespace();
        } while (TryConsume(','));

        Expect('}');
        return new(members);
    }

    private JsonValue.Array ParseArray()
    {
        _pos++; // [
        var items = new List<JsonValue>();
        SkipWhitespace();
        if (TryConsume(']')) return new(items);

        do
        {
            items.Add(ParseValue());
            SkipWhitespace();
        } while (TryConsume(','));

        Expect(']');
        return new(items);
    }

    private string ParseString()
    {
        _pos++; // opening quote
        var sb = new StringBuilder();
        while (true)
        {
            if (_pos >= _text.Length) throw Error("Unterminated string");
            var c = _text[_pos++];
            if (c == '"') return sb.ToString();
            if (c < 0x20) throw Error("Control characters must be escaped");
            if (c != '\\')
            {
                sb.Append(c);
                continue;
            }

            if (_pos >= _text.Length) throw Error("Unterminated escape");
            sb.Append(_text[_pos++] switch
            {
                '"' => '"', '\\' => '\\', '/' => '/', 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t',
                'u' when _pos + 4 <= _text.Length && ushort.TryParse(_text.AsSpan(_pos, 4), NumberStyles.HexNumber, null, out var code)
                    => Advance((char)code, 4),
                var e => throw Error($"Invalid escape '\\{e}'"),
            });
        }
    }

    private JsonValue.Number ParseNumber()
    {
        var start = _pos;
        TryConsume('-');
        if (TryConsume('0')) { }
        else if (Peek() is >= '1' and <= '9') SkipDigits();
        else throw Error("Invalid number");

        if (TryConsume('.'))
        {
            if (Peek() is not (>= '0' and <= '9')) throw Error("Expected digits after the decimal point");
            SkipDigits();
        }
        if (Peek() is 'e' or 'E')
        {
            _pos++;
            if (Peek() is '+' or '-') _pos++;
            if (Peek() is not (>= '0' and <= '9')) throw Error("Expected digits in the exponent");
            SkipDigits();
        }
        return new(double.Parse(_text.AsSpan(start, _pos - start), CultureInfo.InvariantCulture));
    }

    private JsonValue Literal(string word, JsonValue value)
    {
        if (string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0) throw Error($"Expected '{word}'");
        _pos += word.Length;
        return value;
    }

    private static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            sb.Append(c switch
            {
                '"' => "\\\"", '\\' => "\\\\", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t",
                < ' ' => $"\\u{(int)c:x4}",
                _ => c.ToString(),
            });
        }
        return sb.Append('"').ToString();
    }

    private char Advance(char c, int count) { _pos += count; return c; }
    private char Peek() => _pos < _text.Length ? _text[_pos] : '\0';
    private void SkipDigits() { while (Peek() is >= '0' and <= '9') _pos++; }
    private void SkipWhitespace() { while (Peek() is ' ' or '\t' or '\n' or '\r') _pos++; }
    private bool TryConsume(char c) { if (Peek() != c) return false; _pos++; return true; }
    private void Expect(char c) { if (!TryConsume(c)) throw Error($"Expected '{c}'"); }
    private FormatException Error(string message) => new($"{message} at position {_pos}.");
}
