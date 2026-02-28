namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    public sealed record SqlFormattingOptions
    {
        public bool UppercaseKeywords { get; init; } = true;

        // Extension point: future formatting options (line wrapping, alignment, heuristics).
        // Keep defaults stable so introducing new options does not change behavior unexpectedly.
    }
}
