
namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlUpdateStatisticsGrammar
    {
        public static void Build(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var updateStatisticsStatement = gb.NT("UpdateStatisticsStatement");
            var updateStatisticsOptionList = gb.NT("UpdateStatisticsOptionList");
            var updateStatisticsSimpleOption = gb.NT("UpdateStatisticsSimpleOption");
            var updateStatisticsOnOffOptionName = gb.NT("UpdateStatisticsOnOffOptionName");
            var updateStatisticsOption = gb.NT("UpdateStatisticsOption");
            var expression = gb.NT("Expression");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var qualifiedName = gb.NT("QualifiedName");
            var indexOnOffValue = gb.NT("IndexOnOffValue");

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
