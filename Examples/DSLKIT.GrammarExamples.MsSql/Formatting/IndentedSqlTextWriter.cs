using System;
using System.Text;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    internal sealed class IndentedSqlTextWriter
    {
        private readonly StringBuilder _builder;
        private readonly string _indentUnit;
        private int _indentLevel;
        private int _nextLineIndentOffset;
        private bool _isLineStart = true;

        public IndentedSqlTextWriter(StringBuilder builder, string indentUnit = "    ")
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _indentUnit = string.IsNullOrEmpty(indentUnit) ? "    " : indentUnit;
        }

        public bool HasContent => _builder.Length > 0;

        public bool IsLineStart => _isLineStart;

        public IDisposable PushIndent()
        {
            _indentLevel++;
            return new IndentScope(this);
        }

        public void SetNextLineIndentOffset(int indentOffset)
        {
            if (indentOffset <= 0)
            {
                return;
            }

            _nextLineIndentOffset = Math.Max(_nextLineIndentOffset, indentOffset);
        }

        public void WriteToken(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            WriteIndentIfNeeded();
            _builder.Append(text);
            _isLineStart = false;
        }

        public void WriteSpace()
        {
            if (_isLineStart || _builder.Length == 0)
            {
                return;
            }

            var lastChar = _builder[_builder.Length - 1];
            if (char.IsWhiteSpace(lastChar))
            {
                return;
            }

            _builder.Append(' ');
        }

        public void WriteLine()
        {
            TrimLineTrailingWhitespace();

            if (_builder.Length == 0)
            {
                _isLineStart = true;
                return;
            }

            _builder.AppendLine();
            _isLineStart = true;
        }

        public override string ToString()
        {
            return _builder.ToString().TrimEnd();
        }

        private void PopIndent()
        {
            if (_indentLevel == 0)
            {
                return;
            }

            _indentLevel--;
        }

        private void WriteIndentIfNeeded()
        {
            if (!_isLineStart)
            {
                return;
            }

            var effectiveIndent = _indentLevel + _nextLineIndentOffset;
            for (var i = 0; i < effectiveIndent; i++)
            {
                _builder.Append(_indentUnit);
            }

            _nextLineIndentOffset = 0;
            _isLineStart = false;
        }

        private void TrimLineTrailingWhitespace()
        {
            while (_builder.Length > 0)
            {
                var lastChar = _builder[_builder.Length - 1];
                if (lastChar != ' ' && lastChar != '\t')
                {
                    break;
                }

                _builder.Length--;
            }
        }

        private sealed class IndentScope : IDisposable
        {
            private IndentedSqlTextWriter? _owner;

            public IndentScope(IndentedSqlTextWriter owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                _owner.PopIndent();
                _owner = null;
            }
        }
    }
}
