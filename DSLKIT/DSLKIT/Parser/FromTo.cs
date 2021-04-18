namespace DSLKIT.Parser
{
    public class FromTo
    {
        public FromTo(RuleSet from, RuleSet to)
        {
            From = from;
            To = to;
        }

        public RuleSet From { get; set; }
        public RuleSet To { get; set; }
    }
}