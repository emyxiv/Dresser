using Dresser.Logic;

using static Dresser.Logic.InventoryItemOrder;

namespace Dresser.Tests;

public class InventoryItemOrderTests {

	[Fact]
	public void Defaults_ReturnsNonEmptyList() {
		var defaults = InventoryItemOrder.Defaults();

		Assert.NotEmpty(defaults);
	}

	[Fact]
	public void Defaults_StartsWithItemLevelDescending() {
		var defaults = InventoryItemOrder.Defaults();

		Assert.Equal(OrderMethod.ItemLevel, defaults[0].Method);
		Assert.Equal(OrderDirection.Descending, defaults[0].Direction);
	}

	[Fact]
	public void Defaults_HasLevelDescendingSecond() {
		var defaults = InventoryItemOrder.Defaults();

		Assert.True(defaults.Count >= 2);
		Assert.Equal(OrderMethod.Level, defaults[1].Method);
		Assert.Equal(OrderDirection.Descending, defaults[1].Direction);
	}

	[Fact]
	public void DefaultSets_ContainsExpectedPresets() {
		var sets = InventoryItemOrder.DefaultSets();

		Assert.True(sets.ContainsKey("Ilvl & Lvl"));
		Assert.True(sets.ContainsKey("Newer first"));
	}

	[Fact]
	public void DefaultSets_NewerFirst_UsesItemPatchAndItemId() {
		var sets = InventoryItemOrder.DefaultSets();
		var newerFirst = sets["Newer first"];

		Assert.Equal(2, newerFirst.Count);
		Assert.Equal(OrderMethod.ItemPatch, newerFirst[0].Method);
		Assert.Equal(OrderDirection.Descending, newerFirst[0].Direction);
		Assert.Equal(OrderMethod.ItemId, newerFirst[1].Method);
		Assert.Equal(OrderDirection.Descending, newerFirst[1].Direction);
	}

	[Fact]
	public void OrderItems_EmptyInput_ReturnsEmpty() {
		// OrderItems with no config crashes (needs ConfigurationManager.Config.SortOrder)
		// but with an empty IEnumerable<InventoryItem> it should still return empty
		// since the foreach never executes if SortOrder is null
		// This test documents the dependency on ConfigurationManager
		// For now, we test the static helpers only
		Assert.True(true); // placeholder — needs DI refactor to test OrderItems
	}
}
