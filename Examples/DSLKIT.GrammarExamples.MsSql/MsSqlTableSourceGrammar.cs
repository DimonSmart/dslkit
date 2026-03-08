namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlTableSourceGrammar
    {
        public static void Build(MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var symbols = context.Symbols;
            var additiveExpression = symbols.AdditiveExpression;
            var expression = symbols.Expression;
            var functionArgumentList = symbols.FunctionArgumentList;
            var functionCall = symbols.FunctionCall;
            var identifierTerm = symbols.IdentifierTerm;
            var insertColumnList = symbols.InsertColumnList;
            var joinPart = symbols.JoinPart;
            var joinType = symbols.JoinType;
            var openJsonCall = symbols.OpenJsonCall;
            var openJsonColumnDef = symbols.OpenJsonColumnDef;
            var openJsonColumnList = symbols.OpenJsonColumnList;
            var openJsonPath = symbols.OpenJsonPath;
            var openJsonWithClause = symbols.OpenJsonWithClause;
            var pivotClause = symbols.PivotClause;
            var pivotValueList = symbols.PivotValueList;
            var qualifiedName = symbols.QualifiedName;
            var queryExpression = symbols.QueryExpression;
            var rowValueList = symbols.RowValueList;
            var searchCondition = symbols.SearchCondition;
            var tableFactor = symbols.TableFactor;
            var tableHintLimitedList = symbols.TableHintLimitedList;
            var tableSource = symbols.TableSource;
            var tableSourceList = symbols.TableSourceList;
            var temporalClause = symbols.TemporalClause;
            var typeSpec = symbols.TypeSpec;
            var unicodeStringLiteral = symbols.UnicodeStringLiteral;
            var unpivotClause = symbols.UnpivotClause;
            var unpivotColumnList = symbols.UnpivotColumnList;
            var variableReference = symbols.VariableReference;
            var forPathStart = context.ForPathStartTerminal;
            var forSystemTimeStart = context.ForSystemTimeStartTerminal;
            var hasGraphExtensions = context.HasFeature(MsSqlDialectFeatures.GraphExtensions);
            var stringLiteral = context.StringLiteralTerminal;

            gb.Rule(tableSourceList).SeparatedBy(",", tableSource);
            gb.Prod(tableSource).Is(tableFactor);
            gb.Prod(tableSource).Is(tableSource, joinPart);
            gb.Prod(tableFactor).Is(qualifiedName);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, identifierTerm);
            if (hasGraphExtensions)
            {
                gb.Prod(tableFactor).Is(qualifiedName, forPathStart, "PATH");
                gb.Prod(tableFactor).Is(qualifiedName, forPathStart, "PATH", identifierTerm);
            }
            gb.Prod(tableFactor).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(tableFactor).Is(qualifiedName, temporalClause);
            gb.Prod(tableFactor).Is(qualifiedName, temporalClause, "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, temporalClause, identifierTerm);
            gb.Rule(temporalClause).OneOf(
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "AS", "OF", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "ALL"),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "BETWEEN", additiveExpression, "AND", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "FROM", additiveExpression, "TO", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "CONTAINED", "IN", "(", additiveExpression, ",", additiveExpression, ")"));
            gb.Prod(tableFactor).Is(variableReference);
            gb.Prod(tableFactor).Is(variableReference, "AS", identifierTerm);
            gb.Prod(tableFactor).Is(variableReference, identifierTerm);
            gb.Prod(tableFactor).Is(functionCall);
            gb.Prod(tableFactor).Is(functionCall, "AS", identifierTerm);
            gb.Prod(tableFactor).Is(functionCall, identifierTerm);
            gb.Prod(tableFactor).Is(functionCall, "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod(tableFactor).Is(functionCall, identifierTerm, "(", insertColumnList, ")");
            gb.Prod(tableFactor).Is(openJsonCall);
            gb.Prod(tableFactor).Is(openJsonCall, "AS", identifierTerm);
            gb.Prod(tableFactor).Is(openJsonCall, identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "(", insertColumnList, ")");
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod(pivotClause).Is(functionCall, "FOR", identifierTerm, "IN", "(", pivotValueList, ")");
            gb.Prod(pivotValueList).Is(expression);
            gb.Prod(pivotValueList).Is(pivotValueList, ",", expression);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod(unpivotClause).Is(identifierTerm, "FOR", identifierTerm, "IN", "(", unpivotColumnList, ")");
            gb.Prod(unpivotColumnList).Is(identifierTerm);
            gb.Prod(unpivotColumnList).Is(unpivotColumnList, ",", identifierTerm);
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", identifierTerm);
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", identifierTerm, "(", insertColumnList, ")");
            gb.Prod(tableFactor).Is("(", tableSource, ")");
            gb.Prod(tableFactor).Is("(", tableSource, ")", "AS", identifierTerm);
            gb.Prod(tableFactor).Is("(", tableSource, ")", identifierTerm);

            gb.Prod(openJsonCall).Is("OPENJSON", "(", functionArgumentList, ")");
            gb.Prod(openJsonCall).Is("OPENJSON", "(", functionArgumentList, ")", openJsonWithClause);
            gb.Prod(openJsonWithClause).Is("WITH", "(", openJsonColumnList, ")");
            gb.Prod(openJsonColumnList).Is(openJsonColumnDef);
            gb.Prod(openJsonColumnList).Is(openJsonColumnList, ",", openJsonColumnDef);
            gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec);
            gb.Prod(openJsonPath).Is(stringLiteral);
            gb.Prod(openJsonPath).Is(unicodeStringLiteral);
            gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec, openJsonPath);
            gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec, "AS", "JSON");
            gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec, openJsonPath, "AS", "JSON");

            gb.Prod(joinPart).Is("JOIN", tableFactor, "ON", searchCondition);
            gb.Prod(joinPart).Is(joinType, "JOIN", tableFactor, "ON", searchCondition);
            gb.Prod(joinPart).Is("CROSS", "JOIN", tableFactor);
            gb.Prod(joinPart).Is("CROSS", "APPLY", tableFactor);
            gb.Prod(joinPart).Is("OUTER", "APPLY", tableFactor);

            gb.Rule(joinType)
                .CanBe("INNER")
                .Or("INNER", "HASH")
                .Or("INNER", "LOOP")
                .Or("INNER", "MERGE")
                .Or("LEFT")
                .Or("LEFT", "OUTER")
                .Or("LEFT", "HASH")
                .Or("LEFT", "OUTER", "HASH")
                .Or("RIGHT")
                .Or("RIGHT", "OUTER")
                .Or("RIGHT", "HASH")
                .Or("RIGHT", "OUTER", "HASH")
                .Or("FULL")
                .Or("FULL", "OUTER");
        }
    }
}
