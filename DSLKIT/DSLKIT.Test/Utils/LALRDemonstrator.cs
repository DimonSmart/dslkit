using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Test.Transformers;

namespace DSLKIT.Test.Utils
{
    /// <summary>
    /// Utility program to demonstrate LALR state merging functionality.
    /// </summary>
    public static class LALRDemonstrator
    {
        public static void DemonstrateAndSave(string grammarName, string grammarDefinition, string rootName)
        {
            Console.WriteLine($"\n=== LALR State Merging Demo: {grammarName} ===");
            Console.WriteLine($"Grammar: {grammarDefinition}");
            Console.WriteLine($"Root: {rootName}");

            var builder = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition);

            // Capture original LR(0) item sets
            IReadOnlyCollection<RuleSet> originalItemSets = null;
            builder.OnRuleSetCreated += ruleSets =>
            {
                originalItemSets = ruleSets.ToList();
                Console.WriteLine($"\nOriginal LR(0) Item Sets: {originalItemSets.Count}");
                foreach (var set in originalItemSets)
                {
                    Console.WriteLine($"  State {set.SetNumber}: {set.Rules.Count} rules");
                }
            };

            // Capture LALR merge results
            LALRMergeResult mergeResult = null;
            builder.OnLALRMergeCompleted += result =>
            {
                mergeResult = result;
                Console.WriteLine($"\n{result.Statistics}");
                
                if (result.Statistics.MergeGroups.Any())
                {
                    Console.WriteLine("\nMerge Details:");
                    foreach (var group in result.Statistics.MergeGroups)
                    {
                        Console.WriteLine($"  {group}");
                        Console.WriteLine($"    Core: {group.CoreSignature}");
                    }
                }
                else
                {
                    Console.WriteLine("No states were merged (no identical cores found)");
                }
            };

            // Build the grammar
            var grammar = builder.BuildGrammar(rootName);

            // Save detailed output files
            if (originalItemSets != null)
            {
                // Save original LR(0) states
                var lr0Output = RuleSets2Text.Transform(originalItemSets);
                File.WriteAllText($"{grammarName}_LR0_States.txt", lr0Output);
                
                var lr0GraphViz = RuleSets2GraphVizDotFormat.Transform(originalItemSets);
                File.WriteAllText($"{grammarName}_LR0_States.dot", lr0GraphViz);
            }

            if (mergeResult != null)
            {
                // Save merged LALR states
                var lalrOutput = RuleSets2Text.Transform(mergeResult.LALRStates);
                File.WriteAllText($"{grammarName}_LALR_States.txt", lalrOutput);
                
                var lalrGraphViz = RuleSets2GraphVizDotFormat.Transform(mergeResult.LALRStates);
                File.WriteAllText($"{grammarName}_LALR_States.dot", lalrGraphViz);

                // Save merge report
                var mergeReport = GenerateMergeReport(mergeResult);
                File.WriteAllText($"{grammarName}_LALR_MergeReport.txt", mergeReport);
            }

            Console.WriteLine($"\nFiles saved:");
            Console.WriteLine($"  {grammarName}_LR0_States.txt");
            Console.WriteLine($"  {grammarName}_LR0_States.dot");
            Console.WriteLine($"  {grammarName}_LALR_States.txt");
            Console.WriteLine($"  {grammarName}_LALR_States.dot");
            Console.WriteLine($"  {grammarName}_LALR_MergeReport.txt");
        }

        private static string GenerateMergeReport(LALRMergeResult result)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("LALR STATE MERGING REPORT");
            report.AppendLine("========================");
            report.AppendLine();
            report.AppendLine($"Original LR(1) states: {result.Statistics.OriginalLR1StateCount}");
            report.AppendLine($"Merged LALR states: {result.Statistics.MergedLALRStateCount}");
            report.AppendLine($"States reduced: {result.Statistics.StatesReduced}");
            report.AppendLine($"Merge groups: {result.Statistics.MergeGroupCount}");
            report.AppendLine($"Largest merge group: {result.Statistics.LargestMergeGroupSize} states");
            report.AppendLine();

            if (result.Statistics.MergeGroups.Any())
            {
                report.AppendLine("MERGE GROUPS:");
                report.AppendLine("=============");
                for (int i = 0; i < result.Statistics.MergeGroups.Count; i++)
                {
                    var group = result.Statistics.MergeGroups[i];
                    report.AppendLine($"\nGroup {i + 1}:");
                    report.AppendLine($"  Original states: [{string.Join(", ", group.OriginalStateNumbers)}]");
                    report.AppendLine($"  States merged: {group.MergedStateCount}");
                    report.AppendLine($"  Core signature: {group.CoreSignature}");
                }
            }
            else
            {
                report.AppendLine("No merge groups found. All states had unique cores.");
            }

            report.AppendLine();
            report.AppendLine("STATE MAPPING:");
            report.AppendLine("==============");
            foreach (var mapping in result.LR1ToLALRMapping)
            {
                report.AppendLine($"LR(1) State {mapping.Key.SetNumber} → LALR State {mapping.Value.SetNumber}");
            }

            return report.ToString();
        }
    }
}
