using System;

namespace DSLKIT.Parser
{
    public class AcceptAction : IActionItem
    {
        private AcceptAction()
        {
        }

        public static readonly Lazy<AcceptAction> Lazy = new Lazy<AcceptAction>(() => new AcceptAction());
        public static AcceptAction Instance => Lazy.Value;
    }
}