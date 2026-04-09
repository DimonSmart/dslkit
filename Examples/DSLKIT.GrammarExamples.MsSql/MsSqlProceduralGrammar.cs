using DSLKIT.NonTerminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlProceduralGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            INonTerminal ifBranchStatement,
            INonTerminal statement,
            INonTerminal statementListOpt,
            INonTerminal ifStatement,
            INonTerminal beginEndStatement,
            INonTerminal whileStatement,
            INonTerminal setOptionName,
            INonTerminal setStatisticsOption,
            INonTerminal setStatement,
            INonTerminal setTransactionIsolationLevel,
            INonTerminal printStatement,
            INonTerminal returnStatement,
            INonTerminal transactionStatement,
            INonTerminal raiserrorStatement,
            INonTerminal raiserrorArgList,
            INonTerminal raiserrorWithOptionList,
            INonTerminal raiserrorWithOption,
            INonTerminal throwStatement,
            INonTerminal loopControlStatement,
            INonTerminal gotoStatement,
            INonTerminal labelOnlyStatement,
            INonTerminal labelStatement,
            INonTerminal declareStatement,
            INonTerminal declareItemList,
            INonTerminal declareItem,
            INonTerminal declareTableVariable,
            INonTerminal tableTypeDefinition,
            INonTerminal createTableElementList,
            INonTerminal createTableOptions,
            INonTerminal typeArgument,
            INonTerminal procStatementList,
            INonTerminal statementSeparatorList,
            INonTerminal implicitStatementNoLeadingWith,
            INonTerminal tryCatchStatement,
            INonTerminal cursorReference,
            INonTerminal declareCursorStatement,
            INonTerminal cursorOptionList,
            INonTerminal cursorOption,
            INonTerminal cursorOperationStatement,
            INonTerminal fetchStatement,
            INonTerminal fetchDirection,
            INonTerminal fetchTargetList,
            INonTerminal waitforTimeValue,
            INonTerminal waitforStatement)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var qualifiedName = context.Symbols.QualifiedName;
            var queryExpression = context.Symbols.QueryExpression;
            var searchCondition = context.Symbols.SearchCondition;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var typeSpec = context.Symbols.TypeSpec;
            var unicodeStringLiteral = context.Symbols.UnicodeStringLiteral;
            var variableReference = context.Symbols.VariableReference;
            var compoundAssignOp = context.Symbols.CompoundAssignOp;
            var stringLiteral = context.StringLiteralTerminal;

            gb.Prod(ifBranchStatement).Is(statement);
            gb.Prod(ifBranchStatement).Is(statement, ";");
            gb.Prod(ifStatement).Is("IF", searchCondition, ifBranchStatement);
            gb.Prod(ifStatement).Is("IF", searchCondition, ifBranchStatement, "ELSE", ifBranchStatement);
            gb.Prod(ifStatement).Is("IF", searchCondition, ifBranchStatement, "ELSE", ifStatement);
            gb.Prod(beginEndStatement).Is("BEGIN", statementListOpt, "END");
            gb.Prod(whileStatement).Is("WHILE", searchCondition, statement);

            gb.Rule(setOptionName).Keywords(
                "ANSI_DEFAULTS",
                "ANSI_NULL_DFLT_OFF",
                "ANSI_NULL_DFLT_ON",
                "ANSI_NULLS",
                "ANSI_PADDING",
                "ANSI_WARNINGS",
                "ARITHABORT",
                "ARITHIGNORE",
                "CONCAT_NULL_YIELDS_NULL",
                "CURSOR_CLOSE_ON_COMMIT",
                "DATEFIRST",
                "DATEFORMAT",
                "DEADLOCK_PRIORITY",
                "FMTONLY",
                "FORCEPLAN",
                "IMPLICIT_TRANSACTIONS",
                "LANGUAGE",
                "LOCK_TIMEOUT",
                "NOCOUNT",
                "NOEXEC",
                "NUMERIC_ROUNDABORT",
                "PARSEONLY",
                "QUERY_GOVERNOR_COST_LIMIT",
                "QUOTED_IDENTIFIER",
                "REMOTE_PROC_TRANSACTIONS",
                "ROWCOUNT",
                "SHOWPLAN_ALL",
                "SHOWPLAN_TEXT",
                "SHOWPLAN_XML",
                "TEXTSIZE",
                "XACT_ABORT");
            gb.Rule(setStatisticsOption)
                .CanBe("STATISTICS", "IO")
                .Or("STATISTICS", "TIME")
                .Or("STATISTICS", "XML")
                .Or("STATISTICS", "PROFILE");
            gb.Prod(setStatement).Is("SET", variableReference, "=", expression);
            gb.Prod(setStatement).Is("SET", variableReference, compoundAssignOp, expression);
            gb.Prod(setStatement).Is("SET", setOptionName, "ON");
            gb.Prod(setStatement).Is("SET", setOptionName, "OFF");
            gb.Prod(setStatement).Is("SET", setOptionName, "=", expression);
            gb.Prod(setStatement).Is("SET", setOptionName, expression);
            gb.Prod(setStatement).Is("SET", setStatisticsOption, "ON");
            gb.Prod(setStatement).Is("SET", setStatisticsOption, "OFF");
            gb.Prod(setStatement).Is("SET", "IDENTITY_INSERT", qualifiedName, "ON");
            gb.Prod(setStatement).Is("SET", "IDENTITY_INSERT", qualifiedName, "OFF");
            gb.Prod(setStatement).Is("SET", "TRANSACTION", "ISOLATION", "LEVEL", setTransactionIsolationLevel);

            gb.Rule(setTransactionIsolationLevel)
                .CanBe("READ", "UNCOMMITTED")
                .Or("READ", "COMMITTED")
                .Or("REPEATABLE", "READ")
                .Or("SNAPSHOT")
                .Or("SERIALIZABLE");

            gb.Prod(printStatement).Is("PRINT", expression);

            gb.Rule(returnStatement)
                .CanBe("RETURN")
                .Or("RETURN", expression);

            gb.Rule(transactionStatement)
                .CanBe("BEGIN", "TRAN")
                .Or("BEGIN", "TRANSACTION")
                .Or("BEGIN", "TRAN", strictIdentifierTerm)
                .Or("BEGIN", "TRANSACTION", strictIdentifierTerm)
                .Or("SAVE", "TRAN", strictIdentifierTerm)
                .Or("SAVE", "TRANSACTION", strictIdentifierTerm)
                .Or("COMMIT")
                .Or("COMMIT", "TRAN")
                .Or("COMMIT", "TRANSACTION")
                .Or("COMMIT", "TRAN", strictIdentifierTerm)
                .Or("COMMIT", "TRANSACTION", strictIdentifierTerm)
                .Or("ROLLBACK")
                .Or("ROLLBACK", "TRAN")
                .Or("ROLLBACK", "TRANSACTION")
                .Or("ROLLBACK", "TRAN", strictIdentifierTerm)
                .Or("ROLLBACK", "TRANSACTION", strictIdentifierTerm);

            gb.Prod(raiserrorStatement).Is("RAISERROR", "(", raiserrorArgList, ")");
            gb.Prod(raiserrorStatement).Is("RAISERROR", "(", raiserrorArgList, ")", "WITH", raiserrorWithOptionList);
            gb.Prod(raiserrorArgList).Is(expression);
            gb.Prod(raiserrorArgList).Is(raiserrorArgList, ",", expression);
            gb.Prod(raiserrorWithOptionList).Is(raiserrorWithOption);
            gb.Prod(raiserrorWithOptionList).Is(raiserrorWithOptionList, ",", raiserrorWithOption);
            gb.Rule(raiserrorWithOption).Keywords("LOG", "NOWAIT", "SETERROR");

            gb.Rule(throwStatement)
                .CanBe("THROW")
                .Or("THROW", expression, ",", expression, ",", expression);
            gb.Rule(loopControlStatement)
                .CanBe("BREAK")
                .OrKeywords("CONTINUE");
            gb.Prod(gotoStatement).Is("GOTO", strictIdentifierTerm);
            gb.Prod(labelOnlyStatement).Is(strictIdentifierTerm, ":");
            gb.Prod(labelStatement).Is(labelOnlyStatement);

            gb.Prod(declareStatement).Is("DECLARE", declareItemList);
            gb.Prod(declareStatement).Is("DECLARE", declareTableVariable);
            gb.Prod(declareItemList).Is(declareItem);
            gb.Prod(declareItemList).Is(declareItemList, ",", declareItem);
            gb.Prod(declareItem).Is(variableReference, typeSpec);
            gb.Prod(declareItem).Is(variableReference, typeSpec, "=", expression);
            gb.Prod(declareItem).Is(variableReference, typeSpec, "NOT", "NULL");
            gb.Prod(declareItem).Is(variableReference, typeSpec, "NOT", "NULL", "=", expression);
            gb.Prod(declareItem).Is(variableReference, typeSpec, "NULL");
            gb.Prod(declareItem).Is(variableReference, typeSpec, "NULL", "=", expression);
            gb.Prod(declareItem).Is(variableReference, "AS", typeSpec);
            gb.Prod(declareItem).Is(variableReference, "AS", typeSpec, "=", expression);
            gb.Prod(declareItem).Is(variableReference, "AS", typeSpec, "NOT", "NULL");
            gb.Prod(declareItem).Is(variableReference, "AS", typeSpec, "NOT", "NULL", "=", expression);
            gb.Prod(declareTableVariable).Is(variableReference, tableTypeDefinition);
            gb.Prod(declareTableVariable).Is(variableReference, "AS", tableTypeDefinition);
            gb.Prod(tableTypeDefinition).Is("TABLE", "(", createTableElementList, ")");
            gb.Prod(tableTypeDefinition).Is("TABLE", "(", createTableElementList, ")", createTableOptions);
            gb.Rule(typeArgument)
                .CanBe(expression)
                .OrKeywords("MAX");
            gb.Prod(typeSpec).Is(qualifiedName);
            gb.Prod(typeSpec).Is(qualifiedName, "(", typeArgument, ")");
            gb.Prod(typeSpec).Is(qualifiedName, "(", typeArgument, ",", typeArgument, ")");

            gb.Prod(procStatementList).Is(statement);
            gb.Prod(procStatementList).Is(statementSeparatorList, statement);
            gb.Prod(procStatementList).Is(procStatementList, ";", statement);
            gb.Prod(procStatementList).Is(procStatementList, implicitStatementNoLeadingWith);

            gb.Prod(tryCatchStatement).Is(
                "BEGIN", "TRY",
                procStatementList,
                "END", "TRY",
                "BEGIN", "CATCH",
                "END", "CATCH");
            gb.Prod(tryCatchStatement).Is(
                "BEGIN", "TRY",
                procStatementList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList,
                "END", "CATCH");
            gb.Prod(tryCatchStatement).Is(
                "BEGIN", "TRY",
                procStatementList, statementSeparatorList,
                "END", "TRY",
                "BEGIN", "CATCH",
                "END", "CATCH");
            gb.Prod(tryCatchStatement).Is(
                "BEGIN", "TRY",
                procStatementList, statementSeparatorList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList,
                "END", "CATCH");
            gb.Prod(tryCatchStatement).Is(
                "BEGIN", "TRY",
                procStatementList, statementSeparatorList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList, statementSeparatorList,
                "END", "CATCH");
            gb.Prod(tryCatchStatement).Is(
                "BEGIN", "TRY",
                procStatementList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList, statementSeparatorList,
                "END", "CATCH");

            gb.Rule(cursorReference).CanBe(strictIdentifierTerm);
            gb.Rule(declareCursorStatement).OneOf(
                gb.Seq("DECLARE", strictIdentifierTerm, "CURSOR", "FOR", queryExpression),
                gb.Seq("DECLARE", strictIdentifierTerm, "CURSOR", cursorOptionList, "FOR", queryExpression));
            gb.Rule(cursorOptionList).Plus(cursorOption);
            gb.Rule(cursorOption).Keywords(
                "LOCAL",
                "GLOBAL",
                "FORWARD_ONLY",
                "SCROLL",
                "STATIC",
                "KEYSET",
                "DYNAMIC",
                "FAST_FORWARD",
                "READ_ONLY",
                "SCROLL_LOCKS",
                "OPTIMISTIC",
                "TYPE_WARNING",
                "INSENSITIVE");
            gb.Rule(cursorOperationStatement).OneOf(
                gb.Seq("OPEN", cursorReference),
                gb.Seq("CLOSE", cursorReference),
                gb.Seq("DEALLOCATE", cursorReference),
                fetchStatement);
            gb.Rule(fetchStatement).OneOf(
                gb.Seq("FETCH", "FROM", cursorReference),
                gb.Seq("FETCH", "FROM", cursorReference, "INTO", fetchTargetList),
                gb.Seq("FETCH", fetchDirection, "FROM", cursorReference),
                gb.Seq("FETCH", fetchDirection, "FROM", cursorReference, "INTO", fetchTargetList),
                gb.Seq("FETCH", cursorReference),
                gb.Seq("FETCH", cursorReference, "INTO", fetchTargetList));
            gb.Rule(fetchDirection).OneOf(
                "NEXT",
                "PRIOR",
                "FIRST",
                "LAST",
                gb.Seq("ABSOLUTE", expression),
                gb.Seq("RELATIVE", expression));
            gb.Rule(fetchTargetList).SeparatedBy(",", variableReference);

            gb.Rule(waitforTimeValue).OneOf(
                stringLiteral,
                unicodeStringLiteral,
                variableReference);
            gb.Rule(waitforStatement).OneOf(
                gb.Seq("WAITFOR", "DELAY", waitforTimeValue),
                gb.Seq("WAITFOR", "TIME", waitforTimeValue));
        }
    }
}
