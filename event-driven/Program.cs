using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : "input1.txt";

        // Instantiate concretes
        var fileReader = new FileReader();

        // Pass dependencies as interfaces into constructors; constructors register the events
        ITextSanitizer sanitizer = new TextSanitizer(fileReader);
        IWordCounter wordCounter = new WordCounter(sanitizer);
        ICharCounter charCounter = new CharCounter(sanitizer);

        // Aggregator subscribes in ctor to the counters
        var aggregator = new ResultAggregator(wordCounter, charCounter);

        Console.WriteLine("Starting event-driven processing (constructor DI)...");
        fileReader.Read(path);
    }
}


// --- Event arg types ---
public class RawTextEventArgs : EventArgs
{
    public required string Text { get; init; }
}

public class SanitizedTextEventArgs : EventArgs
{
    public required string SanitizedText { get; init; }
}

public class CountEventArgs : EventArgs
{
    public required int Count { get; init; }
}

// --- Interfaces (expose events + minimal ops) ---
public interface IFileReader
{
    event EventHandler<RawTextEventArgs>? RawTextRead;
    void Read(string path);
}

public interface ITextSanitizer
{
    event EventHandler<SanitizedTextEventArgs>? TextSanitized;
}

public interface IWordCounter
{
    event EventHandler<CountEventArgs>? WordCounted;
}

public interface ICharCounter
{
    event EventHandler<CountEventArgs>? CharCounted;
}

// --- Implementations ---

// File reader: raises RawTextRead
public class FileReader : IFileReader
{
    public event EventHandler<RawTextEventArgs>? RawTextRead;

    public void Read(string path)
    {
        Console.WriteLine("-> [FileReader] Reading file...");
        var text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;

        Console.WriteLine("## [FileReader] Raw text read!");
        RawTextRead?.Invoke(this, new RawTextEventArgs { Text = text });
    }
}

// TextSanitizer subscribes to IFileReader (in ctor) and raises SanitizedTextReady
public class TextSanitizer : ITextSanitizer
{
    public event EventHandler<SanitizedTextEventArgs>? TextSanitized;

    public TextSanitizer(IFileReader reader)
    {
        if (reader is null) throw new ArgumentNullException(nameof(reader));
        reader.RawTextRead += OnRawTextRead;
    }

    private void OnRawTextRead(object? _, RawTextEventArgs e)
    {
        Console.WriteLine("-> [TextSanitizer] Sanitizing...");
        var clean = Regex.Replace(e.Text ?? string.Empty, @"[^\w\s]", "");
        clean = Regex.Replace(clean, @"\s+", " ").Trim();

        Console.WriteLine("## [TextSanitizer] Text sanitized!...");
        TextSanitized?.Invoke(this, new SanitizedTextEventArgs { SanitizedText = clean });
    }
}

// WordCounter subscribes to ITextSanitizer (in ctor) and raises WordCounted
public class WordCounter : IWordCounter
{
    public event EventHandler<CountEventArgs>? WordCounted;

    public WordCounter(ITextSanitizer sanitizer)
    {
        if (sanitizer is null) throw new ArgumentNullException(nameof(sanitizer));
        sanitizer.TextSanitized += OnSanitizedTextReady;
    }

    private void OnSanitizedTextReady(object? _, SanitizedTextEventArgs e)
    {
        Console.WriteLine("-> [WordCounter] Counting words...");
        var count = string.IsNullOrWhiteSpace(e.SanitizedText) ? 0 : e.SanitizedText.Split(' ').Length;

        Console.WriteLine("## [WordCounter] Words counted!");
        WordCounted?.Invoke(this, new CountEventArgs { Count = count });
    }
}

// CharCounter subscribes to ITextSanitizer (in ctor) and raises CharCounted
public class CharCounter : ICharCounter
{
    public event EventHandler<CountEventArgs>? CharCounted;

    public CharCounter(ITextSanitizer sanitizer)
    {
        if (sanitizer is null) throw new ArgumentNullException(nameof(sanitizer));
        sanitizer.TextSanitized += OnSanitizedTextReady;
    }

    private void OnSanitizedTextReady(object? _, SanitizedTextEventArgs e)
    {
        Console.WriteLine("-> [CharCounter] Counting characters...");
        var count = e.SanitizedText?.Replace(" ", "").Length ?? 0;

        Console.WriteLine("## [CharCounter] Chars counted!");
        CharCounted?.Invoke(this, new CountEventArgs { Count = count });
    }
}

// ResultAggregator subscribes to IWordCounter and ICharCounter (in ctor) and prints when both are available
public class ResultAggregator
{
    private int? _words;
    private int? _chars;

    public ResultAggregator(IWordCounter wordCounter, ICharCounter charCounter)
    {
        if (wordCounter is null) throw new ArgumentNullException(nameof(wordCounter));
        if (charCounter is null) throw new ArgumentNullException(nameof(charCounter));

        wordCounter.WordCounted += OnWordCounted;
        charCounter.CharCounted += OnCharCounted;
    }

    private void OnWordCounted(object? _, CountEventArgs e)
    {
        _words = e.Count;
        TryPrint();
    }

    private void OnCharCounted(object? _, CountEventArgs e)
    {
        _chars = e.Count;
        TryPrint();
    }

    private void TryPrint()
    {
        if (_words.HasValue && _chars.HasValue)
        {
            Console.WriteLine();
            Console.WriteLine($"Final Result: {_words.Value} words, {_chars.Value} characters.");
            _words = _chars = null;
        }
    }
}