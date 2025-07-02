using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSLKIT.Base;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    public class Production : IEquatable<Production>
    {
        public readonly INonTerminal LeftNonTerminal;
        public readonly IList<ITerm> ProductionDefinition;

        public Production(INonTerminal leftNonTerminal, IList<ITerm> productionDefinition)
        {
            LeftNonTerminal = leftNonTerminal;
            ProductionDefinition = productionDefinition;
        }

        public string ProductionToString(int dotPosition = -1, Func<int, string, string> formatter = null)
        {
            const string dot = "● ";
            var sb = new StringBuilder();
            sb.Append(formatter == null ? LeftNonTerminal.Name : formatter(-1, LeftNonTerminal.Name));
            sb.Append(" →");

            if (ProductionDefinition.Count == 0)
            {
                sb.Append(" ε");
            }
            else
            {
                for (var i = 0; i <= ProductionDefinition.Count; i++)
                {
                    sb.Append(" ");
                    if (i == dotPosition)
                    {
                        sb.Append(dot);
                    }

                    if (i < ProductionDefinition.Count)
                    {
                        var label = ProductionDefinition[i].Name;
                        label = formatter == null ? label : formatter(i, label);
                        sb.Append(label);
                    }
                }
            }

            return sb.ToString();
        }

        #region Equality Implementation

        public bool Equals(Production other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return LeftNonTerminal.Name == other.LeftNonTerminal.Name &&
                   ProductionDefinition.Select(t => t.Name)
                       .SequenceEqual(other.ProductionDefinition.Select(t => t.Name));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Production);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(LeftNonTerminal.Name);

            foreach (var term in ProductionDefinition)
            {
                hash.Add(term.Name);
            }

            return hash.ToHashCode();
        }

        public static bool operator ==(Production left, Production right)
        {
            return EqualityComparer<Production>.Default.Equals(left, right);
        }

        public static bool operator !=(Production left, Production right)
        {
            return !(left == right);
        }

        #endregion
    }
}