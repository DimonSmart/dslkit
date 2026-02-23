using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.GrammarExamples.Ini;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class IniGrammarExampleTests
    {
        [Theory]
        [MemberData(nameof(ValidIniFiles))]
        public void ParseDocument_ShouldParseValidIniExamples(string fileName, string source)
        {
            var result = IniGrammarExample.ParseDocument(source);

            result.ParseResult.IsSuccess.Should().BeTrue(
                $"INI file '{fileName}' should parse, but failed at {result.ParseResult.Error?.ErrorPosition}: {result.ParseResult.Error?.Message}");
            result.Document.Should().NotBeNull();
            result.Diagnostics.Should().BeEmpty($"file '{fileName}' should not need recovery.");
        }

        [Theory]
        [MemberData(nameof(RecoverableIniFiles))]
        public void ParseDocument_ShouldRecoverMissingEquals(string fileName, string source)
        {
            var result = IniGrammarExample.ParseDocument(source);

            result.ParseResult.IsSuccess.Should().BeTrue(
                $"INI file '{fileName}' should be recoverable, but failed at {result.ParseResult.Error?.ErrorPosition}: {result.ParseResult.Error?.Message}");
            result.Document.Should().NotBeNull();
            result.Diagnostics.Should().NotBeEmpty($"file '{fileName}' should produce recovery diagnostics.");
            result.Document!
                .Sections
                .SelectMany(section => section.Properties)
                .Any(property => property.IsRecoveredFromMissingEquals)
                .Should()
                .BeTrue($"file '{fileName}' should contain at least one recovered property.");
        }

        public static IEnumerable<object[]> ValidIniFiles()
        {
            return ReadIniFiles("Valid");
        }

        public static IEnumerable<object[]> RecoverableIniFiles()
        {
            return ReadIniFiles("Recoverable");
        }

        private static IEnumerable<object[]> ReadIniFiles(string profile)
        {
            var filesRoot = ResolveFilesRoot(profile);
            foreach (var filePath in Directory.EnumerateFiles(filesRoot, "*.ini", SearchOption.AllDirectories).OrderBy(i => i))
            {
                var fileName = Path.GetRelativePath(filesRoot, filePath);
                var source = File.ReadAllText(filePath);
                yield return new object[] { fileName, source };
            }
        }

        private static string ResolveFilesRoot(string profile)
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "GrammarExamples", "TestData", "Ini", profile);
            if (Directory.Exists(outputPath))
            {
                return outputPath;
            }

            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "GrammarExamples", "TestData", "Ini", profile);
            if (Directory.Exists(projectPath))
            {
                return projectPath;
            }

            throw new DirectoryNotFoundException($"Could not find INI test data folder for profile '{profile}'.");
        }
    }
}
