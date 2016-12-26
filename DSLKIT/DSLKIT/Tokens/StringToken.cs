namespace DSLKIT.Tokens
{
    public class StringToken : Token
    {
        public override string ToString()
        {
            return ((string) Value).DoubleQuoteIt();
        }
    }
}