using Dresser.Extensions;
using Dresser.Interop.Agents;

namespace Dresser.Models.ViewModels {
	public class ItemRenderData {
		public uint ItemId;
		public uint IconId;
		public string Name = string.Empty;
		public bool IsEmpty;
		public GlamourPlateSlot? Slot;

		/// <summary>
		/// The original InventoryItem backing this render data.
		/// Used by ImGui renderer for tooltip and stain rendering.
		/// KTK renderer should NOT access this.
		/// </summary>
		public InventoryItem? Source;

		/// <summary>
		/// Whether this item is applicable in the current context (not faded).
		/// </summary>
		public bool IsApplicable = true;

		// mod info
		public bool IsModded;
		public string? ModName;

		// dyes
		public bool IsDyeable1;
		public bool IsDyeable2;
		public byte StainId1;
		public byte StainId2;

		public static ItemRenderData Empty(GlamourPlateSlot slot) => new() {
			IsEmpty = true,
			Slot = slot,
		};

		public static ItemRenderData From(InventoryItem? item, GlamourPlateSlot? slot = null) {
			if (item == null || item.ItemId == 0)
				return new ItemRenderData { IsEmpty = true, Slot = slot, Source = item };

			return new ItemRenderData {
				ItemId = item.ItemId,
				IconId = item.Item?.Icon ?? 0,
				Name = item.FormattedName ?? string.Empty,
				IsEmpty = false,
				Slot = slot,
				Source = item,
				IsApplicable = !item.IsFadedInBrowser(),
				IsModded = item.IsModded(),
				ModName = item.ModName,
				IsDyeable1 = item.Item?.IsDyeable1() ?? false,
				IsDyeable2 = item.Item?.IsDyeable2() ?? false,
				StainId1 = item.Stain,
				StainId2 = item.Stain2,
			};
		}
	}
}
