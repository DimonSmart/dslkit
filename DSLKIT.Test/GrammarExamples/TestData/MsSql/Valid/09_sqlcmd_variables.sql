-- SQLCMD/SSDT substitution variables used as database names and in expressions

IF EXISTS (SELECT [name] FROM [master].[sys].[databases] WHERE [name] = N'$(DatabaseName)')
    DROP DATABASE $(DatabaseName);

CREATE DATABASE $(DatabaseName);

USE $(DatabaseName);

IF NOT EXISTS (SELECT 1 FROM dbo.SampleVersion)
BEGIN
    INSERT dbo.SampleVersion (MajorSampleVersion, MinorSampleVersion, MinSQLServerBuild)
    VALUES (2, 0, N'13.0.4000.0')
END

SELECT *
FROM $(DefaultDataPath).dbo.SomeTable;
