namespace DSLKIT.Tokens
{
    public class SpaceToken : Token
    {
        public override string ToString()
        {
            var s = ((char) Value).ToString();
            return s.MakeWhiteSpaceVisible();
        }
    }
}