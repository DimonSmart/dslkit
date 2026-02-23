namespace DSLKIT.Terminals
{
    public class SpaceTerminal : SpaceTerminalBase
    {
        protected override bool IsSpace(char c)
        {
            return char.IsWhiteSpace(c) || char.IsControl(c);
        }
    }
}