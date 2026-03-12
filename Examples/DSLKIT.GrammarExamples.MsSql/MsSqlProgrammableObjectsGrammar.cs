using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlProgrammableObjectsGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            INonTerminal createProcKeyword,
            INonTerminal createProcHead,
            INonTerminal createProcName,
            INonTerminal createProcSignature,
            INonTerminal createProcSignatureParameterListOpt,
            INonTerminal createProcSignatureWithClauseOpt,
            INonTerminal createProcSignatureForReplicationOpt,
            INonTerminal createProcParameterList,
            INonTerminal createProcParameter,
            INonTerminal createProcParameterOptionList,
            INonTerminal createProcParameterOption,
            INonTerminal createProcWithClause,
            INonTerminal createProcOptionList,
            INonTerminal createProcOption,
            INonTerminal createProcExecuteAsClause,
            INonTerminal createProcForReplicationClause,
            INonTerminal createProcBody,
            INonTerminal createProcBodyBlock,
            INonTerminal createProcNativeWithClause,
            INonTerminal createProcNativeAtomicOptionList,
            INonTerminal createProcNativeAtomicOption,
            INonTerminal setTransactionIsolationLevel,
            INonTerminal createProcExternalName,
            INonTerminal createProcStatement,
            INonTerminal createFunctionHead,
            INonTerminal createFunctionName,
            INonTerminal createFunctionSignature,
            INonTerminal createFunctionSignatureParameterListOpt,
            INonTerminal createFunctionParameterList,
            INonTerminal createFunctionParameter,
            INonTerminal createFunctionParameterOptionList,
            INonTerminal createFunctionParameterOption,
            INonTerminal createFunctionScalarReturnsClause,
            INonTerminal createFunctionInlineTableReturnsClause,
            INonTerminal createFunctionTableVariableReturnsClause,
            INonTerminal createFunctionTableReturnDefinition,
            INonTerminal createFunctionTableReturnItemList,
            INonTerminal createFunctionTableReturnItem,
            INonTerminal createFunctionWithClause,
            INonTerminal createFunctionOptionList,
            INonTerminal createFunctionOption,
            INonTerminal createFunctionPreludeStatement,
            INonTerminal createFunctionPreludeStatementNoLeadingWith,
            INonTerminal createFunctionImplicitPreludeStatementNoLeadingWith,
            INonTerminal createFunctionPreludeStatementList,
            INonTerminal createFunctionPreludeBeforeReturnOpt,
            INonTerminal createFunctionBodyTrailingSeparatorsOpt,
            INonTerminal createFunctionScalarReturnStatement,
            INonTerminal createFunctionTableVariableReturnStatement,
            INonTerminal createFunctionScalarBody,
            INonTerminal createFunctionInlineTableBody,
            INonTerminal createFunctionTableVariableBody,
            INonTerminal createFunctionStatement,
            INonTerminal createViewHead,
            INonTerminal createViewColumnList,
            INonTerminal createViewOptionClause,
            INonTerminal createViewQuery,
            INonTerminal createViewBody,
            INonTerminal createViewCheckOptionOpt,
            INonTerminal createViewOptionList,
            INonTerminal createViewOption,
            INonTerminal createViewStatement,
            INonTerminal createTableColumnDefinition,
            INonTerminal createTableComputedColumn,
            INonTerminal createTableConstraint,
            INonTerminal createTableTableIndex,
            INonTerminal leadingWithStatement,
            INonTerminal statementList,
            INonTerminal statementSeparatorList,
            INonTerminal withClause,
            ITerminal withCheckOptionStart,
            object[] createFunctionPreludeStatementNoLeadingWithAlternatives,
            object[] createFunctionImplicitPreludeStatementNoLeadingWithAlternatives)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var qualifiedName = context.Symbols.QualifiedName;
            var queryExpression = context.Symbols.QueryExpression;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var typeSpec = context.Symbols.TypeSpec;
            var unicodeStringLiteral = context.Symbols.UnicodeStringLiteral;
            var variableReference = context.Symbols.VariableReference;
            var number = context.NumberTerminal;
            var stringLiteral = context.StringLiteralTerminal;
            var procExecuteAsIdentifierTerm = gb.NT("ProcExecuteAsIdentifierTerm");
            var nativeAtomicOnOffValue = gb.NT("NativeAtomicOnOffValue");
            var viewColumnIdentifierList = gb.NT("ViewColumnIdentifierList");

            gb.Prod(createProcKeyword).Is("PROC");
            gb.Prod(createProcKeyword).Is("PROCEDURE");

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

            gb.Prod(createProcParameterList).Is(createProcParameter);
            gb.Prod(createProcParameterList).Is(createProcParameterList, ",", createProcParameter);
            gb.Prod(createProcParameter).Is(variableReference, typeSpec);
            gb.Prod(createProcParameter).Is(variableReference, typeSpec, createProcParameterOptionList);
            gb.Prod(createProcParameter).Is(variableReference, "AS", typeSpec);
            gb.Prod(createProcParameter).Is(variableReference, "AS", typeSpec, createProcParameterOptionList);
            gb.Prod(createProcParameterOptionList).Is(createProcParameterOption);
            gb.Prod(createProcParameterOptionList).Is(createProcParameterOptionList, createProcParameterOption);
            gb.Rule(createProcParameterOption)
                .CanBe("VARYING")
                .Or("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("OUT")
                .Or("OUTPUT")
                .Or("READONLY");

            gb.Prod(createProcWithClause).Is("WITH", createProcOptionList);
            gb.Prod(createProcOptionList).Is(createProcOption);
            gb.Prod(createProcOptionList).Is(createProcOptionList, ",", createProcOption);
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

            gb.Prod(createProcNativeAtomicOptionList).Is(createProcNativeAtomicOption);
            gb.Prod(createProcNativeAtomicOptionList).Is(createProcNativeAtomicOptionList, ",", createProcNativeAtomicOption);
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

            gb.Prod(createFunctionParameterList).Is(createFunctionParameter);
            gb.Prod(createFunctionParameterList).Is(createFunctionParameterList, ",", createFunctionParameter);
            gb.Prod(createFunctionParameter).Is(variableReference, typeSpec);
            gb.Prod(createFunctionParameter).Is(variableReference, "AS", typeSpec);
            gb.Prod(createFunctionParameter).Is(variableReference, typeSpec, createFunctionParameterOptionList);
            gb.Prod(createFunctionParameter).Is(variableReference, "AS", typeSpec, createFunctionParameterOptionList);
            gb.Prod(createFunctionParameterOptionList).Is(createFunctionParameterOption);
            gb.Prod(createFunctionParameterOptionList).Is(createFunctionParameterOptionList, createFunctionParameterOption);
            gb.Rule(createFunctionParameterOption)
                .CanBe("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("READONLY");

            gb.Prod(createFunctionScalarReturnsClause).Is("RETURNS", typeSpec);
            gb.Prod(createFunctionInlineTableReturnsClause).Is("RETURNS", "TABLE");
            gb.Prod(createFunctionTableVariableReturnsClause).Is("RETURNS", createFunctionTableReturnDefinition);
            gb.Prod(createFunctionTableReturnDefinition).Is(variableReference, "TABLE", "(", createFunctionTableReturnItemList, ")");
            gb.Prod(createFunctionTableReturnItemList).Is(createFunctionTableReturnItem);
            gb.Prod(createFunctionTableReturnItemList).Is(createFunctionTableReturnItemList, ",", createFunctionTableReturnItem);
            gb.Prod(createFunctionTableReturnItem).Is(createTableColumnDefinition);
            gb.Prod(createFunctionTableReturnItem).Is(createTableComputedColumn);
            gb.Prod(createFunctionTableReturnItem).Is(createTableConstraint);
            gb.Prod(createFunctionTableReturnItem).Is(createTableTableIndex);

            gb.Prod(createFunctionWithClause).Is("WITH", createFunctionOptionList);
            gb.Prod(createFunctionOptionList).Is(createFunctionOption);
            gb.Prod(createFunctionOptionList).Is(createFunctionOptionList, ",", createFunctionOption);
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
            gb.Prod(createFunctionInlineTableBody).Is("AS", "RETURN", "(", queryExpression, ")");
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

            gb.Prod(viewColumnIdentifierList).Is(strictIdentifierTerm);
            gb.Prod(viewColumnIdentifierList).Is(viewColumnIdentifierList, ",", strictIdentifierTerm);
            gb.Prod(createViewColumnList).Is("(", viewColumnIdentifierList, ")");
            gb.Prod(createViewOptionClause).Is("WITH", createViewOptionList);
            gb.Prod(createViewQuery).Is(queryExpression);
            gb.Prod(createViewQuery).Is(withClause, queryExpression);
            gb.Prod(createViewBody).Is("AS", createViewQuery);
            gb.Prod(createViewBody).Is("AS", createViewQuery, createViewCheckOptionOpt);
            gb.Opt(createViewCheckOptionOpt, withCheckOptionStart, "CHECK", "OPTION");
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewColumnList, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewOptionClause, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewColumnList, createViewOptionClause, createViewBody);
            gb.Rule(createViewOptionList).SeparatedBy(",", createViewOption);
            gb.Rule(createViewOption).Keywords("ENCRYPTION", "SCHEMABINDING", "VIEW_METADATA");
        }
    }
}
