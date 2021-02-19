using DSLKIT.Base;
using System;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class RuleSet
    {
        public int SetNumber { get; set; }
        public readonly IList<Rule> Rules = new List<Rule>();
        public IDictionary<ITerm, RuleSet> arrows = new Dictionary<ITerm, RuleSet>();
        public int SetFormRules = 0;

        public RuleSet(int setNumber, IEnumerable<Rule> rules)
        {
            SetNumber = setNumber;
            foreach (var rule in rules)
            {
                Rules.Add(rule);
                SetFormRules++;
            }
        }

        public override string ToString()
        {
            return $"Set({SetNumber}){Environment.NewLine}{string.Join(Environment.NewLine, Rules)}";
        }
    }
}
