using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlProgrammableObjectsGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            ITerminal withCheckOptionStart,
            object[] createFunctionPreludeStatementNoLeadingWithAlternatives,
            object[] createFunctionImplicitPreludeStatementNoLeadingWithAlternatives)
        {
            var gb = context.Gb;
            var createProcKeyword = gb.NT("CreateProcKeyword");
            var createProcHead = gb.NT("CreateProcHead");
            var createProcName = gb.NT("CreateProcName");
            var createProcSignature = gb.NT("CreateProcSignature");
            var createProcSignatureParameterListOpt = gb.NT("CreateProcSignatureParameterListOpt");
            var createProcSignatureWithClauseOpt = gb.NT("CreateProcSignatureWithClauseOpt");
            var createProcSignatureForReplicationOpt = gb.NT("CreateProcSignatureForReplicationOpt");
            var createProcParameterList = gb.NT("CreateProcParameterList");
            var createProcParameter = gb.NT("CreateProcParameter");
            var createProcParameterOptionList = gb.NT("CreateProcParameterOptionList");
            var createProcParameterOption = gb.NT("CreateProcParameterOption");
            var createProcWithClause = gb.NT("CreateProcWithClause");
            var createProcOptionList = gb.NT("CreateProcOptionList");
            var createProcOption = gb.NT("CreateProcOption");
            var createProcExecuteAsClause = gb.NT("CreateProcExecuteAsClause");
            var createProcForReplicationClause = gb.NT("CreateProcForReplicationClause");
            var createProcBody = gb.NT("CreateProcBody");
            var createProcBodyBlock = gb.NT("CreateProcBodyBlock");
            var createProcNativeWithClause = gb.NT("CreateProcNativeWithClause");
            var createProcNativeAtomicOptionList = gb.NT("CreateProcNativeAtomicOptionList");
            var createProcNativeAtomicOption = gb.NT("CreateProcNativeAtomicOption");
            var setTransactionIsolationLevel = gb.NT("SetTransactionIsolationLevel");
            var createProcExternalName = gb.NT("CreateProcExternalName");
            var createProcStatement = gb.NT("CreateProcStatement");
            var createFunctionHead = gb.NT("CreateFunctionHead");
            var createFunctionName = gb.NT("CreateFunctionName");
            var createFunctionSignature = gb.NT("CreateFunctionSignature");
            var createFunctionSignatureParameterListOpt = gb.NT("CreateFunctionSignatureParameterListOpt");
            var createFunctionParameterList = gb.NT("CreateFunctionParameterList");
            var createFunctionParameter = gb.NT("CreateFunctionParameter");
            var createFunctionParameterOptionList = gb.NT("CreateFunctionParameterOptionList");
            var createFunctionParameterOption = gb.NT("CreateFunctionParameterOption");
            var createFunctionScalarReturnsClause = gb.NT("CreateFunctionScalarReturnsClause");
            var createFunctionInlineTableReturnsClause = gb.NT("CreateFunctionInlineTableReturnsClause");
            var createFunctionTableVariableReturnsClause = gb.NT("CreateFunctionTableVariableReturnsClause");
            var createFunctionTableReturnDefinition = gb.NT("CreateFunctionTableReturnDefinition");
            var createFunctionTableReturnItemList = gb.NT("CreateFunctionTableReturnItemList");
            var createFunctionTableReturnItem = gb.NT("CreateFunctionTableReturnItem");
            var createFunctionWithClause = gb.NT("CreateFunctionWithClause");
            var createFunctionOptionList = gb.NT("CreateFunctionOptionList");
            var createFunctionOption = gb.NT("CreateFunctionOption");
            var createFunctionPreludeStatement = gb.NT("CreateFunctionPreludeStatement");
            var createFunctionPreludeStatementNoLeadingWith = gb.NT("CreateFunctionPreludeStatementNoLeadingWith");
            var createFunctionImplicitPreludeStatementNoLeadingWith = gb.NT("CreateFunctionImplicitPreludeStatementNoLeadingWith");
            var createFunctionPreludeStatementList = gb.NT("CreateFunctionPreludeStatementList");
            var createFunctionPreludeBeforeReturnOpt = gb.NT("CreateFunctionPreludeBeforeReturnOpt");
            var createFunctionBodyTrailingSeparatorsOpt = gb.NT("CreateFunctionBodyTrailingSeparatorsOpt");
            var createFunctionScalarReturnStatement = gb.NT("CreateFunctionScalarReturnStatement");
            var createFunctionTableVariableReturnStatement = gb.NT("CreateFunctionTableVariableReturnStatement");
            var createFunctionScalarBody = gb.NT("CreateFunctionScalarBody");
            var createFunctionInlineTableBody = gb.NT("CreateFunctionInlineTableBody");
            var createFunctionTableVariableBody = gb.NT("CreateFunctionTableVariableBody");
            var createFunctionStatement = gb.NT("CreateFunctionStatement");
            var createViewHead = gb.NT("CreateViewHead");
            var createViewColumnList = gb.NT("CreateViewColumnList");
            var createViewOptionClause = gb.NT("CreateViewOptionClause");
            var createViewQuery = gb.NT("CreateViewQuery");
            var createViewBody = gb.NT("CreateViewBody");
            var createViewCheckOptionOpt = gb.NT("CreateViewCheckOptionOpt");
            var createViewOptionList = gb.NT("CreateViewOptionList");
            var createViewOption = gb.NT("CreateViewOption");
            var createViewStatement = gb.NT("CreateViewStatement");
            var createTableColumnDefinition = gb.NT("CreateTableColumnDefinition");
            var createTableComputedColumn = gb.NT("CreateTableComputedColumn");
            var createTableConstraint = gb.NT("CreateTableConstraint");
            var createTableTableIndex = gb.NT("CreateTableTableIndex");
            var leadingWithStatement = gb.NT("LeadingWithStatement");
            var statementList = gb.NT("StatementList");
            var statementSeparatorList = gb.NT("StatementSeparatorList");
            var withClause = gb.NT("WithClause");
            var expression = gb.NT("Expression");
            var qualifiedName = gb.NT("QualifiedName");
            var queryExpression = gb.NT("QueryExpression");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var typeSpec = gb.NT("TypeSpec");
            var unicodeStringLiteral = gb.NT("UnicodeStringLiteral");
            var variableReference = gb.NT("VariableReference");
            var number = context.NumberTerminal;
            var stringLiteral = context.StringLiteralTerminal;
            var procExecuteAsIdentifierTerm = gb.NT("ProcExecuteAsIdentifierTerm");
            var nativeAtomicOnOffValue = gb.NT("NativeAtomicOnOffValue");
            var viewColumnIdentifierList = gb.NT("ViewColumnIdentifierList");
            var viewColumnDefinition = gb.NT("ViewColumnDefinition");
            var viewColumnDefinitionList = gb.NT("ViewColumnDefinitionList");
            var viewColumnComment = gb.NT("ViewColumnComment");
            var viewColumnCommentOpt = gb.NT("ViewColumnCommentOpt");

            gb.Rule(createProcKeyword).Keywords("PROC", "PROCEDURE");

            gb.Prod(createProcHead).Is("CREATE", createProcKeyword);
            gb.Prod(createProcHead).Is("CREATE", "OR", "ALTER", createProcKeyword);
            gb.Prod(createProcHead).Is("ALTER", createProcKeyword);

            gb.Prod(createProcName).Is(qualifiedName);
            gb.Prod(createProcName).Is(qualifiedName, ";", number);

            gb.Opt(createProcSignatureParameterListOpt, createProcParameterList);
            gb.Opt(createProcSignatureWithClauseOpt, createProcWithClause);
            gb.Opt(createProcSignatureForReplicationOpt, createProcForReplicationClause);
            gb.Rule(createProcSignature).OneOf(
                gb.Seq(createProcName, createProcSignatureParameterListOpt, createProcSignatureWithClauseOpt, createProcSignatureForReplicationOpt));

            gb.Rule(createProcParameterList).SeparatedBy(",", createProcParameter);
            gb.Prod(createProcParameter).Is(variableReference, typeSpec);
            gb.Prod(createProcParameter).Is(variableReference, typeSpec, createProcParameterOptionList);
            gb.Prod(createProcParameter).Is(variableReference, "AS", typeSpec);
            gb.Prod(createProcParameter).Is(variableReference, "AS", typeSpec, createProcParameterOptionList);
            gb.Rule(createProcParameterOptionList).Plus(createProcParameterOption);
            gb.Rule(createProcParameterOption)
                .CanBe("VARYING")
                .Or("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("OUT")
                .Or("OUTPUT")
                .Or("READONLY");

            gb.Prod(createProcWithClause).Is("WITH", createProcOptionList);
            gb.Rule(createProcOptionList).SeparatedBy(",", createProcOption);
            gb.Rule(createProcOption)
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "RECOMPILE", "NATIVE_COMPILATION", "SCHEMABINDING");

            gb.Rule(procExecuteAsIdentifierTerm).OneOf(strictIdentifierTerm);
            gb.Rule(createProcExecuteAsClause)
                .CanBe("EXECUTE", "AS", "CALLER")
                .Or("EXECUTE", "AS", "SELF")
                .Or("EXECUTE", "AS", "OWNER")
                .Or("EXECUTE", "AS", stringLiteral)
                .Or("EXECUTE", "AS", unicodeStringLiteral)
                .Or("EXECUTE", "AS", procExecuteAsIdentifierTerm);

            gb.Prod(createProcForReplicationClause).Is("FOR", "REPLICATION");

            gb.Prod(createProcBody).Is("AS", createProcBodyBlock);
            gb.Prod(createProcBody).Is("AS", "EXTERNAL", "NAME", createProcExternalName);
            gb.Prod(createProcBody).Is(createProcNativeWithClause, "AS", "BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, "END");
            gb.Prod(createProcBody).Is(createProcNativeWithClause, "AS", "BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, "END");

            gb.Prod(createProcNativeWithClause).Is("WITH", "NATIVE_COMPILATION", ",", "SCHEMABINDING");
            gb.Prod(createProcNativeWithClause).Is("WITH", "NATIVE_COMPILATION", ",", "SCHEMABINDING", ",", createProcExecuteAsClause);

            gb.Rule(createProcNativeAtomicOptionList).SeparatedBy(",", createProcNativeAtomicOption);
            gb.Rule(nativeAtomicOnOffValue).Keywords("ON", "OFF");
            gb.Prod(createProcNativeAtomicOption).Is("LANGUAGE", "=", expression);
            gb.Prod(createProcNativeAtomicOption).Is("DELAYED_DURABILITY", "=", nativeAtomicOnOffValue);
            gb.Prod(createProcNativeAtomicOption).Is("TRANSACTION", "ISOLATION", "LEVEL", "=", setTransactionIsolationLevel);

            gb.Prod(createProcExternalName).Is(qualifiedName);

            gb.Prod(createProcBodyBlock).Is(statementList);
            gb.Prod(createProcBodyBlock).Is(statementList, statementSeparatorList);
            gb.Prod(createProcBodyBlock).Is(statementSeparatorList, statementList);
            gb.Prod(createProcBodyBlock).Is(statementSeparatorList, statementList, statementSeparatorList);
            gb.Prod(createProcBodyBlock).Is("BEGIN", statementList, "END");
            gb.Prod(createProcBodyBlock).Is("BEGIN", statementList, statementSeparatorList, "END");
            gb.Prod(createProcBodyBlock).Is("BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, "END");
            gb.Prod(createProcBodyBlock).Is("BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, "END");

            gb.Prod(createProcStatement).Is(createProcHead, createProcSignature, createProcBody);

            gb.Prod(createFunctionHead).Is("CREATE", "FUNCTION");
            gb.Prod(createFunctionHead).Is("CREATE", "OR", "ALTER", "FUNCTION");
            gb.Prod(createFunctionHead).Is("ALTER", "FUNCTION");
            gb.Prod(createFunctionName).Is(qualifiedName);

            gb.Opt(createFunctionSignatureParameterListOpt, createFunctionParameterList);
            gb.Prod(createFunctionSignature).Is(
                createFunctionName,
                "(",
                createFunctionSignatureParameterListOpt,
                ")");

            gb.Rule(createFunctionParameterList).SeparatedBy(",", createFunctionParameter);
            gb.Prod(createFunctionParameter).Is(variableReference, typeSpec);
            gb.Prod(createFunctionParameter).Is(variableReference, "AS", typeSpec);
            gb.Prod(createFunctionParameter).Is(variableReference, typeSpec, createFunctionParameterOptionList);
            gb.Prod(createFunctionParameter).Is(variableReference, "AS", typeSpec, createFunctionParameterOptionList);
            gb.Rule(createFunctionParameterOptionList).Plus(createFunctionParameterOption);
            gb.Rule(createFunctionParameterOption)
                .CanBe("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("READONLY");

            gb.Prod(createFunctionScalarReturnsClause).Is("RETURNS", typeSpec);
            gb.Prod(createFunctionInlineTableReturnsClause).Is("RETURNS", "TABLE");
            gb.Prod(createFunctionTableVariableReturnsClause).Is("RETURNS", createFunctionTableReturnDefinition);
            gb.Prod(createFunctionTableReturnDefinition).Is(variableReference, "TABLE", "(", createFunctionTableReturnItemList, ")");
            gb.Rule(createFunctionTableReturnItemList).SeparatedBy(",", createFunctionTableReturnItem);
            gb.Prod(createFunctionTableReturnItem).Is(createTableColumnDefinition);
            gb.Prod(createFunctionTableReturnItem).Is(createTableComputedColumn);
            gb.Prod(createFunctionTableReturnItem).Is(createTableConstraint);
            gb.Prod(createFunctionTableReturnItem).Is(createTableTableIndex);

            gb.Prod(createFunctionWithClause).Is("WITH", createFunctionOptionList);
            gb.Rule(createFunctionOptionList).SeparatedBy(",", createFunctionOption);
            gb.Rule(createFunctionOption)
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "SCHEMABINDING")
                .Or("RETURNS", "NULL", "ON", "NULL", "INPUT")
                .Or("CALLED", "ON", "NULL", "INPUT")
                .Or("INLINE", "=", "ON")
                .Or("INLINE", "=", "OFF");

            gb.Rule(createFunctionPreludeStatement).OneOf(
                createFunctionPreludeStatementNoLeadingWith,
                leadingWithStatement);
            gb.Rule(createFunctionPreludeStatementNoLeadingWith).OneOf(createFunctionPreludeStatementNoLeadingWithAlternatives);
            gb.Rule(createFunctionImplicitPreludeStatementNoLeadingWith).OneOf(createFunctionImplicitPreludeStatementNoLeadingWithAlternatives);
            gb.Rule(createFunctionPreludeStatementList).OneOf(
                createFunctionPreludeStatement,
                gb.Seq(createFunctionPreludeStatementList, statementSeparatorList, createFunctionPreludeStatement),
                gb.Seq(createFunctionPreludeStatementList, createFunctionImplicitPreludeStatementNoLeadingWith));
            gb.Rule(createFunctionPreludeBeforeReturnOpt).OneOf(
                EmptyTerm.Empty,
                createFunctionPreludeStatementList,
                gb.Seq(createFunctionPreludeStatementList, statementSeparatorList));
            gb.Opt(createFunctionBodyTrailingSeparatorsOpt, statementSeparatorList);
            gb.Prod(createFunctionScalarReturnStatement).Is("RETURN", expression);
            gb.Prod(createFunctionTableVariableReturnStatement).Is("RETURN");

            gb.Prod(createFunctionScalarBody).Is(
                "AS",
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionScalarReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");
            gb.Prod(createFunctionInlineTableBody).Is("AS", "RETURN", queryExpression);
            gb.Prod(createFunctionInlineTableBody).Is("AS", "RETURN", "(", withClause, queryExpression, ")");
            gb.Prod(createFunctionTableVariableBody).Is(
                "AS",
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionTableVariableReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");

            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionScalarReturnsClause,
                createFunctionScalarBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionScalarReturnsClause,
                createFunctionWithClause,
                createFunctionScalarBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionInlineTableReturnsClause,
                createFunctionInlineTableBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionInlineTableReturnsClause,
                createFunctionWithClause,
                createFunctionInlineTableBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionTableVariableReturnsClause,
                createFunctionTableVariableBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionTableVariableReturnsClause,
                createFunctionWithClause,
                createFunctionTableVariableBody);

            if (context.HasFeature(MsSqlDialectFeatures.SnowflakeCompat))
            {
                gb.Prod(viewColumnComment).Is("COMMENT", stringLiteral);
                gb.Prod(viewColumnComment).Is("COMMENT", unicodeStringLiteral);
                gb.Opt(viewColumnCommentOpt, viewColumnComment);
                gb.Prod(viewColumnDefinition).Is(strictIdentifierTerm, viewColumnCommentOpt);
                gb.Rule(viewColumnDefinitionList).SeparatedBy(",", viewColumnDefinition);
                gb.Prod(createViewColumnList).Is("(", viewColumnDefinitionList, ")");
            }
            else
            {
                gb.Rule(viewColumnIdentifierList).SeparatedBy(",", strictIdentifierTerm);
                gb.Prod(createViewColumnList).Is("(", viewColumnIdentifierList, ")");
            }

            gb.Prod(createViewOptionClause).Is("WITH", createViewOptionList);
            if (context.HasFeature(MsSqlDialectFeatures.SnowflakeCompat))
            {
                gb.Prod(createViewOptionClause).Is(createViewOption);
            }

            gb.Prod(createViewQuery).Is(queryExpression);
            gb.Prod(createViewQuery).Is(withClause, queryExpression);
            gb.Prod(createViewBody).Is("AS", createViewQuery, createViewCheckOptionOpt);
            gb.Opt(createViewCheckOptionOpt, withCheckOptionStart, "CHECK", "OPTION");
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewColumnList, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewOptionClause, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewColumnList, createViewOptionClause, createViewBody);
            gb.Rule(createViewOptionList).SeparatedBy(",", createViewOption);
            gb.Rule(createViewOption).Keywords("ENCRYPTION", "SCHEMABINDING", "VIEW_METADATA");
            if (context.HasFeature(MsSqlDialectFeatures.SnowflakeCompat))
            {
                gb.Prod(createViewOption).Is("COMMENT", "=", stringLiteral);
                gb.Prod(createViewOption).Is("COMMENT", "=", unicodeStringLiteral);
            }
        }
    }
}
