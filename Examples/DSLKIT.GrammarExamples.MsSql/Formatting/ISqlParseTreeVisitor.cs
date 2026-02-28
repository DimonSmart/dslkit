using DSLKIT.Parser;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    public interface ISqlParseTreeVisitor
    {
        void Visit(ParseTreeNode node);
    }
}
