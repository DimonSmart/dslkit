namespace DSLKIT.Ast
{
    using DSLKIT.Parser;

    /// <summary>
    /// Result of AST building.
    /// </summary>
    public class AstResult
    {
        /// <summary>
        /// Indicates whether the AST was built successfully.
        /// </summary>
        public bool IsSuccess => Error == null;

        /// <summary>
        /// Description of an error that occurred during AST construction.
        /// </summary>
        public ParseErrorDescription Error { get; set; }

        /// <summary>
        /// Root of the constructed AST.
        /// </summary>
        public AstNode Root { get; set; }
    }
}
