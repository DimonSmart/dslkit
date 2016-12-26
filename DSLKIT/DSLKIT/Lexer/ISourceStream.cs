namespace DSLKIT
{
    public interface ISourceStream
    {
        int Position { get; }
        int Length { get; }
        string GetText();
        char Peek();
        char Read();
        void Seek(int newPosition);
    }
}