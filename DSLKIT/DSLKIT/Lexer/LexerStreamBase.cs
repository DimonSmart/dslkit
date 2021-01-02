using DSLKIT.Tokens;
using System.Collections;
using System.Collections.Generic;

namespace DSLKIT
{
    public abstract class LexerStreamBase : IEnumerable<IToken>
    {
        private readonly IEnumerable<IToken> _sourceStream;

        protected LexerStreamBase(IEnumerable<IToken> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public IEnumerator<IToken> GetEnumerator()
        {
            return Filter(_sourceStream).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract IEnumerable<IToken> Filter(IEnumerable<IToken> sourceStream);
    }
}