using System;
using System.IO;
using System.Linq;
using System.Text;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.Parser;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlGrammarConflictDiagnosticsTests
    {
        [Fact]
        public void BuildGrammar_ShouldProduceReproducibleConflictReport()
        {
            var grammar = ModernMsSqlGrammarExample.BuildGrammar(MsSqlDialectFeatures.All);
            var conflicts = grammar.ActionAndGotoTable.Conflicts;
            var unresolved = conflicts.Where(conflict => conflict.Resolution == null).ToArray();
            var families = conflicts
                .GroupBy(conflict => new
                {
                    conflict.Kind,
                    conflict.TerminalName,
                    conflict.Resolution,
                    conflict.ExistingProduction,
                    conflict.IncomingProduction
                })
                .OrderByDescending(group => group.Key.Resolution == null)
                .ThenByDescending(group => group.Count())
                .ThenBy(group => group.Key.Kind)
                .ThenBy(group => group.Key.TerminalName, StringComparer.Ordinal)
                .ThenBy(group => group.Key.ExistingProduction, StringComparer.Ordinal)
                .ThenBy(group => group.Key.IncomingProduction, StringComparer.Ordinal)
                .ToArray();

            var stage4Target = conflicts.Where(IsIndexStorageDefaultConflict).ToArray();
            var stage5Target = conflicts.Where(IsGrantPrincipalQualifiedNameConflict).ToArray();
            var stage6Target = conflicts.Where(IsDmlIdentifierObjectIdentifierConflict).ToArray();
            var stage7Target = conflicts.Where(IsExecuteDefaultContextualIdentifierConflict).ToArray();
            var stage8IdentifierTarget = conflicts.Where(IsNamedOptionContextualIdentifierConflict).ToArray();
            var stage8QualifiedNameTarget = conflicts.Where(IsNamedOptionQualifiedNameConflict).ToArray();
            var stage9Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "AlterIndexPartitionSelector → ALL", "ContextualIdentifierTerm → ALL")).ToArray();
            var stage10Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "DbccOptionValue → StrictIdentifierTerm", "ContextualIdentifierTerm → StrictIdentifierTerm")).ToArray();
            var stage11Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "CreateFunctionInlineTableBody → AS RETURN ( QueryExpression )",
                "QueryPrimary → ( QueryExpression )")).ToArray();
            var stage12Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "CreateViewBody → AS CreateViewQuery",
                "CreateViewCheckOptionOpt → Empty")).ToArray();
            var stage13Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "IfStatement → IF SearchCondition IfBranchStatement ELSE IfStatement",
                "StatementNoLeadingWith → IfStatement")).ToArray();
            var stage14PageTarget = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "IndexOptionValue → PAGE", "ContextualIdentifierTerm → PAGE")).ToArray();
            var stage14ColumnstoreTarget = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "IndexOptionValue → COLUMNSTORE", "ContextualIdentifierTerm → COLUMNSTORE")).ToArray();
            var stage15Target = conflicts.Where(conflict =>
                IsUnresolvedReduceReducePair(conflict,
                    "DeleteQueryHintList → DeleteQueryHint",
                    "DeleteQueryHint → DeleteQueryHint") ||
                IsUnresolvedReduceReducePair(conflict,
                    "DeleteQueryHintList → DeleteQueryHintList , DeleteQueryHint",
                    "DeleteQueryHint → DeleteQueryHint")).ToArray();
            var stage16Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "DropMoveToTarget → DEFAULT", "ContextualIdentifierTerm → DEFAULT")).ToArray();
            var stage17Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "DropFileStreamTarget → DEFAULT", "ContextualIdentifierTerm → DEFAULT")).ToArray();
            var stage18Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "IndexPartitionItem → ALL", "ContextualIdentifierTerm → ALL")).ToArray();
            var stage19Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "TypeArgument → MAX", "ContextualIdentifierTerm → MAX")).ToArray();
            var stage20Target = conflicts.Where(conflict => IsUnresolvedReduceReducePair(
                conflict, "TableHintLimited → TableHintLimitedName",
                "DmlTableHintQualifiedName → TableHintLimitedName")).ToArray();

            var report = new StringBuilder();
            report.AppendLine("# SQL grammar conflict diagnostic");
            report.AppendLine();
            report.AppendLine("Dialect features: `All`. This report is generated from the current parser table, without changing conflict resolution.");
            report.AppendLine();
            report.AppendLine($"- Productions: {grammar.Productions.Count}");
            report.AppendLine($"- LR states: {grammar.RuleSets.Count}");
            report.AppendLine($"- Conflict occurrences: {conflicts.Count}");
            report.AppendLine($"- Unresolved occurrences: {unresolved.Length}");
            report.AppendLine($"- Unresolved shift/reduce: {unresolved.Count(conflict => conflict.Kind == ParserConflictKind.ShiftReduce)}");
            report.AppendLine($"- Unresolved reduce/reduce: {unresolved.Count(conflict => conflict.Kind == ParserConflictKind.ReduceReduce)}");
            report.AppendLine($"- Explicitly resolved shift/reduce: {conflicts.Count(conflict => conflict.Kind == ParserConflictKind.ShiftReduce && conflict.Resolution != null)}");
            report.AppendLine($"- Distinct affected LR states: {conflicts.Select(conflict => conflict.StateNumber).Distinct().Count()}");
            report.AppendLine($"- Distinct conflict families (kind, lookahead, resolution, production pair): {families.Length}");
            report.AppendLine($"- Stage 4 target IndexStorageTarget / ContextualIdentifierTerm R/R occurrences: {stage4Target.Length}");
            report.AppendLine($"- Stage 5 target GrantPrincipal / StrictQualifiedName R/R occurrences: {stage5Target.Length}");
            report.AppendLine($"- Stage 6 target DmlIdentifierTerm / DmlObjectIdentifierTerm R/R occurrences: {stage6Target.Length}");
            report.AppendLine($"- Stage 7 target ExecuteArgValue / ContextualIdentifierTerm R/R occurrences: {stage7Target.Length}");
            report.AppendLine($"- Stage 8 target NamedOptionValue / QualifiedName R/R occurrences: {stage8IdentifierTarget.Length}");
            report.AppendLine($"- Stage 8 target NamedOptionValue / PrimaryExpression R/R occurrences: {stage8QualifiedNameTarget.Length}");
            report.AppendLine($"- Stage 9 target AlterIndexPartitionSelector / ContextualIdentifierTerm R/R occurrences: {stage9Target.Length}");
            report.AppendLine($"- Stage 10 target DbccOptionValue / ContextualIdentifierTerm R/R occurrences: {stage10Target.Length}");
            report.AppendLine($"- Stage 11 target CreateFunctionInlineTableBody / QueryPrimary R/R occurrences: {stage11Target.Length}");
            report.AppendLine($"- Stage 12 target CreateViewBody / CreateViewCheckOptionOpt R/R occurrences: {stage12Target.Length}");
            report.AppendLine($"- Stage 13 target IfStatement / StatementNoLeadingWith R/R occurrences: {stage13Target.Length}");
            report.AppendLine($"- Stage 14 target IndexOptionValue PAGE / ContextualIdentifierTerm PAGE R/R occurrences: {stage14PageTarget.Length}");
            report.AppendLine($"- Stage 14 target IndexOptionValue COLUMNSTORE / ContextualIdentifierTerm COLUMNSTORE R/R occurrences: {stage14ColumnstoreTarget.Length}");
            report.AppendLine($"- Stage 15 target DeleteQueryHintList / DeleteQueryHint self-reduction R/R occurrences: {stage15Target.Length}");
            report.AppendLine($"- Stage 16 target DropMoveToTarget / ContextualIdentifierTerm DEFAULT R/R occurrences: {stage16Target.Length}");
            report.AppendLine($"- Stage 17 target DropFileStreamTarget / ContextualIdentifierTerm DEFAULT R/R occurrences: {stage17Target.Length}");
            report.AppendLine($"- Stage 18 target IndexPartitionItem / ContextualIdentifierTerm ALL R/R occurrences: {stage18Target.Length}");
            report.AppendLine($"- Stage 19 target TypeArgument / ContextualIdentifierTerm MAX R/R occurrences: {stage19Target.Length}");
            report.AppendLine($"- Stage 20 target TableHintLimited / DmlTableHintQualifiedName single-name R/R occurrences: {stage20Target.Length}");
            report.AppendLine();

            report.AppendLine("## Conflict causes aggregated across lookaheads");
            report.AppendLine();
            report.AppendLine("| Kind | Existing reduction | Incoming reduction | Occurrences | LR states | Lookaheads |");
            report.AppendLine("| --- | --- | --- | ---: | ---: | ---: |");
            foreach (var group in unresolved
                .GroupBy(conflict => new
                {
                    conflict.Kind,
                    Existing = conflict.Kind == ParserConflictKind.ShiftReduce
                        ? "(shift)"
                        : ReducedNonTerminal(conflict.ExistingProduction),
                    Incoming = ReducedNonTerminal(conflict.IncomingProduction)
                })
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.Kind)
                .ThenBy(group => group.Key.Existing, StringComparer.Ordinal)
                .ThenBy(group => group.Key.Incoming, StringComparer.Ordinal))
            {
                report.AppendLine($"| {group.Key.Kind} | {Cell(group.Key.Existing)} | {Cell(group.Key.Incoming)} | {group.Count()} | {group.Select(conflict => conflict.StateNumber).Distinct().Count()} | {group.Select(conflict => conflict.TerminalName).Distinct().Count()} |");
            }
            report.AppendLine();

            report.AppendLine("## Most frequent unresolved lookaheads and reductions");
            report.AppendLine();
            report.AppendLine("| Kind | Lookahead | Existing action | Incoming action | Occurrences | States |");
            report.AppendLine("| --- | --- | --- | --- | ---: | --- |");
            foreach (var group in unresolved
                .GroupBy(conflict => new { conflict.Kind, conflict.TerminalName, conflict.ExistingAction, conflict.IncomingAction })
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.TerminalName, StringComparer.Ordinal)
                .Take(40))
            {
                var states = string.Join(", ", group.Select(conflict => conflict.StateNumber).Distinct().OrderBy(number => number).Take(8));
                report.AppendLine($"| {group.Key.Kind} | {Cell(group.Key.TerminalName)} | {Cell(group.Key.ExistingAction)} | {Cell(group.Key.IncomingAction)} | {group.Count()} | {states} |");
            }

            report.AppendLine();
            report.AppendLine("## Full conflict families");
            report.AppendLine();
            for (var index = 0; index < families.Length; index++)
            {
                var family = families[index];
                var example = family.OrderBy(conflict => conflict.StateNumber).First();
                report.AppendLine($"### {index + 1}. {family.Key.Kind} on `{family.Key.TerminalName}` ({family.Count()} occurrences, {family.Key.Resolution?.ToString() ?? "unresolved"})");
                report.AppendLine();
                report.AppendLine($"- Existing action: `{example.ExistingAction}`");
                report.AppendLine($"- Incoming action: `{example.IncomingAction}`");
                report.AppendLine($"- Existing production: `{family.Key.ExistingProduction ?? "(shift / not a reduction)"}`");
                report.AppendLine($"- Incoming production: `{family.Key.IncomingProduction ?? "(unknown)"}`");
                report.AppendLine($"- States: {string.Join(", ", family.Select(conflict => conflict.StateNumber).Distinct().OrderBy(number => number))}");
                report.AppendLine();
                var state = grammar.RuleSets.FirstOrDefault(ruleSet => ruleSet.SetNumber == example.StateNumber);
                if (state != null && example.Kind == ParserConflictKind.ShiftReduce)
                {
                    var shiftItems = state.Rules
                        .Where(rule => !rule.IsFinished && string.Equals(rule.NextTerm.Name, example.TerminalName, StringComparison.Ordinal))
                        .OrderBy(rule => rule.ToString(), StringComparer.Ordinal)
                        .Take(8)
                        .ToArray();
                    if (shiftItems.Length > 0)
                    {
                        report.AppendLine("Items that can shift the lookahead in the sample state:");
                        foreach (var item in shiftItems)
                        {
                            report.AppendLine($"- `{item}`");
                        }
                        report.AppendLine();
                    }
                }
            }

            var outputPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                var fullPath = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, report.ToString());
            }

            report.ToString().Should().Contain("# SQL grammar conflict diagnostic");

            stage4Target.Should().BeEmpty(
                "the redundant IndexStorageTarget → DEFAULT production was removed");
            stage5Target.Should().BeEmpty(
                "the redundant GrantPrincipal → StrictIdentifierTerm production was removed");
            stage6Target.Should().BeEmpty(
                "DELETE target identifiers must not have overlapping StrictIdentifierTerm reductions");
            stage7Target.Should().BeEmpty(
                "EXEC DEFAULT arguments are already accepted through the Expression path");
            stage8IdentifierTarget.Should().BeEmpty(
                "named option identifiers are already accepted through Expression and QualifiedName");
            stage8QualifiedNameTarget.Should().BeEmpty(
                "named option qualified names are already accepted through Expression and PrimaryExpression");
            stage9Target.Should().BeEmpty(
                "ALTER INDEX PARTITION = ALL is already accepted through the Expression path");
            stage10Target.Should().BeEmpty(
                "DBCC options already accept identifiers through the Expression path");
            stage11Target.Should().BeEmpty(
                "inline table-valued functions already accept parenthesized queries through QueryPrimary");
            stage12Target.Should().BeEmpty(
                "CREATE VIEW already accepts the form without WITH CHECK OPTION through optional CreateViewCheckOptionOpt");
            stage13Target.Should().BeEmpty(
                "ELSE IF is already handled through IfBranchStatement and StatementNoLeadingWith");
            stage14PageTarget.Should().BeEmpty(
                "index option PAGE is already accepted through Expression and ContextualIdentifierTerm");
            stage14ColumnstoreTarget.Should().BeEmpty(
                "index option COLUMNSTORE is already accepted through Expression and ContextualIdentifierTerm");
            stage15Target.Should().BeEmpty(
                "DELETE query hints must not compete with an unproductive DeleteQueryHint self-reduction");
            stage16Target.Should().BeEmpty(
                "DROP INDEX MOVE TO DEFAULT is already recognized through QualifiedName and ContextualIdentifierTerm");
            stage17Target.Should().BeEmpty(
                "DROP INDEX FILESTREAM_ON DEFAULT is already recognized through QualifiedName and ContextualIdentifierTerm");
            stage18Target.Should().BeEmpty(
                "index-option ON PARTITIONS ALL is already recognized through Expression and ContextualIdentifierTerm");
            stage19Target.Should().BeEmpty(
                "MAX type arguments already use the Expression and ContextualIdentifierTerm path");
            stage20Target.Should().BeEmpty(
                "single-name DML table hints are already recognized through DmlTableHintQualifiedName");
            conflicts.Where(conflict => conflict.Resolution != null)
                .Should().ContainSingle(conflict =>
                    conflict.Kind == ParserConflictKind.ShiftReduce &&
                    conflict.TerminalName == "ELSE" &&
                    conflict.Resolution == DSLKIT.Terminals.Resolve.Shift,
                    "the sole explicit resolution must remain the dangling ELSE shift");
        }

        [Fact]
        public void BuildGrammar_ShouldNotHaveSqlcmdPrimaryExpressionIdentifierReduceReduceConflict()
        {
            var grammar = ModernMsSqlGrammarExample.BuildGrammar(MsSqlDialectFeatures.All);
            var conflicts = grammar.ActionAndGotoTable.Conflicts;

            var targetConflicts = conflicts.Where(conflict =>
                conflict.Kind == ParserConflictKind.ReduceReduce &&
                conflict.Resolution == null &&
                (IsSqlcmdReductionPair(conflict.ExistingProduction, conflict.IncomingProduction) ||
                 IsSqlcmdReductionPair(conflict.IncomingProduction, conflict.ExistingProduction)));

            targetConflicts.Should().BeEmpty(
                "SQLCMD placeholders should reach PrimaryExpression through QualifiedName only");

        }

        [Fact]
        public void BuildGrammar_ShouldNotHaveRedundantParenthesizedColumnDefaultConflicts()
        {
            var grammar = ModernMsSqlGrammarExample.BuildGrammar(MsSqlDialectFeatures.All);
            var conflicts = grammar.ActionAndGotoTable.Conflicts;

            conflicts.Where(conflict =>
                conflict.Kind == ParserConflictKind.ReduceReduce &&
                conflict.Resolution == null &&
                (IsColumnDefaultParenthesisReductionPair(conflict.ExistingProduction, conflict.IncomingProduction) ||
                 IsColumnDefaultParenthesisReductionPair(conflict.IncomingProduction, conflict.ExistingProduction)))
                .Should().BeEmpty(
                    "column DEFAULT parentheses are already handled by PrimaryExpression");

        }

        private static bool IsNamedOptionContextualIdentifierConflict(ParserConflict conflict) =>
            IsUnresolvedReduceReducePair(conflict,
                "NamedOptionValue → ContextualIdentifierTerm",
                "QualifiedName → ContextualIdentifierTerm");

        private static bool IsNamedOptionQualifiedNameConflict(ParserConflict conflict) =>
            IsUnresolvedReduceReducePair(conflict,
                "NamedOptionValue → QualifiedName",
                "PrimaryExpression → QualifiedName");

        private static bool IsUnresolvedReduceReducePair(ParserConflict conflict, string first, string second) =>
            conflict.Kind == ParserConflictKind.ReduceReduce &&
            conflict.Resolution == null &&
            ((conflict.ExistingProduction?.Trim() == first && conflict.IncomingProduction?.Trim() == second) ||
             (conflict.ExistingProduction?.Trim() == second && conflict.IncomingProduction?.Trim() == first));

        private static bool IsExecuteDefaultContextualIdentifierConflict(ParserConflict conflict) =>
            conflict.Kind == ParserConflictKind.ReduceReduce &&
            conflict.Resolution == null &&
            ((conflict.ExistingProduction?.Trim() == "ExecuteArgValue → DEFAULT" &&
              conflict.IncomingProduction?.Trim() == "ContextualIdentifierTerm → DEFAULT") ||
             (conflict.IncomingProduction?.Trim() == "ExecuteArgValue → DEFAULT" &&
              conflict.ExistingProduction?.Trim() == "ContextualIdentifierTerm → DEFAULT"));

        private static bool IsDmlIdentifierObjectIdentifierConflict(ParserConflict conflict) =>
            conflict.Kind == ParserConflictKind.ReduceReduce &&
            conflict.Resolution == null &&
            ((conflict.ExistingProduction?.Trim() == "DmlIdentifierTerm → StrictIdentifierTerm" &&
              conflict.IncomingProduction?.Trim() == "DmlObjectIdentifierTerm → StrictIdentifierTerm") ||
             (conflict.IncomingProduction?.Trim() == "DmlIdentifierTerm → StrictIdentifierTerm" &&
              conflict.ExistingProduction?.Trim() == "DmlObjectIdentifierTerm → StrictIdentifierTerm"));

        private static bool IsGrantPrincipalQualifiedNameConflict(ParserConflict conflict) =>
            conflict.Kind == ParserConflictKind.ReduceReduce &&
            conflict.Resolution == null &&
            ((conflict.ExistingProduction?.Trim() == "GrantPrincipal → StrictIdentifierTerm" &&
              conflict.IncomingProduction?.Trim() == "StrictQualifiedName → StrictIdentifierTerm") ||
             (conflict.IncomingProduction?.Trim() == "GrantPrincipal → StrictIdentifierTerm" &&
              conflict.ExistingProduction?.Trim() == "StrictQualifiedName → StrictIdentifierTerm"));

        private static bool IsIndexStorageDefaultConflict(ParserConflict conflict) =>
            conflict.Kind == ParserConflictKind.ReduceReduce &&
            conflict.Resolution == null &&
            conflict.ExistingProduction?.Trim() == "IndexStorageTarget → DEFAULT" &&
            conflict.IncomingProduction?.Trim() == "ContextualIdentifierTerm → DEFAULT";

        private static bool IsColumnDefaultParenthesisReductionPair(string columnDefault, string primary) =>
            (columnDefault?.Trim() == "CreateTableColumnOption → DEFAULT ( Expression )" ||
             columnDefault?.Trim() == "CreateTableColumnConstraintBody → DEFAULT ( Expression )") &&
            primary?.Trim() == "PrimaryExpression → ( Expression )";

        private static bool IsSqlcmdReductionPair(string primary, string identifier) =>
            primary?.Trim() == "PrimaryExpression → SqlcmdVariable" &&
            identifier?.Trim() == "StrictIdentifierTerm → SqlcmdVariable";

        private static string ReducedNonTerminal(string? production)
        {
            if (string.IsNullOrEmpty(production))
            {
                return "(unknown)";
            }

            var arrowIndex = production.IndexOf(" →", StringComparison.Ordinal);
            return arrowIndex < 0 ? production : production[..arrowIndex];
        }

        private static string Cell(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
