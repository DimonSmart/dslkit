using System;
using System.Linq;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;

namespace DSLKIT.GrammarExamples.SJackson
{
    /// <summary>
    /// Grammar from Stephen Jackson's LALR(1) tutorial.
    /// </summary>
    public static class SJacksonGrammarExample
    {
        private static readonly Lazy<IGrammar> GrammarCache = new(BuildGrammarCore);

        public static IGrammar BuildGrammar()
        {
            return GrammarCache.Value;
        }

        public static ParseResult ParseInput(string source)
        {
            var grammar = BuildGrammar();
            var lexer = new Lexer.Lexer(CreateLexerSettings(grammar));
            var parser = new SyntaxParser(grammar);

            var tokens = lexer.GetTokens(new StringSourceStream(source)).ToList();
            return parser.Parse(tokens);
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
            return new GrammarBuilder()
                .WithGrammarName("sjackson")
                .AddProductionFromString("<S> → <N>")
                .AddProductionFromString("<N> → <V> = <E>")
                .AddProductionFromString("<N> → <E>")
                .AddProductionFromString("<E> → <V>")
                .AddProductionFromString("<V> → x")
                .AddProductionFromString("<V> → * <E>")
                .BuildGrammar();
        }
    }
}
