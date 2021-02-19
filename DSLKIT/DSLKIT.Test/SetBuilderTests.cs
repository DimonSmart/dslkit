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
        [InlineData("S → N;N → V = E;N → E;E → V;V → x;V → * E")]
        public void SetBuilderTest(string grammarDefinition)
        {
            var grammar = new GrammarBuilder()
                    .WithGrammarName("Firsts & Follow test grammar")
                    .AddProductionsFromString(grammarDefinition)
                    .BuildGrammar("S");
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
            File.WriteAllText(@"c:\temp\sets.txt", sb.ToString());
            File.WriteAllText(@"c:\temp\sets.dot", Set2Dot.Transform(sets));
        }

        private void SetBuiilder_StepEvent(object sender, IEnumerable<RuleSet> sets)
        {
            File.WriteAllText(@"c:\temp\sets.dot", Set2Dot.Transform(sets));
        }
    }
}



