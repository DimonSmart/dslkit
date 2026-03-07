using System.Collections.Generic;
using DSLKIT.Parser;

namespace DSLKIT.GrammarExamples.MsSql
{
    public sealed class SqlScriptDocumentParseResult
    {
        public bool IsSuccess => Error == null;
        public ParseErrorDescription? Error { get; init; }
        public SqlScriptDocument? Document { get; init; }
    }

    public sealed class SqlScriptDocument
    {
        public required IReadOnlyList<SqlScriptDocumentNode> Segments { get; init; }
    }

    public abstract class SqlScriptDocumentNode
    {
        public required int StartPosition { get; init; }
        public required string Text { get; init; }
    }

    public sealed class SqlBatchDocumentNode : SqlScriptDocumentNode
    {
        public required ParseResult ParseResult { get; init; }
    }

    public sealed class SqlBatchSeparatorDocumentNode : SqlScriptDocumentNode
    {
        public int? RepeatCount { get; init; }
    }

    public sealed class SqlcmdCommandDocumentNode : SqlScriptDocumentNode
    {
    }
}
