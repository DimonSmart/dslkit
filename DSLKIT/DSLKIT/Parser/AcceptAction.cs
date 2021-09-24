using System;

namespace DSLKIT.Parser
{
    public class AcceptAction : IActionItem
    {
        public static readonly Lazy<AcceptAction> Lazy = new Lazy<AcceptAction>(() => new AcceptAction());
        public static AcceptAction Instance => Lazy.Value;

        private AcceptAction()
        {
        }

        public override string ToString()
        {
            return "Accept";
        }
    }
}