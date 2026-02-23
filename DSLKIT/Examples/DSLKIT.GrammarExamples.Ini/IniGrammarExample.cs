using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.GrammarExamples.Ini
{
    /// <summary>
    /// Minimal INI grammar example.
    /// Focus: sections, key=value pairs, bool/number/string values and simple recovery for missing '='.
    /// </summary>
    public static class IniGrammarExample
    {
        private static readonly Lazy<IGrammar> GrammarCache = new(BuildGrammarCore);

        public static IGrammar BuildGrammar()
        {
            return GrammarCache.Value;
        }

        public static IniParseOutput ParseDocument(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var grammar = BuildGrammar();
            var lexer = new Lexer.Lexer(CreateLexerSettings(grammar));
            var parser = new SyntaxParser(grammar);

            var tokens = lexer.GetTokens(new StringSourceStream(source))
                .Where(token => token.Terminal.Flags != TermFlags.Space && token.Terminal.Flags != TermFlags.Comment)
                .ToList();

            var parseResult = parser.Parse(tokens);
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                return new IniParseOutput(parseResult, document: null, diagnostics: Array.Empty<IniDiagnostic>());
            }

            var diagnostics = new List<IniDiagnostic>();
            var document = BuildDocument(parseResult.ParseTree, diagnostics);
            return new IniParseOutput(parseResult, document, diagnostics);
        }

        public static void ParseDocumentOrThrow(string source)
        {
            var result = ParseDocument(source);
            if (!result.ParseResult.IsSuccess || result.Document == null)
            {
                throw new InvalidOperationException(
                    $"Parse failed. Position: {result.ParseResult.Error?.ErrorPosition}. Message: {result.ParseResult.Error?.Message}");
            }
        }

        public static LexerSettings CreateLexerSettings(IGrammar grammar)
        {
            var settings = new LexerSettings();
            foreach (var terminal in grammar.Terminals)
            {
                settings.Add(terminal);
            }

            return settings;
        }

        private static IGrammar BuildGrammarCore()
        {
            var word = new RegExpTerminal(
                "Word",
                @"\G[A-Za-z_][A-Za-z0-9_.-]*",
                previewChar: null,
                flags: TermFlags.Identifier);

            var number = new RegExpTerminal(
                "Number",
                @"\G(?:0|[1-9]\d*)(?:\.\d+)?",
                previewChar: null,
                flags: TermFlags.Const);

            var quotedString = new RegExpTerminal(
                "QuotedString",
                "\\G(?:\"(?:[^\"\\\\\\r\\n]|\\\\.)*\"|'(?:[^'\\\\\\r\\n]|\\\\.)*')",
                previewChar: null,
                flags: TermFlags.Const);

            var newLine = new RegExpTerminal(
                "NewLine",
                @"\G(?:\r\n|\r|\n)+",
                previewChar: null,
                flags: TermFlags.None);

            var gb = new GrammarBuilder()
                .WithGrammarName("ini-hello-world")
                .AddTerminal(new CustomSpaceTerminal(new[] { ' ', '\t' }))
                .AddTerminal(newLine)
                .AddTerminal(new SingleLineCommentTerminal(";"))
                .AddTerminal(new SingleLineCommentTerminal("#"))
                .AddTerminal(word)
                .AddTerminal(number)
                .AddTerminal(quotedString);

            var document = gb.NT("Document");
            var lineList = gb.NT("LineList");
            var line = gb.NT("Line");
            var sectionLine = gb.NT("SectionLine");
            var sectionHeader = gb.NT("SectionHeader");
            var propertyLine = gb.NT("PropertyLine");
            var property = gb.NT("Property");
            var value = gb.NT("Value");

            gb.Prod("Start").Is(document);
            gb.Prod("Document").Is(lineList);
            gb.Prod("LineList").Is(EmptyTerm.Empty);
            gb.Prod("LineList").Is(line);
            gb.Prod("LineList").Is(lineList, line);

            gb.Prod("Line").Is(newLine);
            gb.Prod("Line").Is(sectionLine);
            gb.Prod("Line").Is(propertyLine);

            gb.Prod("SectionLine").Is(sectionHeader);
            gb.Prod("SectionLine").Is(sectionHeader, newLine);
            gb.Prod("SectionHeader").Is("[", word, "]");

            gb.Prod("PropertyLine").Is(property);
            gb.Prod("PropertyLine").Is(property, newLine);

            gb.Prod("Property").Is(word, "=", value);
            // Recovery production for common typo: "key value" instead of "key=value".
            gb.Prod("Property").Is(word, value);

            gb.Prod("Value").Is(number);
            gb.Prod("Value").Is(quotedString);
            gb.Prod("Value").Is(word);

            return gb.BuildGrammar("Start");
        }

        private static IniDocument BuildDocument(ParseTreeNode root, ICollection<IniDiagnostic> diagnostics)
        {
            var sectionBuilders = new List<IniSectionBuilder>();
            IniSectionBuilder? currentSection = null;

            foreach (var lineNode in EnumerateNonTerminals(root, "Line"))
            {
                if (TryFindNonTerminal(lineNode, "SectionHeader", out var sectionHeaderNode))
                {
                    var sectionName = ReadTerminalText(sectionHeaderNode, "Word");
                    currentSection = new IniSectionBuilder(sectionName);
                    sectionBuilders.Add(currentSection);
                    continue;
                }

                if (TryFindNonTerminal(lineNode, "Property", out var propertyNode))
                {
                    var property = BuildProperty(propertyNode, diagnostics);
                    if (currentSection == null)
                    {
                        currentSection = new IniSectionBuilder("global");
                        sectionBuilders.Add(currentSection);
                    }

                    currentSection.Properties.Add(property);
                }
            }

            var sections = sectionBuilders
                .Select(section => new IniSection(section.Name, section.Properties))
                .ToList();
            return new IniDocument(sections);
        }

        private static IniProperty BuildProperty(NonTerminalNode propertyNode, ICollection<IniDiagnostic> diagnostics)
        {
            if (propertyNode.Children.Count < 2)
            {
                throw new InvalidOperationException("Invalid Property production shape.");
            }

            if (propertyNode.Children[0] is not TerminalNode keyNode)
            {
                throw new InvalidOperationException("Property key is expected to be a terminal node.");
            }

            var valueNode = propertyNode.Children[^1];
            var rawValue = ReadFirstTerminalText(valueNode);
            var parsedValue = ParseValue(rawValue);
            var isRecovered = propertyNode.Children.Count == 2;

            if (isRecovered)
            {
                diagnostics.Add(new IniDiagnostic(
                    $"Recovered missing '=' after key '{keyNode.Token.OriginalString}'.",
                    keyNode.Token.Position));
            }

            return new IniProperty(keyNode.Token.OriginalString, parsedValue, isRecovered);
        }

        private static IniValue ParseValue(string rawValue)
        {
            if (TryUnquote(rawValue, out var unquoted))
            {
                return IniValue.FromString(rawValue, unquoted);
            }

            if (bool.TryParse(rawValue, out var boolValue))
            {
                return IniValue.FromBoolean(rawValue, boolValue);
            }

            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var numberValue))
            {
                return IniValue.FromNumber(rawValue, numberValue);
            }

            return IniValue.FromString(rawValue, rawValue);
        }

        private static bool TryUnquote(string rawValue, out string value)
        {
            value = string.Empty;
            if (rawValue.Length < 2)
            {
                return false;
            }

            var quote = rawValue[0];
            if ((quote != '"' && quote != '\'') || rawValue[^1] != quote)
            {
                return false;
            }

            var body = rawValue.Substring(1, rawValue.Length - 2);
            body = body.Replace("\\\\", "\\", StringComparison.Ordinal);
            body = quote == '"'
                ? body.Replace("\\\"", "\"", StringComparison.Ordinal)
                : body.Replace("\\'", "'", StringComparison.Ordinal);

            value = body;
            return true;
        }

        private static IEnumerable<NonTerminalNode> EnumerateNonTerminals(ParseTreeNode node, string nonTerminalName)
        {
            if (node is NonTerminalNode nonTerminalNode && nonTerminalNode.NonTerminal.Name == nonTerminalName)
            {
                yield return nonTerminalNode;
            }

            foreach (var child in node.Children)
            {
                foreach (var found in EnumerateNonTerminals(child, nonTerminalName))
                {
                    yield return found;
                }
            }
        }

        private static bool TryFindNonTerminal(ParseTreeNode node, string nonTerminalName, out NonTerminalNode result)
        {
            if (node is NonTerminalNode nonTerminalNode && nonTerminalNode.NonTerminal.Name == nonTerminalName)
            {
                result = nonTerminalNode;
                return true;
            }

            foreach (var child in node.Children)
            {
                if (TryFindNonTerminal(child, nonTerminalName, out result))
                {
                    return true;
                }
            }

            result = null!;
            return false;
        }

        private static string ReadTerminalText(ParseTreeNode node, string terminalName)
        {
            foreach (var terminalNode in EnumerateTerminals(node))
            {
                if (terminalNode.Token.Terminal.Name == terminalName)
                {
                    return terminalNode.Token.OriginalString;
                }
            }

            throw new InvalidOperationException($"Terminal '{terminalName}' was not found.");
        }

        private static string ReadFirstTerminalText(ParseTreeNode node)
        {
            foreach (var terminalNode in EnumerateTerminals(node))
            {
                return terminalNode.Token.OriginalString;
            }

            throw new InvalidOperationException("Expected terminal node was not found.");
        }

        private static IEnumerable<TerminalNode> EnumerateTerminals(ParseTreeNode node)
        {
            if (node is TerminalNode terminalNode)
            {
                yield return terminalNode;
                yield break;
            }

            foreach (var child in node.Children)
            {
                foreach (var found in EnumerateTerminals(child))
                {
                    yield return found;
                }
            }
        }

        private sealed class IniSectionBuilder
        {
            public IniSectionBuilder(string name)
            {
                Name = name;
                Properties = new List<IniProperty>();
            }

            public string Name { get; }
            public List<IniProperty> Properties { get; }
        }
    }

    public sealed class IniParseOutput
    {
        public IniParseOutput(ParseResult parseResult, IniDocument? document, IReadOnlyList<IniDiagnostic> diagnostics)
        {
            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
            Document = document;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public ParseResult ParseResult { get; }
        public IniDocument? Document { get; }
        public IReadOnlyList<IniDiagnostic> Diagnostics { get; }
    }

    public sealed class IniDocument
    {
        public IniDocument(IReadOnlyList<IniSection> sections)
        {
            Sections = sections ?? throw new ArgumentNullException(nameof(sections));
        }

        public IReadOnlyList<IniSection> Sections { get; }
    }

    public sealed class IniSection
    {
        public IniSection(string name, IReadOnlyList<IniProperty> properties)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public string Name { get; }
        public IReadOnlyList<IniProperty> Properties { get; }
    }

    public sealed class IniProperty
    {
        public IniProperty(string key, IniValue value, bool isRecoveredFromMissingEquals)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            IsRecoveredFromMissingEquals = isRecoveredFromMissingEquals;
        }

        public string Key { get; }
        public IniValue Value { get; }
        public bool IsRecoveredFromMissingEquals { get; }
    }

    public sealed class IniDiagnostic
    {
        public IniDiagnostic(string message, int position)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Position = position;
        }

        public string Message { get; }
        public int Position { get; }
    }

    public enum IniValueKind
    {
        String,
        Number,
        Boolean
    }

    public sealed class IniValue
    {
        private IniValue(IniValueKind kind, string rawText, string? stringValue, double? numberValue, bool? booleanValue)
        {
            Kind = kind;
            RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
            StringValue = stringValue;
            NumberValue = numberValue;
            BooleanValue = booleanValue;
        }

        public IniValueKind Kind { get; }
        public string RawText { get; }
        public string? StringValue { get; }
        public double? NumberValue { get; }
        public bool? BooleanValue { get; }

        public static IniValue FromString(string rawText, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new IniValue(IniValueKind.String, rawText, value, numberValue: null, booleanValue: null);
        }

        public static IniValue FromNumber(string rawText, double value)
        {
            return new IniValue(IniValueKind.Number, rawText, stringValue: null, value, booleanValue: null);
        }

        public static IniValue FromBoolean(string rawText, bool value)
        {
            return new IniValue(IniValueKind.Boolean, rawText, stringValue: null, numberValue: null, value);
        }
    }
}
