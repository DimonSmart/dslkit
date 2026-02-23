using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DSLKIT.Tokens;

namespace DSLKIT.Test.Utils
{
    public static class TokenUtils
    {
        public static void Dump(this IEnumerable<IToken> tokens)
        {
            Debug.WriteLine(tokens.GetVerboseDescription());
        }

        public static string GetVerboseDescription(this IEnumerable<IToken> tokens)
        {
            var sb = new StringBuilder();
            foreach (var token in tokens)
            {
                sb.AppendLine(token.Terminal?.GetType().Name ?? "Error");

                sb.AppendLine(GetNonEmptyString("OriginalString: ", token.OriginalString));
                sb.AppendLine(GetNonEmptyString("Value: ", token.Value));
                sb.AppendLine(GetNonEmptyString("Token: ", token.ToString()));
                sb.AppendLine(GetNonEmptyString("Position: ", token.Position));
                sb.AppendLine(GetNonEmptyString("Length: ", token.Length));
            }

            return sb.ToString();
        }

        private static string GetNonEmptyString(string key, object value)
        {
            var s = value?.ToString();

            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            return key + s + Environment.NewLine;
        }
    }
}