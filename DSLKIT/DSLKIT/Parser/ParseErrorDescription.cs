namespace DSLKIT.Parser
{
    public record ParseErrorDescription (string Message, int ErrorPosition)
    {
        public override string ToString()
        {
            return $"Parse error at position {ErrorPosition}: {Message}";
        }
    }
}
