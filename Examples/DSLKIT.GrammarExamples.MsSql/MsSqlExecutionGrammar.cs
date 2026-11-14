
namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlExecutionGrammar
    {
        public static void Build(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var executeStatement = gb.NT("ExecuteStatement");
            var executeModuleCall = gb.NT("ExecuteModuleCall");
            var executeModuleCallCore = gb.NT("ExecuteModuleCallCore");
            var executeWithOptions = gb.NT("ExecuteWithOptions");
            var executeDynamicCall = gb.NT("ExecuteDynamicCall");
            var executeAsContext = gb.NT("ExecuteAsContext");
            var executeAtClause = gb.NT("ExecuteAtClause");
            var executeReturnAssignment = gb.NT("ExecuteReturnAssignment");
            var executeModuleTarget = gb.NT("ExecuteModuleTarget");
            var executeArgList = gb.NT("ExecuteArgList");
            var executeArg = gb.NT("ExecuteArg");
            var executeArgNamePrefix = gb.NT("ExecuteArgNamePrefix");
            var executeArgValue = gb.NT("ExecuteArgValue");
            var executeOptionList = gb.NT("ExecuteOptionList");
            var executeOption = gb.NT("ExecuteOption");
            var executeResultSetsDefList = gb.NT("ExecuteResultSetsDefList");
            var executeResultSetsDef = gb.NT("ExecuteResultSetsDef");
            var executeColumnDefList = gb.NT("ExecuteColumnDefList");
            var executeColumnDef = gb.NT("ExecuteColumnDef");
            var executeNullability = gb.NT("ExecuteNullability");
            var executeLinkedArgList = gb.NT("ExecuteLinkedArgList");
            var executeLinkedArg = gb.NT("ExecuteLinkedArg");
            var useStatement = gb.NT("UseStatement");
            var expression = gb.NT("Expression");
            var qualifiedName = gb.NT("QualifiedName");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var typeSpec = gb.NT("TypeSpec");
            var unicodeStringLiteral = gb.NT("UnicodeStringLiteral");
            var variableReference = gb.NT("VariableReference");
            var stringLiteral = context.StringLiteralTerminal;
            var executeIdentifierTerm = gb.NT("ExecuteIdentifierTerm");

            gb.Rule(executeIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME");

            gb.Rule(executeStatement)
                .CanBe("EXEC", executeModuleCall)
                .Or("EXECUTE", executeModuleCall)
                .Or("EXEC", executeDynamicCall)
                .Or("EXECUTE", executeDynamicCall)
                .Or("EXEC", executeAsContext)
                .Or("EXECUTE", executeAsContext);

            gb.Prod(executeModuleCall).Is(executeModuleCallCore);
            gb.Prod(executeModuleCall).Is(executeModuleCallCore, executeWithOptions);
            gb.Prod(executeModuleCallCore).Is(executeModuleTarget);
            gb.Prod(executeModuleCallCore).Is(executeReturnAssignment, executeModuleTarget);
            gb.Prod(executeModuleCallCore).Is(executeModuleTarget, executeArgList);
            gb.Prod(executeModuleCallCore).Is(executeReturnAssignment, executeModuleTarget, executeArgList);
            gb.Prod(executeReturnAssignment).Is(variableReference, "=");
            gb.Prod(executeModuleTarget).Is(qualifiedName);
            gb.Prod(executeModuleTarget).Is(variableReference);

            gb.Rule(executeArgList).SeparatedBy(",", executeArg);
            gb.Prod(executeArg).Is(executeArgValue);
            gb.Prod(executeArg).Is(executeArgNamePrefix, executeArgValue);
            gb.Prod(executeArgNamePrefix).Is(variableReference, "=");
            gb.Prod(executeArgValue).Is(expression);
            gb.Prod(executeArgValue).Is(variableReference, "OUTPUT");
            gb.Prod(executeArgValue).Is(variableReference, "OUT");
            gb.Prod(executeArgValue).Is("DEFAULT");

            gb.Prod(executeWithOptions).Is("WITH", executeOptionList);
            gb.Rule(executeOptionList).SeparatedBy(",", executeOption);
            gb.Rule(executeOption)
                .CanBe("RECOMPILE")
                .Or("RESULT", "SETS", "UNDEFINED")
                .Or("RESULT", "SETS", "NONE")
                .Or("RESULT", "SETS", "(", executeResultSetsDefList, ")");
            gb.Rule(executeResultSetsDefList).SeparatedBy(",", executeResultSetsDef);
            gb.Prod(executeResultSetsDef).Is("(", executeColumnDefList, ")");
            gb.Prod(executeResultSetsDef).Is("AS", "OBJECT", qualifiedName);
            gb.Prod(executeResultSetsDef).Is("AS", "TYPE", qualifiedName);
            gb.Prod(executeResultSetsDef).Is("AS", "FOR", "XML");
            gb.Rule(executeColumnDefList).SeparatedBy(",", executeColumnDef);
            gb.Prod(executeColumnDef).Is(executeIdentifierTerm, typeSpec);
            gb.Prod(executeColumnDef).Is(executeIdentifierTerm, typeSpec, "COLLATE", strictIdentifierTerm);
            gb.Prod(executeColumnDef).Is(executeIdentifierTerm, typeSpec, executeNullability);
            gb.Prod(executeColumnDef).Is(executeIdentifierTerm, typeSpec, "COLLATE", strictIdentifierTerm, executeNullability);
            gb.Rule(executeNullability)
                .CanBe("NULL")
                .Or("NOT", "NULL");

            gb.Prod(executeDynamicCall).Is("(", expression, ")");
            gb.Prod(executeDynamicCall).Is("(", expression, ")", executeAsContext);
            gb.Prod(executeDynamicCall).Is("(", expression, ")", executeAtClause);
            gb.Prod(executeDynamicCall).Is("(", expression, ")", executeAsContext, executeAtClause);
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")");
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")", executeAsContext);
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")", executeAtClause);
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")", executeAsContext, executeAtClause);
            gb.Prod(executeLinkedArgList).Is(",", executeLinkedArg);
            gb.Prod(executeLinkedArgList).Is(executeLinkedArgList, ",", executeLinkedArg);
            gb.Prod(executeLinkedArg).Is(expression);
            gb.Prod(executeLinkedArg).Is(variableReference, "OUTPUT");
            gb.Prod(executeLinkedArg).Is(variableReference, "OUT");
            gb.Rule(executeAsContext)
                .CanBe("AS", "LOGIN", "=", stringLiteral)
                .Or("AS", "USER", "=", stringLiteral)
                .Or("AS", "LOGIN", "=", unicodeStringLiteral)
                .Or("AS", "USER", "=", unicodeStringLiteral)
                .Or("AS", "LOGIN", "=", strictIdentifierTerm)
                .Or("AS", "USER", "=", strictIdentifierTerm);
            gb.Prod(executeAtClause).Is("AT", executeIdentifierTerm);
            gb.Prod(executeAtClause).Is("AT", "DATA_SOURCE", executeIdentifierTerm);

            gb.Prod(useStatement).Is("USE", strictIdentifierTerm);
        }
    }
}
