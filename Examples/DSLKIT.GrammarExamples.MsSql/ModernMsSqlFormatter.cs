using System;
using DSLKIT.GrammarExamples.MsSql.Formatting;

namespace DSLKIT.GrammarExamples.MsSql
{
    public static class ModernMsSqlFormatter
    {
        public static SqlFormattingResult TryFormat(string source, SqlFormattingOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            var parseResult = ModernMsSqlGrammarExample.ParseScript(source);
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                return SqlFormattingResult.Failure(parseResult.Error?.ToString() ?? "Parse failed.");
            }

            var formattingVisitor = new SqlFormattingVisitor(options ?? new SqlFormattingOptions());
            formattingVisitor.Visit(parseResult.ParseTree);

            return SqlFormattingResult.Success(formattingVisitor.GetFormattedSql());
        }
    }
}
