using System;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [InlineData("SELECT 1 WHERE 1;")]
        [InlineData("SELECT 1 WHERE SomeColumn IN OtherColumn;")]
        [InlineData("SELECT 1 WHERE SomeColumn IS 5;")]
        [InlineData("SELECT 1 WHERE SomeColumn IS OtherColumn + 1;")]
        public void ParseScript_ShouldRejectScalarExpressionsAsSearchConditions(string script)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(
                $"script '{script}' should not parse because WHERE requires a predicate.");
        }

        [Theory]
        [InlineData("SELECT 1 WHERE SomeColumn IN (1, 2, 3);")]
        [InlineData("SELECT 1 WHERE SomeColumn IN (SELECT OtherColumn FROM dbo.OtherTable);")]
        [InlineData("SELECT 1 WHERE SomeColumn IS NULL;")]
        [InlineData("SELECT 1 WHERE SomeColumn IS NOT NULL;")]
        public void ParseScript_ShouldParseStructuredSearchPredicates(string script)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script '{script}' should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseGroupBy_WithRollupAndCube()
        {
            const string script = """
                SELECT a
                FROM dbo.T
                GROUP BY a WITH ROLLUP;

                SELECT a, b
                FROM dbo.T
                GROUP BY a, b WITH CUBE;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectGroupBy_WithUnknownLegacyModifier()
        {
            const string script = """
                SELECT a
                FROM dbo.T
                GROUP BY a WITH BANANA;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("GROUP BY WITH should only accept legacy ROLLUP/CUBE modifiers.");
        }

        [Fact]
        public void ParseScript_ShouldParseBitwiseOrAndXorExpressions()
        {
            const string script = """
                SELECT a | b AS BitwiseOrResult
                FROM dbo.Flags;

                SELECT a ^ b AS BitwiseXorResult
                FROM dbo.Flags;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            """
            MERGE dbo.Target AS WAITFOR
            USING dbo.Source AS src
            ON WAITFOR.Id = src.Id
            WHEN MATCHED THEN DELETE;
            """,
            "MERGE target aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            MERGE dbo.Target WAITFOR
            USING dbo.Source AS src
            ON WAITFOR.Id = src.Id
            WHEN MATCHED THEN DELETE;
            """,
            "MERGE target aliases without AS should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT * FROM PRODUCT P1, PRODUCT P2, ISPARTOF IPO WHERE MATCH(WAITFOR-(IPO)->P2);",
            "Graph MATCH identifiers should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            CREATE EXTERNAL DATA SOURCE WAITFOR
            WITH (TYPE = BLOB_STORAGE, LOCATION = 'https://example.com/path');
            """,
            "CREATE EXTERNAL DATA SOURCE names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectExtensionIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Theory]
        [InlineData(
            "EXECUTE dbo.usp_DoWork WITH RESULT SETS ((WAITFOR INT));",
            "EXECUTE RESULT SETS column names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "EXECUTE (N'SELECT 1') AT DATA_SOURCE WAITFOR;",
            "EXECUTE AT DATA_SOURCE targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            SELECT *
            FROM OPENJSON(@json)
            WITH
            (
                WAITFOR INT
            );
            """,
            "OPENJSON WITH column names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            SELECT *
            FROM
            (
                SELECT 2024 AS Yr, 10 AS Amount
            ) AS src
            PIVOT
            (
                SUM(Amount) FOR WAITFOR IN ([2024])
            ) AS p;
            """,
            "PIVOT FOR targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            SELECT *
            FROM
            (
                SELECT 2024 AS Yr, 10 AS Amount
            ) AS src
            UNPIVOT
            (
                WAITFOR FOR Attr IN (Yr)
            ) AS u;
            """,
            "UNPIVOT value targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            SELECT *
            FROM
            (
                SELECT 2024 AS Yr, 10 AS Amount
            ) AS src
            UNPIVOT
            (
                Amount FOR WAITFOR IN (Yr)
            ) AS u;
            """,
            "UNPIVOT FOR targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            SELECT *
            FROM
            (
                SELECT 2024 AS Yr, 10 AS Amount
            ) AS src
            UNPIVOT
            (
                Amount FOR Attr IN (WAITFOR)
            ) AS u;
            """,
            "UNPIVOT column lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT 1 AS WAITFOR;",
            "SELECT aliases with AS should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT 1 WAITFOR, 2 AS B;",
            "SELECT aliases without AS should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectQuerySideIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Theory]
        [InlineData(
            "SELECT * FROM dbo.T AS WAITFOR;",
            "Table aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT * FROM OPENJSON(@json) AS WAITFOR;",
            "OPENJSON aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT * FROM (SELECT 1 AS X) AS WAITFOR;",
            "Derived table aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT * FROM (VALUES (1)) AS v(WAITFOR);",
            "Derived table column alias lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT * FROM PRODUCT FOR PATH WAITFOR;",
            "FOR PATH aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            SELECT *
            FROM
            (
                SELECT 2024 AS Yr, 10 AS Amount
            ) AS src
            PIVOT
            (
                SUM(Amount) FOR Yr IN ([2024])
            ) AS WAITFOR;
            """,
            "PIVOT aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            SELECT *
            FROM
            (
                SELECT 2024 AS Yr, 10 AS Amount
            ) AS src
            UNPIVOT
            (
                Amount FOR Attr IN (Yr)
            ) AS WAITFOR;
            """,
            "UNPIVOT aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectTableSourceAliases_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Fact]
        public void ParseScript_ShouldParseTemporalTable_ForSystemTimeClauses()
        {
            const string script = """
                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME AS OF '2015-07-28 13:20:00';

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME ALL
                WHERE ProductID = 17
                ORDER BY DateModified DESC;

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME BETWEEN '2015-01-01' AND '2016-01-01';

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME FROM '2015-01-01' TO '2016-01-01';

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME AS OF @date AS p;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseOpenJsonWithClause()
        {
            const string script = """
                SELECT j.RegionId, j.RegionMeta
                FROM OPENJSON(@json)
                WITH
                (
                    RegionId INT '$.id',
                    RegionMeta NVARCHAR(MAX) N'$.meta' AS JSON
                ) AS j;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectOpenJsonWithClause_OnNonOpenJsonFunction()
        {
            const string script = """
                SELECT *
                FROM dbo.SomeTvf(@x) WITH (a INT);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("OPENJSON WITH (...) must not be accepted on arbitrary function calls.");
        }

        [Fact]
        public void ParseScript_ShouldRejectNonLiteralOpenJsonColumnPath()
        {
            const string script = """
                SELECT *
                FROM OPENJSON(@json)
                WITH
                (
                    RegionId INT @path
                );
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("OPENJSON column paths should be string literals in the strict grammar.");
        }

        [Fact]
        public void ParseScript_ShouldParseSetOperators_WithIntersectMix()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                INTERSECT
                SELECT 2 AS X;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldKeepIntersectInsideRightBranch_OfUnionExpression()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                INTERSECT
                SELECT 2 AS X;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var intersectPath = FindTerminalPaths(parseResult.ParseTree!, "INTERSECT").Single();

            CountPathSegment(intersectPath, "QueryUnionExpression").Should().Be(1);
            string.Join(" > ", intersectPath)
                .Should()
                .Contain("QueryExpression > QueryUnionExpression > QueryIntersectExpression");
        }

        [Fact]
        public void ParseScript_ShouldKeepExceptLeftAssociative_InUnionChain()
        {
            const string script = """
                SELECT 1 AS X
                EXCEPT
                SELECT 2 AS X
                UNION
                SELECT 3 AS X;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var exceptPath = FindTerminalPaths(parseResult.ParseTree!, "EXCEPT").Single();

            CountPathSegment(exceptPath, "QueryUnionExpression").Should().Be(2);
        }

        [Fact]
        public void ParseScript_ShouldAttachSetOperatorOrderBy_ToQueryExpression()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                ORDER BY 1;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var orderByPath = FindNonTerminalPath(parseResult.ParseTree!, "OrderByClause");

            orderByPath.Should().NotBeNull();
            var orderByPathText = string.Join(" > ", orderByPath!);
            orderByPathText.Should().Contain("QueryExpression > QueryExpressionTail > QueryExpressionOrderByAndOffsetOpt > OrderByClause");
            orderByPathText.Should().NotContain("QueryPrimary");
        }

        [Fact]
        public void ParseScript_ShouldAttachSetOperatorForClause_ToQueryExpression()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                FOR XML AUTO;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var forClausePath = FindNonTerminalPath(parseResult.ParseTree!, "ForClause");

            forClausePath.Should().NotBeNull();
            var forClausePathText = string.Join(" > ", forClausePath!);
            forClausePathText.Should().Contain("QueryExpression > QueryExpressionTail > QueryExpressionForOpt > ForClause");
            forClausePathText.Should().NotContain("QueryPrimary");
        }

        [Fact]
        public void ParseScript_ShouldAttachSetOperatorOption_ToQueryExpression()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                OPTION (RECOMPILE);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var optionClausePath = FindNonTerminalPath(parseResult.ParseTree!, "OptionClause");

            optionClausePath.Should().NotBeNull();
            var optionClausePathText = string.Join(" > ", optionClausePath!);
            optionClausePathText.Should().Contain("QueryExpression > QueryExpressionTail > QueryExpressionOptionOpt > OptionClause");
            optionClausePathText.Should().NotContain("QueryPrimary");
        }

        [Fact]
        public void ParseScript_ShouldParseOptionClause_CommonHints()
        {
            const string script = """
                SELECT 1
                OPTION (
                    RECOMPILE,
                    MAXDOP 1,
                    QUERYTRACEON 9481,
                    MAXRECURSION 25,
                    MIN_GRANT_PERCENT = 20,
                    LABEL = 'unit-test',
                    USE HINT('DISALLOW_BATCH_MODE'),
                    LOOP JOIN,
                    IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX
                );
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseWhere_WithLogicalAndArithmeticPrecedenceMix()
        {
            const string script = """
                SELECT *
                FROM dbo.T
                WHERE A = 1 OR B = 2 AND C + 3 * D > 10;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseAggregateFunction_WithDistinctModifier()
        {
            const string script = """
                SELECT
                    COUNT(DISTINCT ProductKey)         AS DistinctProducts,
                    COUNT(DISTINCT CustomerKey)        AS DistinctCustomers,
                    COUNT(DISTINCT query_hash)         AS DistinctQueries,
                    AVG(DISTINCT CONVERT(BIGINT, qty)) AS AvgDistinct,
                    SUM(ALL Price)                     AS TotalPrice
                FROM dbo.Sales;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSetOptions_WithContextualKeywords()
        {
            const string script = """
                SET LANGUAGE us_english;
                SET ROWCOUNT 10;
                SET STATISTICS IO ON;
                SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseXmlMethods_OnColumnReferences()
        {
            const string script = """
                SELECT XmlCol.value('(/x)[1]', 'int')
                FROM dbo.T;

                SELECT t.XmlCol.query('/root')
                FROM dbo.T AS t;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseDerivedTableUnpivot()
        {
            const string script = """
                SELECT src.EmployeeId, src.QuarterName, src.SalesAmount
                FROM
                (
                    SELECT EmployeeId, Q1, Q2, Q3, Q4
                    FROM dbo.SalesByQuarter
                ) AS d
                UNPIVOT
                (
                    SalesAmount FOR QuarterName IN (Q1, Q2, Q3, Q4)
                ) AS src;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseBaseTablePivot()
        {
            const string script = """
                SELECT p.[2024]
                FROM dbo.Sales AS s
                PIVOT
                (
                    SUM(Amount) FOR Yr IN ([2024])
                ) AS p;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseParenthesizedScalarExpressions()
        {
            const string script = """
                SELECT (1) AS SingleValue;
                SELECT ((1)) AS NestedValue;
                SELECT CAST((1) AS INT) AS CastValue;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }
    }
}