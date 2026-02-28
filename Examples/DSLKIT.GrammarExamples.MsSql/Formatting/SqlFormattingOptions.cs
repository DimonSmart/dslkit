using System.Collections.Generic;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    public enum SqlKeywordCase
    {
        Upper,
        Lower,
        Preserve
    }

    public enum SqlParenthesesSpacing
    {
        Never,
        Always,
        Smart
    }

    public enum SqlStatementTerminationMode
    {
        Never,
        ExistingOnly,
        Always
    }

    public enum SqlBlankLineBetweenClausesMode
    {
        None,
        BetweenMajorClauses
    }

    public enum SqlCommaStyle
    {
        Trailing,
        Leading
    }

    public enum SqlListLayoutStyle
    {
        OnePerLine,
        WrapByWidth
    }

    public enum SqlCaseStyle
    {
        Multiline,
        CompactWhenShort
    }

    public enum SqlSubqueryIndentStyle
    {
        Indented
    }

    public enum SqlInListItemsStyle
    {
        Inline,
        OnePerLine,
        WrapByWidth
    }

    public enum SqlInlineExpressionContext
    {
        SelectItem,
        On,
        Where
    }

    public enum SqlDmlListStyle
    {
        OnePerLine,
        WrapByWidth
    }

    public enum SqlCreateProcLayout
    {
        Expanded,
        Compact
    }

    public enum SqlCommentsFormattingMode
    {
        Keep,
        ReflowSafeOnly
    }

    public enum SqlJoinMultilineBreakOnMode
    {
        AndOnly,
        AndOr
    }

    public enum SqlLogicalOperatorLineBreakMode
    {
        BeforeOperator,
        AfterOperator
    }

    public enum SqlParenthesizeMixedAndOrMode
    {
        None,
        Minimal,
        AlwaysForOrGroups
    }

    public sealed record SqlFormattingOptions
    {
        public SqlKeywordCase KeywordCase { get; init; } = SqlKeywordCase.Upper;

        public SqlSpacesFormattingOptions Spaces { get; init; } = new();

        public SqlStatementFormattingOptions Statement { get; init; } = new();

        public SqlEndOfFileFormattingOptions Eof { get; init; } = new();

        public SqlLayoutFormattingOptions Layout { get; init; } = new();

        public SqlListsFormattingOptions Lists { get; init; } = new();

        public SqlAlignFormattingOptions Align { get; init; } = new();

        public SqlJoinsFormattingOptions Joins { get; init; } = new();

        public SqlPredicatesFormattingOptions Predicates { get; init; } = new();

        public SqlExpressionsFormattingOptions Expressions { get; init; } = new();

        public SqlSubqueriesFormattingOptions Subqueries { get; init; } = new();

        public SqlDmlFormattingOptions Dml { get; init; } = new();

        public SqlDdlFormattingOptions Ddl { get; init; } = new();

        public SqlCommentsFormattingOptions Comments { get; init; } = new();

        public SqlPreserveFormattingOptions Preserve { get; init; } = new();
    }

    public sealed record SqlSpacesFormattingOptions
    {
        public bool AfterComma { get; init; } = true;

        public bool AroundBinaryOperators { get; init; } = true;

        public SqlParenthesesSpacing InsideParentheses { get; init; } = SqlParenthesesSpacing.Never;

        public bool BeforeSemicolon { get; init; } = false;
    }

    public sealed record SqlStatementFormattingOptions
    {
        public SqlStatementTerminationMode TerminateWithSemicolon { get; init; } = SqlStatementTerminationMode.ExistingOnly;
    }

    public sealed record SqlEndOfFileFormattingOptions
    {
        public bool Newline { get; init; } = false;
    }

    public sealed record SqlLayoutFormattingOptions
    {
        public int IndentSize { get; init; } = 4;

        public SqlClauseNewlineOptions NewlineBeforeClause { get; init; } = new();

        public SqlBlankLineBetweenClausesMode BlankLineBetweenClauses { get; init; } = SqlBlankLineBetweenClausesMode.None;
    }

    public sealed record SqlClauseNewlineOptions
    {
        public bool With { get; init; } = true;

        public bool Select { get; init; } = true;

        public bool From { get; init; } = true;

        public bool Where { get; init; } = true;

        public bool GroupBy { get; init; } = true;

        public bool Having { get; init; } = true;

        public bool OrderBy { get; init; } = true;

        public bool Option { get; init; } = true;
    }

    public sealed record SqlListsFormattingOptions
    {
        public SqlCommaStyle CommaStyle { get; init; } = SqlCommaStyle.Trailing;

        public SqlListLayoutStyle SelectItems { get; init; } = SqlListLayoutStyle.OnePerLine;

        public SqlListLayoutStyle GroupByItems { get; init; } = SqlListLayoutStyle.OnePerLine;

        public SqlListLayoutStyle OrderByItems { get; init; } = SqlListLayoutStyle.OnePerLine;

        public SqlSelectCompactThresholdOptions SelectCompactThreshold { get; init; } = new();

        public SqlInListItemsStyle InListItems { get; init; } = SqlInListItemsStyle.Inline;

        public SqlInlineInListThresholdOptions InlineInListThreshold { get; init; } = new();
    }

    public sealed record SqlSelectCompactThresholdOptions
    {
        public int MaxItems { get; init; } = 0;

        public int MaxLineLength { get; init; } = 80;

        public bool AllowExpressions { get; init; } = false;
    }

    public sealed record SqlInlineInListThresholdOptions
    {
        public int MaxItemsInline { get; init; } = 0;

        public int MaxLineLength { get; init; } = 120;
    }

    public sealed record SqlAlignFormattingOptions
    {
        public bool SelectAliases { get; init; } = false;
    }

    public sealed record SqlJoinsFormattingOptions
    {
        public bool NewlinePerJoin { get; init; } = true;

        public bool OnNewLine { get; init; } = true;

        public SqlJoinMultilineOnThresholdOptions MultilineOnThreshold { get; init; } = new();
    }

    public sealed record SqlJoinMultilineOnThresholdOptions
    {
        public int MaxTokensSingleLine { get; init; } = 0;

        public SqlJoinMultilineBreakOnMode BreakOn { get; init; } = SqlJoinMultilineBreakOnMode.AndOnly;
    }

    public sealed record SqlPredicatesFormattingOptions
    {
        public bool MultilineWhere { get; init; } = false;

        public SqlLogicalOperatorLineBreakMode LogicalOperatorLineBreak { get; init; } = SqlLogicalOperatorLineBreakMode.BeforeOperator;

        public SqlInlineSimplePredicateOptions InlineSimplePredicate { get; init; } = new();

        public SqlParenthesizeMixedAndOrOptions ParenthesizeMixedAndOr { get; init; } = new();
    }

    public sealed record SqlInlineSimplePredicateOptions
    {
        public int MaxConditions { get; init; } = 0;

        public int MaxLineLength { get; init; } = 120;

        public bool AllowOnlyAnd { get; init; } = true;
    }

    public sealed record SqlParenthesizeMixedAndOrOptions
    {
        public SqlParenthesizeMixedAndOrMode Mode { get; init; } = SqlParenthesizeMixedAndOrMode.None;
    }

    public sealed record SqlExpressionsFormattingOptions
    {
        public SqlCaseStyle CaseStyle { get; init; } = SqlCaseStyle.Multiline;

        public SqlCompactCaseThresholdOptions CompactCaseThreshold { get; init; } = new();

        public SqlInlineShortExpressionOptions InlineShortExpression { get; init; } = new();
    }

    public sealed record SqlCompactCaseThresholdOptions
    {
        public int MaxWhenClauses { get; init; } = 0;

        public int MaxTokens { get; init; } = 0;

        public int MaxLineLength { get; init; } = 120;
    }

    public sealed record SqlInlineShortExpressionOptions
    {
        public int MaxTokens { get; init; } = 0;

        public int MaxDepth { get; init; } = 0;

        public int MaxLineLength { get; init; } = 120;

        public IReadOnlyCollection<SqlInlineExpressionContext> ForContexts { get; init; } = [];
    }

    public sealed record SqlSubqueriesFormattingOptions
    {
        public SqlSubqueryIndentStyle IndentStyle { get; init; } = SqlSubqueryIndentStyle.Indented;
    }

    public sealed record SqlDmlFormattingOptions
    {
        public SqlDmlListStyle UpdateSetStyle { get; init; } = SqlDmlListStyle.OnePerLine;

        public SqlDmlListStyle InsertColumnsStyle { get; init; } = SqlDmlListStyle.OnePerLine;
    }

    public sealed record SqlDdlFormattingOptions
    {
        public SqlCreateProcLayout CreateProcLayout { get; init; } = SqlCreateProcLayout.Expanded;
    }

    public sealed record SqlCommentsFormattingOptions
    {
        public bool PreserveAttachment { get; init; } = true;

        public SqlCommentsFormattingMode Formatting { get; init; } = SqlCommentsFormattingMode.Keep;
    }

    public sealed record SqlPreserveFormattingOptions
    {
        public bool StringLiterals { get; init; } = true;
    }
}
