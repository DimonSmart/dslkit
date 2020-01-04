namespace DSLKIT.Tokens
{
    public class ErrorToken : Token
    {
        public ErrorToken(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; }

        public override string ToString()
        {
            return $"{ErrorMessage} at position: {Position}";
        }
    }
}