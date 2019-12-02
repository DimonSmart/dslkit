using System.IO;

namespace DSLKIT.Test
{
    public static class LexerTestData
    {
        public static string SampleText;
        static LexerTestData()
        {
            SampleText = File.ReadAllText("LexerTestData.txt");
        }
    }
}