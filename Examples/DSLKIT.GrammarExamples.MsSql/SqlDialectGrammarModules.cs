using System;
using System.Collections.Generic;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal abstract class SqlDialectGrammarModule
    {
        public abstract SqlDialect Dialect { get; }

        public abstract MsSqlDialectFeatures DefaultFeatures { get; }

        public abstract MsSqlDialectFeatures NormalizeFeatures(MsSqlDialectFeatures dialectFeatures);

        public abstract IReadOnlyCollection<object> CreateLeadingWithStatementAlternatives(SqlDialectGrammarModuleContext context);

        public abstract void RegisterStatements(MsSqlStatementRegistry statementRegistry, SqlDialectGrammarModuleContext context);

        public abstract void Apply(SqlDialectGrammarModuleContext context);
    }

    internal static class SqlDialectGrammarModules
    {
        private static readonly SqlDialectGrammarModule SqlServer = new SqlServerDialectGrammarModule();
        private static readonly SqlDialectGrammarModule Snowflake = new SnowflakeDialectGrammarModule();

        public static SqlDialectGrammarModule Resolve(SqlDialect dialect)
        {
            return dialect switch
            {
                SqlDialect.Snowflake => Snowflake,
                _ => SqlServer
            };
        }

        public static SqlDialectGrammarModule Resolve(MsSqlDialectFeatures dialectFeatures)
        {
            return (dialectFeatures & MsSqlDialectFeatures.SnowflakeCompat) == MsSqlDialectFeatures.SnowflakeCompat
                ? Snowflake
                : SqlServer;
        }

        public static MsSqlDialectFeatures GetDefaultFeatures(SqlDialect dialect)
        {
            return Resolve(dialect).DefaultFeatures;
        }

        public static MsSqlDialectFeatures NormalizeFeatures(MsSqlDialectFeatures dialectFeatures)
        {
            return Resolve(dialectFeatures).NormalizeFeatures(dialectFeatures);
        }
    }
}
