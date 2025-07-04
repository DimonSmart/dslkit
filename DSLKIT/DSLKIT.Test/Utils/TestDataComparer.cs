using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DSLKIT.Test.Utils
{
    /// <summary>
    /// Utility for comparing objects with expected results from JSON files
    /// </summary>
    public static class TestDataComparer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        public static void AssertMatches<T>(T actual, string expectedDataFileName)
        {
            var expectedJson = LoadExpectedData(expectedDataFileName);
            var actualJson = JsonSerializer.Serialize(actual, JsonOptions);

            var expectedNormalized = NormalizeJson(expectedJson);
            var actualNormalized = NormalizeJson(actualJson);

            if (expectedNormalized != actualNormalized)
            {
                var differences = FindDifferences(expectedNormalized, actualNormalized);
                var errorMessage = $"Objects do not match expected data from file '{expectedDataFileName}'.\n\n" +
                                 $"Found differences:\n{differences}\n\n" +
                                 $"Expected:\n{expectedJson}\n\n" +
                                 $"Actual:\n{actualJson}";

                throw new AssertionException(errorMessage);
            }
        }

        private static string LoadExpectedData(string fileName)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(basePath, "LexerTests", "TestData", fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Expected data file not found: {filePath}");
            }

            return File.ReadAllText(filePath);
        }

        private static string NormalizeJson(string json)
        {
            var jsonDocument = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = false });
        }

        private static string FindDifferences(string expected, string actual)
        {
            var differences = new List<string>();

            if (expected.Length != actual.Length)
            {
                differences.Add($"Different JSON length: expected {expected.Length}, got {actual.Length}");
            }

            // Find first differing characters
            var minLength = Math.Min(expected.Length, actual.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (expected[i] != actual[i])
                {
                    var context = Math.Max(0, i - 50);
                    var contextLength = Math.Min(100, minLength - context);

                    differences.Add($"First difference at position {i}:");
                    differences.Add($"Expected: ...{expected.Substring(context, contextLength)}...");
                    differences.Add($"Got:      ...{actual.Substring(context, contextLength)}...");
                    break;
                }
            }

            return string.Join("\n", differences);
        }
    }

    public class AssertionException(string message) : Exception(message)
    {
    }
}
