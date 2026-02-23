using System;
using System.Linq;
using System.Reflection;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class RuleSetTests
    {
        [Fact]
        public void StartsFrom_ShouldMatchKernelRegardlessOfOrder()
        {
            var first = CreateRule("A", "a");
            var second = CreateRule("B", "b");
            var set = new RuleSet(0, new[] { first, second });

            var result = InvokeStartsFrom(set, new[] { second, first });

            result.Should().BeTrue();
        }

        [Fact]
        public void StartsFrom_ShouldReturnFalseForDifferentKernel()
        {
            var first = CreateRule("A", "a");
            var second = CreateRule("B", "b");
            var different = CreateRule("C", "c");
            var set = new RuleSet(0, new[] { first, second });

            var result = InvokeStartsFrom(set, new[] { first, different });

            result.Should().BeFalse();
        }

        private static bool InvokeStartsFrom(RuleSet ruleSet, Rule[] candidateKernel)
        {
            var method = typeof(RuleSet).GetMethod("StartsFrom", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull("RuleSet must keep internal StartsFrom for state lookup");

            var value = method!.Invoke(ruleSet, new object[] { candidateKernel });
            value.Should().BeOfType<bool>();
            return (bool)value!;
        }

        private static Rule CreateRule(string leftNonTerminalName, params string[] terms)
        {
            var left = new NonTerminal(leftNonTerminalName);
            var definition = terms.Select(term => (ITerm)new KeywordTerminal(term)).ToList();
            var production = new Production(left, definition);
            return new Rule(production);
        }
    }
}
