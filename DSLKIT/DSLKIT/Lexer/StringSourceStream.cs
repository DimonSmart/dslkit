using System.IO;

namespace DSLKIT
{
    public class StringSourceStream : ISourceStream
    {
        private readonly string _buffer;

        public StringSourceStream(string buffer)
        {
            _buffer = buffer;
        }

        public int Position { get; set; }
        public int Length => _buffer.Length;

        public string GetText()
        {
            return _buffer;
        }

        public char Peek()
        {
            if (Position < Length)
            {
                return _buffer[Position];
            }

            throw new EndOfStreamException();
        }

        public char Read()
        {
            if (Position < Length)
            {
                return _buffer[Position++];
            }

            throw new EndOfStreamException();
        }

        public void Seek(int newPosition)
        {
            if (newPosition > Length)
            {
                throw new EndOfStreamException();
            }

            Position = newPosition;
        }
    }
}