using DSLKIT.Terminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal sealed class MsSqlGrammarContext(
        GrammarBuilder gb,
        MsSqlDialectFeatures dialectFeatures,
        MsSqlGrammarSymbols symbols,
        ITerminal identifierTerminal,
        ITerminal bracketIdentifierTerminal,
        ITerminal quotedIdentifierTerminal,
        ITerminal tempIdentifierTerminal,
        ITerminal variableTerminal,
        ITerminal sqlcmdVariableTerminal,
        ITerminal numberTerminal,
        ITerminal stringLiteralTerminal,
        ITerminal forSystemTimeStartTerminal,
        ITerminal forPathStartTerminal,
        ITerminal graphColumnRefTerminal)
    {
        public GrammarBuilder Gb { get; } = gb;
        public MsSqlDialectFeatures DialectFeatures { get; } = dialectFeatures;
        public MsSqlGrammarSymbols Symbols { get; } = symbols;
        public ITerminal IdentifierTerminal { get; } = identifierTerminal;
        public ITerminal BracketIdentifierTerminal { get; } = bracketIdentifierTerminal;
        public ITerminal QuotedIdentifierTerminal { get; } = quotedIdentifierTerminal;
        public ITerminal TempIdentifierTerminal { get; } = tempIdentifierTerminal;
        public ITerminal VariableTerminal { get; } = variableTerminal;
        public ITerminal SqlcmdVariableTerminal { get; } = sqlcmdVariableTerminal;
        public ITerminal NumberTerminal { get; } = numberTerminal;
        public ITerminal StringLiteralTerminal { get; } = stringLiteralTerminal;
        public ITerminal ForSystemTimeStartTerminal { get; } = forSystemTimeStartTerminal;
        public ITerminal ForPathStartTerminal { get; } = forPathStartTerminal;
        public ITerminal GraphColumnRefTerminal { get; } = graphColumnRefTerminal;

        public bool HasFeature(MsSqlDialectFeatures feature)
        {
            return (DialectFeatures & feature) == feature;
        }
    }
}
