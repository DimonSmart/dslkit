
namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlDmlGrammar
    {
        public static void Build(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var updateStatement = gb.NT("UpdateStatement");
            var updateSetList = gb.NT("UpdateSetList");
            var updateSetItem = gb.NT("UpdateSetItem");
            var compoundAssignOp = gb.NT("CompoundAssignOp");
            var insertStatement = gb.NT("InsertStatement");
            var insertTarget = gb.NT("InsertTarget");
            var insertColumnList = gb.NT("InsertColumnList");
            var insertValueList = gb.NT("InsertValueList");
            var rowValue = gb.NT("RowValue");
            var rowValueList = gb.NT("RowValueList");
            var deleteStatement = gb.NT("DeleteStatement");
            var deleteTopClause = gb.NT("DeleteTopClause");
            var deleteTarget = gb.NT("DeleteTarget");
            var deleteTargetSimple = gb.NT("DeleteTargetSimple");
            var deleteTargetRowset = gb.NT("DeleteTargetRowset");
            var rowsetFunctionLimited = gb.NT("RowsetFunctionLimited");
            var tableHintLimitedList = gb.NT("TableHintLimitedList");
            var tableHintLimited = gb.NT("TableHintLimited");
            var tableHintLimitedName = gb.NT("TableHintLimitedName");
            var deleteStatementTail = gb.NT("DeleteStatementTail");
            var deleteStatementTailNoOutput = gb.NT("DeleteStatementTailNoOutput");
            var deleteStatementTailNoFrom = gb.NT("DeleteStatementTailNoFrom");
            var deleteOptionOpt = gb.NT("DeleteOptionOpt");
            var dmlOutputClause = gb.NT("DeleteOutputClause");
            var dmlOutputTarget = gb.NT("DeleteOutputTarget");
            var dmlOutputIntoColumnListOpt = gb.NT("DeleteOutputIntoColumnListOpt");
            var deleteSourceFromClause = gb.NT("DeleteSourceFromClause");
            var deleteWhereClause = gb.NT("DeleteWhereClause");
            var queryOptionClause = gb.NT("DeleteOptionClause");
            var queryHintList = gb.NT("DeleteQueryHintList");
            var queryHint = gb.NT("DeleteQueryHint");
            var optionClause = gb.NT("OptionClause");
            var executeStatement = gb.NT("ExecuteStatement");
            var expression = gb.NT("Expression");
            var expressionList = gb.NT("ExpressionList");
            var functionCall = gb.NT("FunctionCall");
            var graphColumnRef = context.GraphColumnRefTerminal;
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var qualifiedName = gb.NT("QualifiedName");
            var queryExpression = gb.NT("QueryExpression");
            var searchCondition = gb.NT("SearchCondition");
            var selectItemList = gb.NT("SelectItemList");
            var tableFactor = gb.NT("TableFactor");
            var tableSourceList = gb.NT("TableSourceList");
            var variableReference = gb.NT("VariableReference");
            var dmlIdentifierTerm = gb.NT("DmlIdentifierTerm");
            var dmlIdentifierList = gb.NT("DmlIdentifierList");
            var dmlObjectIdentifierTerm = gb.NT("DmlObjectIdentifierTerm");
            var dmlQualifiedName = gb.NT("DmlQualifiedName");
            var dmlTableHintQualifiedName = gb.NT("DmlTableHintQualifiedName");

            gb.Rule(dmlIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME", "SOURCE", "TARGET");
            gb.Rule(dmlObjectIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("POLICY");
            gb.Rule(dmlIdentifierList).SeparatedBy(",", dmlIdentifierTerm);
            gb.Rule(dmlQualifiedName).SeparatedBy(".", dmlObjectIdentifierTerm);
            gb.Prod(dmlQualifiedName).Is(dmlQualifiedName, ".", ".", dmlObjectIdentifierTerm);
            gb.Rule(dmlTableHintQualifiedName).SeparatedBy(".", tableHintLimitedName);
            gb.Prod(dmlTableHintQualifiedName).Is(dmlTableHintQualifiedName, ".", ".", tableHintLimitedName);

            gb.Rule(updateStatement).CanBe(
                "UPDATE", tableFactor, "SET", updateSetList,
                gb.Optional("FROM", tableSourceList),
                gb.Optional("WHERE", searchCondition));
            gb.Rule(updateSetList).SeparatedBy(",", updateSetItem);
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
            gb.Rule(insertColumnList).SeparatedBy(",", dmlIdentifierTerm);
            if (context.HasFeature(MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Rule(insertColumnList).SeparatedBy(",", graphColumnRef);
            }

            gb.Rule(insertValueList).SeparatedBy(",", expression);
            gb.Prod(rowValue).Is("(", insertValueList, ")");
            gb.Rule(rowValueList).SeparatedBy(",", rowValue);

            gb.Prod(deleteStatement).Is("DELETE", deleteTarget, deleteStatementTail);
            gb.Prod(deleteStatement).Is("DELETE", "FROM", deleteTarget, deleteStatementTail);
            gb.Prod(deleteStatement).Is("DELETE", deleteTopClause, deleteTarget, deleteStatementTail);
            gb.Prod(deleteStatement).Is("DELETE", deleteTopClause, "FROM", deleteTarget, deleteStatementTail);

            gb.Prod(deleteTopClause).Is("TOP", "(", expression, ")");
            gb.Prod(deleteTopClause).Is("TOP", "(", expression, ")", "PERCENT");

            gb.Prod(deleteTarget).Is(deleteTargetSimple);
            gb.Prod(deleteTarget).Is(deleteTargetRowset);

            // Ordinary identifiers and POLICY use DmlQualifiedName, including one-part names.
            // Keep NAME/SOURCE/TARGET as distinct DELETE aliases without a second StrictIdentifierTerm path.
            gb.Prod(deleteTargetSimple).Is("NAME");
            gb.Prod(deleteTargetSimple).Is("SOURCE");
            gb.Prod(deleteTargetSimple).Is("TARGET");
            gb.Prod(deleteTargetSimple).Is(dmlQualifiedName);
            gb.Prod(deleteTargetSimple).Is(variableReference);
            gb.Prod(deleteTargetSimple).Is("NAME", "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(deleteTargetSimple).Is("SOURCE", "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(deleteTargetSimple).Is("TARGET", "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(deleteTargetSimple).Is(dmlQualifiedName, "WITH", "(", tableHintLimitedList, ")");

            gb.Prod(deleteTargetRowset).Is(rowsetFunctionLimited);
            gb.Prod(deleteTargetRowset).Is(rowsetFunctionLimited, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(rowsetFunctionLimited).Is("OPENQUERY", "(", expressionList, ")");
            gb.Prod(rowsetFunctionLimited).Is("OPENROWSET", "(", expressionList, ")");

            gb.Rule(tableHintLimitedList).SeparatedBy(",", tableHintLimited);
            gb.Rule(tableHintLimited)
                .CanBe(tableHintLimitedName)
                .Or(tableHintLimitedName, "=", expression)
                .Or(tableHintLimitedName, "(", expressionList, ")")
                .Or(dmlTableHintQualifiedName)
                .Or(dmlTableHintQualifiedName, "=", expression)
                .Or(dmlTableHintQualifiedName, "(", expressionList, ")");
            gb.Rule(tableHintLimitedName).Keywords(
                "HOLDLOCK",
                "NOLOCK",
                "READUNCOMMITTED",
                "UPDLOCK",
                "REPEATABLEREAD",
                "SERIALIZABLE",
                "READCOMMITTED",
                "READCOMMITTEDLOCK",
                "TABLOCK",
                "TABLOCKX",
                "PAGLOCK",
                "ROWLOCK",
                "NOWAIT",
                "READPAST",
                "XLOCK",
                "SNAPSHOT",
                "NOEXPAND",
                "INDEX",
                "FORCESEEK",
                "FORCESCAN");

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
            gb.Rule(queryHintList).SeparatedBy(",", queryHint);
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
            gb.Rule(queryHint)
                .Keywords("RECOMPILE", "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX");

            gb.Prod(optionClause).Is("OPTION", "(", queryHintList, ")");
        }
    }
}
