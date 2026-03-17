using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [MemberData(nameof(ValidSqlScripts))]
        public void ParseScript_ShouldParseModernMsSqlExamples(string scriptName, string scriptText)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(scriptText);

            parseResult.IsSuccess.Should().BeTrue(
                $"script '{scriptName}' should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        public static IEnumerable<object[]> ValidSqlScripts()
        {
            var scriptsRoot = ResolveScriptsRoot();
            foreach (var filePath in Directory.EnumerateFiles(scriptsRoot, "*.sql", SearchOption.AllDirectories).OrderBy(i => i))
            {
                var scriptName = Path.GetRelativePath(scriptsRoot, filePath);
                var scriptText = File.ReadAllText(filePath);
                yield return new object[] { scriptName, scriptText };
            }
        }

        private static string ResolveScriptsRoot()
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "GrammarExamples", "TestData", "MsSql", "Valid");
            if (Directory.Exists(outputPath))
            {
                return outputPath;
            }

            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "GrammarExamples", "TestData", "MsSql", "Valid");
            if (Directory.Exists(projectPath))
            {
                return projectPath;
            }

            throw new DirectoryNotFoundException("Could not find SQL test data folder.");
        }

        private static bool TryReadSqlDatasetFile(string fileName, out string scriptText)
        {
            scriptText = string.Empty;
            if (!TryResolveRepositoryRoot(out var repositoryRoot))
            {
                return false;
            }

            var datasetFilePath = Path.Combine(repositoryRoot, "sql-dataset", fileName);
            if (!File.Exists(datasetFilePath))
            {
                return false;
            }

            scriptText = File.ReadAllText(datasetFilePath);
            return true;
        }

        private static bool TryResolveRepositoryRoot(out string repositoryRoot)
        {
            var startingPoints = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            foreach (var startingPoint in startingPoints)
            {
                var current = new DirectoryInfo(startingPoint);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "DSLKIT.sln")))
                    {
                        repositoryRoot = current.FullName;
                        return true;
                    }

                    current = current.Parent;
                }
            }

            repositoryRoot = string.Empty;
            return false;
        }

        private static IReadOnlyList<TerminalNode> GetTerminalNodes(ParseTreeNode rootNode)
        {
            var terminalNodes = new List<TerminalNode>();
            CollectTerminalNodes(rootNode, terminalNodes);
            return terminalNodes;
        }

        private static void CollectTerminalNodes(ParseTreeNode node, List<TerminalNode> output)
        {
            if (node is TerminalNode terminalNode)
            {
                output.Add(terminalNode);
                return;
            }

            foreach (var childNode in node.Children)
            {
                CollectTerminalNodes(childNode, output);
            }
        }

        private static IReadOnlyList<string> FindNonTerminalPath(ParseTreeNode rootNode, string nonTerminalName)
        {
            if (rootNode is not NonTerminalNode rootNonTerminalNode)
            {
                return null;
            }

            if (string.Equals(rootNonTerminalNode.NonTerminal.Name, nonTerminalName, StringComparison.Ordinal))
            {
                return [rootNonTerminalNode.NonTerminal.Name];
            }

            foreach (var childNode in rootNonTerminalNode.Children)
            {
                var childPath = FindNonTerminalPath(childNode, nonTerminalName);
                if (childPath == null)
                {
                    continue;
                }

                return [rootNonTerminalNode.NonTerminal.Name, .. childPath];
            }

            return null;
        }

        private static IReadOnlyList<IReadOnlyList<string>> FindTerminalPaths(ParseTreeNode rootNode, string tokenText)
        {
            var paths = new List<IReadOnlyList<string>>();
            FindTerminalPaths(rootNode, tokenText, [], paths);
            return paths;
        }

        private static void FindTerminalPaths(
            ParseTreeNode node,
            string tokenText,
            IReadOnlyList<string> path,
            List<IReadOnlyList<string>> output)
        {
            if (node is TerminalNode terminalNode)
            {
                if (string.Equals(terminalNode.Token.OriginalString, tokenText, StringComparison.OrdinalIgnoreCase))
                {
                    output.Add([.. path, terminalNode.Token.OriginalString.ToUpperInvariant()]);
                }

                return;
            }

            if (node is not NonTerminalNode nonTerminalNode)
            {
                return;
            }

            var nextPath = path.Append(nonTerminalNode.NonTerminal.Name).ToArray();
            foreach (var childNode in nonTerminalNode.Children)
            {
                FindTerminalPaths(childNode, tokenText, nextPath, output);
            }
        }

        private static IReadOnlyList<ConflictInventoryItem> BuildConflictInventory(IEnumerable<ParserConflict> conflicts)
        {
            return conflicts
                .GroupBy(conflict => new
                {
                    conflict.Kind,
                    conflict.TerminalName,
                    conflict.Resolution
                })
                .Select(group => new ConflictInventoryItem(
                    group.Key.Kind,
                    group.Key.TerminalName,
                    group.Key.Resolution,
                    group.Count()))
                .OrderBy(item => item.Kind)
                .ThenBy(item => item.TerminalName, StringComparer.Ordinal)
                .ThenBy(item => item.Resolution?.ToString(), StringComparer.Ordinal)
                .ToArray();
        }

        private static int CountPathSegment(IReadOnlyList<string> path, string segment)
        {
            return path.Count(item => string.Equals(item, segment, StringComparison.Ordinal));
        }

        private sealed record ConflictInventoryItem(
            ParserConflictKind Kind,
            string TerminalName,
            Resolve? Resolution,
            int Count);
    }
}
