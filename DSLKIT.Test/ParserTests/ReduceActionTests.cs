using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class ReduceActionTests
    {
        [Fact]
        public void ReduceAction_ShouldInitializeCorrectly()
        {
            // Arrange
            var leftNonTerminal = new NonTerminal("A");
            var productionDefinition = new List<ITerm> { new NonTerminal("B"), new NonTerminal("C") };
            var production = new Production(leftNonTerminal, productionDefinition);
            var popLength = 2;

            // Act
            var reduceAction = new ReduceAction(production, popLength);

            // Assert
            reduceAction.Production.Should().Be(production);
            reduceAction.PopLength.Should().Be(popLength);
        }

        [Fact]
        public void ReduceAction_ToString_ShouldReturnCorrectFormat()
        {
            // Arrange
            var leftNonTerminal = new NonTerminal("Expression");
            var productionDefinition = new List<ITerm> { new NonTerminal("Term") };
            var production = new Production(leftNonTerminal, productionDefinition);
            var reduceAction = new ReduceAction(production, 1);

            // Act
            var result = reduceAction.ToString();

            // Assert
            result.Should().Be("rExpression");
        }

        [Fact]
        public void ReduceAction_ShouldImplementIActionItem()
        {
            // Arrange
            var leftNonTerminal = new NonTerminal("A");
            var productionDefinition = new List<ITerm>();
            var production = new Production(leftNonTerminal, productionDefinition);
            var reduceAction = new ReduceAction(production, 0);

            // Act & Assert
            reduceAction.Should().BeAssignableTo<IActionItem>();
        }
    }
}
