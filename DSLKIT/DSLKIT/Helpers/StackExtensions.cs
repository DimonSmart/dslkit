using System.Collections.Generic;

namespace DSLKIT.Helpers
{
    public static class StackExtensions
    {
        /// <summary>
        /// Pops the specified number of items from the stack.
        /// </summary>
        public static void PopMany<T>(this Stack<T> stack, int count)
        {
            for (var i = 0; i < count; i++)
            {
                stack.Pop();
            }
        }
    }
}
