# saap-text-analyzer

## 1. Use Case Description
The goal was to build a "Text Analyzer" application that processes a raw text file through several distinct steps: reading the file, sanitizing the text (removing punctuation/formatting), and analyzing the content (counting words and characters).

**Use Case:** `Read File` $\rightarrow$ `Sanitize Text` $\rightarrow$ `Count Words` & `Count Characters`

---

## 2. Pattern A: Pipes and Filters
We selected the **Pipes and Filters** architectural pattern. This pattern decomposes the task into self-contained processing steps (Filters) connected by a standard data transfer mechanism (Pipes).

### Structure:
* **`IFilter<TInput, TOutput>`:** A generic interface enforcing a standard contract for all components.
* **`Pipeline` Class:** An orchestrator responsible for chaining filters together. It encapsulates the flow control, allowing `Main` to focus on configuration rather than execution.
* **Concrete Filters:** `FileReadFilter`, `TextSanitizerFilter`, `WordCountFilter`.

### Pros & Cons

**Advantages (Pros)**
* **Reusability:** Each filter is independent. The `TextSanitizer` can be reused anywhere that needs string cleaning without needing the File Reader.
* **Extensibility:** Adding a new step (e.g., `CapitalizeFilter`) is as simple as injecting it into the pipeline construction in `Main`.
* **Clarity:** The data flow is explicit and linear, making it easy to understand the sequence of operations at a glance.

**Disadvantages (Cons)**
* **Linear Data Dependency (The "Context" Problem):** In a strict linear pipeline (`A` $\rightarrow$ `B` $\rightarrow$ `C`), Filter `C` depends entirely on `B`'s output. We wanted to count *Words* (int) and *Chars* (int) from the same *Sanitized Text* (string). Once the data transformed into an `int`, the original string was lost to the next filter.
* **Flexibility:** Dealing with "branching" logic (one output feeding two inputs) required awkward workarounds like splitting the pipeline manually.

---

## 3. Pattern B: Event-Driven Architecture (Broker Topology)
We implemented a **Broker Topology** where components generate events (e.g., `TextSanitized`) and other components listen for them. There is no central orchestrator forcing the flow; the flow emerges from the subscriptions.

### Structure:
* **Events (`EventArgs`):** Custom classes like `SanitizedTextEventArgs` carry the payload between components.
* **Publishers & Subscribers:** Components like `TextSanitizer` expose C# `EventHandler` delegates. `WordCounter` and `CharCounter` subscribe to these events via constructor injection.
* **Aggregator:** A `ResultAggregator` listens to multiple analysis events and combines the results when ready.

### Pros & Cons

**Advantages (Pros)**
* **Solved the Branching Problem:** Multiple components (`WordCounter` and `CharCounter`) could subscribe to the exact same `TextSanitized` event. We did not need to split pipelines or pass complex context objects; the data was simply "broadcast" to whoever needed it.
* **Decoupling:** The `TextSanitizer` has absolutely no knowledge of who is consuming its data. It simply shouts "I have sanitized text!" and moves on.
* **Asynchronous Potential:** While our implementation was synchronous, this pattern easily supports asynchronous execution where counters run in parallel threads.

**Disadvantages (Cons)**
* **Boilerplate Code:** This was immediately noticeable. We had to define interfaces, custom `EventArgs` classes, and event delegates for every single step. The code size increased significantly compared to the functional pipeline.
* **Complexity & Flow:** The control flow is inverted. You cannot look at `Main` and see exactly what happens in what order; you have to trace the event subscriptions to understand the system.
* **Debugging:** Tracing a bug is harder because the stack trace is often broken up by event invocations, and there is no single "script" of execution.

---

## 4. Final Comparison & Conclusion

| Feature | Pipes & Filters | Event-Driven |
| :--- | :--- | :--- |
| **Data Flow** | Linear (Stream) | Broadcast (Pub/Sub) |
| **Coupling** | Low (Interface-based) | Very Low (Event-based) |
| **Complexity** | Low (Easy to trace) | High (Harder to trace) |
| **Code Volume** | Concise | Verbose (Boilerplate) |
| **Branching** | Difficult (Linear constraint) | Natural (Multiple listeners) |

### Conclusion
For this specific Use Case, **Event-Driven Architecture** felt like "over-engineering" due to the high amount of boilerplate code required for simple string passing. However, it was technically superior for the requirements because it naturally handled the split between *Word Counting* and *Char Counting* (the branching problem) which Pipes & Filters struggled with.

If the requirement were strictly linear (A -> B -> C), **Pipes & Filters** would be the winner. Since we needed parallel processing of the same data, **Event-Driven** offered a cleaner architectural solution, despite the extra code.