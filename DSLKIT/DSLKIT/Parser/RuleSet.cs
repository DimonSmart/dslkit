using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public class RuleSet
    {
        public int SetNumber { get; set; }
        public readonly IList<Rule> Rules = new List<Rule>();
        public IDictionary<ITerm, RuleSet> Arrows = new Dictionary<ITerm, RuleSet>();
        public int SetFormRules;

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

        internal bool StartsFrom(IEnumerable<Rule> newRules)
        {
            var otherRules = newRules.ToArray();
            if (Rules.Count < otherRules.Length)
            {
                return false;
            }
            for (var i = 0; i < otherRules.Length; i++)
            {
                if (Rules[i] != otherRules[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}