using DSLKIT.Terminals;

namespace DSLKIT.Tokens
{
    public class Token : IToken
    {
        //public Token(int position, int length, string originalString, object value, ITerminal terminal)
        //{
        //    Position = position;
        //    Length = length;
        //    OriginalString = originalString;
        //    Value = value;
        //    Terminal = terminal;
        //}

        /// <summary>
        ///     First character position in stream
        /// </summary>
        public int Position { get; internal set; }

        /// <summary>
        ///     Original token length
        /// </summary>
        public int Length { get; internal set; }

        /// <summary>
        ///     Original string, token converted from (including quotes for string)
        /// </summary>
        public string OriginalString { get; internal set; }

        /// <summary>
        ///     Typed token value. If token represent integer - it contain integer value
        ///     If token is string - Value is pure string without quotes,
        /// </summary>
        public object Value { get; internal set; }

        public ITerminal Terminal { get; internal set; }
    }
}