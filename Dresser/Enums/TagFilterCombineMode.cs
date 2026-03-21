namespace Dresser.Enums
{
	/// <summary>
	/// Determines how multiple inclusive tags are combined when filtering items.
	/// </summary>
	public enum TagFilterCombineMode
	{
		/// <summary>
		/// Items matching ANY of the inclusive tags are shown (OR logic).
		/// </summary>
		Any = 0,

		/// <summary>
		/// Only items matching ALL of the inclusive tags are shown (AND logic).
		/// </summary>
		All = 1,
	}
}
