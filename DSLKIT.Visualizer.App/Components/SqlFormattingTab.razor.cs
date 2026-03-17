using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using DSLKIT.Parser;
using DSLKIT.Visualizer.App.Components.SqlFormatting;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DSLKIT.Visualizer.App.Components;

public partial class SqlFormattingTab
{
    private const int SourcePreviewLength = 80;

    private readonly SqlFormattingState state = new();

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeFormatterAsync();
        }

        if (!firstRender && !state.ResizeEditorsPending)
        {
            return;
        }

        state.ResizeEditorsPending = false;
        await JsRuntime.InvokeVoidAsync(
            "dslkitSqlEditor.autoSizeTextAreaById",
            "sql-source-input",
            170);
    }

    private async Task InitializeFormatterAsync()
    {
        if (state.IsFormatterInitialized || state.IsFormatterInitializing)
        {
            return;
        }

        state.IsFormatterInitializing = true;
        state.FormattingError = null;
        state.FormattingParseError = null;
        state.FormattedSql = string.Empty;
        await InvokeAsync(StateHasChanged);

        await Task.Yield();
        FormatCurrentSql();

        state.IsFormatterInitializing = false;
        state.IsFormatterInitialized = true;
        state.ResizeEditorsPending = true;
        await InvokeAsync(StateHasChanged);
    }

    private string GetSourcePreview()
    {
        var sourcePreview = string.Join(' ', state.SourceSql.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(sourcePreview))
        {
            return "(empty)";
        }

        return sourcePreview.Length <= SourcePreviewLength
            ? sourcePreview
            : $"{sourcePreview[..SourcePreviewLength]}...";
    }

    private bool HideOption(string searchTokens) => !MatchesOptionSearch(searchTokens);

    private static string GetPropertyRowClass(bool disabled, bool isCheck = false)
    {
        var cssClass = isCheck
            ? "dsl-sql-property-row dsl-sql-property-row--check"
            : "dsl-sql-property-row";
        return disabled ? $"{cssClass} dsl-sql-property-row--disabled" : cssClass;
    }

    private bool MatchesOptionSearch(string searchTokens)
    {
        if (string.IsNullOrWhiteSpace(state.OptionSearch))
        {
            return true;
        }

        return searchTokens.Contains(state.OptionSearch.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private Task OnSourceInputAsync(ChangeEventArgs args)
    {
        if (state.IsFormatterInitializing)
        {
            return Task.CompletedTask;
        }

        state.SourceSql = args.Value?.ToString() ?? string.Empty;
        if (state.AutoFormat)
        {
            FormatCurrentSql();
        }
        else
        {
            state.ResizeEditorsPending = true;
        }

        return Task.CompletedTask;
    }

    private Task OnOptionsChangedAsync(ChangeEventArgs _)
    {
        return OnOptionValueChangedAsync();
    }

    private async Task OnOptionValueChangedAsync()
    {
        if (state.IsFormatterInitializing)
        {
            return;
        }

        await Task.Yield();
        if (state.AutoFormatOptions)
        {
            FormatCurrentSql();
        }
    }

    private Task OnLoadDemoRequestedAsync()
    {
        if (state.IsFormatterInitializing)
        {
            return Task.CompletedTask;
        }

        state.SourceSql = GetDemoSql(state.Dialect);
        FormatCurrentSql();
        return Task.CompletedTask;
    }

    private Task OnOptionExampleRequestedAsync(SqlOptionExampleRequest request)
    {
        if (state.IsFormatterInitializing)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(request.ExampleSql))
        {
            return Task.CompletedTask;
        }

        ApplyOptionExamplePreset(request.OptionId);
        state.SourceSql = request.ExampleSql;
        FormatCurrentSql();
        return Task.CompletedTask;
    }

    private void ApplyOptionExamplePreset(string optionId)
    {
        var isJoinBreakOption = string.Equals(optionId, "sql-joins-break-on-and", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-joins-break-on-or", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-joins-multiline-threshold", StringComparison.Ordinal);
        if (isJoinBreakOption && state.JoinsMultilineOnMaxConditionsSingleLine == 0)
        {
            state.JoinsMultilineOnMaxConditionsSingleLine = 2;
        }

        if (string.Equals(optionId, "sql-joins-break-on-and", StringComparison.Ordinal))
        {
            state.JoinsMultilineOnBreakOnAnd = true;
        }

        if (string.Equals(optionId, "sql-joins-break-on-or", StringComparison.Ordinal))
        {
            state.JoinsMultilineOnBreakOnOr = true;
        }

        var isPredicateInlineOption = string.Equals(optionId, "sql-predicates-inline-enable", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-predicates-inline-max-conditions", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-predicates-inline-max-line-length", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-predicates-inline-allow-only-and", StringComparison.Ordinal);
        if (isPredicateInlineOption)
        {
            state.PredicatesMultilineWhere = true;
            if (state.PredicatesInlineSimpleMaxConditions == 0)
            {
                state.PredicatesInlineSimpleMaxConditions = 2;
            }
        }

        if (string.Equals(optionId, "sql-predicates-inline-enable", StringComparison.Ordinal))
        {
            state.PredicatesInlineSimpleMaxConditions = Math.Max(state.PredicatesInlineSimpleMaxConditions, 2);
            state.PredicatesInlineSimpleMaxLineLength = Math.Max(state.PredicatesInlineSimpleMaxLineLength, 120);
        }

        if (string.Equals(optionId, "sql-predicates-inline-max-line-length", StringComparison.Ordinal))
        {
            state.PredicatesInlineSimpleMaxConditions = Math.Max(state.PredicatesInlineSimpleMaxConditions, 4);
        }

        if (string.Equals(optionId, "sql-predicates-inline-allow-only-and", StringComparison.Ordinal))
        {
            state.PredicatesInlineSimpleMaxConditions = Math.Max(state.PredicatesInlineSimpleMaxConditions, 2);
            state.PredicatesInlineSimpleMaxLineLength = Math.Max(state.PredicatesInlineSimpleMaxLineLength, 120);
        }

        var isMixedAndOrOption = string.Equals(optionId, "sql-predicates-mixed-and-or-parenthesize-or-groups", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-predicates-mixed-and-or-break-or-groups", StringComparison.Ordinal);
        if (isMixedAndOrOption)
        {
            state.PredicatesMultilineWhere = true;
        }

        if (string.Equals(optionId, "sql-predicates-logical-break", StringComparison.Ordinal))
        {
            state.PredicatesMultilineWhere = true;
        }

        var isCaseThresholdOption = string.Equals(optionId, "sql-case-threshold-max-when", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-case-threshold-max-line", StringComparison.Ordinal);
        if (isCaseThresholdOption)
        {
            state.ExpressionsCaseStyle = SqlCaseStyle.CompactWhenShort;
        }

        var isInlineShortOption = string.Equals(optionId, "sql-inline-short-max-tokens", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-inline-short-max-line", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-inline-short-select-item", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-inline-short-on", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-inline-short-where", StringComparison.Ordinal);
        if (isInlineShortOption)
        {
            state.SelectItemsStyle = SqlListLayoutStyle.OnePerLine;
            state.JoinsMultilineOnMaxConditionsSingleLine = Math.Max(state.JoinsMultilineOnMaxConditionsSingleLine, 1);
            state.JoinsMultilineOnBreakOnAnd = true;
            state.PredicatesMultilineWhere = true;
            state.InlineShortExpressionMaxTokens = Math.Max(state.InlineShortExpressionMaxTokens, 20);
            state.InlineShortExpressionMaxLineLength = Math.Max(state.InlineShortExpressionMaxLineLength, 120);
        }

        if (string.Equals(optionId, "sql-inline-short-max-tokens", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-inline-short-max-line", StringComparison.Ordinal))
        {
            state.InlineShortContextSelectItem = true;
            state.InlineShortContextOn = true;
            state.InlineShortContextWhere = true;
        }

        if (string.Equals(optionId, "sql-inline-short-max-tokens", StringComparison.Ordinal))
        {
            state.InlineShortExpressionMaxTokens = 12;
            state.InlineShortExpressionMaxLineLength = Math.Max(state.InlineShortExpressionMaxLineLength, 120);
        }

        if (string.Equals(optionId, "sql-inline-short-select-item", StringComparison.Ordinal))
        {
            state.InlineShortContextSelectItem = true;
            state.InlineShortContextOn = false;
            state.InlineShortContextWhere = false;
        }

        if (string.Equals(optionId, "sql-inline-short-on", StringComparison.Ordinal))
        {
            state.InlineShortContextSelectItem = false;
            state.InlineShortContextOn = true;
            state.InlineShortContextWhere = false;
        }

        if (string.Equals(optionId, "sql-inline-short-where", StringComparison.Ordinal))
        {
            state.InlineShortContextSelectItem = false;
            state.InlineShortContextOn = false;
            state.InlineShortContextWhere = true;
        }

        var isShortQueryOption = string.Equals(optionId, "sql-short-queries-enabled", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-short-queries-max-line-length", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-short-queries-max-select-items", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-short-queries-max-predicate-conditions", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-short-queries-apply-to-parenthesized-subqueries", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-short-queries-allow-single-join", StringComparison.Ordinal);
        if (isShortQueryOption)
        {
            state.ShortQueriesEnabled = true;
            state.ShortQueriesMaxLineLength = Math.Max(60, state.ShortQueriesMaxLineLength);
            state.ShortQueriesMaxSelectItems = Math.Max(2, state.ShortQueriesMaxSelectItems);
            state.ShortQueriesMaxPredicateConditions = Math.Max(2, state.ShortQueriesMaxPredicateConditions);
        }

        if (string.Equals(optionId, "sql-short-queries-apply-to-parenthesized-subqueries", StringComparison.Ordinal))
        {
            state.ShortQueriesApplyToParenthesizedSubqueries = true;
        }

        if (string.Equals(optionId, "sql-short-queries-allow-single-join", StringComparison.Ordinal))
        {
            state.ShortQueriesAllowSingleJoin = true;
        }

        var isSelectCompactOption = string.Equals(optionId, "sql-select-compact-max-items", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-select-compact-max-line-length", StringComparison.Ordinal);
        if (isSelectCompactOption)
        {
            state.SelectItemsStyle = SqlListLayoutStyle.OnePerLine;
            state.SelectCompactMaxItems = Math.Max(2, state.SelectCompactMaxItems);
            state.SelectCompactMaxLineLength = Math.Max(40, state.SelectCompactMaxLineLength);
        }

        if (string.Equals(optionId, "sql-select-compact-max-items", StringComparison.Ordinal))
        {
            state.SelectCompactMaxItems = 1;
            state.SelectCompactMaxLineLength = Math.Max(120, state.SelectCompactMaxLineLength);
        }

        if (string.Equals(optionId, "sql-select-compact-max-line-length", StringComparison.Ordinal))
        {
            state.SelectCompactMaxItems = Math.Max(2, state.SelectCompactMaxItems);
            state.SelectCompactMaxLineLength = 40;
        }

        var isInlineInListOption = string.Equals(optionId, "sql-inline-in-list-max-items", StringComparison.Ordinal) ||
            string.Equals(optionId, "sql-inline-in-list-max-line", StringComparison.Ordinal);
        if (isInlineInListOption)
        {
            state.InListItemsStyle = SqlInListItemsStyle.OnePerLine;
        }

        if (string.Equals(optionId, "sql-wrap-column", StringComparison.Ordinal))
        {
            state.SelectItemsStyle = SqlListLayoutStyle.WrapByWidth;
            state.GroupByItemsStyle = SqlListLayoutStyle.WrapByWidth;
            state.OrderByItemsStyle = SqlListLayoutStyle.WrapByWidth;
            state.InListItemsStyle = SqlInListItemsStyle.WrapByWidth;
            state.WrapColumn = 60;
        }

        if (string.Equals(optionId, "sql-indent-cte-body", StringComparison.Ordinal))
        {
            state.IndentCteBody = true;
        }

        if (string.Equals(optionId, "sql-newline-qualify", StringComparison.Ordinal))
        {
            state.Dialect = SqlDialect.Snowflake;
        }
    }

    private static string GetDemoSql(SqlDialect dialect)
    {
        return dialect == SqlDialect.Snowflake
            ? SqlFormattingExamples.SnowflakeDemoSql
            : SqlFormattingExamples.DemoSql;
    }

    private Task OnFormatRequestedAsync()
    {
        if (state.IsFormatterInitializing)
        {
            return Task.CompletedTask;
        }

        FormatCurrentSql();
        return Task.CompletedTask;
    }

    private bool CanGoToFormattingError => state.FormattingParseError is { ErrorPosition: >= 0 };

    private static string GetFormattingErrorSummary(ParseErrorDescription parseError)
    {
        return parseError.Message;
    }

    private static string GetFormattingErrorPositionText(ParseErrorDescription parseError)
    {
        return $"Position: {parseError.ErrorPosition}";
    }

    private async Task OnGoToFormattingErrorAsync()
    {
        if (state.FormattingParseError == null)
        {
            return;
        }

        var selectionStart = Math.Max(0, state.FormattingParseError.ErrorPosition);
        var selectionLength = string.IsNullOrEmpty(state.FormattingParseError.ActualTokenText)
            ? 0
            : state.FormattingParseError.ActualTokenText.Length;
        await JsRuntime.InvokeVoidAsync(
            "dslkitSourceEditor.revealSelectionById",
            "sql-source-input",
            selectionStart,
            selectionStart + selectionLength);
    }

    private Task OnResetOptionsRequestedAsync()
    {
        if (state.IsFormatterInitializing)
        {
            return Task.CompletedTask;
        }

        ApplyDefaultOptions();
        FormatCurrentSql();
        return Task.CompletedTask;
    }

    private void ApplyDefaultOptions()
    {
        state.ResetOptions();
    }

    private void FormatCurrentSql()
    {
        try
        {
            var options = SqlFormattingOptionsFactory.Create(state);
            var result = ModernMsSqlFormatter.TryFormat(state.SourceSql, options);
            if (!result.IsSuccess)
            {
                state.FormattedSql = string.Empty;
                state.FormattingParseError = result.ParseError;
                state.FormattingError = result.ParseError == null
                    ? result.ErrorMessage ?? "Parse failed."
                    : null;
                return;
            }

            state.FormattingParseError = null;
            state.FormattingError = null;
            state.FormattedSql = result.FormattedSql ?? string.Empty;
        }
        catch (Exception ex)
        {
            state.FormattedSql = string.Empty;
            state.FormattingParseError = null;
            state.FormattingError = $"Unexpected formatter error: {ex.Message}";
        }
        finally
        {
            state.ResizeEditorsPending = true;
        }
    }
}
