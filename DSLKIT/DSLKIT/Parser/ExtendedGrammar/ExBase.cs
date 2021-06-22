using DSLKIT.Base;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public abstract class ExBase : IExBase
    {
        protected ExBase(RuleSet from, RuleSet to)
        {
            From = from;
            To = to;
        }

        public RuleSet From { get; }
        public RuleSet To { get; }
        public abstract ITerm Term { get; }

        public override string ToString()
        {
            return $"{From?.SetNumber}_{Term.Name}_{(To != null ? To.SetNumber.ToString() : "$")}";
        }

        public static bool operator ==(ExBase exBase1, ExBase exBase2)
        {
            return !(exBase1 is null) && exBase1.Equals(exBase2);
        }

        public static bool operator !=(ExBase exBase1, ExBase exBase2)
        {
            return !(exBase1 == exBase2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((ExBase) obj);
        }

        public bool Equals(ExBase other)
        {
            return Equals((IExBase)other);
        }

        public int GetHashCode(ExBase obj)
        {
            unchecked
            {
                var hashCode = (obj.From != null ? obj.From.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.To != null ? obj.To.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.Term != null ? obj.Term.GetHashCode() : 0);
                return hashCode;
            }
        }

        public bool Equals(IExBase other)
        {
            return other != null && Equals(From, other.From) && Equals(To, other.To) && Equals(Term, other.Term);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}