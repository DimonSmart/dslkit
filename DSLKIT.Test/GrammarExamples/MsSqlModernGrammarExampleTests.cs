using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [MemberData(nameof(ValidSqlScripts))]
        public void ParseScript_ShouldParseModernMsSqlExamples(string scriptName, string scriptText)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseScript(scriptText);

            parseResult.IsSuccess.Should().BeTrue(
                $"script '{scriptName}' should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateDatabase_WithPopularOptions()
        {
            const string script = """
                CREATE DATABASE Sales
                CONTAINMENT = NONE
                ON PRIMARY
                (
                    NAME = SalesData,
                    FILENAME = 'C:\data\sales.mdf',
                    SIZE = 64MB,
                    MAXSIZE = 512MB,
                    FILEGROWTH = 64MB
                ),
                FILEGROUP FG_Archive CONTAINS FILESTREAM DEFAULT
                (
                    NAME = ArchiveFs,
                    FILENAME = 'C:\data\archive'
                ),
                LOG ON
                (
                    NAME = SalesLog,
                    FILENAME = 'C:\data\sales.ldf',
                    FILEGROWTH = 10%
                )
                COLLATE Latin1_General_100_CI_AS
                WITH
                DEFAULT_LANGUAGE = us_english,
                NESTED_TRIGGERS = ON,
                TRUSTWORTHY OFF,
                LEDGER = OFF;
                GO
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateRole_WithAuthorization()
        {
            const string script = "CREATE ROLE [Plains Sales] AUTHORIZATION [dbo];";

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseUseStatement()
        {
            const string script = "USE [Clinic]; GO";

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
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
    }
}
