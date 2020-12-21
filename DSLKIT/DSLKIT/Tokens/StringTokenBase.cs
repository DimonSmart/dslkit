namespace DSLKIT.Tokens
{
    public abstract class StringTokenBase : Token
    {
        public override string ToString()
        {
            return ((string)Value).MakeWhiteSpaceVisible().DoubleQuoteIt();
        }
    }
}