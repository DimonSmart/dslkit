using System;
using System.Collections;
using System.Collections.Generic;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public class Rule : IEnumerable<Rule>
    {
        public int DotPosition { get; }
        public Production Production { get; }
        public bool IsFinished => DotPosition == Production.ProductionDefinition.Count;
        public ITerm NextTerm => Production.ProductionDefinition[DotPosition];

        public Rule(Production production, int dotPosition = 0)
        {
            Production = production;
            DotPosition = dotPosition;
        }

        public IEnumerator<Rule> GetEnumerator()
        {
            return new List<Rule> { this }.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return Production.ProductionToString(DotPosition);
        }

        public Rule MoveDot()
        {
            if (IsFinished)
            {
                throw new Exception($"Could not move dot for rule: {ToString()}");
            }

            return new Rule(Production, DotPosition + 1);
        }

        public override bool Equals(object? obj)
        {
            return obj is Rule rule &&
                   DotPosition == rule.DotPosition &&
                   EqualityComparer<Production>.Default.Equals(Production, rule.Production);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DotPosition, Production);
        }

        public static bool operator ==(Rule left, Rule right)
        {
            return EqualityComparer<Rule>.Default.Equals(left, right);
        }

        public static bool operator !=(Rule left, Rule right)
        {
            return !(left == right);
        }
    }
}
