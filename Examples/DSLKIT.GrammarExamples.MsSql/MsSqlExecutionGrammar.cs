using DSLKIT.NonTerminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlExecutionGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            INonTerminal executeStatement,
            INonTerminal executeModuleCall,
            INonTerminal executeModuleCallCore,
            INonTerminal executeWithOptions,
            INonTerminal executeDynamicCall,
            INonTerminal executeAsContext,
            INonTerminal executeAtClause,
            INonTerminal executeReturnAssignment,
            INonTerminal executeModuleTarget,
            INonTerminal executeArgList,
            INonTerminal executeArg,
            INonTerminal executeArgNamePrefix,
            INonTerminal executeArgValue,
            INonTerminal executeOptionList,
            INonTerminal executeOption,
            INonTerminal executeResultSetsDefList,
            INonTerminal executeResultSetsDef,
            INonTerminal executeColumnDefList,
            INonTerminal executeColumnDef,
            INonTerminal executeNullability,
            INonTerminal executeLinkedArgList,
            INonTerminal executeLinkedArg,
            INonTerminal useStatement)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var qualifiedName = context.Symbols.QualifiedName;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var typeSpec = context.Symbols.TypeSpec;
            var unicodeStringLiteral = context.Symbols.UnicodeStringLiteral;
            var variableReference = context.Symbols.VariableReference;
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

            gb.Prod(executeArgList).Is(executeArg);
            gb.Prod(executeArgList).Is(executeArgList, ",", executeArg);
            gb.Prod(executeArg).Is(executeArgValue);
            gb.Prod(executeArg).Is(executeArgNamePrefix, executeArgValue);
            gb.Prod(executeArgNamePrefix).Is(variableReference, "=");
            gb.Prod(executeArgValue).Is(expression);
            gb.Prod(executeArgValue).Is(variableReference, "OUTPUT");
            gb.Prod(executeArgValue).Is(variableReference, "OUT");
            gb.Prod(executeArgValue).Is("DEFAULT");

            gb.Prod(executeWithOptions).Is("WITH", executeOptionList);
            gb.Prod(executeOptionList).Is(executeOption);
            gb.Prod(executeOptionList).Is(executeOptionList, ",", executeOption);
            gb.Rule(executeOption)
                .CanBe("RECOMPILE")
                .Or("RESULT", "SETS", "UNDEFINED")
                .Or("RESULT", "SETS", "NONE")
                .Or("RESULT", "SETS", "(", executeResultSetsDefList, ")");
            gb.Prod(executeResultSetsDefList).Is(executeResultSetsDef);
            gb.Prod(executeResultSetsDefList).Is(executeResultSetsDefList, ",", executeResultSetsDef);
            gb.Prod(executeResultSetsDef).Is("(", executeColumnDefList, ")");
            gb.Prod(executeResultSetsDef).Is("AS", "OBJECT", qualifiedName);
            gb.Prod(executeResultSetsDef).Is("AS", "TYPE", qualifiedName);
            gb.Prod(executeResultSetsDef).Is("AS", "FOR", "XML");
            gb.Prod(executeColumnDefList).Is(executeColumnDef);
            gb.Prod(executeColumnDefList).Is(executeColumnDefList, ",", executeColumnDef);
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
