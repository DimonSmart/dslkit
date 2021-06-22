namespace DSLKIT.NonTerminals
{
    public class NonTerminal : INonTerminal
    {
        public NonTerminal(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}