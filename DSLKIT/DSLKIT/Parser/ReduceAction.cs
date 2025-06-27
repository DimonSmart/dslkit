namespace DSLKIT.Parser
{
    public class ReduceAction : IActionItem
    {
        public Production Production { get; }
        public int PopLength { get; }

        public ReduceAction(Production production, int popLength)
        {
            Production = production;
            PopLength = popLength;
        }

        public override string ToString()
        {
            return $"r{Production.LeftNonTerminal.Name}";
        }
    }
}
