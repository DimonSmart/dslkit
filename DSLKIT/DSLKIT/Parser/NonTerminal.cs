using System;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class NonTerminal : ITerm
    {
        public NonTerminal(string name)
        {
            Name = name;
        }

        public NonTerminal() : this(Guid.NewGuid().ToString())
        {
        }

        public string Name { get; }

        public override string ToString()
        {
            return $"NT:{Name}";
        }
    }
}