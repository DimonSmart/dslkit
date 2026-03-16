using System;
using DSLKIT.Parser;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    public sealed record SqlFormattingResult
    {
        private SqlFormattingResult(
            bool isSuccess,
            string? formattedSql,
            string? errorMessage,
            ParseErrorDescription? parseError)
        {
            IsSuccess = isSuccess;
            FormattedSql = formattedSql;
            ErrorMessage = errorMessage;
            ParseError = parseError;
        }

        public bool IsSuccess { get; }

        public string? FormattedSql { get; }

        public string? ErrorMessage { get; }

        public ParseErrorDescription? ParseError { get; }

        public static SqlFormattingResult Success(string formattedSql)
        {
            ArgumentNullException.ThrowIfNull(formattedSql);
            return new SqlFormattingResult(isSuccess: true, formattedSql, errorMessage: null, parseError: null);
        }

        public static SqlFormattingResult Failure(ParseErrorDescription parseError)
        {
            ArgumentNullException.ThrowIfNull(parseError);
            return new SqlFormattingResult(
                isSuccess: false,
                formattedSql: null,
                errorMessage: parseError.ToString(),
                parseError);
        }

        public static SqlFormattingResult Failure(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
            }

            return new SqlFormattingResult(isSuccess: false, formattedSql: null, errorMessage, parseError: null);
        }
    }
}
