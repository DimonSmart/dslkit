using System.Linq;

namespace DSLKIT.Terminals
{
    public class CustomSpaceTerminal : SpaceTerminalBase
    {
        private readonly char[] _spaces;

        public CustomSpaceTerminal(char[] spaces)
        {
            _spaces = spaces;
        }

        public CustomSpaceTerminal()
        {
            _spaces = new[] { '\n', '\r', '\v', '\t', ' ' };
        }

        protected override bool IsSpace(char c)
        {
            return _spaces.Contains(c);
        }
    }
}