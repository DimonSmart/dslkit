using DSLKIT.GrammarExamples.MsSql.Formatting;

namespace DSLKIT.Visualizer.App.Components.SqlFormatting;

internal sealed class SqlFormattingState
{
    public string SourceSql { get; set; } = SqlFormattingExamples.DemoSql;

    public string FormattedSql { get; set; } = string.Empty;

    public string? FormattingError { get; set; }

    public string OptionSearch { get; set; } = string.Empty;

    public bool AutoFormat { get; set; } = true;

    public bool AutoFormatOptions { get; set; } = true;

    public SqlKeywordCase KeywordCase { get; set; } = SqlKeywordCase.Upper;

    public SqlParenthesesSpacing InsideParentheses { get; set; } = SqlParenthesesSpacing.Never;

    public SqlStatementTerminationMode TerminateWithSemicolon { get; set; } = SqlStatementTerminationMode.ExistingOnly;

    public int BlankLinesBetweenStatements { get; set; }

    public bool BlankLineBetweenClauses { get; set; }

    public SqlCommaStyle CommaStyle { get; set; } = SqlCommaStyle.Trailing;

    public SqlListLayoutStyle SelectItemsStyle { get; set; } = SqlListLayoutStyle.OnePerLine;

    public SqlListLayoutStyle GroupByItemsStyle { get; set; } = SqlListLayoutStyle.OnePerLine;

    public SqlListLayoutStyle OrderByItemsStyle { get; set; } = SqlListLayoutStyle.OnePerLine;

    public SqlInListItemsStyle InListItemsStyle { get; set; } = SqlInListItemsStyle.Inline;

    public bool SpacesAfterComma { get; set; } = true;

    public bool SpacesAroundBinaryOperators { get; set; } = true;

    public bool SpacesBeforeSemicolon { get; set; }

    public bool EofNewline { get; set; }

    public bool AlignSelectAliases { get; set; }

    public bool JoinsNewlinePerJoin { get; set; } = true;

    public bool JoinsOnNewLine { get; set; } = true;

    public bool JoinsMultilineOnBreakOnAnd { get; set; } = true;

    public bool JoinsMultilineOnBreakOnOr { get; set; }

    public int JoinsMultilineOnMaxTokensSingleLine { get; set; }

    public bool PredicatesMultilineWhere { get; set; }

    public SqlLogicalOperatorLineBreakMode PredicatesLogicalOperatorLineBreak { get; set; } = SqlLogicalOperatorLineBreakMode.BeforeOperator;

    public int PredicatesInlineSimpleMaxConditions { get; set; }

    public int PredicatesInlineSimpleMaxLineLength { get; set; } = 120;

    public bool PredicatesInlineSimpleAllowOnlyAnd { get; set; } = true;

    public bool PredicatesMixedAndOrParenthesizeOrGroups { get; set; }

    public bool PredicatesMixedAndOrBreakOrGroups { get; set; }

    public SqlCaseStyle ExpressionsCaseStyle { get; set; } = SqlCaseStyle.Multiline;

    public int CompactCaseMaxWhenClauses { get; set; }

    public int CompactCaseMaxTokens { get; set; }

    public int CompactCaseMaxLineLength { get; set; } = 120;

    public int InlineShortExpressionMaxTokens { get; set; }

    public int InlineShortExpressionMaxDepth { get; set; }

    public int InlineShortExpressionMaxLineLength { get; set; } = 120;

    public bool InlineShortContextSelectItem { get; set; }

    public bool InlineShortContextOn { get; set; }

    public bool InlineShortContextWhere { get; set; }

    public bool ShortQueriesEnabled { get; set; }

    public int ShortQueriesMaxLineLength { get; set; } = 100;

    public int ShortQueriesMaxSelectItems { get; set; } = 2;

    public int ShortQueriesMaxPredicateConditions { get; set; } = 2;

    public bool ShortQueriesApplyToParenthesizedSubqueries { get; set; }

    public bool ShortQueriesAllowSingleJoin { get; set; }

    public SqlDmlListStyle UpdateSetStyle { get; set; } = SqlDmlListStyle.OnePerLine;

    public SqlDmlListStyle InsertColumnsStyle { get; set; } = SqlDmlListStyle.OnePerLine;

    public bool InsertColumnsStartOnNewLine { get; set; }

    public SqlDmlListStyle InsertValuesStyle { get; set; } = SqlDmlListStyle.OnePerLine;

    public SqlCreateProcLayout CreateProcLayout { get; set; } = SqlCreateProcLayout.Expanded;

    public bool CommentsPreserveAttachment { get; set; } = true;

    public SqlCommentsFormattingMode CommentsFormatting { get; set; } = SqlCommentsFormattingMode.Keep;

    public bool NewlineBeforeWith { get; set; } = true;

    public bool NewlineBeforeSelect { get; set; } = true;

    public bool NewlineBeforeFrom { get; set; } = true;

    public bool NewlineBeforeWhere { get; set; } = true;

    public bool NewlineBeforeGroupBy { get; set; } = true;

    public bool NewlineBeforeHaving { get; set; } = true;

    public bool NewlineBeforeOrderBy { get; set; } = true;

    public bool NewlineBeforeOption { get; set; } = true;

    public int IndentSize { get; set; } = 4;

    public int WrapColumn { get; set; } = 120;

    public bool IndentCteBody { get; set; }

    public int SelectCompactMaxItems { get; set; }

    public int SelectCompactMaxLineLength { get; set; } = 120;

    public int InlineInListMaxItems { get; set; }

    public int InlineInListMaxLineLength { get; set; } = 120;

    public bool ResizeEditorsPending { get; set; } = true;

    public bool IsFormatterInitializing { get; set; }

    public bool IsFormatterInitialized { get; set; }

    public bool SpacesInsideParentheses
    {
        get => InsideParentheses == SqlParenthesesSpacing.Always;
        set => InsideParentheses = value
            ? SqlParenthesesSpacing.Always
            : SqlParenthesesSpacing.Never;
    }

    public bool PredicatesLogicalOperatorsAtLineStart
    {
        get => PredicatesLogicalOperatorLineBreak == SqlLogicalOperatorLineBreakMode.BeforeOperator;
        set => PredicatesLogicalOperatorLineBreak = value
            ? SqlLogicalOperatorLineBreakMode.BeforeOperator
            : SqlLogicalOperatorLineBreakMode.AfterOperator;
    }

    public bool PredicatesInlineSimpleEnabled
    {
        get => PredicatesInlineSimpleMaxConditions > 0;
        set
        {
            if (value)
            {
                PredicatesInlineSimpleMaxConditions = Math.Max(2, PredicatesInlineSimpleMaxConditions);
                return;
            }

            PredicatesInlineSimpleMaxConditions = 0;
        }
    }

    public bool AreJoinBreakOptionsEnabled => JoinsMultilineOnMaxTokensSingleLine > 0;

    public bool ArePredicateMultilineLayoutSettingsEnabled => PredicatesMultilineWhere;

    public bool ArePredicateInlineSettingsEnabled => PredicatesMultilineWhere;

    public bool ArePredicateInlineSecondarySettingsEnabled =>
        PredicatesMultilineWhere && PredicatesInlineSimpleMaxConditions > 0;

    public bool AreCompactCaseThresholdsEnabled => ExpressionsCaseStyle == SqlCaseStyle.CompactWhenShort;

    public bool AreInlineShortSecondarySettingsEnabled => InlineShortExpressionMaxTokens > 0;

    public bool AreInlineShortContextsSelected =>
        InlineShortContextSelectItem ||
        InlineShortContextOn ||
        InlineShortContextWhere;

    public IReadOnlyCollection<SqlInlineExpressionContext> BuildInlineShortExpressionContexts()
    {
        List<SqlInlineExpressionContext> contexts = [];

        if (InlineShortContextSelectItem)
        {
            contexts.Add(SqlInlineExpressionContext.SelectItem);
        }

        if (InlineShortContextOn)
        {
            contexts.Add(SqlInlineExpressionContext.On);
        }

        if (InlineShortContextWhere)
        {
            contexts.Add(SqlInlineExpressionContext.Where);
        }

        return contexts;
    }

    public void ResetOptions()
    {
        KeywordCase = SqlKeywordCase.Upper;
        InsideParentheses = SqlParenthesesSpacing.Never;
        TerminateWithSemicolon = SqlStatementTerminationMode.ExistingOnly;
        BlankLinesBetweenStatements = 0;
        BlankLineBetweenClauses = false;
        CommaStyle = SqlCommaStyle.Trailing;
        SelectItemsStyle = SqlListLayoutStyle.OnePerLine;
        GroupByItemsStyle = SqlListLayoutStyle.OnePerLine;
        OrderByItemsStyle = SqlListLayoutStyle.OnePerLine;
        InListItemsStyle = SqlInListItemsStyle.Inline;
        SpacesAfterComma = true;
        SpacesAroundBinaryOperators = true;
        SpacesBeforeSemicolon = false;
        EofNewline = false;
        AlignSelectAliases = false;
        JoinsNewlinePerJoin = true;
        JoinsOnNewLine = true;
        JoinsMultilineOnBreakOnAnd = true;
        JoinsMultilineOnBreakOnOr = false;
        JoinsMultilineOnMaxTokensSingleLine = 0;
        PredicatesMultilineWhere = false;
        PredicatesLogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.BeforeOperator;
        PredicatesInlineSimpleMaxConditions = 0;
        PredicatesInlineSimpleMaxLineLength = 120;
        PredicatesInlineSimpleAllowOnlyAnd = true;
        PredicatesMixedAndOrParenthesizeOrGroups = false;
        PredicatesMixedAndOrBreakOrGroups = false;
        ExpressionsCaseStyle = SqlCaseStyle.Multiline;
        CompactCaseMaxWhenClauses = 0;
        CompactCaseMaxTokens = 0;
        CompactCaseMaxLineLength = 120;
        InlineShortExpressionMaxTokens = 0;
        InlineShortExpressionMaxDepth = 0;
        InlineShortExpressionMaxLineLength = 120;
        InlineShortContextSelectItem = false;
        InlineShortContextOn = false;
        InlineShortContextWhere = false;
        ShortQueriesEnabled = false;
        ShortQueriesMaxLineLength = 100;
        ShortQueriesMaxSelectItems = 2;
        ShortQueriesMaxPredicateConditions = 2;
        ShortQueriesApplyToParenthesizedSubqueries = false;
        ShortQueriesAllowSingleJoin = false;
        UpdateSetStyle = SqlDmlListStyle.OnePerLine;
        InsertColumnsStyle = SqlDmlListStyle.OnePerLine;
        InsertColumnsStartOnNewLine = false;
        InsertValuesStyle = SqlDmlListStyle.OnePerLine;
        CreateProcLayout = SqlCreateProcLayout.Expanded;
        CommentsPreserveAttachment = true;
        CommentsFormatting = SqlCommentsFormattingMode.Keep;
        NewlineBeforeWith = true;
        NewlineBeforeSelect = true;
        NewlineBeforeFrom = true;
        NewlineBeforeWhere = true;
        NewlineBeforeGroupBy = true;
        NewlineBeforeHaving = true;
        NewlineBeforeOrderBy = true;
        NewlineBeforeOption = true;
        IndentSize = 4;
        WrapColumn = 120;
        IndentCteBody = false;
        SelectCompactMaxItems = 0;
        SelectCompactMaxLineLength = 120;
        InlineInListMaxItems = 0;
        InlineInListMaxLineLength = 120;
    }
}
