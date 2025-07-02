using System;
using System.Collections.Generic;
using DSLKIT.Base;
using DSLKIT.Helpers;
using DSLKIT.Parser;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using Xunit.Abstractions;

namespace DSLKIT.Test.Common
{
    public class GrammarTestsBase
    {
        protected readonly ITestOutputHelper _testOutputHelper;

        public GrammarTestsBase(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected static void ShowGrammar(IGrammar grammar)
        {
            Console.WriteLine(GrammarVisualizer.DumpGrammar(grammar));
        }

        protected static Dictionary<string, List<ITerm>> GetSet(Dictionary<string, ITerminal> terminals,
            string setLines,
            string[] delimiter = null)
        {
            if (delimiter == null)
            {
                delimiter = [Environment.NewLine, ";"];
            }

            var result = new Dictionary<string, List<ITerm>>();
            var lines = setLines.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var record = GetSetRecord(terminals, line);
                result.Add(record.Key, record.Value);
            }

            return result;
        }

        private static KeyValuePair<string, List<ITerm>> GetSetRecord(Dictionary<string, ITerminal> terminals,
            string setDefinition)
        {
            var pair = setDefinition.Split(["→", "->"], StringSplitOptions.RemoveEmptyEntries);
            if (pair.Length != 2)
            {
                throw new ArgumentException($"{setDefinition} should be in form A→zxcA with → as delimiter");
            }

            var left = pair[0].Trim();
            var right = new List<ITerm>();
            foreach (var item in pair[1].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item == "ε")
                {
                    right.Add(EmptyTerm.Empty);
                    continue;
                }

                if (item == "$")
                {
                    right.Add(EofTerminal.Instance);
                    continue;
                }

                right.Add(terminals[item]);
            }

            return new KeyValuePair<string, List<ITerm>>(left, right);
        }
    }
}