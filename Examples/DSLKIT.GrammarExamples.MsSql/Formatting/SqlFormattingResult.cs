using System;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    public sealed record SqlFormattingResult
    {
        private SqlFormattingResult(bool isSuccess, string? formattedSql, string? errorMessage)
        {
            IsSuccess = isSuccess;
            FormattedSql = formattedSql;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string? FormattedSql { get; }

        public string? ErrorMessage { get; }

        public static SqlFormattingResult Success(string formattedSql)
        {
            ArgumentNullException.ThrowIfNull(formattedSql);
            return new SqlFormattingResult(isSuccess: true, formattedSql, errorMessage: null);
        }

        public static SqlFormattingResult Failure(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
            }

            return new SqlFormattingResult(isSuccess: false, formattedSql: null, errorMessage);
        }
    }
}
