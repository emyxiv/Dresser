/// ApplyGearChange.cs
/// Root partial: constructor, disposal, and shared fields used across all partials.
///
/// This service orchestrates all gear/glamour appearance changes in Dresser.
/// It is split into the following partial files:
///   - ApplyGearChange.cs           — Constructor, disposal, shared state (this file)
///   - ApplyGearChange.Appearance.cs — Browsing lifecycle, item application, backup/restore
///   - ApplyGearChange.Mods.cs       — Penumbra mod enable/disable for modded items
///   - ApplyGearChange.Plates.cs     — Plate data access, switching, overwrite, todo tasks
///   - ApplyGearChange.Dye.cs        — Dye application, history, swapping
///   - ApplyGearChange.DresserSync.cs — Applying portable plates to actual glamour plates
///   - ApplyGearChange.Dialogs.cs    — ImGui dialog/popup rendering for dresser sync flow

using Dresser.Logic;
using Dresser.Models;

using System;

using InventoryItem = Dresser.Models.InventoryItem;

namespace Dresser.Services {
	public partial class ApplyGearChange : IDisposable {
		private Plugin Plugin;

		public ApplyGearChange(Plugin plugin) {
			Plugin = plugin;
		}

		public void Dispose() { }


		// ── Shared State ─────────────────────────────────────────────────
		// These fields are accessed by multiple partials.

		/// <summary>The player's appearance before browsing started, used for restore on exit.</summary>
		private InventoryItemSet? BackedUpItems = null;

		/// <summary>
		/// Tracks the previous modded item in a slot before a new item was placed there.
		/// Used by mod cleanup to know which Penumbra mod to remove.
		/// </summary>
		private InventoryItem? CurrentPreviousModdedItem = null;
	}
}
