using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public interface IExEmptyTerm : IExTerm
    {
        IEmptyTerm EmptyTerm { get; }
    }
}