using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public interface IExTerm
    {
        RuleSet From { get; }
        RuleSet To { get; }
        ITerm Term { get; }
    }






}