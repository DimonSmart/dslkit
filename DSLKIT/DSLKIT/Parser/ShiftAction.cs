namespace DSLKIT.Parser
{
    public class ShiftAction : IActionItem
    {
        public RuleSet RuleSet { get; }

        public ShiftAction(RuleSet ruleSet)
        {
            RuleSet = ruleSet;
        }

        public override string ToString()
        {
            return $"s{RuleSet.SetNumber}";
        }
    }
}