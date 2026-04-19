using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using AllaganLib.GameSheets.Caches;
using AllaganLib.GameSheets.ItemSources;
using AllaganLib.GameSheets.Sheets.Rows;

using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Dresser.Core;
using Dresser.Extensions;
using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Models;
using Dresser.Gui.Components;

using Dalamud.Bindings.ImGui;

using InventoryItem = Dresser.Models.InventoryItem;

using static Dresser.Services.Storage;
using OtterGui.Text.Widget;

namespace Dresser.Gui
{
	public partial class GearBrowser
	{
		private Vector2 DrawInfoSearchBarClothes(Vector2 posInfoSearchInitial, float darkenAmount) {

			string infoSearchTextPart1 = ItemsCount.ToString();
			string infoSearchTextPart2 = "";
			if (ConfigurationManager.Config.DebugDisplayModedInTitleBar && ItemCountModded > 0) {
				infoSearchTextPart2 = "(" + ItemCountModded + ")";
			}

			var sizeInfoSearchPart2 = ImGui.CalcTextSize(infoSearchTextPart2);
			var sizeInfoSearchPart1 = ImGui.CalcTextSize(infoSearchTextPart1);

			var posInfoSearchPart2 = posInfoSearchInitial - (infoSearchTextPart2.Length > 0 ? new Vector2(sizeInfoSearchPart2.X + ImGui.GetStyle().ItemSpacing.X, 0) : Vector2.Zero);
			var posInfoSearchPart1 = posInfoSearchPart2 - new Vector2(sizeInfoSearchPart1.X + ImGui.GetStyle().ItemSpacing.X, 0);

			// part 2
			if (ConfigurationManager.Config.DebugDisplayModedInTitleBar && ItemCountModded > 0) {
				ImGui.GetWindowDrawList().AddText(
					posInfoSearchPart2,
					ImGui.ColorConvertFloat4ToU32(ConfigurationManager.Config.ModdedItemColor.Darken(darkenAmount)),
					infoSearchTextPart2);
				GuiHelpers.Tooltip(() => {
					ImGui.Text($"{ItemsCount} modded items are applied temporarily");
				}, ImGui.IsMouseHoveringRect(posInfoSearchPart2, posInfoSearchPart2 + sizeInfoSearchPart2));
			}

			// part 1
			ImGui.GetWindowDrawList().AddText(
				posInfoSearchPart1,
				ImGui.ColorConvertFloat4ToU32(Vector4.One.Darken(darkenAmount)),
				infoSearchTextPart1);
			GuiHelpers.Tooltip(() => {
				ImGui.TextUnformatted($"{ItemsCount} items found with the selected filters");
			}, ImGui.IsMouseHoveringRect(posInfoSearchPart1, posInfoSearchPart1 + sizeInfoSearchPart1));

			return posInfoSearchPart1;
		}
		private static BrowserIndex? HoveredItem = null;
		public static List<InventoryCategory> AllowedCategories = new() {
			InventoryCategory.GlamourChest,
			InventoryCategory.Armoire,
			InventoryCategory.CharacterBags,
			InventoryCategory.CharacterArmoryChest,
			InventoryCategory.CharacterEquipped,
			InventoryCategory.CharacterSaddleBags,
			//InventoryCategory.CharacterPremiumSaddleBags,
			InventoryCategory.RetainerBags,
			InventoryCategory.RetainerEquipped,
			InventoryCategory.RetainerMarket,
			InventoryCategory.FreeCompanyBags,
		};
		//public static List<InventoryType> AllowedType => Storage.FilterVendorNames.Select(vfic => vfic.Value).Concat(new List<InventoryType>() {
		//InventoryType.RetainerBag0
		//}).ToList();
		public static GlamourPlateSlot? SelectedSlot = null;
		public enum DisplayMode {
			Vertical,
			SidebarOnRight,
			//SidebarOnLeft,
		}

		private void DrawClothes() {
			switch (ConfigurationManager.Config.GearBrowserDisplayMode) {
				case DisplayMode.Vertical: DrawWithMode_Vertical(); break;
				case DisplayMode.SidebarOnRight: DrawWithMode_SidebarOnRight(); break;
				default: DrawWithMode_Vertical(); break;
			}
		}
		private void DrawWithMode_Vertical() {

			if (DrawFilters()) RecomputeItems();

			DrawItems();

		}
		private void DrawWithMode_SidebarOnRight() {

			ImGui.BeginGroup();
			DrawItems();
			ImGui.EndGroup();
			if (!ConfigurationManager.Config.GearBrowserSideBarHide) {
				ImGui.SameLine();

				ImGui.BeginGroup();
				ImGui.BeginChildFrame(84, ImGui.GetContentRegionAvail() - new Vector2(0, 0));
				if (DrawFilters()) RecomputeItems();
				ImGui.EndChildFrame();
				ImGui.EndGroup();
			}
		}

		private unsafe static bool DrawSort() {
			bool recompute = DrawSavedSortOrdersList();

			ImGui.Spacing();
			ImGui.Separator();

			var sortMethods = Enum.GetNames<InventoryItemOrder.OrderMethod>();
			var MethodsComboSize = ImGui.CalcTextSize(sortMethods.OrderByDescending(m => ImGui.CalcTextSize(m).X).First()).X + (ImGui.GetFontSize() * 2);

			if(ConfigurationManager.Config.SortOrder == null)
				ConfigurationManager.Config.SortOrder = InventoryItemOrder.Defaults();
			if (ConfigurationManager.Config.SortOrder != null && ConfigurationManager.Config.SortOrder.Count > 0) {
				for (int j = 0; j < ConfigurationManager.Config.SortOrder.Count; j++) {


					var sorter = ConfigurationManager.Config.SortOrder[j];
					var method = sorter.Method;
					var direction = sorter.Direction;

					var methodInt = (int)method;
					var directionInt = (int)direction;
					int* indexPtr = &j;
					ReadOnlySpan<byte> payloadReadOnly = BitConverter.GetBytes(j);


					var text = $"{method.ToString().AddSpaceBeforeCapital()}";
					var iconDirection = direction == InventoryItemOrder.OrderDirection.Ascending ? FontAwesomeIcon.SortAlphaUp : FontAwesomeIcon.SortAlphaDown;

					ImGui.AlignTextToFramePadding();
					GuiHelpers.Icon(iconDirection);
					ImGui.SameLine();
					ImGui.Selectable(text);


					if (ImGui.BeginPopupContextItem($"ContextMenuGearBrowser##{j}")) {

						ImGui.AlignTextToFramePadding();
						GuiHelpers.Icon(iconDirection);
						ImGui.SameLine();
						ImGui.Text(text);

						ImGui.SetNextItemWidth(MethodsComboSize);
						if (ImGui.Combo($"##ChangeMethodSort##{j}", ref methodInt, sortMethods, sortMethods.Length)) {
							ConfigurationManager.Config.SortOrder[j] = new() {
								Method = (InventoryItemOrder.OrderMethod)methodInt,
								Direction = direction
							};
							recompute = true;
						}

						ImGui.SameLine();
						if (GuiHelpers.IconButton(iconDirection, default, $"ChangeDirectionSorter##{j}")) {
							ConfigurationManager.Config.SortOrder[j] = new() {
								Method = method,
								Direction = direction == InventoryItemOrder.OrderDirection.Ascending ? InventoryItemOrder.OrderDirection.Descending : InventoryItemOrder.OrderDirection.Ascending
							};
							recompute = true;
						}

						ImGui.SameLine();
						if (GuiHelpers.IconButton(FontAwesomeIcon.Trash, default, $"RemoveSortSorter##{j}")) {
							ConfigurationManager.Config.SortOrder.RemoveAt(j);
							recompute = true;
							ImGui.CloseCurrentPopup();
						}
						ImGui.EndPopup();

					}

					if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.AcceptNoPreviewTooltip | ImGuiDragDropFlags.SourceNoPreviewTooltip)) {

						ImGui.SetDragDropPayload("DND_ORDER_INDEX", payloadReadOnly);
						ImGui.EndDragDropSource();
					}
					if (ImGui.BeginDragDropTarget()) {
						var payload = ImGui.AcceptDragDropPayload("DND_ORDER_INDEX", ImGuiDragDropFlags.AcceptNoPreviewTooltip | ImGuiDragDropFlags.SourceNoPreviewTooltip);

						try {

							if (payload.DataSize == sizeof(int)) {
								int payload_j = *(int*)payload.Data;

								// swap
								var tmp = ConfigurationManager.Config.SortOrder[j];
								ConfigurationManager.Config.SortOrder[j] = ConfigurationManager.Config.SortOrder[payload_j];
								ConfigurationManager.Config.SortOrder[payload_j] = tmp;
								recompute = true;
							}

						} catch (Exception) {
							// TODO: fix error on payload sizeof when it's not delivery
							//PluginLog.Warning(e, "Exception during Drag and Drop");
						}

						ImGui.EndDragDropTarget();
					}

				}
			}

			ImGui.Separator();
			ImGui.Spacing();

			if (GuiHelpers.IconButton(FontAwesomeIcon.Plus, default, "AddSortSorter")) {
				if (ConfigurationManager.Config.SortOrder != null) {
					var used = ConfigurationManager.Config.SortOrder.Select(s => s.Method);
					var available = Enum.GetValues<InventoryItemOrder.OrderMethod>().ToHashSet();
					var notUsed = available.Except(used).FirstOrDefault();

					ConfigurationManager.Config.SortOrder?.Add((notUsed, InventoryItemOrder.OrderDirection.Descending));
				}
			}
			ImGui.SameLine();
			if (ConfigurationManager.Config.SavedSortOrders == null) ConfigurationManager.Config.SavedSortOrders = new();

			if (GuiHelpers.IconButton(FontAwesomeIcon.Save, default, "AddSortSorter")) {
				var random = new Random();
				const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
				var newKey = "";
				do {
					newKey = new string(Enumerable.Repeat(chars, 3).Select(s => s[random.Next(s.Length)]).ToArray());
				} while (ConfigurationManager.Config.SavedSortOrders.ContainsKey(newKey));
				ConfigurationManager.Config.SavedSortOrders.Add(newKey, ConfigurationManager.Config.SortOrder!);
			}
			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Recycle, "Restore default order sets", default, "RestoreDefaultSorters")) {
				if (ConfigurationManager.Config.SavedSortOrders == null)
					ConfigurationManager.Config.SavedSortOrders = new();
				ConfigurationManager.Config.SavedSortOrders = ConfigurationManager.Config.SavedSortOrders.Concat(InventoryItemOrder.DefaultSets()).ToLookup(pair => pair.Key, pair => pair.Value).ToDictionary(group=>group.Key,group=>group.First());
			}

			return recompute;
		}

		private static bool DrawSavedSortOrdersList() {
			var recompute = false;
			if (ConfigurationManager.Config.SavedSortOrders == null) ConfigurationManager.Config.SavedSortOrders = new();

			if (ConfigurationManager.Config.SavedSortOrders.Any()) {
				(string OldKey, string NewKey)? keyToRename = null;
				foreach ((string key, var order) in ConfigurationManager.Config.SavedSortOrders) {
					var sizeX = ImGui.CalcTextSize(key).X + ImGui.GetStyle().ItemSpacing.X + (ImGui.GetStyle().FramePadding.X * 2);
					if (ImGui.GetContentRegionAvail().X < sizeX) {
						ImGui.NewLine();
					}
					if (ImGui.Button($"{key}##SavedSortOrders")) {
						ConfigurationManager.Config.SortOrder = order;
						recompute = true;
					}
					if (ImGui.BeginPopupContextItem($"SavedSortOrders##context##{key}")) {
						var editedKey = key;
						ImGui.Text($"Rename {key}:");
						ImGui.SetNextItemWidth(ImGui.GetFontSize() * 20);
						if (ImGui.InputText($"##EditKey##{key}##SavedSortOrders", ref editedKey, 20, ImGuiInputTextFlags.EnterReturnsTrue)) {
							keyToRename = (key, editedKey);
						}
						if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, "Hold Ctrl + Shift and Click to delete this saved preset", default, $"RemoveSavedSortOrders##{key}")) {
							ConfigurationManager.Config.SavedSortOrders.Remove(key);
							ImGui.CloseCurrentPopup();
						}
						ImGui.EndPopup();
					}
					ImGui.SameLine();
				}
				ImGui.NewLine();
				if (keyToRename != null) {
					if (ConfigurationManager.Config.SavedSortOrders.TryGetValue(keyToRename.Value.OldKey, out var objects)) {
						ConfigurationManager.Config.SavedSortOrders.Remove(keyToRename.Value.OldKey);
						ConfigurationManager.Config.SavedSortOrders[keyToRename.Value.NewKey] = objects;
					}
				}
			}
			return recompute;
		}

		private static Dictionary<InventoryCategory, int> SavedQuantityInventoryCategoryCache = new();
		private static Dictionary<InventoryType, int> SavedQuantityInventoryTypeCache = new();
		public static void SavedQuantityCacheMake(IEnumerable<InventoryItem> items) {
			SavedQuantityInventoryTypeCache.Clear();
			SavedQuantityInventoryCategoryCache.Clear();

			foreach (var item in items) {
				var itemCat = item.SortedCategory;
				var itemType = item.SortedContainer;
				if ((int)itemType >= (int)InventoryTypeExtra.AllItems) continue;
				if (!SavedQuantityInventoryCategoryCache.TryAdd(itemCat, 1)) {
					SavedQuantityInventoryCategoryCache[itemCat]++;
				}
				if (!SavedQuantityInventoryTypeCache.TryAdd(itemType, 1)) {
					SavedQuantityInventoryTypeCache[itemType]++;
				}
			}
			foreach ((var type, var list) in PluginServices.Storage.AdditionalItems) {
				SavedQuantityInventoryTypeCache[type] = list.Count;
			}

		}
		private static int SavedQuantityCacheGet(InventoryCategory cat) {
			SavedQuantityInventoryCategoryCache.TryGetValue(cat, out var count);
			return count;
		}
		private static int SavedQuantityCacheGet(InventoryType type) {
			SavedQuantityInventoryTypeCache.TryGetValue(type, out var count);
			return count;
		}

		private static bool DrawTagFilters() {
			bool changed = false;
			var allTags = Tag.All();

			if (!allTags.Any()) {
				ImGui.TextDisabled("No tags available");
				return changed;
			}

			// Display buttons to modify all tags at once
			if (GuiHelpers.IconButton(FontAwesomeIcon.TrashAlt, default, "##ClearAllTagFilters##TagFilter")) {
				ConfigurationManager.Config.FilterTagStates.Clear();
				changed = true;
			}
			ImGui.SameLine();
			GuiHelpers.Tooltip("Clear all tag filters");

			ImGui.SameLine();
			if (ImGui.Button($"{char.ConvertFromUtf32(0xE04A)}##IncludeAllTagFilters##TagFilter")) {
				foreach (var tag in allTags) {
					ConfigurationManager.Config.FilterTagStates[tag.Id] = 1;
				}
				changed = true;
			}
			ImGui.SameLine();
			GuiHelpers.Tooltip("Include all tags");

			ImGui.SameLine();
			if (ImGui.Button($"{char.ConvertFromUtf32(0xE043)}##ExcludeAllTagFilters##TagFilter")) {
				foreach (var tag in allTags) {
					ConfigurationManager.Config.FilterTagStates[tag.Id] = -1;
				}
				changed = true;
			}
			ImGui.SameLine();
			GuiHelpers.Tooltip("Exclude all tags");

			//ImGui.SameLine();
			if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog, default, "OpenTagManager")) {
				Plugin.GetInstance().TagManager.IsOpen = !Plugin.GetInstance().TagManager.IsOpen;
			}
			GuiHelpers.Tooltip("Open Tag Manager");


			// Tag combine mode selector
			ImGui.AlignTextToFramePadding();


			var modesTooltip = $"Tag Combine Mode determines how multiple include tags are combined:\n\nAny: Any item matching one or more tag\nAll: Only items matching all checked tags";
			var modes = new[] { "Any", "All" };
			var currentMode = (int)ConfigurationManager.Config.FilterTagCombineMode;
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 3);
			if (ImGui.Combo("##TagCombineMode", ref currentMode, modes, modes.Length)) {
				ConfigurationManager.Config.FilterTagCombineMode = (Dresser.Enums.TagFilterCombineMode)currentMode;
				changed = true;
			}
			ImGui.SameLine();

			GuiHelpers.Tooltip(modesTooltip);

			ImGui.Text("Include Mode");
			GuiHelpers.Tooltip(modesTooltip);

			ImGui.Separator();

			// Display each tag with a 3-state checkbox
			GlamourPlateSlot? previousSlot = null;
			var tagsSorted = allTags.OrderBy(t => t.Slot).ThenBy(t => t.Name);
			if(tagsSorted.First().Slot == null) {
				ImGui.Text("Universal");
			}
			foreach (var tag in tagsSorted) {
				if(tag.Slot.HasValue && SelectedSlot.HasValue && tag.Slot.Value != SelectedSlot.Value) {
					continue; // Skip tags that don't match the selected slot
				}

				// Add slot header when slot changes
				if (tag.Slot != previousSlot) {
					ImGui.Spacing(); // Add spacing between slot groups
					var slotHeaderText = tag.Slot.HasValue ? tag.Slot.Value.ToString().AddSpaceBeforeCapital() : "Universal";
					ImGui.Text(slotHeaderText);
					previousSlot = tag.Slot;
				}

				ConfigurationManager.Config.FilterTagStates.TryGetValue(tag.Id, out var state);

				var stateEnum = state switch {
					1  => TagFilterStateFlag.Include,
					-1 => TagFilterStateFlag.Exclude,
					_  => TagFilterStateFlag.Neutral
				};

				if ((new TriStateCheckbox()).Draw($"{tag.Name}##TagFilters##Clothes##Browser", ref stateEnum, TagFilterStateFlag.Include, TagFilterStateFlag.Exclude)) {
					// Cycle through states: 0 -> 1 -> -1 -> 0
					int newState = (state + 2) % 3 - 1;
					if (newState == 0) {
						ConfigurationManager.Config.FilterTagStates.Remove(tag.Id);
					} else {
						ConfigurationManager.Config.FilterTagStates[tag.Id] = newState;
					}
					changed = true;
				}

			}
			return changed;
		}

		[Flags]
		enum TagFilterStateFlag {
			Neutral,
			Include,
			Exclude,
		}

		private static bool PassesTagFilters(InventoryItem item) {
				if (ConfigurationManager.Config.FilterTagStates.Count == 0) {
					return true; // No tag filters active
				}

				var itemTags = Tag.ByItemId(item.ItemId);
				var itemTagIds = itemTags.Select(t => t.Id).ToHashSet();
				var itemSlot = item.Item.GlamourPlateSlot();

				var allTagsDict = Tag.All().ToDictionary(t => t.Id);

				// Collect include and exclude tags separately
				var includeTagIds = new List<uint>();
				var excludeTagIds = new List<uint>();

				foreach ((var tagId, var state) in ConfigurationManager.Config.FilterTagStates) {
					// Find the tag to check its slot restriction
					if (!allTagsDict.TryGetValue(tagId, out var tag)) continue;

					// Skip this tag filter if it's restricted to a different slot
					if (tag.Slot.HasValue && tag.Slot.Value != itemSlot) {
						continue;
					}

					if (state == 1) { // Include
						includeTagIds.Add(tagId);
					} else if (state == -1) { // Exclude
						excludeTagIds.Add(tagId);
					}
				}

				// Check exclude tags first - if any match, exclude the item
				if (excludeTagIds.Count > 0) {
					foreach (var tagId in excludeTagIds) {
						if (itemTagIds.Contains(tagId)) {
							return false; // Item has an exclude tag
						}
					}
				}

				// Check include tags based on combine mode
				if (includeTagIds.Count > 0) {
					var combineMode = ConfigurationManager.Config.FilterTagCombineMode;
					if (combineMode == Dresser.Enums.TagFilterCombineMode.Any) {
						// OR mode: item passes if it has ANY of the include tags
						foreach (var tagId in includeTagIds) {
							if (itemTagIds.Contains(tagId)) {
								return true;
							}
						}
						return false;
					} else { // All mode
						// AND mode: item passes only if it has ALL of the include tags
						foreach (var tagId in includeTagIds) {
							if (!itemTagIds.Contains(tagId)) {
								return false;
							}
						}
						return true;
					}
				}

				// No include tags - item passes if no exclude tags matched (already checked above)
				return true;
			}

		public static IEnumerable<InventoryItem>? Items = null;
		private static int ItemsCount = 0;
		private static int ItemCountModded = 0;
		private static bool JustRecomputed = false;
		public static void RecomputeItems() {

			PluginLog.Verbose($"RecomputeItems for slot {SelectedSlot}");
			IEnumerable<InventoryItem> items = new HashSet<InventoryItem>();

			foreach ((var inventoryType, var itemsToAdd) in PluginServices.Storage.AdditionalItems) {
				if (ConfigurationManager.Config.FilterInventoryType.TryGetValue(inventoryType, out var isEnabled) && isEnabled) {
					items = items.Concat(itemsToAdd);
					// PluginLog.Debug($"included {(InventoryTypeExtra)inventoryType} {itemsToAdd.Count} cat:{string.Join(",", itemsToAdd.Select(p => p.SortedCategory).Distinct())} types:{string.Join(",", itemsToAdd.Select(p => $"{(InventoryTypeExtra)p.SortedContainer}({(int)p.SortedContainer})").Distinct())}");
				}
			}

			//PluginLog.Debug($"all items => {items.Count()} cat:{string.Join(",", items.Select(p => p.SortedCategory).Distinct())} types:{string.Join(",", items.Select(p => p.SortedContainer).Distinct())}");

			// items from saved inventory (critical impact lib)
			items = items.Concat(PluginServices.AllaganTools.GetItemsLocalCharsRetainers(true).SelectMany(t => t.Value));

			items = items.Where(i => !i.IsEmpty && i.Item.Base.ModelMain != 0).ToArray();

			SavedQuantityCacheMake(items);
			items = items.Where(i =>
					(!ConfigurationManager.Config.filterCurrentRace || i.Item.CanBeEquipedByPlayedRaceGenderGc())
					&& (ConfigurationManager.Config.FilterClassJobCategories.Count == 0 || i.Item.CanBeEquipedByFilteredJobs())
					&& i.IsInGearBrowserSelectedSlot()
					&& i.IsFilterDisplayable()
					&& i.IsInFilterLevelRanges()
					&& (!ConfigurationManager.Config.filterRarity.HasValue || i.Item.Base.Rarity == ConfigurationManager.Config.filterRarity)
					&& i.IsNotInBlackList()
					&& (!ConfigurationManager.Config.filterGlamourSetsOnly || i.Item.IsPartOfGlamourSet())
					&& (!ConfigurationManager.Config.filterOrnateObtained || i.IsObtained() || !i.Item.IsOrnate())
					&& (!ConfigurationManager.Config.filterHideCashShop || i.IsObtained() || !i.Item.IsCashShop())
					&& (!ConfigurationManager.Config.filterHideNoSource || i.IsObtained() || !i.Item.HasNoSource())
					&& (!ConfigurationManager.Config.filterHideOwned || !i.IsObtained())
					&& PassesTagFilters(i)
				);


			if (!Search.IsNullOrWhitespace())
				items = items.Where(i =>
					i.FormattedName.Contains(Search, StringComparison.OrdinalIgnoreCase) // search for item name
					|| i.StainName().Contains(Search, StringComparison.OrdinalIgnoreCase) // search for stain name
					|| i.Stain2Name().Contains(Search, StringComparison.OrdinalIgnoreCase) // search for stain name
					|| (i.ModName?.Contains(Search, StringComparison.OrdinalIgnoreCase)??false) // search for mod name
					|| (i.ModAuthor?.Contains(Search, StringComparison.OrdinalIgnoreCase)??false) // search for mod author
					|| (i.HasTagContains(Search))
					);

			if (!items.Any()) { Items = new List<InventoryItem>(); FinishRecomputeItems(); return; }

			// remove duplicates
			IEnumerable< InventoryItem> uniqueItems;
			if (items.Any(i => i.IsModded())){
				uniqueItems = items;
			} else {
				uniqueItems = items.GroupBy(i => i.GetHashCode()).Select(i => i.Last());
			}

			Items = InventoryItemOrder.OrderItems(uniqueItems);

			FinishRecomputeItems();

		}
		private static void FinishRecomputeItems() {

			ItemsCount = Items?.Count() ?? 0;
			ItemCountModded = Items?.Count(i => i.IsModded()) ?? 0;
			JustRecomputed = true;
		}


		public static InventoryItem? SelectedInventoryItem {
			get{
				var selectedItemCurrentGear = CurrentGear.SelectedInventoryItem();
				if (selectedItemCurrentGear == null) return null;
				return Items?.Where(i => (BrowserIndex)i == (BrowserIndex)selectedItemCurrentGear).FirstOrDefault();
			}
		}

		public int RowSize = 1;
		public int? HoveredIncrement = null;
		public int? HotkeyNextSelect = null;
		public void DrawItems() {
			Styler.PushStyleCollection();
			Vector2 available = ImGui.GetContentRegionAvail();
			var isSidebarFitting = available.X > (ImGui.GetFontSize() * 25);
			if (ConfigurationManager.Config.GearBrowserSideBarHide) isSidebarFitting = false;
			if (ConfigurationManager.Config.GearBrowserDisplayMode == DisplayMode.Vertical) isSidebarFitting = false;

			var sidebarPercent = ConfigurationManager.Config.GearBrowserSideBarSizePercent;

			var maxIcons = 1;
			if (isSidebarFitting) {
				maxIcons = (int)(((available.X * (1-sidebarPercent) )/ ItemIcon.IconSize.X));
			} else {
				maxIcons = int.Clamp(maxIcons-1,1,int.MaxValue);
			}

			RowSize = maxIcons;
			float widthAdjusted;

			// Calculate Gear slot frame width
			if (isSidebarFitting) {
				widthAdjusted =
					((maxIcons) * ItemIcon.IconSize.X)
					// +((ImGui.GetStyle().WindowPadding.X ) * 2)
					+ ((ImGui.GetStyle().ScrollbarSize) * 1)
					+ ((ImGui.GetStyle().ItemSpacing.X) * (maxIcons - 1))
					// +((ImGui.GetStyle().FramePadding.X *2 ) * (maxIcons-1))
					+ (ImGui.GetStyle().FramePadding.X * 2);

			} else {
				widthAdjusted = available.X;
			}

			ImGui.BeginChildFrame(76,  new Vector2(widthAdjusted, available.Y));

			BrowserIndex? selectedItemHash = SelectedInventoryItem == null ? null : (BrowserIndex)SelectedInventoryItem;
			if (Items != null && ItemsCount > 0)
				try {

					bool isTooltipActive = false;
					var i = 0;
					var r = 0;
					bool rowSizeChecked = false;
					bool hotkeySelected = false;

					foreach (var item in Items) {

						// icon
						var itemHash = (BrowserIndex)item;
						bool isHovered = itemHash == HoveredItem;
						bool wasHovered = isHovered;

						var selectedInCurrentGear = itemHash == selectedItemHash;
						if (selectedInCurrentGear) HoveredIncrement = i;
						isHovered |= selectedInCurrentGear;
						var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive, out bool clickedMiddle, out bool clickedStain, null, ContextMenuBrowser);
						var hoverDown = !selectedInCurrentGear && isHovered && (ImGui.GetIO().KeyCtrl || ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left]);

						if (JustRecomputed && selectedInCurrentGear) ImGui.SetScrollHereY();
						if (isHovered)
							HoveredItem = itemHash;
						else if (!isHovered && wasHovered)
							HoveredItem = null;

						// execute when clicked
						if (iconClicked || hoverDown) {
							PluginServices.ApplyGearChange.ExecuteBrowserItem(item);
						}
						if(HotkeyNextSelect == i && !hotkeySelected) {
							PluginServices.ApplyGearChange.ExecuteBrowserItem(item);
							hotkeySelected = true;
							HotkeyNextSelect = null;
							ImGui.SetScrollHereY();
						}

						// break row if sidebar
						if (isSidebarFitting) {
							r++;
							if (r >= maxIcons) {
								r = 0;
							} else ImGui.SameLine();

						}
						// break row if no sidebar
						else {
							ImGui.SameLine();
							if (ImGui.GetContentRegionAvail().X < ItemIcon.IconSize.X) {
								if (!rowSizeChecked) {
									rowSizeChecked = true;
									RowSize = i + 1;
								}
								ImGui.NewLine();
							}
						}

						i++;
					}
				} catch (Exception ex) {
					PluginLog.Error(ex.ToString());
				}
			else {
				var message = "No item found.\nThe filters may be too strong, or you may need to open various inventories(e.g. retainers, Glamour Dresser, Armoire) to populate the item memory with your belongings.";
				if (SelectedSlot == null) message = "No slot selected.\nStart looking for items by selecting a slot (e.g. chest or legs) in Plate Creation window.";
				ImGui.BeginDisabled();
				ImGui.TextWrapped($"{message}");
				ImGui.EndDisabled();
			}

			JustRecomputed = false;

			ImGui.EndChildFrame();
			Styler.PopStyleCollection();
		}

		public static void ContextMenuBrowser(InventoryItem itemInv, GlamourPlateSlot? slot) {

			var item = itemInv.Item;

			if (itemInv.IsModded() && ImGui.Selectable("Open in Penumbra"))
				PluginServices.Penumbra.OpenModWindow((itemInv.ModDirectory, itemInv.ModName)!);
			if (PluginServices.ItemVendorLocation.IsInitialized() && PluginServices.ItemVendorLocation.HasItemInfoProvider(item.RowId))
				if (ImGui.Selectable($"{char.ConvertFromUtf32(0xE086)} Vendor Location"))
					item.OpenInItemVendorLocation();
			if (ImGui.Selectable("Open in Garland Tools"))
				item.OpenInGarlandTools();
			if (ImGui.Selectable("Open in Teamcraft"))
				item.OpenInTeamcraft();
			if (ImGui.Selectable("Open in Gamer Escape"))
				item.OpenInGamerEscape();

			if (ImGui.Selectable("Open in Universalis"))
				item.OpenInUniversalis();
			if (ImGui.Selectable("Copy Name"))
				item.CopyNameToClipboard();
			if (itemInv.IsModded() && ImGui.Selectable("Copy Mod Name"))
				itemInv.ModName?.ToClipboard();
			if (itemInv.IsModded()) {

				if (itemInv.ModWebsite.IsNullOrWhitespace()) ImGui.BeginDisabled();
				if(ImGui.Selectable("Go to mod site") && !itemInv.ModWebsite.IsNullOrWhitespace())
					itemInv.ModWebsite?.OpenBrowser();
				if (itemInv.ModWebsite.IsNullOrWhitespace()) ImGui.EndDisabled();

			}
			if (ImGui.Selectable("Link"))
				item.LinkInChatHistory();
			if (itemInv.IsModded() && ImGui.Selectable("Blacklist this Mod"))
				ConfigWindow.AddModToBlacklist((itemInv.ModDirectory, itemInv.ModName)!);
			if (itemInv.IsModded() && ImGui.Selectable("Blacklist this item for this mod"))
				ConfigWindow.AddModItemToBlacklist((itemInv.ModDirectory!, (uint)itemInv.Item.RowId));

			if (item.CanTryOn && ImGui.Selectable("Try On"))
				item.TryOn();
			//if (item.CanOpenCraftLog && ImGui.Selectable("Open Crafting Log"))
			//	PluginServices.GameInterface.OpenCraftingLog(item.RowId);

			DrawTags(itemInv);
			DrawSameModels(itemInv);
			DrawSets(itemInv);

		}

		private static string NewTagInput = string.Empty;
		private static bool NewTagSlot = true;

		private static void DrawTags(InventoryItem itemInv) {
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.PlusCircle, "Add tag")) ImGui.OpenPopup($"AddTagPopup##{itemInv.Item.RowId}");

			Tag? tagToDetach = null;
			var isCtrlShiftHeld = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
			ItemIcon.DrawTags(itemInv, false, isCtrlShiftHeld, out tagToDetach, $"##{itemInv.Item.RowId}");
			if(tagToDetach != null) {
				itemInv.Item.RemoveTag(tagToDetach);
				tagToDetach = null;
			}
			


			var popupId = $"AddTagPopup##{itemInv.Item.RowId}";
			ImGui.SetNextWindowSize((new Vector2(4, 3) * ImGui.GetFontSize() * 7f), ImGuiCond.Appearing);
			//ImGui.SetNextWindowSizeConstraints(Vector2.One, (new Vector2(4, 3) * ImGui.GetFontSize() * 10));
			if (ImGui.BeginPopup(popupId)) {
				var rowId = itemInv.Item.RowId;
				var tags = Tag.All().Where(t=>!t.Slot.HasValue || t.Slot.Value == GearBrowser.SelectedSlot).ToHashSet();
				if (!NewTagInput.IsNullOrEmpty()) {
					tags = [.. tags.Where(t => t.Name.Contains(NewTagInput, StringComparison.OrdinalIgnoreCase))];
				}

				ImGui.Text("Add a new tag:");
				ImGui.Spacing();

				// EnterReturnsTrue: pressing Enter will return true here
				ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
				var inputChanged = ImGui.InputText($"##newtag_{rowId}", ref NewTagInput, 128, ImGuiInputTextFlags.EnterReturnsTrue);
				if( tags.Count == 0) {
					ImGui.Checkbox($"Bind new tag [{NewTagInput}] to current slot ({GearBrowser.SelectedSlot})##newtag_filter", ref NewTagSlot);
				}
				if (inputChanged) {
					NewTagInput = NewTagInput.Trim();
					if (!string.IsNullOrEmpty(NewTagInput)) {


						var anyExisting = Tag.TagNameEquals(NewTagInput);
						if (anyExisting != null) {
							itemInv.Item.AssignTag(anyExisting);
						} else {
							var tag = Tag.NewAndAssign(NewTagInput, itemInv.Item, NewTagSlot);
						}
						NewTagInput = string.Empty;
						//ImGui.CloseCurrentPopup();
					} else {
						// Clear input even if nothing was added
						NewTagInput = string.Empty;
					}
				}

				ImGui.Text("Existing tags:");

				if (tags.Count == 0) {
					ImGui.TextUnformatted(NewTagInput.IsNullOrEmpty() ? "(no tags)" : "(no tags with current filter)");
				} else {

					Tag? tagClicked = null;
					if (ItemIcon.DrawTags(tags, true, true, out tagClicked, $"ListOfTags##addTags", NewTagInput) && tagClicked != null) {
						// Placeholder logic when clicking an existing tag


						if (ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift) {
							tagClicked.Delete();
							PluginLog.Verbose($"Removed tag '{tagClicked.Name}' from item {rowId}");
						} else {
							itemInv.Item.AssignTag(tagClicked);
							PluginLog.Verbose($"Clicked tag '{tagClicked.Name}' on item {rowId}");
							// TODO: replace with desired behavior (filter, apply tag actions, etc.)
							//ImGui.CloseCurrentPopup();
						}
					}
					tagClicked = null;
				}

				ImGui.Spacing();
				if (ImGui.Button("Close")) ImGui.CloseCurrentPopup();
				ImGui.EndPopup();
			}
		}

		public static void DrawSameModels(InventoryItem item) {

			var sharedModels = item.Item.GetSharedModels();
			if (sharedModels != null && sharedModels.Count != 0) {
				ImGui.Spacing();
				ImGui.Text("Shared model:");
				DrawListOfItemIcons(sharedModels);
			}
		}
		public static void DrawSets(InventoryItem item) {

			var setUses = item.Item.GetUsesByType<ItemGlamourReadySetItemSource>(ItemInfoType.GlamourReadySetItem);
			if (setUses.Count == 0) return;

			int outfitCounter = 0;
			foreach (var setUse in setUses) {
				outfitCounter++;
				var setName = setUse.ConvertedItem.NameString;
				var label = $"{setName}:##itemset##browser";

				if (GuiHelpers.TextButtonNoBg(label, "Click on the Glamour Outfit name to apply all items", ItemIcon.ColorBronze)) {
					foreach (var it in setUse.SetItems.Select(i => InventoryItem.New(i.RowId, 0, 0))) {
						PluginServices.ApplyGearChange.ExecuteBrowserItem(it);
					}
				}
				DrawListOfItemIcons(setUse.SetItems, 5000+ outfitCounter);
			}
		}

		private static Dictionary<int,int> DrawListOfItemIconsHoveredIcon = [];
		public static void DrawListOfItemIcons(List<ItemRow> items, int key = 0)
			=> DrawListOfItemIcons(items.Select(i => InventoryItem.New(i.RowId, 0, 0)).ToList(), key);
		public static void DrawListOfItemIcons(List<InventoryItem> items, int listKey = 0) {
			if (items.Count == 0) return;
			bool isAnotherTooltipActive = false;
			int iconKey = 0;
			var sizeMod = 0.45f;

			var slot = items.First().Item.GlamourPlateSlot();
			foreach (var item in items) {
				bool isHovering = DrawListOfItemIconsHoveredIcon.TryGetValue(listKey, out int savedHoveringIconKey) && savedHoveringIconKey == iconKey;
				if (ItemIcon.DrawIcon(item, ref isHovering, ref isAnotherTooltipActive, out bool clickedMiddle, out bool clickedStain, slot, null, sizeMod)) {
					PluginServices.ApplyGearChange.ExecuteBrowserItem(item);
				}
				if (isHovering) DrawListOfItemIconsHoveredIcon[listKey] = iconKey;
				iconKey++;
				if(iconKey < items.Count) {
					ImGui.SameLine();
					if (iconKey % 5 == 0) {
						ImGui.NewLine();
					}
				}
			}

			if (!isAnotherTooltipActive) DrawListOfItemIconsHoveredIcon.Remove(listKey);
		}
	}
}