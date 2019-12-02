namespace DSLKIT
{
    public interface ISourceStream
    {
        /// <summary>
        ///     Current zero based position
        /// </summary>
        int Position { get; }

        /// <summary>
        ///     Text stream length. Zero - for empty stream
        /// </summary>
        int Length { get; }

        /// <summary>
        ///     Return whole text
        /// </summary>
        /// <returns></returns>
        string GetText();

        /// <summary>
        ///     Lookup for next character
        /// </summary>
        /// <returns></returns>
        char Peek();

        char Read();

        /// <summary>
        ///     Seek to a new position. Zero based
        /// </summary>
        /// <param name="newPosition"></param>
        void Seek(int newPosition);
    }
}