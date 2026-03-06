using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlFormatterTests
    {
        [Theory]
        [MemberData(nameof(ValidFormattingScripts))]
        public void TryFormat_ShouldPreserveSqlContent_WhenIgnoringWhitespaceAndCase(string scriptName, string scriptText)
        {
            var formattingOptions = new SqlFormattingOptions
            {
                KeywordCase = SqlKeywordCase.Upper
            };

            var result = ModernMsSqlFormatter.TryFormat(scriptText, formattingOptions);

            result.IsSuccess.Should().BeTrue(
                $"script '{scriptName}' should format, but failed with: {result.ErrorMessage}");
            result.FormattedSql.Should().NotBeNullOrWhiteSpace();

            NormalizeSql(scriptText).Should().Be(
                NormalizeSql(result.FormattedSql!),
                $"formatted script '{scriptName}' should preserve all significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldReturnError_ForInvalidSql()
        {
            const string invalidSql = "SELECT FROM dbo.Orders;";

            var result = ModernMsSqlFormatter.TryFormat(invalidSql, new SqlFormattingOptions
            {
                KeywordCase = SqlKeywordCase.Upper
            });

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            result.FormattedSql.Should().BeNull();
        }

        [Fact]
        public void TryFormat_ShouldApplyKeywordCaseOnlyToKeywords()
        {
            const string sourceSql = "select o.CustomerId as customerAlias from dbo.Orders as o where o.CustomerId=@customerId";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                KeywordCase = SqlKeywordCase.Upper
            });

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql);
            formattedSql.Should().Contain("SELECT");
            formattedSql.Should().Contain("AS");
            formattedSql.Should().Contain("FROM");
            formattedSql.Should().Contain("WHERE");
            formattedSql.Should().Contain("o.CustomerId");
            formattedSql.Should().Contain("customerAlias");
            formattedSql.Should().Contain("dbo.Orders");
            formattedSql.Should().Contain("@customerId");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage1Settings()
        {
            const string sourceSql = "SELECT(a) AS A,b AS B FROM dbo.t AS t WHERE a=1";
            var options = new SqlFormattingOptions
            {
                Spaces = new SqlSpacesFormattingOptions
                {
                    AfterComma = false,
                    AroundBinaryOperators = false,
                    InsideParentheses = SqlParenthesesSpacing.Always,
                    BeforeSemicolon = true
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always
                },
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.WrapByWidth
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);

            result.IsSuccess.Should().BeTrue();
            result.FormattedSql.Should().Contain("SELECT ( a ) AS A,b AS B");
            result.FormattedSql.Should().Contain("WHERE a=1 ;");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage2LayoutSettings()
        {
            const string sourceSql = "SELECT a AS A, b AS B FROM dbo.t AS t WHERE x = 1 ORDER BY a";
            var options = new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    IndentSize = 2,
                    BlankLineBetweenClauses = SqlBlankLineBetweenClausesMode.BetweenMajorClauses,
                    NewlineBeforeClause = new SqlClauseNewlineOptions
                    {
                        With = true,
                        Select = true,
                        From = true,
                        Where = true,
                        GroupBy = true,
                        Having = true,
                        OrderBy = true,
                        Option = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("\n  a AS A,");
            formattedSql.Should().Contain("FROM dbo.t AS t\n\nWHERE");
            formattedSql.Should().Contain("WHERE x = 1\n\nORDER BY");
        }

        [Fact]
        public void TryFormat_ShouldToggleNewlineBeforeWithClause()
        {
            const string sourceSql =
                "CREATE VIEW dbo.v_sales AS WITH sales_cte AS (SELECT o.CustomerId FROM dbo.Orders AS o) SELECT CustomerId FROM sales_cte";

            var withNewlineOptions = new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    NewlineBeforeClause = new SqlClauseNewlineOptions
                    {
                        With = true,
                        Select = false,
                        From = false,
                        Where = false,
                        GroupBy = false,
                        Having = false,
                        OrderBy = false,
                        Option = false
                    }
                }
            };

            var withoutNewlineOptions = new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    NewlineBeforeClause = new SqlClauseNewlineOptions
                    {
                        With = false,
                        Select = false,
                        From = false,
                        Where = false,
                        GroupBy = false,
                        Having = false,
                        OrderBy = false,
                        Option = false
                    }
                }
            };

            var withNewlineResult = ModernMsSqlFormatter.TryFormat(sourceSql, withNewlineOptions);
            var withoutNewlineResult = ModernMsSqlFormatter.TryFormat(sourceSql, withoutNewlineOptions);

            withNewlineResult.IsSuccess.Should().BeTrue();
            withoutNewlineResult.IsSuccess.Should().BeTrue();

            NormalizeLineEndings(withNewlineResult.FormattedSql).Should().Contain("AS\nWITH sales_cte AS");
            NormalizeLineEndings(withoutNewlineResult.FormattedSql).Should().Contain("AS WITH sales_cte AS");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage3ListSettings()
        {
            const string sourceSql = "SELECT SUM(x) AS Total, COUNT(*) AS Cnt FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    CommaStyle = SqlCommaStyle.Leading,
                    SelectItems = SqlListLayoutStyle.OnePerLine
                },
                Align = new SqlAlignFormattingOptions
                {
                    SelectAliases = true
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().MatchRegex("SUM\\s*\\(x\\)\\s{2,}AS\\s+Total");
            formattedSql.Should().MatchRegex("\\n\\s*,\\s*COUNT\\s*\\(\\s*\\*\\s*\\)\\s+AS\\s+Cnt");
        }

        [Fact]
        public void TryFormat_ShouldCompactShortSelect_WhenThresholdAllowsIt()
        {
            const string sourceSql = "SELECT a AS A, b AS B FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine,
                    SelectCompactThreshold = new SqlSelectCompactThresholdOptions
                    {
                        MaxItems = 2,
                        MaxLineLength = 120,
                        AllowExpressions = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT a AS A, b AS B");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage4JoinSettings()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id=b.Id AND a.Type=b.Type AND a.IsActive=1";
            var options = new SqlFormattingOptions
            {
                Joins = new SqlJoinsFormattingOptions
                {
                    NewlinePerJoin = true,
                    OnNewLine = true,
                    MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                    {
                        MaxTokensSingleLine = 5,
                        BreakOnAnd = true,
                        BreakOnOr = false
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("INNER JOIN dbo.B AS b");
            formattedSql.Should().MatchRegex("\\n\\s+ON\\s+a\\.Id\\s*=\\s*b\\.Id");
            formattedSql.Should().MatchRegex("\\n\\s+AND\\s+a\\.TYPE\\s*=\\s*b\\.TYPE");
            formattedSql.Should().MatchRegex("\\n\\s+AND\\s+a\\.IsActive\\s*=\\s*1");
        }

        [Fact]
        public void TryFormat_ShouldApplyJoinBreakOnFlags()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id=b.Id AND a.Type=b.Type OR a.Flag=b.Flag";

            var andOnlyOptions = new SqlFormattingOptions
            {
                Joins = new SqlJoinsFormattingOptions
                {
                    NewlinePerJoin = true,
                    OnNewLine = true,
                    MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                    {
                        MaxTokensSingleLine = 1,
                        BreakOnAnd = true,
                        BreakOnOr = false
                    }
                }
            };

            var andOnlyResult = ModernMsSqlFormatter.TryFormat(sourceSql, andOnlyOptions);
            var andOnlyFormattedSql = NormalizeLineEndings(andOnlyResult.FormattedSql);

            andOnlyResult.IsSuccess.Should().BeTrue();
            andOnlyFormattedSql.Should().MatchRegex("AND\\s+a\\.TYPE\\s*=\\s*b\\.TYPE\\s+OR\\s+a\\.Flag\\s*=\\s*b\\.Flag");

            var andOrOptions = new SqlFormattingOptions
            {
                Joins = new SqlJoinsFormattingOptions
                {
                    NewlinePerJoin = true,
                    OnNewLine = true,
                    MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                    {
                        MaxTokensSingleLine = 1,
                        BreakOnAnd = true,
                        BreakOnOr = true
                    }
                }
            };

            var andOrResult = ModernMsSqlFormatter.TryFormat(sourceSql, andOrOptions);
            var andOrFormattedSql = NormalizeLineEndings(andOrResult.FormattedSql);

            andOrResult.IsSuccess.Should().BeTrue();
            andOrFormattedSql.Should().MatchRegex("\\n\\s+OR\\s+a\\.Flag\\s*=\\s*b\\.Flag");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage5PredicateSettings()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1 AND b = 2 OR c = 3";
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = true,
                    LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.BeforeOperator,
                    InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                    {
                        MaxConditions = 0,
                        MaxLineLength = 120,
                        AllowOnlyAnd = true
                    },
                    ParenthesizeMixedAndOr = new SqlParenthesizeMixedAndOrOptions
                    {
                        Mode = SqlParenthesizeMixedAndOrMode.AlwaysForOrGroups
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().MatchRegex("WHERE\\n\\s+a\\s*=\\s*1\\n\\s+AND\\s+\\(b\\s*=\\s*2\\s+OR\\s+c\\s*=\\s*3\\)");
        }

        [Fact]
        public void TryFormat_ShouldInlineSimplePredicate_WhenHeuristicAllowsIt()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1";
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = true,
                    InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                    {
                        MaxConditions = 1,
                        MaxLineLength = 120,
                        AllowOnlyAnd = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("WHERE a = 1");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage6CaseLayoutSettings()
        {
            const string sourceSql = "SELECT CASE WHEN x=1 THEN 'A' ELSE 'B' END AS v FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Expressions = new SqlExpressionsFormattingOptions
                {
                    CaseStyle = SqlCaseStyle.CompactWhenShort,
                    CompactCaseThreshold = new SqlCompactCaseThresholdOptions
                    {
                        MaxWhenClauses = 1,
                        MaxTokens = 20,
                        MaxLineLength = 120
                    }
                },
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("CASE WHEN x = 1 THEN 'A' ELSE 'B' END AS v");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage6InListSettings()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a IN (1,2,3,4)";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    InListItems = SqlInListItemsStyle.OnePerLine,
                    InlineInListThreshold = new SqlInlineInListThresholdOptions
                    {
                        MaxItemsInline = 0,
                        MaxLineLength = 120
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("IN (\n");
            formattedSql.Should().Contain("\n    1,\n");
            formattedSql.Should().Contain("\n    4\n");
        }

        [Fact]
        public void TryFormat_ShouldInlineShortSelectExpression_WhenStage6HeuristicAllowsIt()
        {
            const string sourceSql = "SELECT a+b+c+d AS s FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine
                },
                Expressions = new SqlExpressionsFormattingOptions
                {
                    InlineShortExpression = new SqlInlineShortExpressionOptions
                    {
                        MaxTokens = 16,
                        MaxDepth = 0,
                        MaxLineLength = 120,
                        ForContexts = [SqlInlineExpressionContext.SelectItem]
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT a + b + c + d AS s");
            formattedSql.Should().Contain("\nFROM dbo.t AS t");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage7DmlDdlSettings()
        {
            const string sourceSql = "UPDATE dbo.t SET a=1,b=2 WHERE id=@id; CREATE PROC p AS BEGIN SELECT 1 END";
            var options = new SqlFormattingOptions
            {
                Dml = new SqlDmlFormattingOptions
                {
                    UpdateSetStyle = SqlDmlListStyle.OnePerLine,
                    InsertColumnsStyle = SqlDmlListStyle.OnePerLine
                },
                Ddl = new SqlDdlFormattingOptions
                {
                    CreateProcLayout = SqlCreateProcLayout.Expanded
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("UPDATE dbo.t\nSET\n");
            formattedSql.Should().Contain("\n    a = 1,\n");
            formattedSql.Should().Contain("\nWHERE id = @id;\n");
            formattedSql.Should().Contain("CREATE PROC p\nAS\nBEGIN\n");
            formattedSql.Should().Contain("\n    SELECT\n        1\nEND");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage8CommentFormatting()
        {
            const string sourceSql = "SELECT a /*  keep   spacing */ AS b FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Comments = new SqlCommentsFormattingOptions
                {
                    PreserveAttachment = true,
                    Formatting = SqlCommentsFormattingMode.ReflowSafeOnly
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);

            result.IsSuccess.Should().BeTrue();
            result.FormattedSql.Should().Contain("/* keep spacing */");
        }

        [Fact]
        public void TryFormat_ShouldKeepSingleLineCommentOnDedicatedLine_WhenRenderingInlineNodes()
        {
            const string sourceSql = """
                SELECT
                    -- keep   spacing
                    c.CustomerId /*  keep   spacing */ AS customer_id,
                    c.Region AS region
                FROM dbo.Customers AS c;
                """;

            var options = new SqlFormattingOptions
            {
                Comments = new SqlCommentsFormattingOptions
                {
                    PreserveAttachment = true,
                    Formatting = SqlCommentsFormattingMode.ReflowSafeOnly
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);

            result.IsSuccess.Should().BeTrue();

            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            formattedSql.Should().Contain("-- keep spacing\n");
            formattedSql.Should().Contain("/* keep spacing */");
            formattedSql.Should().NotContain("-- keep spacing c.CustomerId");
        }

        [Fact]
        public void TryFormat_ShouldPreserveCommentsInsideSplitMultiKeywordConstructs()
        {
            const string sourceSql = """
                CREATE VIEW dbo.vTrivia AS
                SELECT 1 AS A
                WITH /*check-before*/ CHECK /*option-before*/ OPTION;

                SELECT ProductID
                FROM Product FOR /*system-time-before*/ SYSTEM_TIME AS OF '2015-07-28 13:20:00';

                SELECT 1
                FROM Product FOR /*path-before*/ PATH p;
                """;

            var options = new SqlFormattingOptions
            {
                Comments = new SqlCommentsFormattingOptions
                {
                    PreserveAttachment = true,
                    Formatting = SqlCommentsFormattingMode.Keep
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);

            result.IsSuccess.Should().BeTrue();

            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(NormalizeSql(sourceSql));
            formattedSql.Should().Contain("WITH /*check-before*/ CHECK");
            formattedSql.Should().Contain("/*option-before*/");
            formattedSql.Should().Contain("FOR /*system-time-before*/ SYSTEM_TIME AS OF");
            formattedSql.Should().Contain("FOR /*path-before*/ PATH p");
        }

        [Fact]
        public void TryFormat_ShouldFormatCreateDatabase_WithPopularOptions()
        {
            const string sourceSql = """
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
                LOG ON
                (
                    NAME = SalesLog,
                    FILENAME = 'C:\data\sales.ldf',
                    FILEGROWTH = 10%
                )
                WITH
                FILESTREAM ( DIRECTORY_NAME = 'salesfs', NON_TRANSACTED_ACCESS = FULL ),
                DEFAULT_FULLTEXT_LANGUAGE = 1033,
                DB_CHAINING ON,
                LEDGER = OFF;
                GO
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            formattedSql.Should().Contain("CREATE DATABASE Sales");
            formattedSql.Should().Contain("FILESTREAM (DIRECTORY_NAME = 'salesfs', NON_TRANSACTED_ACCESS =");
            formattedSql.Should().Contain("FULL");
            formattedSql.Should().Contain("GO");
        }

        [Fact]
        public void TryFormat_ShouldFormatCreateRole_WithAuthorization()
        {
            const string sourceSql = "CREATE ROLE [Plains Sales] AUTHORIZATION [dbo];";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql("CREATE ROLE [Plains Sales] AUTHORIZATION [dbo];"),
                "formatted CREATE ROLE should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatUseStatement()
        {
            const string sourceSql = """
                USE [Clinic];
                GO
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted USE should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatCreateSchema_BasicAndAuthorization()
        {
            const string sourceSql = """
                CREATE SCHEMA ext;
                GO
                CREATE SCHEMA [sales] AUTHORIZATION [dbo];
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted CREATE SCHEMA should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldReturnError_ForInlineGoBatchSeparator()
        {
            const string sourceSql = "USE [Clinic]; GO";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void TryFormat_ShouldFormatGoBatchRepeat_OnDedicatedLine()
        {
            const string sourceSql = """
                SELECT 1;
                GO 5
                SELECT 2;
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            NormalizeSql(result.FormattedSql!).Should().Be(
                NormalizeSql(sourceSql),
                "formatted GO batch repeat should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatSqlcmdPreprocessorCommands_OnDedicatedLines()
        {
            const string sourceSql = """
                :r .\setup.sql
                :setvar JobOwner sa
                :on error exit
                PRINT N'after sqlcmd preprocessor commands';
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            NormalizeSql(result.FormattedSql!).Should().Be(
                NormalizeSql(sourceSql),
                "formatted SQLCMD control lines should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldReturnError_ForInlineSqlcmdPreprocessorCommand()
        {
            const string sourceSql = "PRINT N'before'; :setvar JobOwner sa";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void TryFormat_ShouldFormatIfDeclareSetAndCreateView()
        {
            const string sourceSql = """
                IF EXISTS (SELECT 1 FROM dbo.TestTable)
                BEGIN
                    DECLARE @counter INT = 1;
                    SET @counter = @counter + 1;
                    PRINT @counter;
                    SELECT @counter;
                END;
                GO
                CREATE VIEW dbo.vTest AS SELECT 1 AS A;
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted output should preserve significant SQL tokens for IF/DECLARE/SET/CREATE VIEW.");
        }

        [Fact]
        public void TryFormat_ShouldFormatExecuteStatement_Variants()
        {
            const string sourceSql = """
                DECLARE @policy_id INT
                EXEC msdb.dbo.sp_syspolicy_add_policy @name=N'Policy', @enabled=True, @policy_id=@policy_id OUTPUT
                SELECT @policy_id;

                EXECUTE @return_code = dbo.usp_DoWork @arg1 = DEFAULT, @arg2 = @policy_id OUT WITH RECOMPILE;
                EXECUTE ('SELECT 1' + N' AS Value') AS USER = 'dbo';
                EXECUTE (N'SELECT * FROM dbo.T WHERE Id = ?', @policy_id OUTPUT) AT DATA_SOURCE [RemoteSource];
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted EXEC/EXECUTE variants should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatInsertExec_Variants()
        {
            const string sourceSql = """
                INSERT INTO dbo.TargetTable EXEC dbo.usp_FillTarget;
                INSERT dbo.TargetTable (A, B) EXECUTE dbo.usp_FillTargetByParams @a = 1, @b = DEFAULT;
                INSERT INTO [dbo].[models]
                EXEC sp_execute_external_script
                    @language = N'R',
                    @script = N'SELECT 1';
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted INSERT EXEC variants should preserve significant SQL tokens.");
        }

        public static IEnumerable<object[]> ValidFormattingScripts()
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
            var outputPath = Path.Combine(AppContext.BaseDirectory, "GrammarExamples", "TestData", "MsSql", "Formatting", "Valid");
            if (Directory.Exists(outputPath))
            {
                return outputPath;
            }

            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "GrammarExamples", "TestData", "MsSql", "Formatting", "Valid");
            if (Directory.Exists(projectPath))
            {
                return projectPath;
            }

            throw new DirectoryNotFoundException("Could not find SQL formatter test data folder.");
        }

        private static string NormalizeSql(string sqlText)
        {
            return new string(
                sqlText
                    .Where(symbol => !char.IsWhiteSpace(symbol))
                    .Select(char.ToUpperInvariant)
                    .ToArray());
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n");
        }
    }
}
