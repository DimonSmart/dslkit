using DSLKIT.Base;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public abstract class ExBase : IExBase
    {
        protected ExBase(RuleSet from, RuleSet? to)
        {
            From = from;
            To = to;
        }

        public RuleSet From { get; }
        public RuleSet? To { get; }
        public abstract ITerm Term { get; }

        #region Equality

        public bool Equals(ExBase? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(From, other.From) && Equals(To, other.To) && Equals(Term, other.Term);
        }

        public override bool Equals(object? obj)
        {
            return obj is ExBase other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = From != null ? From.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (To != null ? To.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Term != null ? Term.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{From?.SetNumber}_{Term.Name}_{(To != null ? To.SetNumber.ToString() : "$")}";
        }

        #endregion
    }
}
