using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

// --- 1. The Pipe Abstractions ---

// A generic interface for any filter
public interface IFilter<TInput, TOutput>
{
    TOutput Process(TInput input);
}

// The "Pipe" - A connector that holds a sequence of operations
// This allows us to chain filters together generically.
public class Pipeline<TInput, TOutput>
{
    // A function pointer that represents the "chain" built so far
    private readonly Func<TInput, TOutput> _pipelineChain;

    // Constructor for the initial step
    public Pipeline(Func<TInput, TOutput> step)
    {
        _pipelineChain = step;
    }

    // The "Connect" method (The actual "Pipe" logic)
    // It takes the current chain and appends a new filter to it.
    public Pipeline<TInput, TNewOutput> PipeTo<TNewOutput>(IFilter<TOutput, TNewOutput> filter)
    {
        // We return a NEW pipeline that first runs the old chain, 
        // then feeds that result into the new filter.
        return new Pipeline<TInput, TNewOutput>(input => 
        {
            var resultOfPrevious = _pipelineChain(input);
            return filter.Process(resultOfPrevious);
        });
    }

    // Execute the entire chain
    public TOutput Execute(TInput input)
    {
        return _pipelineChain(input);
    }
}

// --- 2. Concrete Filters (Same logic, slight naming tweak) ---

public class FileReadFilter : IFilter<string, string>
{
    public string Process(string filePath)
    {
        Console.WriteLine("-> [Filter 1] Reading File...");
        return File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
    }
}

public class TextSanitizerFilter : IFilter<string, string>
{
    public string Process(string input)
    {
        Console.WriteLine("-> [Filter 2] Sanitizing...");
        var clean = Regex.Replace(input, @"[^\w\s]", "");
        return Regex.Replace(clean, @"\s+", " ").Trim();
    }
}

public class WordCountFilter : IFilter<string, int>
{
    public int Process(string input)
    {
        Console.WriteLine("-> [Filter 3] Counting Words...");
        return string.IsNullOrWhiteSpace(input) ? 0 : input.Split(' ').Length;
    }
}

// --- 3. The Orchestration ---

class Program
{
    static void Main(string[] args)
    {
        // Dummy Data
        string path = "input1.txt";

        // 1. SETUP: We build the pipeline structure ONCE.
        // Main doesn't touch the data; it just connects the pipes.
        var source = new FileReadFilter();

        // Create the start of the pipe
        var pipelineBuilder = new Pipeline<string, string>(source.Process);

        var sanitizer = new TextSanitizerFilter();
        
        // Chain the filters (The "Pipes")
        var completePipeline = pipelineBuilder
            .PipeTo(sanitizer)
            .PipeTo(new WordCountFilter());
        
        // 2. EXECUTION: The pipeline runs itself.
        Console.WriteLine("Starting Pipeline...");
        int result = completePipeline.Execute(path);

        // 3. Output
        Console.WriteLine($"\nFinal Result: {result} words found.");
    }
}
