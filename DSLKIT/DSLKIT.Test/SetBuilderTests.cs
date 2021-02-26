using DSLKIT.Parser;
using DSLKIT.Terminals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace DSLKIT.Test
{
    public class SetBuilderTests : GrammarTestsBase
    {
        [Theory]
        // https://web.cs.dal.ca/~sjackson/lalr1.html
        [InlineData("S → N;N → V = E;N → E;E → V;V → x;V → * E", "S", "sjackson")]
        // https://www.cs.colostate.edu/~mstrout/CS453Spring11/Slides/19-LR-table-build.ppt.pdf
        [InlineData("S' → S e;S → ( S );S → i", "S'", "mstrout")]
        // https://www.javatpoint.com/lalr-1-parsing
        [InlineData("S' → S; S → A A;A → a A;A → b", "S'", "javatpoint")]
        public void SetBuilderTest(string grammarDefinition, string rootName, string graphFileName)
        {
            var grammar = new GrammarBuilder()
                    .WithGrammarName("Firsts & Follow test grammar")
                    .AddProductionsFromString(grammarDefinition)
                    .BuildGrammar(rootName);
            ShowGrammar(grammar);

            var setBuiilder = new SetBuilder(grammar);
            setBuiilder.StepEvent += SetBuiilder_StepEvent;
            var sets = setBuiilder.Build();

            var sb = new StringBuilder();
            foreach (var set in sets)
            {
                sb.AppendLine(set.ToString());
            }

            Console.WriteLine(sb.ToString());
            File.WriteAllText($@"{graphFileName}.txt", sb.ToString());
            File.WriteAllText($@"{graphFileName}.dot", Set2Dot.Transform(sets));
        }

        private void SetBuiilder_StepEvent(object sender, IEnumerable<RuleSet> sets, string grammarName)
        {
            File.WriteAllText($@"{grammarName}_step.dot", Set2Dot.Transform(sets));
        }
    }
}



