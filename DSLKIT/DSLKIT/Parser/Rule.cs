namespace DSLKIT.Parser
{
    public class Rule : Production
    {
        public Rule(Production production, int dotPosition = 0) : base(production.LeftNonTerminal, production.ProductionDefinition)
        {
            DotPosition = dotPosition;
        }

        public int DotPosition { get; set; }

        public bool IsFinished()
        {
            return DotPosition == ProductionDefinition.Count;
        }

        public override string ToString()
        {
            return ProductionToString(DotPosition);
        }
    }
}
