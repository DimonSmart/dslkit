using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class RuleSet
    {
        public int SetNumber { get; set; }
        public readonly IList<Rule> Rules = new List<Rule>();

        public RuleSet(int setNumber)
        {
            SetNumber = setNumber;
        }
    }
}
