using DSLKIT.Ast;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Tokens;
using DSLKIT.Visualizer.Abstractions;
using DSLKIT.Visualizer.App.GrammarProviders;
using DSLKIT.Visualizer.App.Visualization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DSLKIT.Visualizer.App.Pages;

public partial class Grammar
{
    [Inject]
    private IGrammarProviderCatalog ProviderCatalog { get; set; } = null!;

    [Inject]
    private IGrammarProviderAssemblyLoader AssemblyLoader { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private IReadOnlyList<IDslGrammarProvider> _providerOptions = [];
    private string? selectedProviderId;
    private string? selectedExampleId;
    private string? selectedExampleProviderId;
    private IGrammar? loadedGrammar;
    private string sourceText = string.Empty;
    private string? loadErrorMessage;
    private IReadOnlyList<string> loadMessages = [];
    private IReadOnlyList<TokenRowDto> tokenRows = [];
    private TreeNodeViewModel? parseTreeRoot;
    private TreeNodeViewModel? astSemanticTreeRoot;
    private TreeNodeViewModel? astTechnicalTreeRoot;
    private TreeViewState parseTreeState = new();
    private TreeViewState astSemanticTreeState = new();
    private TreeViewState astTechnicalTreeState = new();
    private TreeNodeViewModel? selectedAstSemanticNode;
    private TreeNodeViewModel? selectedAstTechnicalNode;
    private bool isParsing;
    private bool showAstTechnicalView;
    private string inputStatusMessage = "Enter source text and click Parse.";
    private InputStatusKind inputStatusKind = InputStatusKind.Warning;

    private const string SourceInputElementId = "source-input";

    private IDslGrammarProvider? selectedProvider =>
        string.IsNullOrWhiteSpace(selectedProviderId)
            ? null
            : ProviderCatalog.FindById(selectedProviderId);

    private TreeNodeViewModel? activeAstTreeRoot => showAstTechnicalView
        ? astTechnicalTreeRoot
        : astSemanticTreeRoot;

    private TreeViewState activeAstTreeState => showAstTechnicalView
        ? astTechnicalTreeState
        : astSemanticTreeState;

    private TreeNodeViewModel? activeSelectedAstNode => showAstTechnicalView
        ? selectedAstTechnicalNode
        : selectedAstSemanticNode;

    private bool HasParseOutput =>
        tokenRows.Count > 0 ||
        parseTreeRoot != null ||
        astSemanticTreeRoot != null ||
        astTechnicalTreeRoot != null;

    private bool IsParseDisabled => loadedGrammar == null || isParsing;

    private string SourceText
    {
        get => sourceText;
        set => SetSourceText(value);
    }

    protected override async Task OnInitializedAsync()
    {
        await RefreshProvidersAsync();
        await LoadSelectedGrammarAsync();
    }

    private async Task LoadAssembliesAsync(InputFileChangeEventArgs args)
    {
        var beforeProviderIds = ProviderCatalog.GetAll()
            .Select(provider => provider.Id)
            .ToHashSet(StringComparer.Ordinal);

        var files = args.GetMultipleFiles();
        var report = await AssemblyLoader.LoadProvidersAsync(files);
        loadMessages = BuildLoadMessages(report);

        await RefreshProvidersAsync();

        var addedProvider = _providerOptions.FirstOrDefault(provider => !beforeProviderIds.Contains(provider.Id));
        if (addedProvider != null)
        {
            selectedProviderId = addedProvider.Id;
        }

        await LoadSelectedGrammarAsync();
    }

    private Task OnSelectedProviderChangedAsync(ChangeEventArgs args)
    {
        selectedProviderId = args.Value?.ToString();
        return LoadSelectedGrammarAsync();
    }

    private Task OnSelectedExampleChangedAsync(ChangeEventArgs args)
    {
        selectedExampleId = args.Value?.ToString();

        if (selectedProvider == null)
        {
            return Task.CompletedTask;
        }

        var example = selectedProvider.Examples.FirstOrDefault(item => item.Id == selectedExampleId);
        if (example == null)
        {
            return Task.CompletedTask;
        }

        SetSourceText(example.SourceText);
        return Task.CompletedTask;
    }

    private Task OnSourceTextChangedAsync(string value)
    {
        SetSourceText(value);
        return Task.CompletedTask;
    }

    private async Task RunParseAsync()
    {
        if (isParsing)
        {
            return;
        }

        isParsing = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            ClearParseOutput();

            if (loadedGrammar == null || string.IsNullOrWhiteSpace(selectedProviderId))
            {
                loadErrorMessage = "Select and load a grammar provider first.";
                SetInputStatus("Select and load a grammar before parsing.", InputStatusKind.Error);
                return;
            }

            var provider = ProviderCatalog.FindById(selectedProviderId);
            if (provider == null)
            {
                loadErrorMessage = $"Provider '{selectedProviderId}' is not registered.";
                SetInputStatus("Selected grammar provider is not registered.", InputStatusKind.Error);
                return;
            }

            loadErrorMessage = null;

            var lexerSettings = provider.CreateLexerSettings(loadedGrammar);
            var lexer = new DSLKIT.Lexer.Lexer(lexerSettings);

            var allTokens = lexer.GetTokens(new StringSourceStream(sourceText)).ToList();
            tokenRows = MapTokenRows(allTokens);

            var lexerError = allTokens.OfType<ErrorToken>().FirstOrDefault();
            if (lexerError != null)
            {
                loadErrorMessage = lexerError.ToString();
                SetInputStatus("Parsing failed. Check lexer error details in Result.", InputStatusKind.Error);
                return;
            }

            var syntaxTokens = allTokens
                .Where(token => token.Terminal.Flags != TermFlags.Space && token.Terminal.Flags != TermFlags.Comment)
                .ToList();

            var parser = new SyntaxParser(loadedGrammar);
            var parseResult = parser.Parse(syntaxTokens);
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                loadErrorMessage = parseResult.Error?.ToString() ?? "Parse failed.";
                SetInputStatus("Parsing failed. Check parser error details in Result.", InputStatusKind.Error);
                return;
            }

            parseTreeRoot = MapParseTreeNode(parseResult.ParseTree, "parse:0", sourceText);
            parseTreeState = new TreeViewState();

            var astRoot = new AstBuilder(loadedGrammar.AstBindings).Build(parseResult.ParseTree, sourceText);
            astSemanticTreeRoot = MapAstNodeSemantic(astRoot, "ast:semantic:0", sourceText, isRoot: true);
            astTechnicalTreeRoot = MapAstNodeTechnical(astRoot, "ast:technical:0", sourceText);
            astSemanticTreeState = new TreeViewState();
            astTechnicalTreeState = new TreeViewState();
            selectedAstSemanticNode = astSemanticTreeRoot;
            selectedAstTechnicalNode = astTechnicalTreeRoot;
            showAstTechnicalView = false;
            SetInputStatus($"Parsed ok, {GetNodeCount(astSemanticTreeRoot!)} nodes.", InputStatusKind.Success);
        }
        catch (Exception ex)
        {
            loadErrorMessage = $"Unexpected error: {ex.Message}";
            SetInputStatus("Parsing failed due to an unexpected error.", InputStatusKind.Error);
        }
        finally
        {
            isParsing = false;
        }
    }

    private async Task RevealActiveAstNodeInSourceAsync(TreeNodeViewModel? node)
    {
        if (!TryGetNodeSpan(node, out var spanStart, out var spanEnd))
        {
            return;
        }

        await JS.InvokeVoidAsync("dslkitSourceEditor.revealSelectionById", SourceInputElementId, spanStart, spanEnd);
    }

    private async Task RefreshProvidersAsync()
    {
        _providerOptions = ProviderCatalog.GetAll();

        var nextProviderId = ResolveSelectedProviderId();
        if (string.Equals(selectedProviderId, nextProviderId, StringComparison.Ordinal))
        {
            return;
        }

        selectedProviderId = nextProviderId;
        await Task.CompletedTask;
    }

    private async Task LoadSelectedGrammarAsync()
    {
        loadedGrammar = null;
        loadErrorMessage = null;
        ClearParseOutput();

        if (string.IsNullOrWhiteSpace(selectedProviderId))
        {
            selectedExampleId = null;
            selectedExampleProviderId = null;
            SetSourceText(string.Empty);
            loadErrorMessage = "Select a grammar provider.";
            SetInputStatus("Select a grammar provider to start.", InputStatusKind.Warning);
            return;
        }

        var provider = ProviderCatalog.FindById(selectedProviderId);
        if (provider == null)
        {
            selectedExampleId = null;
            selectedExampleProviderId = null;
            SetSourceText(string.Empty);
            loadErrorMessage = $"Provider '{selectedProviderId}' is not registered.";
            SetInputStatus("Selected grammar provider is not registered.", InputStatusKind.Error);
            return;
        }

        EnsureExampleSelection(provider);

        try
        {
            loadedGrammar = provider.BuildGrammar();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            loadErrorMessage = $"Failed to load grammar: {ex.Message}";
            SetInputStatus("Failed to load grammar.", InputStatusKind.Error);
        }

        if (string.IsNullOrWhiteSpace(loadErrorMessage))
        {
            SetInputStatus("Enter source text and click Parse.", InputStatusKind.Warning);
        }
    }

    private string GetProviderDescription()
    {
        if (selectedProvider == null || string.IsNullOrWhiteSpace(selectedProvider.Description))
        {
            return "No description available.";
        }

        return selectedProvider.Description;
    }

    private void EnsureExampleSelection(IDslGrammarProvider provider)
    {
        if (!string.Equals(selectedExampleProviderId, provider.Id, StringComparison.Ordinal))
        {
            selectedExampleProviderId = provider.Id;
            ApplyDefaultExample(provider);
            return;
        }

        if (provider.Examples.Count == 0)
        {
            selectedExampleId = null;
            SetSourceText(string.Empty);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedExampleId) ||
            provider.Examples.All(example => example.Id != selectedExampleId))
        {
            ApplyDefaultExample(provider);
        }
    }

    private void ApplyDefaultExample(IDslGrammarProvider provider)
    {
        var firstExample = provider.Examples.FirstOrDefault();
        if (firstExample == null)
        {
            selectedExampleId = null;
            SetSourceText(string.Empty);
            return;
        }

        selectedExampleId = firstExample.Id;
        SetSourceText(firstExample.SourceText);
    }

    private void SelectExample(DslGrammarExample example)
    {
        selectedExampleId = example.Id;
        SetSourceText(example.SourceText);
    }

    private void ClearParseOutput()
    {
        tokenRows = [];
        parseTreeRoot = null;
        astSemanticTreeRoot = null;
        astTechnicalTreeRoot = null;
        parseTreeState = new TreeViewState();
        astSemanticTreeState = new TreeViewState();
        astTechnicalTreeState = new TreeViewState();
        selectedAstSemanticNode = null;
        selectedAstTechnicalNode = null;
        showAstTechnicalView = false;
    }

    private static IReadOnlyList<string> BuildLoadMessages(AssemblyLoadReport report)
    {
        var messages = new List<string>
        {
            $"Selected files: {report.SelectedFileCount}. Processed assemblies: {report.ProcessedAssemblyFileCount}. Registered providers: {report.RegisteredProviderCount}."
        };
        messages.AddRange(report.Messages);
        return messages;
    }

    private string? ResolveSelectedProviderId()
    {
        if (!string.IsNullOrWhiteSpace(selectedProviderId) &&
            _providerOptions.Any(provider => provider.Id == selectedProviderId))
        {
            return selectedProviderId;
        }

        return null;
    }

    private static IReadOnlyList<TokenRowDto> MapTokenRows(IReadOnlyList<IToken> tokens)
    {
        return tokens
            .Select((token, index) => new TokenRowDto
            {
                Index = index,
                Kind = token.GetType().Name,
                Terminal = token is ErrorToken ? "LexerError" : token.Terminal.Name,
                Text = token.OriginalString ?? string.Empty,
                Value = token.Value?.ToString() ?? string.Empty,
                Position = token.Position,
                Length = token.Length,
                IsIgnoredForParsing = token is not ErrorToken &&
                                     (token.Terminal.Flags == TermFlags.Space ||
                                      token.Terminal.Flags == TermFlags.Comment)
            })
            .ToList();
    }

    private static TreeNodeViewModel MapParseTreeNode(ParseTreeNode node, string nodeId, string source)
    {
        var (label, kind) = node switch
        {
            NonTerminalNode nonTerminalNode => ($"NT: {nonTerminalNode.NonTerminal.Name}", TreeNodeKind.ParseNonTerminal),
            TerminalNode terminalNode => ($"T: {terminalNode.Token.Terminal.Name} '{terminalNode.Token.OriginalString}'", TreeNodeKind.ParseTerminal),
            _ => (node.Term.Name, TreeNodeKind.ParseNode)
        };

        var children = node.Children
            .Select((child, index) => MapParseTreeNode(child, $"{nodeId}/{index}", source))
            .ToList();

        var span = ResolveParseTreeSpan(node);

        return new TreeNodeViewModel
        {
            NodeId = nodeId,
            Label = label,
            Kind = kind,
            TypeName = node.GetType().Name,
            SpanStart = span?.Start,
            SpanEnd = span?.End,
            SourcePreview = BuildSourcePreview(source, span),
            Children = children
        };
    }

    private static TreeNodeViewModel MapAstNodeTechnical(IAstNode node, string nodeId, string source)
    {
        var children = node.Children
            .Select((child, index) => MapAstNodeTechnical(child, $"{nodeId}/{index}", source))
            .ToList();

        var span = ResolveAstNodeSpan(node);

        return new TreeNodeViewModel
        {
            NodeId = nodeId,
            Label = GetTechnicalAstLabel(node),
            Kind = node is AstTokenNode ? TreeNodeKind.AstToken : TreeNodeKind.AstNode,
            Description = GetTechnicalAstDescription(node),
            TypeName = node.GetType().Name,
            SpanStart = span?.Start,
            SpanEnd = span?.End,
            SourcePreview = BuildSourcePreview(source, span),
            Children = children
        };
    }

    private static string GetTechnicalAstLabel(IAstNode node)
    {
        if (node is AstTokenNode tokenNode)
        {
            return $"Token: {tokenNode.TerminalName} '{tokenNode.Text}'";
        }

        if (node is AstNodeBase astNodeBase)
        {
            return $"{node.GetType().Name} ({astNodeBase.ParseNode.Term.Name})";
        }

        return node.GetType().Name;
    }

    private static string? GetTechnicalAstDescription(IAstNode node)
    {
        if (node is AstTokenNode tokenNode)
        {
            return $"Terminal: {tokenNode.TerminalName}; Value: {tokenNode.Value}";
        }

        if (node is AstNodeBase astNodeBase)
        {
            return $"Parse term: {astNodeBase.ParseNode.Term.Name}";
        }

        return null;
    }

    private static TreeNodeViewModel MapAstNodeSemantic(IAstNode node, string nodeId, string source, bool isRoot)
    {
        var children = node.ChildrenDisplayMode == AstChildrenDisplayMode.Hide
            ? []
            : node.Children
                .Select((child, index) => MapAstNodeSemantic(child, $"{nodeId}/{index}", source, isRoot: false))
                .ToList();

        var span = ResolveAstNodeSpan(node);

        return new TreeNodeViewModel
        {
            NodeId = nodeId,
            Label = BuildSemanticAstLabel(node),
            Kind = ResolveSemanticAstNodeKind(node, isRoot, hasChildren: children.Count > 0),
            Description = NormalizeDescription(node.Description),
            TypeName = node.GetType().Name,
            SpanStart = span?.Start,
            SpanEnd = span?.End,
            SourcePreview = BuildSourcePreview(source, span),
            Children = children
        };
    }

    private static string BuildSemanticAstLabel(IAstNode node)
    {
        return string.IsNullOrWhiteSpace(node.DisplayName)
            ? node.GetType().Name
            : node.DisplayName;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return string.Join(" ",
            description
                .Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim()));
    }

    private static TreeNodeKind ResolveSemanticAstNodeKind(IAstNode node, bool isRoot, bool hasChildren)
    {
        if (isRoot)
        {
            return TreeNodeKind.AstSemanticRoot;
        }

        var nodeTypeName = node.GetType().Name;
        if (nodeTypeName.Contains("Section", StringComparison.OrdinalIgnoreCase))
        {
            return TreeNodeKind.AstSemanticSection;
        }

        if (nodeTypeName.Contains("Property", StringComparison.OrdinalIgnoreCase))
        {
            return TreeNodeKind.AstSemanticProperty;
        }

        if (!hasChildren || node.ChildrenDisplayMode == AstChildrenDisplayMode.Hide)
        {
            return TreeNodeKind.AstSemanticField;
        }

        return TreeNodeKind.AstNode;
    }

    private static int GetNodeCount(TreeNodeViewModel root)
    {
        return EnumerateNodes(root).Count();
    }

    private static IEnumerable<TreeNodeViewModel> EnumerateNodes(TreeNodeViewModel node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in EnumerateNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private void ResetParseTreeState()
    {
        parseTreeState = new TreeViewState();
    }

    private void ResetActiveAstTreeState()
    {
        if (showAstTechnicalView)
        {
            astTechnicalTreeState = new TreeViewState();
            selectedAstTechnicalNode = astTechnicalTreeRoot;
            return;
        }

        astSemanticTreeState = new TreeViewState();
        selectedAstSemanticNode = astSemanticTreeRoot;
    }

    private void ExpandActiveAstTree()
    {
        var root = activeAstTreeRoot;
        if (root == null)
        {
            return;
        }

        var state = activeAstTreeState;
        state.ExpandedNodeIds.Clear();
        foreach (var node in EnumerateNodes(root))
        {
            if (node.Children.Count > 0)
            {
                state.ExpandedNodeIds.Add(node.NodeId);
            }
        }
    }

    private void CollapseActiveAstTree()
    {
        activeAstTreeState.ExpandedNodeIds.Clear();
    }

    private Task OnActiveAstNodeSelectedAsync(TreeNodeViewModel node)
    {
        if (showAstTechnicalView)
        {
            selectedAstTechnicalNode = node;
            return Task.CompletedTask;
        }

        selectedAstSemanticNode = node;
        return Task.CompletedTask;
    }

    private void ShowAstSemanticView()
    {
        showAstTechnicalView = false;
        if (selectedAstSemanticNode == null)
        {
            selectedAstSemanticNode = ResolveSelectedNode(astSemanticTreeRoot, astSemanticTreeState.SelectedNodeId)
                ?? astSemanticTreeRoot;
        }
    }

    private void ShowAstTechnicalView()
    {
        showAstTechnicalView = true;
        if (selectedAstTechnicalNode == null)
        {
            selectedAstTechnicalNode = ResolveSelectedNode(astTechnicalTreeRoot, astTechnicalTreeState.SelectedNodeId)
                ?? astTechnicalTreeRoot;
        }
    }

    private string GetInputStatusClass()
    {
        var toneClass = inputStatusKind switch
        {
            InputStatusKind.Success => "dsl-input-status-success",
            InputStatusKind.Error => "dsl-input-status-error",
            _ => "dsl-input-status-warning"
        };

        return $"dsl-input-status {toneClass}";
    }

    private string GetInputStatusText()
    {
        if (isParsing)
        {
            return "Parsing in progress...";
        }

        return inputStatusMessage;
    }

    private void SetInputStatus(string message, InputStatusKind statusKind)
    {
        inputStatusMessage = message;
        inputStatusKind = statusKind;
    }

    private void SetSourceText(string value)
    {
        if (string.Equals(sourceText, value, StringComparison.Ordinal))
        {
            return;
        }

        sourceText = value;
    }

    private async Task OnSourceInputKeyDownAsync(KeyboardEventArgs args)
    {
        if (!args.CtrlKey || !string.Equals(args.Key, "Enter", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await RunParseAsync();
    }

    private static TreeNodeViewModel? ResolveSelectedNode(TreeNodeViewModel? root, string? nodeId)
    {
        if (root == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return root;
        }

        return FindNodeById(root, nodeId) ?? root;
    }

    private static TreeNodeViewModel? FindNodeById(TreeNodeViewModel node, string nodeId)
    {
        if (string.Equals(node.NodeId, nodeId, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var foundNode = FindNodeById(child, nodeId);
            if (foundNode != null)
            {
                return foundNode;
            }
        }

        return null;
    }

    private static bool TryGetNodeSpan(TreeNodeViewModel? node, out int spanStart, out int spanEnd)
    {
        spanStart = 0;
        spanEnd = 0;

        if (node?.SpanStart is not int start || node.SpanEnd is not int end)
        {
            return false;
        }

        if (end < start)
        {
            return false;
        }

        spanStart = start;
        spanEnd = end;
        return true;
    }

    private static (int Start, int End)? ResolveParseTreeSpan(ParseTreeNode node)
    {
        if (node is TerminalNode terminalNode)
        {
            var spanStart = Math.Max(0, terminalNode.Token.Position);
            var tokenLength = terminalNode.Token.Length;
            if (tokenLength <= 0)
            {
                tokenLength = terminalNode.Token.OriginalString?.Length ?? 0;
            }

            var spanEnd = spanStart + Math.Max(0, tokenLength);
            return (spanStart, spanEnd);
        }

        return MergeSpans(node.Children.Select(ResolveParseTreeSpan));
    }

    private static (int Start, int End)? ResolveAstNodeSpan(IAstNode node)
    {
        if (node is AstTokenNode tokenNode)
        {
            var spanStart = Math.Max(0, tokenNode.Token.Position);
            var tokenLength = tokenNode.Token.Length;
            if (tokenLength <= 0)
            {
                tokenLength = tokenNode.Text?.Length ?? 0;
            }

            var spanEnd = spanStart + Math.Max(0, tokenLength);
            return (spanStart, spanEnd);
        }

        if (node is AstNodeBase astNodeBase)
        {
            var parseSpan = ResolveParseTreeSpan(astNodeBase.ParseNode);
            if (parseSpan != null)
            {
                return parseSpan;
            }
        }

        return MergeSpans(node.Children.Select(ResolveAstNodeSpan));
    }

    private static (int Start, int End)? MergeSpans(IEnumerable<(int Start, int End)?> spans)
    {
        var hasSpan = false;
        var minStart = int.MaxValue;
        var maxEnd = int.MinValue;

        foreach (var span in spans)
        {
            if (span == null)
            {
                continue;
            }

            hasSpan = true;
            if (span.Value.Start < minStart)
            {
                minStart = span.Value.Start;
            }

            if (span.Value.End > maxEnd)
            {
                maxEnd = span.Value.End;
            }
        }

        return hasSpan ? (minStart, maxEnd) : null;
    }

    private static string? BuildSourcePreview(string source, (int Start, int End)? span)
    {
        if (string.IsNullOrEmpty(source) || span == null)
        {
            return null;
        }

        var safeStart = Math.Clamp(span.Value.Start, 0, source.Length);
        var safeEnd = Math.Clamp(span.Value.End, safeStart, source.Length);
        if (safeEnd <= safeStart)
        {
            return null;
        }

        const int maxSliceLength = 240;
        var sliceLength = Math.Min(safeEnd - safeStart, maxSliceLength);
        var slice = source.Substring(safeStart, sliceLength)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        var previewLines = slice.Split('\n').Take(2).ToArray();
        var preview = string.Join('\n', previewLines);

        if (slice.Length > preview.Length || safeEnd - safeStart > maxSliceLength)
        {
            preview += " ...";
        }

        return preview;
    }

    private enum InputStatusKind
    {
        Warning,
        Success,
        Error
    }
}
