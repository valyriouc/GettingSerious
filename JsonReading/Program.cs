
using JsonReading;

internal class Program
{
    public static void Main()
    {
        string json =
            """
            {
                "hello": true,
                "nice": false,
                "wir": "hello my friend",
            }
            """;

        var t = JsonReader.ReadObject<Test>(json);
        
        Console.WriteLine($"Hello: {t.Hello}");
        Console.WriteLine($"Nice: {t.Nice}");
        Console.WriteLine($"Wir: {t.Wir}");
    }
}

class Test : IJsonReadable<Test>
{
    public bool Hello { get; private set; } = false;
    
    public bool Nice { get; private set; } = true;
    
    public string Wir { get; private set; } = "";

    public static Test Read(JsonReader reader)
    {
        Test t = new Test();
        t.Hello = reader.ReadBool("hello");
        t.Nice = reader.ReadBool("nice");
        t.Wir = reader.ReadString("wir");
        return t;
    }
}