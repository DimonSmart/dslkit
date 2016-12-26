using System;
using System.Collections.Generic;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class TermList : List<ITerm>
    {
        public override string ToString()
        {
            try
            {
                return string.Join("+", this);
            }
            catch (Exception e)
            {
                return "(error: " + e.Message + ")";
            }
        }
    }
}