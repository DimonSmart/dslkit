using DSLKIT.GrammarExamples.MsSql.Formatting;

namespace DSLKIT.Visualizer.App.Components.SqlFormatting;

internal static class SqlFormattingOptionsFactory
{
    public static SqlFormattingOptions Create(SqlFormattingState state)
    {
        return new SqlFormattingOptions
        {
            Dialect = state.Dialect,
            KeywordCase = state.KeywordCase,
            Spaces = new SqlSpacesFormattingOptions
            {
                AfterComma = state.SpacesAfterComma,
                AroundBinaryOperators = state.SpacesAroundBinaryOperators,
                InsideParentheses = state.InsideParentheses,
                BeforeSemicolon = state.SpacesBeforeSemicolon
            },
            Statement = new SqlStatementFormattingOptions
            {
                TerminateWithSemicolon = state.TerminateWithSemicolon,
                BlankLinesBetweenStatements = Math.Max(0, state.BlankLinesBetweenStatements)
            },
            Eof = new SqlEndOfFileFormattingOptions
            {
                Newline = state.EofNewline
            },
            Layout = new SqlLayoutFormattingOptions
            {
                IndentSize = Math.Max(1, state.IndentSize),
                WrapColumn = Math.Max(10, state.WrapColumn),
                IndentCteBody = state.IndentCteBody,
                NewlineBeforeClause = new SqlClauseNewlineOptions
                {
                    With = state.NewlineBeforeWith,
                    Select = state.NewlineBeforeSelect,
                    From = state.NewlineBeforeFrom,
                    Where = state.NewlineBeforeWhere,
                    GroupBy = state.NewlineBeforeGroupBy,
                    Having = state.NewlineBeforeHaving,
                    Qualify = state.NewlineBeforeQualify,
                    OrderBy = state.NewlineBeforeOrderBy,
                    Option = state.NewlineBeforeOption
                },
                BlankLineBetweenClauses = state.BlankLineBetweenClauses
                    ? SqlBlankLineBetweenClausesMode.BetweenMajorClauses
                    : SqlBlankLineBetweenClausesMode.None
            },
            Lists = new SqlListsFormattingOptions
            {
                CommaStyle = state.CommaStyle,
                SelectItems = state.SelectItemsStyle,
                GroupByItems = state.GroupByItemsStyle,
                OrderByItems = state.OrderByItemsStyle,
                InListItems = state.InListItemsStyle,
                SelectCompactThreshold = new SqlSelectCompactThresholdOptions
                {
                    MaxItems = Math.Max(0, state.SelectCompactMaxItems),
                    MaxLineLength = Math.Max(10, state.SelectCompactMaxLineLength)
                },
                InlineInListThreshold = new SqlInlineInListThresholdOptions
                {
                    MaxItemsInline = Math.Max(0, state.InlineInListMaxItems),
                    MaxLineLength = Math.Max(10, state.InlineInListMaxLineLength)
                }
            },
            Align = new SqlAlignFormattingOptions
            {
                SelectAliases = state.AlignSelectAliases
            },
            Joins = new SqlJoinsFormattingOptions
            {
                NewlinePerJoin = state.JoinsNewlinePerJoin,
                OnNewLine = state.JoinsOnNewLine,
                MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                {
                    MaxConditionsSingleLine = Math.Max(0, state.JoinsMultilineOnMaxConditionsSingleLine),
                    BreakOnAnd = state.JoinsMultilineOnBreakOnAnd,
                    BreakOnOr = state.JoinsMultilineOnBreakOnOr
                }
            },
            Predicates = new SqlPredicatesFormattingOptions
            {
                MultilineWhere = state.PredicatesMultilineWhere,
                LogicalOperatorLineBreak = state.PredicatesLogicalOperatorLineBreak,
                InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                {
                    MaxConditions = Math.Max(0, state.PredicatesInlineSimpleMaxConditions),
                    MaxLineLength = Math.Max(10, state.PredicatesInlineSimpleMaxLineLength),
                    AllowOnlyAnd = state.PredicatesInlineSimpleAllowOnlyAnd
                },
                MixedAndOrParentheses = new SqlMixedAndOrParenthesesOptions
                {
                    ParenthesizeOrGroups = state.PredicatesMixedAndOrParenthesizeOrGroups,
                    BreakOrGroups = state.PredicatesMixedAndOrBreakOrGroups
                }
            },
            Expressions = new SqlExpressionsFormattingOptions
            {
                CaseStyle = state.ExpressionsCaseStyle,
                CompactCaseThreshold = new SqlCompactCaseThresholdOptions
                {
                    MaxWhenClauses = Math.Max(0, state.CompactCaseMaxWhenClauses),
                    MaxTokens = Math.Max(0, state.CompactCaseMaxTokens),
                    MaxLineLength = Math.Max(10, state.CompactCaseMaxLineLength)
                },
                InlineShortExpression = new SqlInlineShortExpressionOptions
                {
                    MaxTokens = Math.Max(0, state.InlineShortExpressionMaxTokens),
                    MaxDepth = Math.Max(0, state.InlineShortExpressionMaxDepth),
                    MaxLineLength = Math.Max(10, state.InlineShortExpressionMaxLineLength),
                    ForContexts = state.BuildInlineShortExpressionContexts()
                }
            },
            ShortQueries = new SqlShortQueriesFormattingOptions
            {
                Enabled = state.ShortQueriesEnabled,
                MaxLineLength = Math.Max(10, state.ShortQueriesMaxLineLength),
                MaxSelectItems = Math.Max(1, state.ShortQueriesMaxSelectItems),
                MaxPredicateConditions = Math.Max(1, state.ShortQueriesMaxPredicateConditions),
                ApplyToParenthesizedSubqueries = state.ShortQueriesApplyToParenthesizedSubqueries,
                AllowSingleJoin = state.ShortQueriesAllowSingleJoin
            },
            Dml = new SqlDmlFormattingOptions
            {
                UpdateSetStyle = state.UpdateSetStyle,
                InsertColumnsStyle = state.InsertColumnsStyle,
                InsertColumnsStartOnNewLine = state.InsertColumnsStartOnNewLine,
                InsertValuesStyle = state.InsertValuesStyle
            },
            Ddl = new SqlDdlFormattingOptions
            {
                CreateProcLayout = state.CreateProcLayout
            },
            Comments = new SqlCommentsFormattingOptions
            {
                PreserveAttachment = state.CommentsPreserveAttachment,
                Formatting = state.CommentsFormatting
            }
        };
    }
}
