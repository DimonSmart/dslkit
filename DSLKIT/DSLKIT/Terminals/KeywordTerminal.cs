using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DSLKIT.Terminals
{
    public class KeywordTerminal : RegExpTerminalBase
    {
        private static readonly ConcurrentDictionary<string, KeywordTerminal> AllTerminals =
            new ConcurrentDictionary<string, KeywordTerminal>();

        public static Dictionary<string, TermFlags> PredefinedFlags = new Dictionary<string, TermFlags>
        {
            {"(", TermFlags.OpenBrace},
            {")", TermFlags.CloseBrace}
        };

        private KeywordTerminal(string keyword) : base(@"\G" + Regex.Escape(keyword), keyword[0])
        {
            Keyword = keyword;
            Flags = TermFlags.None;
            Name = Keyword;
        }

        private KeywordTerminal(string keyword, TermFlags flags) : this(keyword)
        {
            Keyword = keyword;
            Flags = flags;
        }

        public override TerminalPriority Priority => TerminalPriority.Normal;

        public override TermFlags Flags { get; }
        public override string Name { get; }
        private string Keyword { get; }

        public static KeywordTerminal CreateTerminal(string keyword, TermFlags flags = TermFlags.None)
        {
            if (flags == TermFlags.None)
            {
                PredefinedFlags.TryGetValue(keyword, out flags);
            }

            var terminal = AllTerminals.GetOrAdd(keyword, s => new KeywordTerminal(keyword, flags));
            if (terminal.Flags != flags)
            {
                throw new InvalidOperationException(
                    $"Different flags for same keyword:[{keyword}] {terminal.Flags}, {flags}");
            }

            return terminal;
        }

        public static implicit operator KeywordTerminal(string keyword)
        {
            return CreateTerminal(keyword);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Keyword) ? "[Empty]" : Keyword;
        }
    }
}