using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public sealed record ParseErrorDescription
    {
        public required string Message { get; init; }

        public required int ErrorPosition { get; init; }

        public string? ActualTokenText { get; init; }

        public IReadOnlyList<string> ExpectedTokens { get; init; } = [];

        public override string ToString()
        {
            return $"Parse error at position {ErrorPosition}: {Message}";
        }
    }
}
