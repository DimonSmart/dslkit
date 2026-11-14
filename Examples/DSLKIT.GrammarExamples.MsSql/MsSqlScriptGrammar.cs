using System.Collections.Generic;
using DSLKIT.SpecialTerms;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlScriptGrammar
    {
        public static void Build(
            MsSqlGrammarContext context,
            IReadOnlyCollection<object> additionalWithClauseLeadingAlternatives,
            IReadOnlyCollection<object> statementNoLeadingWithAlternatives,
            IReadOnlyCollection<object> implicitStatementNoLeadingWithAlternatives)
        {
            var gb = context.Gb;
            var script = gb.NT("Script");
            var statementList = gb.NT("StatementList");
            var statementListOpt = gb.NT("StatementListOpt");
            var statementSeparator = gb.NT("StatementSeparator");
            var statementSeparatorList = gb.NT("StatementSeparatorList");
            var statement = gb.NT("Statement");
            var statementNoLeadingWith = gb.NT("StatementNoLeadingWith");
            var implicitStatementNoLeadingWith = gb.NT("ImplicitStatementNoLeadingWith");
            var leadingWithStatement = gb.NT("LeadingWithStatement");
            var queryStatement = gb.NT("QueryStatement");
            var queryStatementNoLeadingWith = gb.NT("QueryStatementNoLeadingWith");
            var implicitQueryStatementNoLeadingWith = gb.NT("ImplicitQueryStatementNoLeadingWith");
            var withXmlNamespacesClause = gb.NT("WithXmlNamespacesClause");
            var xmlNamespaceItemList = gb.NT("XmlNamespaceItemList");
            var xmlNamespaceItem = gb.NT("XmlNamespaceItem");
            var withClause = gb.NT("WithClause");
            var cteDefinitionList = gb.NT("CteDefinitionList");
            var cteDefinition = gb.NT("CteDefinition");
            var updateStatement = gb.NT("UpdateStatement");
            var insertStatement = gb.NT("InsertStatement");
            var deleteStatement = gb.NT("DeleteStatement");
            var queryExpression = gb.NT("QueryExpression");
            var implicitQueryExpression = gb.NT("ImplicitQueryExpression");
            var optionClause = gb.NT("OptionClause");
            var expression = gb.NT("Expression");
            var contextualIdentifierTerm = gb.NT("ContextualIdentifierTerm");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var strictQualifiedName = gb.NT("StrictQualifiedName");
            var qualifiedName = gb.NT("QualifiedName");
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
            gb.Rule(xmlNamespaceItemList).SeparatedBy(",", xmlNamespaceItem);
            gb.Rule(xmlNamespaceAliasTerm).OneOf(strictIdentifierTerm);
            gb.Prod(xmlNamespaceItem).Is(expression, "AS", xmlNamespaceAliasTerm);
            gb.Prod(xmlNamespaceItem).Is("DEFAULT", expression);

            gb.Prod(withClause).Is("WITH", cteDefinitionList);
            gb.Rule(cteDefinitionList).SeparatedBy(",", cteDefinition);
            gb.Rule(cteIdentifierTerm).OneOf(strictIdentifierTerm);
            gb.Rule(cteIdentifierList).SeparatedBy(",", cteIdentifierTerm);
            gb.Prod(cteDefinition).Is(cteIdentifierTerm, "AS", "(", queryExpression, ")");
            gb.Prod(cteDefinition).Is(cteIdentifierTerm, "(", cteIdentifierList, ")", "AS", "(", queryExpression, ")");

            gb.Rule(strictQualifiedName).SeparatedBy(".", strictIdentifierTerm);
            gb.Prod(strictQualifiedName).Is(strictQualifiedName, ".", ".", strictIdentifierTerm);
            gb.Rule(qualifiedName).SeparatedBy(".", contextualIdentifierTerm);
            if (context.HasFeature(MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod(qualifiedName).Is(qualifiedName, ".", graphColumnRef);
            }

            gb.Prod(qualifiedName).Is(qualifiedName, ".", ".", contextualIdentifierTerm);
        }
    }
}
