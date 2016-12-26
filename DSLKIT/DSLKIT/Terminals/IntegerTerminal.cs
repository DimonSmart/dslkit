﻿using System.Text.RegularExpressions;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public class IntegerTerminal : ITerminal
    {
        private readonly Regex _regex;

        public IntegerTerminal()
        {
            _regex = new Regex(@"\G\d+");
        }

        public TermFlags Flags => TermFlags.Const;
        public string Name => "Integer";

        public TerminalPriority Priority => TerminalPriority.Normal;
        public bool CanStartWith(char c)
        {
            return char.IsDigit(c);
        }

        public bool TryMatch(ISourceStream source, out Token token)
        {
            token = null;
            var result = _regex.Match(source);
            if (!result.Success)
                return false;

            var stringBody = result.Value;
            int intValue;
            if (!int.TryParse(stringBody, out intValue))
            {
                return false;
            }

            token = new IntegerToken
            {
                Position = source.Position,
                Length = result.Length,
                Terminal = this,
                StringValue = stringBody,
                Value = intValue
            };

            return true;
        }
    }
}