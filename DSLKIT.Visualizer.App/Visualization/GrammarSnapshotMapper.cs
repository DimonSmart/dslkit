using DSLKIT.Base;
using DSLKIT.Parser;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Visualizer.App.Visualization;

public sealed class GrammarSnapshotMapper : IGrammarSnapshotMapper
{
    public GrammarSnapshotDto Map(IGrammar grammar)
    {
        return new GrammarSnapshotDto
        {
            GrammarName = grammar.Name,
            RootName = grammar.Root.Name,
            TerminalCount = grammar.Terminals.Count,
            NonTerminalCount = grammar.NonTerminals.Count,
            RuleSetCount = grammar.RuleSets.Count,
            Productions = MapProductions(grammar),
            TranslationTable = MapTranslationTable(grammar.TranslationTable),
            ActionAndGotoTable = MapActionAndGotoTable(grammar.ActionAndGotoTable),
            FirstsTable = MapNamedSetTable("Firsts", grammar.Firsts),
            FollowsTable = MapNamedSetTable("Follows", grammar.Follows)
        };
    }

    private static IReadOnlyList<ProductionRowDto> MapProductions(IGrammar grammar)
    {
        return grammar.Productions
            .Select((production, index) => new ProductionRowDto
            {
                Number = index,
                Left = production.LeftNonTerminal.Name,
                Right = string.Join(" ", production.ProductionDefinition.Select(term => term.Name))
            })
            .ToList();
    }

    private static TableDto MapTranslationTable(TranslationTable translationTable)
    {
        var columns = translationTable.GetAllTerms()
            .Distinct()
            .OrderBy(term => term.Name, StringComparer.Ordinal)
            .ToList();

        var rows = translationTable.GetAllSets()
            .OrderBy(set => set.SetNumber)
            .Select(set =>
            {
                var row = new List<string> { set.SetNumber.ToString() };
                foreach (var column in columns)
                {
                    row.Add(
                        translationTable.TryGetValue(column, set, out var destinationSet)
                            ? destinationSet.SetNumber.ToString()
                            : string.Empty);
                }

                return (IReadOnlyList<string>)row;
            })
            .ToList();

        var mappedColumns = new[] { "State" }
            .Concat(columns.Select(column => column.Name))
            .ToList();

        return new TableDto
        {
            Name = "Translation Table",
            Columns = mappedColumns,
            Rows = rows
        };
    }

    private static TableDto MapActionAndGotoTable(ActionAndGotoTable table)
    {
        var actionColumns = table.GetActionColumns()
            .Distinct()
            .OrderBy(term => term.Name, StringComparer.Ordinal)
            .ToList();

        var gotoColumns = table.GetGotoColumns()
            .Distinct()
            .OrderBy(nonTerminal => nonTerminal.Name, StringComparer.Ordinal)
            .ToList();

        var rows = table.GetAllSets()
            .OrderBy(set => set.SetNumber)
            .Select(set =>
            {
                var row = new List<string> { set.SetNumber.ToString() };

                foreach (var actionColumn in actionColumns)
                {
                    row.Add(
                        table.TryGetActionValue(actionColumn, set, out var action)
                            ? action.ToString() ?? string.Empty
                            : string.Empty);
                }

                foreach (var gotoColumn in gotoColumns)
                {
                    row.Add(
                        table.TryGetGotoValue(gotoColumn, set, out var gotoSet)
                            ? gotoSet.SetNumber.ToString()
                            : string.Empty);
                }

                return (IReadOnlyList<string>)row;
            })
            .ToList();

        var mappedColumns = new[] { "State" }
            .Concat(actionColumns.Select(column => column.Name))
            .Concat(gotoColumns.Select(column => column.Name))
            .ToList();

        return new TableDto
        {
            Name = "Action/Goto Table",
            Columns = mappedColumns,
            Rows = rows
        };
    }

    private static TableDto MapNamedSetTable(
        string name,
        IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> sets)
    {
        var rows = sets
            .OrderBy(item => item.Key.NonTerminal.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Key.From.SetNumber)
            .Select(item =>
            {
                var values = string.Join(
                    ", ",
                    item.Value
                        .Select(term => term.Name)
                        .Distinct()
                        .OrderBy(termName => termName, StringComparer.Ordinal));

                return (IReadOnlyList<string>)new[]
                {
                    item.Key.ToString() ?? string.Empty,
                    item.Key.NonTerminal.Name,
                    values
                };
            })
            .ToList();

        return new TableDto
        {
            Name = name,
            Columns = ["ExNonTerminal", "NonTerminal", "Terms"],
            Rows = rows
        };
    }
}
