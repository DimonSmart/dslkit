using System.Collections;
using System.Collections.Generic;
using DSLKIT.Tokens;

namespace DSLKIT
{
    public abstract class LexerStreamBase : IEnumerable<Token>
    {
        private readonly IEnumerable<Token> _sourceStream;

        protected LexerStreamBase(IEnumerable<Token> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            return Filter(_sourceStream).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract IEnumerable<Token> Filter(IEnumerable<Token> sourceStream);
    }
}