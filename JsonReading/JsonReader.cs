using System.Text;
using System.Text.Json;

namespace JsonReading;

public interface IJsonReadable<T>
    where T : IJsonReadable<T>
{
    public static abstract T Read(JsonReader reader);
}

internal enum JsonToken
{   
    Colon = 0x3a, // :
    QuoteMark= 0x22, // "
    SquareBracketOpen = 0x5b, // [
    SquareBracketClose = 0x5d, // ]
    CurlyBraceOpen = 0x7b, // {
    CurlyBraceClose = 0x7d, // }
    Comma = 0x2c // ,
}

public class JsonReader
{
    
    private bool isArray;
    private ReadOnlyMemory<byte> buffer;
    
    private Dictionary<string, string> parseResult;
    
    private JsonReader(ReadOnlyMemory<byte> buffer)
    {
        this.isArray = false;
        this.buffer = buffer;
        this.parseResult = new Dictionary<string, string>();
    }

    private void Read()
    {
        ReadOnlySpan<byte> span = this.buffer.SkipWhitespaces();
        if (span[0] == (byte)JsonToken.CurlyBraceOpen)
        {
            ObjectReadingMode(span);
            return;
        }

        if (span[0] == (byte)JsonToken.SquareBracketOpen)
        {
            ArrayReadingMode(span);
            return;
        }

        throw new JsonException("Unexpected [ or {");
    }

    private void ArrayReadingMode(ReadOnlySpan<byte> span)
    {
        span = span.SkipWhitespaces();
        int hierachy = 0;
        if (span[0] != (byte)JsonToken.CurlyBraceOpen)
        {
            throw new JsonException("An array can only contain objects!");
        }
        int count = 0;

    }

    private void ObjectReadingMode(ReadOnlySpan<byte> span)
    {
        while (true)
        {
            span = span[1..].SkipWhitespaces();

            if (span[0] == (byte)JsonToken.CurlyBraceClose)
            {
                break;
            }
            
            span = span.ReadJsonString(out string propName);
            span = span.EnsureColumn();
            span = span.SkipWhitespaces();
            string propValue = string.Empty;
            switch (span[0])
            {
                case (byte)JsonToken.QuoteMark:
                    // TODO: READ string value
                    span = span.ReadJsonString(out propValue);
                    break;
                case (byte)JsonToken.CurlyBraceOpen:
                    // TODO: READ object
                    break;
                case (byte)JsonToken.SquareBracketOpen:
                    // TODO: READ array
                    break;
                case 0x66:
                    // TODO: READ false
                    propValue = Encoding.UTF8.GetString(span[..5]);
                    span = span[5..];
                    break;
                case 0x74:
                    // TODO: READ true
                    propValue = Encoding.UTF8.GetString(span[..4]);
                    span = span[4..];
                    break;
                default:
                    throw new JsonException("No such property value!");
            }
            this.parseResult.Add(propName, propValue);
            span = span.EnsureComma();
        }
    }
    
    #region CONVERSION

    public int ReadInt(string propName)
    {
        if (string.IsNullOrEmpty(propName)) 
            throw new ArgumentNullException(nameof(propName));
        if (!parseResult.ContainsKey(propName))
            throw new KeyNotFoundException($"The parsed json does not contain a property named {propName}");
        return int.Parse(parseResult[propName]);
    }
    
    public double ReadDouble(string propName)
    {
        if (string.IsNullOrEmpty(propName)) 
            throw new ArgumentNullException(nameof(propName));
        if (!parseResult.ContainsKey(propName))
            throw new KeyNotFoundException($"The parsed json does not contain a property named {propName}");
        return double.Parse(parseResult[propName]);
    }

    public string ReadString(string propName)
    {
        if (string.IsNullOrEmpty(propName)) 
            throw new ArgumentNullException(nameof(propName));
        if (!parseResult.ContainsKey(propName))
            throw new KeyNotFoundException($"The parsed json does not contain a property named {propName}");
        return parseResult[propName];
    }

    public bool ReadBool(string propName)
    {
        if (string.IsNullOrEmpty(propName)) 
            throw new ArgumentNullException(nameof(propName));
        if (!parseResult.ContainsKey(propName))
            throw new KeyNotFoundException($"The parsed json does not contain a property named {propName}");
        return bool.Parse(parseResult[propName]);
    }

    public JsonReader ReadStructure(string propName)
    {
        if (string.IsNullOrEmpty(propName)) 
            throw new ArgumentNullException(nameof(propName));
        if (!parseResult.ContainsKey(propName))
            throw new KeyNotFoundException($"The parsed json does not contain a property named {propName}");
        JsonReader reader = new JsonReader(
            Encoding.UTF8.GetBytes(parseResult[propName]));
        return reader;
    } 
    
    #endregion
    
    public static T[] ReadArray<T>(string json)
        where T : IJsonReadable<T>
    {
        JsonReader reader = new JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        T[] arr = new T[reader.parseResult.Count];
        int counter = 0;
        foreach (string item in reader.parseResult.Keys)
        {
            JsonReader r = reader.ReadStructure(item);
            arr[counter] = T.Read(r);
        }

        return arr;
    }
    
    public static T ReadObject<T>(string json)
        where T : IJsonReadable<T>
    {
        JsonReader reader = new JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        T result = T.Read(reader);
        return result;
    }
}

internal static class MemoryExtensions
{
    private enum Whitespace
    {
        Space = 0x20,
        CarriageReturn = 0x0d,
        LineFeed =  0x0a,
    }
    
    public static ReadOnlySpan<byte> SkipWhitespaces(
        this ReadOnlyMemory<byte> self)
    {
        ReadOnlySpan<byte> span = self.Span;
        if (span.IsEmpty)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        
        while (true)
        {
            if (span[0] == (byte)Whitespace.LineFeed || 
                span[0] == (byte)Whitespace.Space ||
                span[0] == (byte)Whitespace.CarriageReturn)
            {
                span = span[1..];
            }
            else
            {
                break;
            }
        }

        return span;
    }
    
    public static ReadOnlySpan<byte> SkipWhitespaces(
        this ReadOnlySpan<byte> self)
    {
        if (self.IsEmpty)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        
        while (true)
        {
            if (self[0] == (byte)Whitespace.LineFeed || 
                self[0] == (byte)Whitespace.Space ||
                self[0] == (byte)Whitespace.CarriageReturn)
            {
                self = self[1..];
            }
            else
            {
                break;
            }
        }

        return self;
    }

    public static ReadOnlySpan<byte> ReadJsonString(this ReadOnlySpan<byte> self, out string value)
    {
        value = null;
        if (self.IsEmpty)
        {
            throw new JsonException("Expected a string start got empty!");
        }

        if (self[0] != (byte)JsonToken.QuoteMark)
        {
            throw new JsonException("Expected string start!");
        }
        
        self = self[1..];
        int count = 0;
        while (true)
        {
            if (self[count] == (byte)JsonToken.QuoteMark)
            {
                break;
            }

            count++;
        }

        value = Encoding.UTF8.GetString(self[..count]);
        return self[(count + 1)..];
    }

    public static ReadOnlySpan<byte> EnsureColumn(this ReadOnlySpan<byte> self)
    {
        if (self.IsEmpty)
        {
            throw new JsonException("Expected : got empty!");
        }

        if (self[0] != (byte)JsonToken.Colon)
        {
            throw new JsonException("Expected colon");
        }

        return self[1..];
    }

    public static ReadOnlySpan<byte> EnsureComma(this ReadOnlySpan<byte> self)
    {
        if (self.IsEmpty)
        {
            throw new JsonException("Expected , got empty!");
        }

        if (self[0] != (byte)JsonToken.Comma)
        {
            throw new JsonException("Expected comma");
        }

        return self[1..];
    }
}