/// ApplyGearChange.Plates.cs
/// Plate data access and management: reading/writing the current plate, switching between
/// portable plates, overwriting pending plates from actual game data, and compiling
/// "todo" tasks (items not yet owned) for each plate.

using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Models;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using InventoryItem = Dresser.Models.InventoryItem;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		// ── Current Plate Access ─────────────────────────────────────────

		/// <summary>Returns the InventoryItemSet for the currently selected portable plate, or null.</summary>
		public InventoryItemSet? GetCurrentPlate() {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				return currentPlate;
			}
			return null;
		}

		/// <summary>Returns the item in a specific slot of the current plate, or null.</summary>
		public InventoryItem? GetCurrentPlateItem(GlamourPlateSlot slot) {
			return GetCurrentPlate()?.GetSlot(slot);
		}


		// ── Plate Switching ──────────────────────────────────────────────

		/// <summary>
		/// Switches to a different portable plate: removes current mods, un-applies current plate,
		/// updates the selected plate number, and applies the new plate's appearance.
		/// </summary>
		public void changeCurrentPendingPlate(ushort plateNumber) {
			Task.Run(delegate {
				RemoveAllModsFromPenumbra();
				PluginServices.ApplyGearChange.UnApplyCurrentPendingPlateAppearance();
				ConfigurationManager.Config.SelectedCurrentPlate = plateNumber;
				PluginServices.ApplyGearChange.ApplyCurrentPendingPlateAppearance();
				return Task.CompletedTask;
			});
		}


		// ── Plate Overwrite ──────────────────────────────────────────────

		/// <summary>
		/// Overwrites the current portable plate with whatever plate is currently displayed
		/// in the glamour dresser or plate selection UI.
		/// </summary>
		public void OverwritePendingWithCurrentPlate() {
			ConfigurationManager.Config.PendingPlateItemsCurrentChar[ConfigurationManager.Config.SelectedCurrentPlate] = GlamourPlates.CurrentSet().RemoveEmpty();
			ReApplyAppearanceAfterEquipUpdate();
		}

		/// <summary>
		/// Populates all portable plates from the actual in-game glamour plates.
		/// Parses plate data from the game agent, waits for data availability, then copies.
		/// </summary>
		public void OverwritePendingWithActualPlates() {
			Task.Run(async delegate {
				Gathering.ParseGlamourPlates();
				await Task.Delay(2500);
				if (PluginServices.Storage.Pages == null) {
					return;
				}
				foreach ((var plateNumber, var set) in Storage.PagesInv) {
					if (plateNumber == Storage.PlateNumber || set.IsEmpty()) continue;
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[plateNumber] = set;
				}
			});
		}

		/// <summary>
		/// Called when the glamour dresser is opened.
		/// If no portable plates have content, populates them from actual plates.
		/// </summary>
		public void OpenGlamourDresser() {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.Any(s => !s.Value.IsEmpty())) {
				PluginLog.Verbose($"Found found no portable plates, populating them with current");
				PluginServices.ApplyGearChange.OverwritePendingWithActualPlates();
			}
		}


		// ── Todo Tasks ───────────────────────────────────────────────────

		/// <summary>Per-plate lists of items that the player does not own (need to acquire).</summary>
		public Dictionary<ushort, List<InventoryItem>> TasksOnCurrentPlate = new();

		/// <summary>
		/// Compiles the list of not-owned items for the given plate (or all plates if null).
		/// Runs on a background thread.
		/// </summary>
		public void CompileTodoTasks(ushort? plateNumber = null) {
			Task.Run(delegate {
				foreach ((var plateN, var set) in ConfigurationManager.Config.PendingPlateItemsCurrentChar) {
					if (plateNumber != null && plateN != plateNumber) continue;
					TasksOnCurrentPlate[plateN] = set.FindNotOwned();
				}
			});
		}
	}
}
