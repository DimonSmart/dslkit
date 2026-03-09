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
            INonTerminal dmlOutputClause,
            INonTerminal dmlOutputTarget,
            INonTerminal dmlOutputIntoColumnListOpt,
            INonTerminal deleteSourceFromClause,
            INonTerminal deleteWhereClause,
            INonTerminal queryOptionClause,
            INonTerminal queryHintList,
            INonTerminal queryHint,
            INonTerminal queryHintName,
            INonTerminal optionClause,
            INonTerminal executeStatement)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var expressionList = context.Symbols.ExpressionList;
            var functionCall = context.Symbols.FunctionCall;
            var graphColumnRef = context.GraphColumnRefTerminal;
            var identifierTerm = context.Symbols.IdentifierTerm;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var qualifiedName = context.Symbols.QualifiedName;
            var queryExpression = context.Symbols.QueryExpression;
            var searchCondition = context.Symbols.SearchCondition;
            var selectItemList = context.Symbols.SelectItemList;
            var tableFactor = context.Symbols.TableFactor;
            var tableSourceList = context.Symbols.TableSourceList;
            var variableReference = context.Symbols.VariableReference;
            var dmlIdentifierTerm = gb.NT("DmlIdentifierTerm");
            var dmlIdentifierList = gb.NT("DmlIdentifierList");
            var dmlObjectIdentifierTerm = gb.NT("DmlObjectIdentifierTerm");
            var dmlQualifiedName = gb.NT("DmlQualifiedName");

            gb.Rule(dmlIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME", "SOURCE", "TARGET");
            gb.Rule(dmlObjectIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("POLICY");
            gb.Prod(dmlIdentifierList).Is(dmlIdentifierTerm);
            gb.Prod(dmlIdentifierList).Is(dmlIdentifierList, ",", dmlIdentifierTerm);
            gb.Prod(dmlQualifiedName).Is(dmlObjectIdentifierTerm);
            gb.Prod(dmlQualifiedName).Is(dmlQualifiedName, ".", dmlObjectIdentifierTerm);
            gb.Prod(dmlQualifiedName).Is(dmlQualifiedName, ".", ".", dmlObjectIdentifierTerm);

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
            gb.Prod(insertStatement).Is("INSERT", insertTarget, dmlOutputClause, "VALUES", rowValueList);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, dmlOutputClause, queryExpression);
            gb.Prod(insertTarget).Is("INTO", dmlQualifiedName);
            gb.Prod(insertTarget).Is(dmlQualifiedName);
            gb.Prod(insertTarget).Is("INTO", dmlQualifiedName, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(dmlQualifiedName, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is("INTO", variableReference);
            gb.Prod(insertTarget).Is(variableReference);
            gb.Prod(insertTarget).Is("INTO", variableReference, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(variableReference, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is("INTO", dmlQualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is(dmlQualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is("INTO", dmlQualifiedName, "(", insertColumnList, ")", "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is("INTO", dmlQualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(dmlQualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod(insertColumnList).Is(dmlIdentifierTerm);
            gb.Prod(insertColumnList).Is(insertColumnList, ",", dmlIdentifierTerm);
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

            gb.Prod(deleteTargetSimple).Is(dmlIdentifierTerm);
            gb.Prod(deleteTargetSimple).Is(dmlQualifiedName);
            gb.Prod(deleteTargetSimple).Is(variableReference);
            gb.Prod(deleteTargetSimple).Is(dmlIdentifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(deleteTargetSimple).Is(dmlQualifiedName, "WITH", "(", tableHintLimitedList, ")");

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

            gb.Prod(deleteStatementTail).Is(dmlOutputClause, deleteStatementTailNoOutput);
            gb.Prod(deleteStatementTail).Is(deleteStatementTailNoOutput);
            gb.Prod(deleteStatementTailNoOutput).Is(deleteSourceFromClause, deleteStatementTailNoFrom);
            gb.Prod(deleteStatementTailNoOutput).Is(deleteStatementTailNoFrom);
            gb.Prod(deleteStatementTailNoFrom).Is(deleteWhereClause, deleteOptionOpt);
            gb.Prod(deleteStatementTailNoFrom).Is(deleteOptionOpt);
            gb.Opt(deleteOptionOpt, queryOptionClause);

            gb.Prod(dmlOutputClause).Is("OUTPUT", selectItemList);
            gb.Prod(dmlOutputClause).Is("OUTPUT", selectItemList, "INTO", dmlOutputTarget, dmlOutputIntoColumnListOpt);
            gb.Prod(dmlOutputTarget).Is(dmlQualifiedName);
            gb.Prod(dmlOutputTarget).Is(variableReference);
            gb.Opt(dmlOutputIntoColumnListOpt, "(", dmlIdentifierList, ")");

            gb.Prod(deleteSourceFromClause).Is("FROM", tableSourceList);

            gb.Rule(deleteWhereClause)
                .CanBe("WHERE", searchCondition)
                .Or("WHERE", "CURRENT", "OF", dmlIdentifierTerm)
                .Or("WHERE", "CURRENT", "OF", variableReference)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", dmlIdentifierTerm)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", variableReference);

            gb.Prod(queryOptionClause).Is("OPTION", "(", queryHintList, ")");
            gb.Prod(queryHintList).Is(queryHint);
            gb.Prod(queryHintList).Is(queryHintList, ",", queryHint);
            gb.Prod(queryHint).Is(queryHintName);
            gb.Prod(queryHint).Is("MAXDOP", expression);
            gb.Prod(queryHint).Is("MAXDOP", "=", expression);
            gb.Prod(queryHint).Is("MAXRECURSION", expression);
            gb.Prod(queryHint).Is("MAXRECURSION", "=", expression);
            gb.Prod(queryHint).Is("QUERYTRACEON", expression);
            gb.Prod(queryHint).Is("MIN_GRANT_PERCENT", "=", expression);
            gb.Prod(queryHint).Is("MAX_GRANT_PERCENT", "=", expression);
            gb.Prod(queryHint).Is("LABEL", "=", expression);
            gb.Prod(queryHint).Is("USE", "HINT", "(", expressionList, ")");
            gb.Prod(queryHint).Is("HASH", "JOIN");
            gb.Prod(queryHint).Is("MERGE", "JOIN");
            gb.Prod(queryHint).Is("LOOP", "JOIN");
            gb.Prod(queryHint).Is("HASH", "GROUP");
            gb.Prod(queryHint).Is("ORDER", "GROUP");
            gb.Prod(queryHint).Is("MERGE", "UNION");
            gb.Prod(queryHint).Is("HASH", "UNION");
            gb.Prod(queryHint).Is("CONCAT", "UNION");
            gb.Prod(queryHint).Is("FORCE", "ORDER");
            gb.Prod(queryHint).Is("KEEP", "PLAN");
            gb.Prod(queryHint).Is("KEEPFIXED", "PLAN");
            gb.Prod(queryHint).Is("ROBUST", "PLAN");
            gb.Rule(queryHintName)
                .Keywords("RECOMPILE", "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX");

            gb.Prod(optionClause).Is("OPTION", "(", queryHintList, ")");
        }
    }
}
