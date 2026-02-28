using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public class RuleSet
    {
        private readonly List<Rule> _rules;
        private readonly Dictionary<ITerm, RuleSet> _arrows;
        private readonly IReadOnlyList<Rule> _rulesView;
        private readonly IReadOnlyDictionary<ITerm, RuleSet> _arrowsView;
        private readonly HashSet<Rule> _kernelRules;
        private readonly HashSet<Rule> _ruleLookup;

        public RuleSet(int setNumber, IEnumerable<Rule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _rules = rules.ToList();
            _arrows = new Dictionary<ITerm, RuleSet>();
            _rulesView = _rules.AsReadOnly();
            _arrowsView = new ReadOnlyDictionary<ITerm, RuleSet>(_arrows);
            _kernelRules = _rules.ToHashSet();
            _ruleLookup = _rules.ToHashSet();

            SetNumber = setNumber;
            KernelRuleCount = _rules.Count;
        }

        public IReadOnlyList<Rule> Rules => _rulesView;
        public IReadOnlyDictionary<ITerm, RuleSet> Arrows => _arrowsView;
        public int KernelRuleCount { get; }
        public int SetNumber { get; set; }

        internal bool AddRule(Rule rule)
        {
            if (rule is null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (!_ruleLookup.Add(rule))
            {
                return false;
            }

            _rules.Add(rule);
            return true;
        }

        internal void SetArrow(ITerm term, RuleSet target)
        {
            if (term == null)
            {
                throw new ArgumentNullException(nameof(term));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            _arrows[term] = target;
        }

        public override string ToString()
        {
            return $"Set({SetNumber}){Environment.NewLine}{string.Join(Environment.NewLine, _rules)}";
        }

        internal bool StartsFrom(IEnumerable<Rule> newRules)
        {
            if (newRules == null)
            {
                throw new ArgumentNullException(nameof(newRules));
            }

            var candidateKernel = newRules as ICollection<Rule> ?? newRules.ToArray();
            if (candidateKernel.Count != KernelRuleCount)
            {
                return false;
            }

            return _kernelRules.SetEquals(candidateKernel);
        }
    }
}
