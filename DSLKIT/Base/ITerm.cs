namespace DSLKIT.Base
{
    /// <summary>
    /// Base interface for both Terminal's and NonTerminals
    /// </summary>
    public interface ITerm
    {
        /// <summary>
        /// User-friendly term label used in grammar text and diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Stable identity key used for deduplication and structural equality.
        /// Defaults to <see cref="Name"/>.
        /// </summary>
        string DictionaryKey => Name;
    }
}
