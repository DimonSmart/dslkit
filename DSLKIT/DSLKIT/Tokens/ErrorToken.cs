namespace DSLKIT.Tokens
{
    public class ErrorToken : Token
    {
        public string ErrorMessage { get; }

        public ErrorToken(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public override string ToString()
        {
            return $"{ErrorMessage} at position: {Position}";
        }
    }
}