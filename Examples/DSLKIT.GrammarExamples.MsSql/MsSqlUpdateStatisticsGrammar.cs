using DSLKIT.NonTerminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlUpdateStatisticsGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            INonTerminal updateStatisticsStatement,
            INonTerminal updateStatisticsOptionList,
            INonTerminal updateStatisticsSimpleOption,
            INonTerminal updateStatisticsOnOffOptionName,
            INonTerminal updateStatisticsOption)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var qualifiedName = context.Symbols.QualifiedName;
            var indexOnOffValue = context.Symbols.IndexOnOffValue;

            gb.Rule(updateStatisticsStatement)
                .CanBe("UPDATE", "STATISTICS", qualifiedName)
                .Or("UPDATE", "STATISTICS", qualifiedName, strictIdentifierTerm)
                .Or("UPDATE", "STATISTICS", qualifiedName, "WITH", updateStatisticsOptionList)
                .Or("UPDATE", "STATISTICS", qualifiedName, strictIdentifierTerm, "WITH", updateStatisticsOptionList);
            gb.Rule(updateStatisticsOptionList).SeparatedBy(",", updateStatisticsOption);
            gb.Rule(updateStatisticsSimpleOption).Keywords("FULLSCAN", "NORECOMPUTE", "RESAMPLE");
            gb.Rule(updateStatisticsOnOffOptionName).Keywords("AUTO_DROP", "INCREMENTAL", "PERSIST_SAMPLE_PERCENT");
            gb.Rule(updateStatisticsOption).OneOf(
                updateStatisticsSimpleOption,
                gb.Seq("SAMPLE", expression, "PERCENT"),
                gb.Seq("SAMPLE", expression, "ROWS"),
                gb.Seq("STATS_STREAM", "=", expression),
                gb.Seq("MAXDOP", "=", expression),
                gb.Seq(updateStatisticsOnOffOptionName, "=", indexOnOffValue),
                gb.Seq("ROWCOUNT", "=", expression),
                gb.Seq("PAGECOUNT", "=", expression));
        }
    }
}
