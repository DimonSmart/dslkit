using DSLKIT.Terminals;

namespace DSLKIT.Tokens
{
    public class Token : IToken
    {
        /// <summary>
        ///     Token length in chars
        /// </summary>
        public int Length { get; internal set; }

        /// <summary>
        ///     First token charackter stream position
        /// </summary>
        public int Position { get; internal set; }

        /// <summary>
        ///     Original string, token converted from
        /// </summary>
        public string StringValue { get; internal set; }

        public ITerminal Terminal { get; internal set; }
        /// <summary>
        ///     Typed token value. If token represent integer - it contain integer value
        ///     If token is string - Value is pure string without quotes,
        /// </summary>
        public object Value { get; internal set; }
    }
}