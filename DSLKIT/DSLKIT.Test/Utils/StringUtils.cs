namespace DSLKIT.Test.Utils
{
    public static class StringUtils
    {
        /// <summary>
        /// Normalizes line endings in text to Windows format (\r\n)
        /// </summary>
        /// <param name="text">Text to normalize</param>
        /// <returns>Text with normalized line endings</returns>
        public static string NormalizeLineEndings(this string text)
        {
            return text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
    }
}
