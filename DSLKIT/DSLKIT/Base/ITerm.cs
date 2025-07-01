namespace DSLKIT.Base
{
    /// <summary>
    /// Base interface for both Terminal's and NonTerminals
    /// </summary>
    public interface ITerm
    {
        /// <summary>
        /// Term name used like ID.
        /// For simplicity, we assumed that all names are unique.
        /// </summary>
        string Name { get; }
    }
}