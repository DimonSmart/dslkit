using DSLKIT.NonTerminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlDmlGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            INonTerminal updateStatement,
            INonTerminal updateSetList,
            INonTerminal updateSetItem,
            INonTerminal compoundAssignOp,
            INonTerminal insertStatement,
            INonTerminal insertTarget,
            INonTerminal insertColumnList,
            INonTerminal insertValueList,
            INonTerminal rowValue,
            INonTerminal rowValueList,
            INonTerminal deleteStatement,
            INonTerminal deleteTopClause,
            INonTerminal deleteTarget,
            INonTerminal deleteTargetSimple,
            INonTerminal deleteTargetRowset,
            INonTerminal rowsetFunctionLimited,
            INonTerminal tableHintLimitedList,
            INonTerminal tableHintLimited,
            INonTerminal tableHintLimitedName,
            INonTerminal deleteStatementTail,
            INonTerminal deleteStatementTailNoOutput,
            INonTerminal deleteStatementTailNoFrom,
            INonTerminal deleteOptionOpt,
            INonTerminal deleteOutputClause,
            INonTerminal deleteOutputTarget,
            INonTerminal deleteOutputIntoColumnListOpt,
            INonTerminal deleteSourceFromClause,
            INonTerminal deleteWhereClause,
            INonTerminal deleteOptionClause,
            INonTerminal deleteQueryHintList,
            INonTerminal deleteQueryHint,
            INonTerminal deleteQueryHintName,
            INonTerminal optionClause,
            INonTerminal executeStatement)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var expressionList = context.Symbols.ExpressionList;
            var functionCall = context.Symbols.FunctionCall;
            var graphColumnRef = context.GraphColumnRefTerminal;
            var identifierList = context.Symbols.IdentifierList;
            var identifierTerm = context.Symbols.IdentifierTerm;
            var qualifiedName = context.Symbols.QualifiedName;
            var queryExpression = context.Symbols.QueryExpression;
            var searchCondition = context.Symbols.SearchCondition;
            var selectItemList = context.Symbols.SelectItemList;
            var tableFactor = context.Symbols.TableFactor;
            var tableSourceList = context.Symbols.TableSourceList;
            var variableReference = context.Symbols.VariableReference;

            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList);
            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList, "WHERE", searchCondition);
            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList, "FROM", tableSourceList);
            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList, "FROM", tableSourceList, "WHERE", searchCondition);
            gb.Prod(updateSetList).Is(updateSetItem);
            gb.Prod(updateSetList).Is(updateSetList, ",", updateSetItem);
            gb.Prod(updateSetItem).Is(qualifiedName, "=", expression);
            gb.Prod(updateSetItem).Is(qualifiedName, compoundAssignOp, expression);
            gb.Prod(updateSetItem).Is(variableReference, "=", expression);
            gb.Prod(updateSetItem).Is(variableReference, compoundAssignOp, expression);
            gb.Prod(updateSetItem).Is(functionCall);

            gb.Rule(compoundAssignOp)
                .CanBe("+=")
                .Or("-=")
                .Or("*=")
                .Or("/=")
                .Or("%=")
                .Or("&=")
                .Or("|=")
                .Or("^=");

            gb.Prod(insertStatement).Is("INSERT", insertTarget, "VALUES", rowValueList);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, executeStatement);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, queryExpression);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, deleteOutputClause, "VALUES", rowValueList);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, deleteOutputClause, queryExpression);
            gb.Prod(insertTarget).Is("INTO", qualifiedName);
            gb.Prod(insertTarget).Is(qualifiedName);
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(qualifiedName, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is("INTO", variableReference);
            gb.Prod(insertTarget).Is(variableReference);
            gb.Prod(insertTarget).Is("INTO", variableReference, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(variableReference, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "(", insertColumnList, ")", "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod(insertColumnList).Is(identifierTerm);
            gb.Prod(insertColumnList).Is(insertColumnList, ",", identifierTerm);
            if (context.HasFeature(MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod(insertColumnList).Is(graphColumnRef);
                gb.Prod(insertColumnList).Is(insertColumnList, ",", graphColumnRef);
            }

            gb.Prod(insertValueList).Is(expression);
            gb.Prod(insertValueList).Is(insertValueList, ",", expression);
            gb.Prod(rowValue).Is("(", insertValueList, ")");
            gb.Prod(rowValueList).Is(rowValue);
            gb.Prod(rowValueList).Is(rowValueList, ",", rowValue);

            gb.Prod(deleteStatement).Is("DELETE", deleteTarget, deleteStatementTail);
            gb.Prod(deleteStatement).Is("DELETE", "FROM", deleteTarget, deleteStatementTail);
            gb.Prod(deleteStatement).Is("DELETE", deleteTopClause, deleteTarget, deleteStatementTail);
            gb.Prod(deleteStatement).Is("DELETE", deleteTopClause, "FROM", deleteTarget, deleteStatementTail);

            gb.Prod(deleteTopClause).Is("TOP", "(", expression, ")");
            gb.Prod(deleteTopClause).Is("TOP", "(", expression, ")", "PERCENT");

            gb.Prod(deleteTarget).Is(deleteTargetSimple);
            gb.Prod(deleteTarget).Is(deleteTargetRowset);

            gb.Prod(deleteTargetSimple).Is(identifierTerm);
            gb.Prod(deleteTargetSimple).Is(qualifiedName);
            gb.Prod(deleteTargetSimple).Is(variableReference);
            gb.Prod(deleteTargetSimple).Is(identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(deleteTargetSimple).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");

            gb.Prod(deleteTargetRowset).Is(rowsetFunctionLimited);
            gb.Prod(deleteTargetRowset).Is(rowsetFunctionLimited, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(rowsetFunctionLimited).Is("OPENQUERY", "(", expressionList, ")");
            gb.Prod(rowsetFunctionLimited).Is("OPENROWSET", "(", expressionList, ")");

            gb.Prod(tableHintLimitedList).Is(tableHintLimited);
            gb.Prod(tableHintLimitedList).Is(tableHintLimitedList, ",", tableHintLimited);
            gb.Rule(tableHintLimited)
                .CanBe(tableHintLimitedName)
                .Or(tableHintLimitedName, "=", expression)
                .Or(tableHintLimitedName, "(", expressionList, ")")
                .Or(qualifiedName)
                .Or(qualifiedName, "=", expression)
                .Or(qualifiedName, "(", expressionList, ")");
            gb.Rule(tableHintLimitedName)
                .CanBe(identifierTerm)
                .OrKeywords("INDEX");

            gb.Prod(deleteStatementTail).Is(deleteOutputClause, deleteStatementTailNoOutput);
            gb.Prod(deleteStatementTail).Is(deleteStatementTailNoOutput);
            gb.Prod(deleteStatementTailNoOutput).Is(deleteSourceFromClause, deleteStatementTailNoFrom);
            gb.Prod(deleteStatementTailNoOutput).Is(deleteStatementTailNoFrom);
            gb.Prod(deleteStatementTailNoFrom).Is(deleteWhereClause, deleteOptionOpt);
            gb.Prod(deleteStatementTailNoFrom).Is(deleteOptionOpt);
            gb.Opt(deleteOptionOpt, deleteOptionClause);

            gb.Prod(deleteOutputClause).Is("OUTPUT", selectItemList);
            gb.Prod(deleteOutputClause).Is("OUTPUT", selectItemList, "INTO", deleteOutputTarget, deleteOutputIntoColumnListOpt);
            gb.Prod(deleteOutputTarget).Is(qualifiedName);
            gb.Prod(deleteOutputTarget).Is(variableReference);
            gb.Opt(deleteOutputIntoColumnListOpt, "(", identifierList, ")");

            gb.Prod(deleteSourceFromClause).Is("FROM", tableSourceList);

            gb.Rule(deleteWhereClause)
                .CanBe("WHERE", searchCondition)
                .Or("WHERE", "CURRENT", "OF", identifierTerm)
                .Or("WHERE", "CURRENT", "OF", variableReference)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", identifierTerm)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", variableReference);

            gb.Prod(deleteOptionClause).Is("OPTION", "(", deleteQueryHintList, ")");
            gb.Prod(deleteQueryHintList).Is(deleteQueryHint);
            gb.Prod(deleteQueryHintList).Is(deleteQueryHintList, ",", deleteQueryHint);
            gb.Prod(deleteQueryHint).Is(deleteQueryHintName);
            gb.Prod(deleteQueryHint).Is("MAXDOP", expression);
            gb.Prod(deleteQueryHint).Is("MAXDOP", "=", expression);
            gb.Prod(deleteQueryHint).Is("MAXRECURSION", expression);
            gb.Prod(deleteQueryHint).Is("MAXRECURSION", "=", expression);
            gb.Prod(deleteQueryHint).Is("QUERYTRACEON", expression);
            gb.Prod(deleteQueryHint).Is("MIN_GRANT_PERCENT", "=", expression);
            gb.Prod(deleteQueryHint).Is("MAX_GRANT_PERCENT", "=", expression);
            gb.Prod(deleteQueryHint).Is("LABEL", "=", expression);
            gb.Prod(deleteQueryHint).Is("USE", "HINT", "(", expressionList, ")");
            gb.Prod(deleteQueryHint).Is("HASH", "JOIN");
            gb.Prod(deleteQueryHint).Is("MERGE", "JOIN");
            gb.Prod(deleteQueryHint).Is("LOOP", "JOIN");
            gb.Prod(deleteQueryHint).Is("HASH", "GROUP");
            gb.Prod(deleteQueryHint).Is("ORDER", "GROUP");
            gb.Prod(deleteQueryHint).Is("MERGE", "UNION");
            gb.Prod(deleteQueryHint).Is("HASH", "UNION");
            gb.Prod(deleteQueryHint).Is("CONCAT", "UNION");
            gb.Prod(deleteQueryHint).Is("FORCE", "ORDER");
            gb.Prod(deleteQueryHint).Is("KEEP", "PLAN");
            gb.Prod(deleteQueryHint).Is("KEEPFIXED", "PLAN");
            gb.Prod(deleteQueryHint).Is("ROBUST", "PLAN");
            gb.Rule(deleteQueryHintName)
                .Keywords("RECOMPILE", "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX");

            gb.Prod(optionClause).Is("OPTION", "(", deleteQueryHintList, ")");
        }
    }
}
