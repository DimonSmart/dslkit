
namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlProceduralGrammar
    {
        public static void Build(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var ifBranchStatement = gb.NT("IfBranchStatement");
            var statement = gb.NT("Statement");
            var statementListOpt = gb.NT("StatementListOpt");
            var ifStatement = gb.NT("IfStatement");
            var beginEndStatement = gb.NT("BeginEndStatement");
            var whileStatement = gb.NT("WhileStatement");
            var setOptionName = gb.NT("SetOptionName");
            var setStatisticsOption = gb.NT("SetStatisticsOption");
            var setStatement = gb.NT("SetStatement");
            var setTransactionIsolationLevel = gb.NT("SetTransactionIsolationLevel");
            var printStatement = gb.NT("PrintStatement");
            var returnStatement = gb.NT("ReturnStatement");
            var transactionStatement = gb.NT("TransactionStatement");
            var raiserrorStatement = gb.NT("RaiserrorStatement");
            var raiserrorArgList = gb.NT("RaiserrorArgList");
            var raiserrorWithOptionList = gb.NT("RaiserrorWithOptionList");
            var raiserrorWithOption = gb.NT("RaiserrorWithOption");
            var throwStatement = gb.NT("ThrowStatement");
            var loopControlStatement = gb.NT("LoopControlStatement");
            var gotoStatement = gb.NT("GotoStatement");
            var labelOnlyStatement = gb.NT("LabelOnlyStatement");
            var labelStatement = gb.NT("LabelStatement");
            var declareStatement = gb.NT("DeclareStatement");
            var declareItemList = gb.NT("DeclareItemList");
            var declareItem = gb.NT("DeclareItem");
            var declareTableVariable = gb.NT("DeclareTableVariable");
            var tableTypeDefinition = gb.NT("TableTypeDefinition");
            var createTableElementList = gb.NT("CreateTableElementList");
            var createTableOptions = gb.NT("CreateTableOptions");
            var typeArgument = gb.NT("TypeArgument");
            var procStatementList = gb.NT("ProcStatementList");
            var statementSeparatorList = gb.NT("StatementSeparatorList");
            var implicitStatementNoLeadingWith = gb.NT("ImplicitStatementNoLeadingWith");
            var tryCatchStatement = gb.NT("TryCatchStatement");
            var cursorReference = gb.NT("CursorReference");
            var declareCursorStatement = gb.NT("DeclareCursorStatement");
            var cursorOptionList = gb.NT("CursorOptionList");
            var cursorOption = gb.NT("CursorOption");
            var cursorOperationStatement = gb.NT("CursorOperationStatement");
            var fetchStatement = gb.NT("FetchStatement");
            var fetchDirection = gb.NT("FetchDirection");
            var fetchTargetList = gb.NT("FetchTargetList");
            var waitforTimeValue = gb.NT("WaitforTimeValue");
            var waitforStatement = gb.NT("WaitforStatement");
            var expression = gb.NT("Expression");
            var qualifiedName = gb.NT("QualifiedName");
            var queryExpression = gb.NT("QueryExpression");
            var searchCondition = gb.NT("SearchCondition");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var typeSpec = gb.NT("TypeSpec");
            var unicodeStringLiteral = gb.NT("UnicodeStringLiteral");
            var variableReference = gb.NT("VariableReference");
            var compoundAssignOp = gb.NT("CompoundAssignOp");
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
            gb.Rule(raiserrorArgList).SeparatedBy(",", expression);
            gb.Rule(raiserrorWithOptionList).SeparatedBy(",", raiserrorWithOption);
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
            gb.Rule(declareItemList).SeparatedBy(",", declareItem);
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
