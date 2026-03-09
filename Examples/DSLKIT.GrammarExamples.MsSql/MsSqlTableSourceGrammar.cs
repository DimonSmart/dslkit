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
            var tableSourceColumnIdentifierTerm = gb.NT("TableSourceColumnIdentifierTerm");
            var tableSourceAliasTerm = gb.NT("TableSourceAliasTerm");
            var tableSourceColumnAliasList = gb.NT("TableSourceColumnAliasList");

            gb.Rule(tableSourceColumnIdentifierTerm)
                .CanBe(context.Symbols.StrictIdentifierTerm)
                .OrKeywords("NAME");
            gb.Rule(tableSourceAliasTerm)
                .CanBe(tableSourceColumnIdentifierTerm)
                .OrKeywords("SOURCE", "TARGET");
            gb.Prod(tableSourceColumnAliasList).Is(tableSourceColumnIdentifierTerm);
            gb.Prod(tableSourceColumnAliasList).Is(tableSourceColumnAliasList, ",", tableSourceColumnIdentifierTerm);

            gb.Rule(tableSourceList).SeparatedBy(",", tableSource);
            gb.Prod(tableSource).Is(tableFactor);
            gb.Prod(tableSource).Is(tableSource, joinPart);
            gb.Prod(tableFactor).Is(qualifiedName);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, tableSourceAliasTerm);
            if (hasGraphExtensions)
            {
                gb.Prod(tableFactor).Is(qualifiedName, forPathStart, "PATH");
                gb.Prod(tableFactor).Is(qualifiedName, forPathStart, "PATH", tableSourceAliasTerm);
            }
            gb.Prod(tableFactor).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(tableFactor).Is(qualifiedName, "AS", tableSourceAliasTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(tableFactor).Is(qualifiedName, tableSourceAliasTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(tableFactor).Is(qualifiedName, temporalClause);
            gb.Prod(tableFactor).Is(qualifiedName, temporalClause, "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, temporalClause, tableSourceAliasTerm);
            gb.Rule(temporalClause).OneOf(
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "AS", "OF", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "ALL"),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "BETWEEN", additiveExpression, "AND", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "FROM", additiveExpression, "TO", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "CONTAINED", "IN", "(", additiveExpression, ",", additiveExpression, ")"));
            gb.Prod(tableFactor).Is(variableReference);
            gb.Prod(tableFactor).Is(variableReference, "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(variableReference, tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(functionCall);
            gb.Prod(tableFactor).Is(functionCall, "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(functionCall, tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(functionCall, "AS", tableSourceAliasTerm, "(", tableSourceColumnAliasList, ")");
            gb.Prod(tableFactor).Is(functionCall, tableSourceAliasTerm, "(", tableSourceColumnAliasList, ")");
            gb.Prod(tableFactor).Is(openJsonCall);
            gb.Prod(tableFactor).Is(openJsonCall, "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(openJsonCall, tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", tableSourceAliasTerm, "(", tableSourceColumnAliasList, ")");
            gb.Prod(tableFactor).Is("(", queryExpression, ")", tableSourceAliasTerm, "(", tableSourceColumnAliasList, ")");
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "PIVOT", "(", pivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "PIVOT", "(", pivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, tableSourceAliasTerm, "PIVOT", "(", pivotClause, ")", tableSourceAliasTerm);
            gb.Prod(pivotClause).Is(functionCall, "FOR", tableSourceColumnIdentifierTerm, "IN", "(", pivotValueList, ")");
            gb.Prod(pivotValueList).Is(expression);
            gb.Prod(pivotValueList).Is(pivotValueList, ",", expression);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", queryExpression, ")", tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, "AS", tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is(qualifiedName, tableSourceAliasTerm, "UNPIVOT", "(", unpivotClause, ")", tableSourceAliasTerm);
            gb.Prod(unpivotClause).Is(tableSourceColumnIdentifierTerm, "FOR", tableSourceColumnIdentifierTerm, "IN", "(", unpivotColumnList, ")");
            gb.Prod(unpivotColumnList).Is(tableSourceColumnIdentifierTerm);
            gb.Prod(unpivotColumnList).Is(unpivotColumnList, ",", tableSourceColumnIdentifierTerm);
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", "AS", tableSourceAliasTerm, "(", tableSourceColumnAliasList, ")");
            gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", tableSourceAliasTerm, "(", tableSourceColumnAliasList, ")");
            gb.Prod(tableFactor).Is("(", tableSource, ")");
            gb.Prod(tableFactor).Is("(", tableSource, ")", "AS", tableSourceAliasTerm);
            gb.Prod(tableFactor).Is("(", tableSource, ")", tableSourceAliasTerm);

            gb.Prod(openJsonCall).Is("OPENJSON", "(", functionArgumentList, ")");
            gb.Prod(openJsonCall).Is("OPENJSON", "(", functionArgumentList, ")", openJsonWithClause);
            gb.Prod(openJsonWithClause).Is("WITH", "(", openJsonColumnList, ")");
            gb.Prod(openJsonColumnList).Is(openJsonColumnDef);
            gb.Prod(openJsonColumnList).Is(openJsonColumnList, ",", openJsonColumnDef);
            gb.Prod(openJsonColumnDef).Is(tableSourceColumnIdentifierTerm, typeSpec);
            gb.Prod(openJsonPath).Is(stringLiteral);
            gb.Prod(openJsonPath).Is(unicodeStringLiteral);
            gb.Prod(openJsonColumnDef).Is(tableSourceColumnIdentifierTerm, typeSpec, openJsonPath);
            gb.Prod(openJsonColumnDef).Is(tableSourceColumnIdentifierTerm, typeSpec, "AS", "JSON");
            gb.Prod(openJsonColumnDef).Is(tableSourceColumnIdentifierTerm, typeSpec, openJsonPath, "AS", "JSON");

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
