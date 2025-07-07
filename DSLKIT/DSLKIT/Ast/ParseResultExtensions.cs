using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    /// <summary>
    /// Extension helpers to create an AST from a <see cref="ParseResult"/>.
    /// </summary>
    public static class ParseResultExtensions
    {
        /// <summary>
        /// Builds an AST for the parse tree inside the result using the provided builder.
        /// </summary>
        /// <param name="result">The parse result containing the parse tree.</param>
        /// <param name="builder">The AST builder configured with node mappings.</param>
        /// <returns>AST build result.</returns>
        public static AstResult BuildAst(this ParseResult result, AstBuilder builder)
        {
            return builder.Build(result);
        }
    }
}
