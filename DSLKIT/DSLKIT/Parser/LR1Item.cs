using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    /// <summary>
    /// Represents an LR(1) item with lookahead information.
    /// This extends the basic Rule concept to include lookahead tokens.
    /// </summary>
    public class LR1Item : IEquatable<LR1Item>
    {
        public Rule CoreRule { get; }
        public ISet<ITerm> Lookaheads { get; }

        public LR1Item(Rule coreRule, IEnumerable<ITerm> lookaheads)
        {
            CoreRule = coreRule ?? throw new ArgumentNullException(nameof(coreRule));
            Lookaheads = new HashSet<ITerm>(lookaheads ?? throw new ArgumentNullException(nameof(lookaheads)));
        }

        /// <summary>
        /// Creates a new LR(1) item with the dot moved one position to the right.
        /// </summary>
        public LR1Item MoveDot()
        {
            return new LR1Item(CoreRule.MoveDot(), Lookaheads);
        }

        /// <summary>
        /// Checks if this item has the same core (rule and dot position) as another item.
        /// </summary>
        public bool HasSameCore(LR1Item other)
        {
            return CoreRule.Equals(other.CoreRule);
        }

        /// <summary>
        /// Merges lookaheads from another LR(1) item with the same core.
        /// </summary>
        public LR1Item MergeLookaheads(LR1Item other)
        {
            if (!HasSameCore(other))
            {
                throw new ArgumentException("Cannot merge lookaheads from items with different cores", nameof(other));
            }

            var mergedLookaheads = new HashSet<ITerm>(Lookaheads);
            mergedLookaheads.UnionWith(other.Lookaheads);
            
            return new LR1Item(CoreRule, mergedLookaheads);
        }

        public bool Equals(LR1Item other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return CoreRule.Equals(other.CoreRule) && 
                   Lookaheads.SetEquals(other.Lookaheads);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LR1Item);
        }

        public override int GetHashCode()
        {
            var hash = CoreRule.GetHashCode();
            foreach (var lookahead in Lookaheads.OrderBy(l => l.ToString()))
            {
                hash = HashCode.Combine(hash, lookahead.GetHashCode());
            }
            return hash;
        }

        public override string ToString()
        {
            var lookaheadStr = string.Join(", ", Lookaheads.Select(l => l.ToString()));
            return $"{CoreRule} [{lookaheadStr}]";
        }

        public static bool operator ==(LR1Item left, LR1Item right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LR1Item left, LR1Item right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents a set of LR(1) items that form a state in the LR(1) automaton.
    /// </summary>
    public class LR1ItemSet
    {
        public int StateNumber { get; set; }
        public ISet<LR1Item> Items { get; }
        public Dictionary<ITerm, LR1ItemSet> Transitions { get; }

        public LR1ItemSet(int stateNumber, IEnumerable<LR1Item> items = null)
        {
            StateNumber = stateNumber;
            Items = new HashSet<LR1Item>(items ?? Enumerable.Empty<LR1Item>());
            Transitions = new Dictionary<ITerm, LR1ItemSet>();
        }

        /// <summary>
        /// Adds an LR(1) item to this set, merging lookaheads if an item with the same core already exists.
        /// </summary>
        public bool AddItem(LR1Item item)
        {
            var existingItem = Items.FirstOrDefault(i => i.HasSameCore(item));
            if (existingItem != null)
            {
                var mergedItem = existingItem.MergeLookaheads(item);
                if (mergedItem.Equals(existingItem))
                {
                    return false; // No change
                }
                
                Items.Remove(existingItem);
                Items.Add(mergedItem);
                return true;
            }
            
            Items.Add(item);
            return true;
        }

        /// <summary>
        /// Gets the core signature of this item set (items without lookahead information).
        /// </summary>
        public string GetCoreSignature()
        {
            var sortedCores = Items
                .Select(item => item.CoreRule)
                .OrderBy(r => r.Production.LeftNonTerminal.Name)
                .ThenBy(r => string.Join(",", r.Production.ProductionDefinition.Select(t => t.ToString())))
                .ThenBy(r => r.DotPosition)
                .ToList();

            var signatures = sortedCores.Select(rule => 
                $"{rule.Production.LeftNonTerminal.Name}→{string.Join("", rule.Production.ProductionDefinition.Select(t => t.ToString()))}•{rule.DotPosition}");
            
            return string.Join("|", signatures);
        }

        /// <summary>
        /// Checks if this item set has the same core as another (ignoring lookaheads).
        /// </summary>
        public bool HasSameCore(LR1ItemSet other)
        {
            return GetCoreSignature() == other.GetCoreSignature();
        }

        /// <summary>
        /// Converts this LR(1) item set to a traditional RuleSet for compatibility.
        /// </summary>
        public RuleSet ToRuleSet()
        {
            var rules = Items.Select(item => item.CoreRule).ToList();
            var ruleSet = new RuleSet(StateNumber, rules);
            
            // Convert transitions
            foreach (var transition in Transitions)
            {
                ruleSet.Arrows[transition.Key] = transition.Value.ToRuleSet();
            }
            
            return ruleSet;
        }

        public override string ToString()
        {
            var itemStrings = Items.Select(item => item.ToString());
            return $"State {StateNumber}:\n  {string.Join("\n  ", itemStrings)}";
        }
    }
}
