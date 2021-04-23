using DSLKIT.Base;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class Rule : IEnumerable<Rule>
    {
        public Rule(Production production, int dotPosition = 0)
        {
            Production = production;
            DotPosition = dotPosition;
        }

        public int DotPosition { get; }
        public Production Production { get; }
        public bool IsFinished => DotPosition == Production.ProductionDefinition.Count;
        public ITerm NextTerm => Production.ProductionDefinition[DotPosition];

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

        public override bool Equals(object obj)
        {
            return obj is Rule rule &&
                   DotPosition == rule.DotPosition &&
                   EqualityComparer<Production>.Default.Equals(Production, rule.Production);
        }

        public override int GetHashCode()
        {
            var hashCode = -1803403243;
            hashCode = hashCode * -1521134295 + DotPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Production>.Default.GetHashCode(Production);
            return hashCode;
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