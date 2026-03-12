using System.Collections.Generic;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlScriptGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            INonTerminal script,
            INonTerminal statementList,
            INonTerminal statementListOpt,
            INonTerminal statementSeparator,
            INonTerminal statementSeparatorList,
            INonTerminal statement,
            INonTerminal statementNoLeadingWith,
            INonTerminal implicitStatementNoLeadingWith,
            INonTerminal leadingWithStatement,
            INonTerminal queryStatement,
            INonTerminal queryStatementNoLeadingWith,
            INonTerminal implicitQueryStatementNoLeadingWith,
            INonTerminal withXmlNamespacesClause,
            INonTerminal xmlNamespaceItemList,
            INonTerminal xmlNamespaceItem,
            INonTerminal withClause,
            INonTerminal cteDefinitionList,
            INonTerminal cteDefinition,
            INonTerminal updateStatement,
            INonTerminal insertStatement,
            INonTerminal deleteStatement,
            INonTerminal queryExpression,
            INonTerminal implicitQueryExpression,
            INonTerminal optionClause,
            IReadOnlyCollection<object> additionalWithClauseLeadingAlternatives,
            IReadOnlyCollection<object> statementNoLeadingWithAlternatives,
            IReadOnlyCollection<object> implicitStatementNoLeadingWithAlternatives)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var contextualIdentifierTerm = context.Symbols.ContextualIdentifierTerm;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var strictQualifiedName = context.Symbols.StrictQualifiedName;
            var qualifiedName = context.Symbols.QualifiedName;
            var graphColumnRef = context.GraphColumnRefTerminal;
            var cteIdentifierTerm = gb.NT("CteIdentifierTerm");
            var cteIdentifierList = gb.NT("CteIdentifierList");
            var xmlNamespaceAliasTerm = gb.NT("XmlNamespaceAliasTerm");

            gb.Rule("Start").CanBe(script);
            gb.Rule(script).OneOf(
                statementList,
                gb.Seq(statementList, statementSeparatorList),
                gb.Seq(statementSeparatorList, statementList),
                gb.Seq(statementSeparatorList, statementList, statementSeparatorList),
                statementSeparatorList,
                EmptyTerm.Empty);
            gb.Rule(statementList).OneOf(
                statement,
                gb.Seq(statementList, statementSeparatorList, statement),
                gb.Seq(statementList, implicitStatementNoLeadingWith));
            gb.Rule(statementListOpt).OneOf(
                EmptyTerm.Empty,
                statementList,
                gb.Seq(statementList, statementSeparatorList));
            gb.Rule(statementSeparatorList).Plus(statementSeparator);
            gb.Rule(statementSeparator).OneOf(";");
            gb.Rule(statement).OneOf(statementNoLeadingWith, leadingWithStatement);
            gb.Rule(statementNoLeadingWith).OneOf([.. statementNoLeadingWithAlternatives]);
            gb.Rule(implicitStatementNoLeadingWith).OneOf([.. implicitStatementNoLeadingWithAlternatives]);
            var leadingWithAlternatives = new List<object>
            {
                gb.Seq(withClause, queryExpression),
                gb.Seq(withClause, queryExpression, optionClause),
                gb.Seq(withClause, updateStatement),
                gb.Seq(withClause, insertStatement),
                gb.Seq(withClause, deleteStatement),
                gb.Seq(withXmlNamespacesClause, updateStatement),
                gb.Seq(withXmlNamespacesClause, insertStatement),
                gb.Seq(withXmlNamespacesClause, deleteStatement),
                gb.Seq(withXmlNamespacesClause, queryStatement)
            };
            leadingWithAlternatives.AddRange(additionalWithClauseLeadingAlternatives);
            gb.Rule(leadingWithStatement).OneOf([.. leadingWithAlternatives]);

            gb.Rule(queryStatement).OneOf(
                queryExpression,
                gb.Seq(withClause, queryExpression));
            gb.Rule(queryStatementNoLeadingWith).OneOf(queryExpression);
            gb.Rule(implicitQueryStatementNoLeadingWith).OneOf(implicitQueryExpression);

            gb.Prod(withXmlNamespacesClause).Is("WITH", "XMLNAMESPACES", "(", xmlNamespaceItemList, ")");
            gb.Prod(xmlNamespaceItemList).Is(xmlNamespaceItem);
            gb.Prod(xmlNamespaceItemList).Is(xmlNamespaceItemList, ",", xmlNamespaceItem);
            gb.Rule(xmlNamespaceAliasTerm).OneOf(strictIdentifierTerm);
            gb.Prod(xmlNamespaceItem).Is(expression, "AS", xmlNamespaceAliasTerm);
            gb.Prod(xmlNamespaceItem).Is("DEFAULT", expression);

            gb.Prod(withClause).Is("WITH", cteDefinitionList);
            gb.Prod(cteDefinitionList).Is(cteDefinition);
            gb.Prod(cteDefinitionList).Is(cteDefinitionList, ",", cteDefinition);
            gb.Rule(cteIdentifierTerm).OneOf(strictIdentifierTerm);
            gb.Prod(cteIdentifierList).Is(cteIdentifierTerm);
            gb.Prod(cteIdentifierList).Is(cteIdentifierList, ",", cteIdentifierTerm);
            gb.Prod(cteDefinition).Is(cteIdentifierTerm, "AS", "(", queryExpression, ")");
            gb.Prod(cteDefinition).Is(cteIdentifierTerm, "(", cteIdentifierList, ")", "AS", "(", queryExpression, ")");

            gb.Prod(strictQualifiedName).Is(strictIdentifierTerm);
            gb.Prod(strictQualifiedName).Is(strictQualifiedName, ".", strictIdentifierTerm);
            gb.Prod(strictQualifiedName).Is(strictQualifiedName, ".", ".", strictIdentifierTerm);
            gb.Prod(qualifiedName).Is(contextualIdentifierTerm);
            gb.Prod(qualifiedName).Is(qualifiedName, ".", contextualIdentifierTerm);
            if (context.HasFeature(MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod(qualifiedName).Is(qualifiedName, ".", graphColumnRef);
            }

            gb.Prod(qualifiedName).Is(qualifiedName, ".", ".", contextualIdentifierTerm);
        }
    }
}
