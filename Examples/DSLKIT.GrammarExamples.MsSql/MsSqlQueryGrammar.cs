using DSLKIT.SpecialTerms;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlQueryGrammar
    {
        public static void Build(MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var symbols = context.Symbols;
            var expression = symbols.Expression;
            var expressionList = symbols.ExpressionList;
            var forClause = symbols.ForClause;
            var forJsonMode = symbols.ForJsonMode;
            var forJsonOption = symbols.ForJsonOption;
            var forJsonOptionList = symbols.ForJsonOptionList;
            var forXmlMode = symbols.ForXmlMode;
            var forXmlOption = symbols.ForXmlOption;
            var forXmlOptionList = symbols.ForXmlOptionList;
            var groupingSetList = symbols.GroupingSetList;
            var identifierTerm = symbols.IdentifierTerm;
            var implicitQueryExpression = symbols.ImplicitQueryExpression;
            var implicitQueryIntersectExpression = symbols.ImplicitQueryIntersectExpression;
            var implicitQueryUnionExpression = symbols.ImplicitQueryUnionExpression;
            var optionClause = symbols.OptionClause;
            var orderByClause = symbols.OrderByClause;
            var offsetFetchClause = symbols.OffsetFetchClause;
            var qualifiedName = symbols.QualifiedName;
            var queryExpression = symbols.QueryExpression;
            var queryExpressionForOpt = symbols.QueryExpressionForOpt;
            var queryExpressionOptionOpt = symbols.QueryExpressionOptionOpt;
            var queryExpressionOrderByAndOffsetOpt = symbols.QueryExpressionOrderByAndOffsetOpt;
            var queryExpressionTail = symbols.QueryExpressionTail;
            var queryIntersectExpression = symbols.QueryIntersectExpression;
            var queryPrimary = symbols.QueryPrimary;
            var querySpecification = symbols.QuerySpecification;
            var querySpecificationGroupByClause = symbols.QuerySpecificationGroupByClause;
            var querySpecificationGroupByExpressionList = symbols.QuerySpecificationGroupByExpressionList;
            var querySpecificationGroupByGroupingSets = symbols.QuerySpecificationGroupByGroupingSets;
            var querySpecificationGroupByWithOpt = symbols.QuerySpecificationGroupByWithOpt;
            var querySpecificationHavingOpt = symbols.QuerySpecificationHavingOpt;
            var querySpecificationWhereClause = symbols.QuerySpecificationWhereClause;
            var queryUnionExpression = symbols.QueryUnionExpression;
            var searchCondition = symbols.SearchCondition;
            var selectCore = symbols.SelectCore;
            var selectCoreIntoClause = symbols.SelectCoreIntoClause;
            var selectCorePrefix = symbols.SelectCorePrefix;
            var selectCoreTail = symbols.SelectCoreTail;
            var selectItem = symbols.SelectItem;
            var selectItemList = symbols.SelectItemList;
            var selectList = symbols.SelectList;
            var setOperator = symbols.SetOperator;
            var setQuantifier = symbols.SetQuantifier;
            var tableSourceList = symbols.TableSourceList;
            var topClause = symbols.TopClause;
            var topClauseTail = symbols.TopClauseTail;
            var topValue = symbols.TopValue;
            var variableReference = symbols.VariableReference;
            var compoundAssignOp = symbols.CompoundAssignOp;
            var number = context.NumberTerminal;
            var stringLiteral = context.StringLiteralTerminal;

            gb.Prod(implicitQueryExpression).Is(implicitQueryUnionExpression, queryExpressionTail);
            gb.Prod(implicitQueryUnionExpression).Is(implicitQueryIntersectExpression);
            gb.Prod(implicitQueryUnionExpression).Is(implicitQueryUnionExpression, setOperator, queryIntersectExpression);
            gb.Prod(implicitQueryIntersectExpression).Is(querySpecification);
            gb.Prod(implicitQueryIntersectExpression).Is(implicitQueryIntersectExpression, "INTERSECT", queryPrimary);
            gb.Prod(queryExpression).Is(queryUnionExpression, queryExpressionTail);
            gb.Prod(queryUnionExpression).Is(queryIntersectExpression);
            gb.Prod(queryUnionExpression).Is(queryUnionExpression, setOperator, queryIntersectExpression);
            gb.Prod(queryIntersectExpression).Is(queryPrimary);
            gb.Prod(queryIntersectExpression).Is(queryIntersectExpression, "INTERSECT", queryPrimary);

            gb.Rule(setOperator)
                .CanBe("UNION")
                .Or("UNION", "ALL")
                .OrKeywords("EXCEPT");

            gb.Prod(queryExpressionTail).Is(queryExpressionOrderByAndOffsetOpt, queryExpressionForOpt, queryExpressionOptionOpt);
            gb.Opt(queryExpressionOrderByAndOffsetOpt, orderByClause);
            gb.Prod(queryExpressionOrderByAndOffsetOpt).Is(orderByClause, offsetFetchClause);
            gb.Opt(queryExpressionForOpt, forClause);
            gb.Opt(queryExpressionOptionOpt, optionClause);
            gb.Prod(queryPrimary).Is(querySpecification);
            gb.Prod(queryPrimary).Is("(", queryExpression, ")");

            gb.Rule(forClause)
                .CanBe("FOR", "BROWSE")
                .Or("FOR", "JSON", forJsonMode)
                .Or("FOR", "JSON", forJsonMode, ",", forJsonOptionList)
                .Or("FOR", "XML", forXmlMode)
                .Or("FOR", "XML", forXmlMode, ",", forXmlOptionList);

            gb.Rule(forJsonMode).Keywords("AUTO", "PATH", "NONE");

            gb.Prod(forJsonOptionList).Is(forJsonOption);
            gb.Prod(forJsonOptionList).Is(forJsonOptionList, ",", forJsonOption);
            gb.Rule(forJsonOption).Keywords("WITHOUT_ARRAY_WRAPPER", "INCLUDE_NULL_VALUES", "ROOT");
            gb.Prod(forJsonOption).Is("ROOT", "(", expression, ")");

            gb.Rule(forXmlMode)
                .CanBe("AUTO")
                .Or("PATH")
                .Or("PATH", "(", expression, ")")
                .Or("RAW")
                .Or("RAW", "(", expression, ")")
                .Or("EXPLICIT");

            gb.Prod(forXmlOptionList).Is(forXmlOption);
            gb.Prod(forXmlOptionList).Is(forXmlOptionList, ",", forXmlOption);
            gb.Rule(forXmlOption)
                .CanBe("TYPE")
                .Or("XMLDATA")
                .Or("XMLSCHEMA")
                .Or("XMLSCHEMA", "(", expression, ")")
                .Or("ELEMENTS")
                .Or("ELEMENTS", "XSINIL")
                .Or("ELEMENTS", "ABSENT")
                .Or("ROOT")
                .Or("ROOT", "(", expression, ")")
                .Or("BINARY", "BASE64")
                .Or("WITHOUT_ARRAY_WRAPPER");

            gb.Rule(selectCorePrefix).OneOf(
                EmptyTerm.Empty,
                setQuantifier,
                topClause,
                gb.Seq(setQuantifier, topClause));
            gb.Rule(selectCoreTail).OneOf(
                EmptyTerm.Empty,
                gb.Seq("FROM", tableSourceList),
                selectCoreIntoClause);
            gb.Prod(selectCoreIntoClause).Is("INTO", qualifiedName);
            gb.Prod(selectCoreIntoClause).Is("INTO", qualifiedName, "FROM", tableSourceList);
            gb.Prod(selectCore).Is("SELECT", selectCorePrefix, selectList, selectCoreTail);

            gb.Prod(querySpecificationWhereClause).Is("WHERE", searchCondition);
            gb.Opt(querySpecificationHavingOpt, "HAVING", searchCondition);
            gb.Rule(querySpecificationGroupByWithOpt).OneOf(
                EmptyTerm.Empty,
                gb.Seq("WITH", "ROLLUP"),
                gb.Seq("WITH", "CUBE"));
            gb.Prod(querySpecificationGroupByExpressionList).Is(expressionList, querySpecificationGroupByWithOpt);
            gb.Prod(querySpecificationGroupByGroupingSets).Is("GROUPING", "SETS", "(", groupingSetList, ")");
            gb.Rule(querySpecificationGroupByClause).OneOf(
                gb.Seq("GROUP", "BY", querySpecificationGroupByExpressionList, querySpecificationHavingOpt),
                gb.Seq("GROUP", "BY", querySpecificationGroupByGroupingSets, querySpecificationHavingOpt));
            gb.Rule(querySpecification).OneOf(
                selectCore,
                gb.Seq(selectCore, querySpecificationWhereClause),
                gb.Seq(selectCore, querySpecificationGroupByClause),
                gb.Seq(selectCore, querySpecificationWhereClause, querySpecificationGroupByClause));

            gb.Rule(setQuantifier).Keywords("ALL", "DISTINCT");

            gb.Rule(topClauseTail).OneOf(
                EmptyTerm.Empty,
                "PERCENT",
                gb.Seq("WITH", "TIES"),
                gb.Seq("PERCENT", "WITH", "TIES"));
            gb.Prod(topClause).Is("TOP", topValue, topClauseTail);
            gb.Prod(topValue).Is(number);
            gb.Prod(topValue).Is("(", expression, ")");

            gb.Rule(selectList).CanBe(selectItemList);
            gb.Rule(selectItemList).SeparatedBy(",", selectItem);
            gb.Prod(selectItem).Is("*");
            gb.Prod(selectItem).Is(expression, "AS", identifierTerm);
            gb.Prod(selectItem).Is(expression, "AS", stringLiteral);
            gb.Prod(selectItem).Is(expression, identifierTerm);
            gb.Prod(selectItem).Is(expression, stringLiteral);
            gb.Prod(selectItem).Is(expression);
            gb.Prod(selectItem).Is(qualifiedName, ".", "*");
            gb.Prod(selectItem).Is(variableReference, "=", expression);
            gb.Prod(selectItem).Is(variableReference, compoundAssignOp, expression);
        }
    }
}
