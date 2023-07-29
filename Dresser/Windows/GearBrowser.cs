using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;
using Dresser.Windows.Components;
using InventoryItem = Dresser.Structs.Dresser.InventoryItem;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using static Dresser.Services.Storage;

namespace Dresser.Windows {
	public class GearBrowser : Window, IWindowWithHotkey, IDisposable {
		private Plugin Plugin;

		public GearBrowser(Plugin plugin) : base(
			"Gear Browser", ImGuiWindowFlags.None) {
			this.SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};
			this.Plugin = plugin;
		}
		public void Dispose() { }



		private static BrowserIndex? HoveredItem = null;
		private static string Search = "";
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

		public override void OnOpen() {
			RecomputeItems();
		}

		public bool OnHotkey(HotkeyPurpose hotkeyType) {
			switch (hotkeyType) {
				case HotkeyPurpose.Up:
					HotkeyNextSelect = HoveredIncrement - RowSize;
					if (HotkeyNextSelect < 0) HotkeyNextSelect = HoveredIncrement;
					return true;
				case HotkeyPurpose.Down:
					HotkeyNextSelect = HoveredIncrement + RowSize;
					if (HotkeyNextSelect > ItemsCount) HotkeyNextSelect = HoveredIncrement;
					return true;
				case HotkeyPurpose.Left:
					HotkeyNextSelect = HoveredIncrement - 1;
					return true;
				case HotkeyPurpose.Right:
					HotkeyNextSelect = HoveredIncrement + 1;
					return true;
				default:
					return false;
			}
		}

		public override void Draw() {

			if (this.Collapsed == false) this.Collapsed = null; // restore collapsed state after uncollapse

			switch (ConfigurationManager.Config.GearBrowserDisplayMode) {
				case DisplayMode.Vertical: DrawWithMode_Vertical(); break;
				case DisplayMode.SidebarOnRight: DrawWithMode_SidebarOnRight(); break;
				default: DrawWithMode_Vertical(); break;
			}

			//TestWindow();
		}

		public enum DisplayMode {
			Vertical,
			SidebarOnRight,
			//SidebarOnLeft,
		}
		private void DrawWithMode_Vertical() {
			DrawSearchBar();

			if (DrawFilters()) RecomputeItems();

			DrawItems();

		}
		private void DrawWithMode_SidebarOnRight() {

			DrawSearchBar();
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
		private void DrawSearchBar() {

			ImGui.AlignTextToFramePadding();
			var available = ImGui.GetContentRegionAvail().X;
			float sideBarSize = ConfigurationManager.Config.GearBrowserSideBarSize;

			var isSidebarFitting = available > sideBarSize;
			float searchFrameMult = isSidebarFitting ? 2.5f : 1f;
			if (ConfigurationManager.Config.GearBrowserSideBarHide) isSidebarFitting = false;
			var size = isSidebarFitting ? available - sideBarSize : available;
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding * searchFrameMult);
			ImGui.SetNextItemWidth(size);
			if (ImGui.InputTextWithHint("##SearchByName##GearBrowser", "Search", ref Search, 100))
				RecomputeItems();

			if(isSidebarFitting) ImGui.SameLine();
			ImGui.Text($"{ItemsCount}");
			GuiHelpers.Tooltip($"{ItemsCount} items found with the selected filters");

			ImGui.SameLine();

			var sidebarShowHideIcon = ConfigurationManager.Config.GearBrowserSideBarHide ? Dalamud.Interface.FontAwesomeIcon.Columns : Dalamud.Interface.FontAwesomeIcon.Expand;

			var spacing = ImGui.GetContentRegionAvail().X
				- GuiHelpers.CalcIconSize(sidebarShowHideIcon).X // setting icon
				- GuiHelpers.CalcIconSize(Dalamud.Interface.FontAwesomeIcon.Cog).X // setting icon
				- ImGui.GetStyle().ItemInnerSpacing.X * 2 // * by number of icon, cause it's between them (and left end item)
				- (ImGui.GetStyle().FramePadding.X * 4); // * by number of icons x2, cause on each sides of the icon
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);


			if (GuiHelpers.IconButton(sidebarShowHideIcon)) {
				ConfigurationManager.Config.GearBrowserSideBarHide = !ConfigurationManager.Config.GearBrowserSideBarHide;
			}
			ImGui.SameLine();
			if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog)) {
				this.Plugin.ToggleConfigUI();
			}

			ImGui.PopStyleVar();

		}
		private static bool DrawFilters() {
			bool filterChanged = false;

			if (ImGui.CollapsingHeader($"Source##Source##GearBrowser", ConfigurationManager.Config.FilterSourceCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) {
				ConfigurationManager.Config.FilterSourceCollapse = true;
				ImGui.Columns(ConfigurationManager.Config.FilterInventoryCategoryColumnNumber is >= 1 and <= 5 ? ConfigurationManager.Config.FilterInventoryCategoryColumnNumber : 2);
				ImGui.BeginGroup();

				int i = 0;
				foreach ((var cat, var willDisplay) in ConfigurationManager.Config.FilterInventoryCategory) {
					var numberOfItems = SavedQuantityCacheGet(cat);


					var willDisplayValue = willDisplay;
					if (numberOfItems < 1) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * new Vector4(1, 1, 1, 0.5f));
					if (filterChanged |= ImGui.Checkbox($"{cat.ToFriendlyName()} ({numberOfItems})##displayCategory", ref willDisplayValue))
						ConfigurationManager.Config.FilterInventoryCategory[cat] = willDisplayValue;
					if (numberOfItems < 1) {
						ImGui.PopStyleColor();
						GuiHelpers.Tooltip("No item available for this filter");
					}


					// column breaker
					i++;
					bool columnBreak = false;
					//var valu =  (int)(ConfigurationManager.Config.FilterInventoryCategoryColumnDistribution);
					var valu = 1;
					for (int colNum = 1; colNum <= ConfigurationManager.Config.FilterInventoryCategoryColumnNumber; colNum++)
						columnBreak |= i == valu * colNum;
					if (i > 0 && columnBreak)
						ImGui.EndGroup(); ImGui.NextColumn(); ImGui.BeginGroup();

				}
				ImGui.EndGroup();
				ImGui.Columns();
			} else
				ConfigurationManager.Config.FilterSourceCollapse = false;

			if (ImGui.CollapsingHeader($"Unobtained##Source##GearBrowser", ConfigurationManager.Config.FilterAdditionalCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) {
				ConfigurationManager.Config.FilterAdditionalCollapse = true;

				ImGui.Columns(ConfigurationManager.Config.FilterInventoryTypeColumnNumber is >= 1 and <= 5 ? ConfigurationManager.Config.FilterInventoryTypeColumnNumber : 2);
				ImGui.BeginGroup();
				int i = 0;
				foreach ((var AddItemKind, var option) in PluginServices.Storage.FilterNames) {
					ImGui.TextDisabled(AddItemKind.ToString().AddSpaceBeforeCapital());
					foreach ((var inventoryType, var addItemTitle) in option) {
						var numberOfItems = SavedQuantityCacheGet(inventoryType);

						bool isChecked = false;
						ConfigurationManager.Config.FilterInventoryType.TryGetValue(inventoryType, out isChecked);

						if (AddItemKind == AdditionalItem.Currency && PluginServices.Storage.FilterCurrencyItemEx.TryGetValue(inventoryType, out var itex) && itex != null && PluginServices.Storage.FilterCurrencyIconTexture.TryGetValue(inventoryType, out var texWrap) && texWrap != null) {
							var savedPosX = ImGui.GetCursorPosX();
							if (ImGui.ImageButton(texWrap.ImGuiHandle, ItemIcon.IconSize / 2, Vector2.Zero, Vector2.One, 0, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg], isChecked ? Styler.ColorIconImageTintEnabled : Styler.ColorIconImageTintDisabled)) {


								filterChanged = true;
								ConfigurationManager.Config.FilterInventoryType[inventoryType] = !isChecked;
							}
							GuiHelpers.Tooltip($"{itex.NameString} ({numberOfItems})");

							ImGui.SameLine();
							var itemSize = ImGui.GetCursorPosX() - savedPosX + ImGui.GetStyle().ItemSpacing.X;
							if (ImGui.GetContentRegionAvail().X < itemSize || option.Last().Key == inventoryType) ImGui.NewLine();
						} else
							if (filterChanged |= ImGui.Checkbox($"{((InventoryTypeExtra)inventoryType).ToString().AddSpaceBeforeCapital()} ({numberOfItems})##displayInventoryTypeAdditionalItem", ref isChecked))
							ConfigurationManager.Config.FilterInventoryType[inventoryType] = isChecked;

					}

					// column breaker
					i++;
					bool columnBreak = false;
					//var valu =  (int)(ConfigurationManager.Config.FilterInventoryCategoryColumnDistribution);
					var valu = 1;
					for (int colNum = 1; colNum <= ConfigurationManager.Config.FilterInventoryTypeColumnNumber; colNum++)
						columnBreak |= i == valu * colNum;
					if (i > 0 && columnBreak)
						ImGui.EndGroup(); ImGui.NextColumn(); ImGui.BeginGroup();

				}
				ImGui.EndGroup();
				ImGui.Columns();

			} else
				ConfigurationManager.Config.FilterAdditionalCollapse = false;


			if (ImGui.CollapsingHeader($"Filters##Source##GearBrowser", ConfigurationManager.Config.FilterAdvancedCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) {
				ConfigurationManager.Config.FilterAdvancedCollapse = true;
				var columnMode = !ConfigurationManager.Config.GearBrowserDisplayMode.HasFlag(DisplayMode.Vertical);

				filterChanged |= ImGui.Checkbox($"Current Job##displayCategory", ref ConfigurationManager.Config.filterCurrentJob);
				if (!ConfigurationManager.Config.filterCurrentJob) ImGui.BeginDisabled();
				ImGui.SameLine();
				filterChanged |= ImGui.Checkbox($"##Current Job Strict##displayCategory", ref ConfigurationManager.Config.filterCurrentJobStrict);
				if (!ConfigurationManager.Config.filterCurrentJob) ImGui.EndDisabled();
				GuiHelpers.Tooltip($"Strict\nOnly show items of current job\nHandy to find job gear");
				if (columnMode) ImGui.SameLine();
				filterChanged |= ImGui.Checkbox($"Current Race##displayCategory", ref ConfigurationManager.Config.filterCurrentRace);

				var numberInputFrameWidth = ImGui.GetFontSize() * 2;
				// todo: level
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if (filterChanged |= ImGui.DragFloat($"##Min##EquipLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterEquipLevel.X, 1, 1, 90, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
					if (ConfigurationManager.Config.filterEquipLevel.Y < ConfigurationManager.Config.filterEquipLevel.X) ConfigurationManager.Config.filterEquipLevel.Y = ConfigurationManager.Config.filterEquipLevel.X;
				}
				ImGui.SameLine();
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if(filterChanged |= ImGui.DragFloat($"##Max##EquipLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterEquipLevel.Y, 1, 1, 90, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
					if (ConfigurationManager.Config.filterEquipLevel.X > ConfigurationManager.Config.filterEquipLevel.Y) ConfigurationManager.Config.filterEquipLevel.X = ConfigurationManager.Config.filterEquipLevel.Y;
				}
				ImGui.SameLine();
				ImGui.Text("Equip Level");
				// todo: ilvl
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if (filterChanged |= ImGui.DragFloat($"##Min##ItemLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterItemLevel.X, 1, 1, 665, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
					if (ConfigurationManager.Config.filterItemLevel.Y < ConfigurationManager.Config.filterItemLevel.X) ConfigurationManager.Config.filterItemLevel.Y = ConfigurationManager.Config.filterItemLevel.X;
				}
				ImGui.SameLine();
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if (filterChanged |= ImGui.DragFloat($"##Max##ItemLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterItemLevel.Y, 1, 1, 665, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
					if (ConfigurationManager.Config.filterItemLevel.X > ConfigurationManager.Config.filterItemLevel.Y) ConfigurationManager.Config.filterItemLevel.X = ConfigurationManager.Config.filterItemLevel.Y;
				}
				ImGui.SameLine();
				ImGui.Text("Item Level");

				// todo: rarity
				Vector4? selectedRarityColor = null;
				var hoveredAlphaMod = new Vector4(1, 1, 1, 0.5f);
				if (ConfigurationManager.Config.filterRarity.HasValue && ConfigurationManager.Config.filterRarity.Value < PluginServices.Storage.RarityColors.Count)
					if (PluginServices.Storage.RarityColors.TryGetValue(ConfigurationManager.Config.filterRarity.Value, out var selectedRarityColor2))
						selectedRarityColor = selectedRarityColor2;

				if (selectedRarityColor.HasValue) {
					ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered]);
					ImGui.PushStyleColor(ImGuiCol.FrameBg, selectedRarityColor.Value * hoveredAlphaMod);
					ImGui.PushStyleColor(ImGuiCol.Button, selectedRarityColor.Value * hoveredAlphaMod);
				}

				ImGui.SetNextItemWidth(numberInputFrameWidth * 2 + ImGui.GetStyle().ItemSpacing.X);
				if (ImGui.BeginCombo("Rarity##Filters##GearBrowser", selectedRarityColor.HasValue ? "" : "Any")) {
					var newItemSpacing = new Vector2(ImGui.GetFontSize() * 0.2f);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, newItemSpacing);
					if (filterChanged |= ImGui.Selectable($"##Any##Rarity##Filters##GearBrowser"))
						ConfigurationManager.Config.filterRarity = null;
					ImGui.SameLine();
					GuiHelpers.TextCenter("Any", newItemSpacing.X);

					var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X,ImGui.GetTextLineHeightWithSpacing());
					foreach ((var rarityValue, var rarityCol) in PluginServices.Storage.RarityColors) {
						if (!PluginServices.Storage.RarityAllowed.Contains(rarityValue)) continue;
						ImGui.PushStyleColor(ImGuiCol.Button, rarityCol * hoveredAlphaMod);
						ImGui.PushStyleColor(ImGuiCol.ButtonHovered, rarityCol);
						if (!filterChanged && (filterChanged |= ImGui.Button($"##{rarityValue}##Rarity##Filters##GearBrowser", buttonSize))) {
							ConfigurationManager.Config.filterRarity = rarityValue;
							ImGui.CloseCurrentPopup();
						}
						ImGui.PopStyleColor(2);

					}
					ImGui.PopStyleVar();
					ImGui.EndCombo();
				}
				if (selectedRarityColor.HasValue) ImGui.PopStyleColor(3);
				// todo: dyeable only / not dyeable / all
				// todo: dyed with
			} else
				ConfigurationManager.Config.FilterAdvancedCollapse = false;

			if (ImGui.CollapsingHeader($"Sort##Source##GearBrowser", ConfigurationManager.Config.FilterAdditionalCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) {
				ConfigurationManager.Config.FilterSortCollapse = true;

				filterChanged |= DrawSort();

			} else
				ConfigurationManager.Config.FilterSortCollapse = false;


			return filterChanged;
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


					var text = $"{method.ToString().AddSpaceBeforeCapital()}";
					var iconDirection = direction == InventoryItemOrder.OrderDirection.Ascending ? Dalamud.Interface.FontAwesomeIcon.SortAlphaUp : Dalamud.Interface.FontAwesomeIcon.SortAlphaDown;

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
						if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash, default, $"RemoveSortSorter##{j}")) {
							ConfigurationManager.Config.SortOrder.RemoveAt(j);
							recompute = true;
							ImGui.CloseCurrentPopup();
						}
						ImGui.EndPopup();

					}

					if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.AcceptNoPreviewTooltip | ImGuiDragDropFlags.SourceNoPreviewTooltip)) {

						ImGui.SetDragDropPayload("DND_ORDER_INDEX", (nint)indexPtr, sizeof(int));
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

			if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus, default, "AddSortSorter")) {
				if (ConfigurationManager.Config.SortOrder != null) {
					var used = ConfigurationManager.Config.SortOrder.Select(s => s.Method);
					var available = Enum.GetValues<InventoryItemOrder.OrderMethod>().ToHashSet();
					var notUsed = available.Except(used).FirstOrDefault();

					ConfigurationManager.Config.SortOrder?.Add((notUsed, InventoryItemOrder.OrderDirection.Descending));
				}
			}
			ImGui.SameLine();
			if (ConfigurationManager.Config.SavedSortOrders == null) ConfigurationManager.Config.SavedSortOrders = new();

			if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Save, default, "AddSortSorter")) {
				var random = new Random();
				const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
				var newKey = "";
				do {
					newKey = new string(Enumerable.Repeat(chars, 3).Select(s => s[random.Next(s.Length)]).ToArray());
				} while (ConfigurationManager.Config.SavedSortOrders.ContainsKey(newKey));
				ConfigurationManager.Config.SavedSortOrders.Add(newKey, ConfigurationManager.Config.SortOrder!);
			}
			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(Dalamud.Interface.FontAwesomeIcon.Recycle, "Restore default order sets", default, "RestoreDefaultSorters")) {
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
						if (GuiHelpers.IconButtonHoldConfirm(Dalamud.Interface.FontAwesomeIcon.Trash, "Hold Ctrl + Shift and Click to delete this saved preset", default, $"RemoveSavedSortOrders##{key}")) {
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
		//=> SavedQuantityCache = ConfigurationManager.Config.GetSavedInventoryLocalChar().ToDictionary(c => c.Key, c => c.Value.Count(i => i.ItemId != 0));
		private static int SavedQuantityCacheGet(InventoryCategory cat) {
			SavedQuantityInventoryCategoryCache.TryGetValue(cat, out var count);
			return count;
		}
		private static int SavedQuantityCacheGet(InventoryType type) {
			SavedQuantityInventoryTypeCache.TryGetValue(type, out var count);
			return count;
		}


		public static IEnumerable<InventoryItem>? Items = null;
		private static int ItemsCount = 0;
		private static bool JustRecomputed = false;
		public static void RecomputeItems() {

			IEnumerable<InventoryItem> items = new HashSet<InventoryItem>();

			foreach ((var inventoryType, var itemsToAdd) in PluginServices.Storage.AdditionalItems) {
				if (ConfigurationManager.Config.FilterInventoryType.TryGetValue(inventoryType, out var isEnabled) && isEnabled) {
					items = items.Concat(itemsToAdd);
					//PluginLog.Debug($"included {inventoryType} {itemsToAdd.Count} cat:{string.Join(",", itemsToAdd.Select(p => p.SortedCategory).Distinct())} types:{string.Join(",", itemsToAdd.Select(p => p.SortedContainer).Distinct())}");
				}
			}

			//PluginLog.Debug($"all items => {items.Count()} cat:{string.Join(",", items.Select(p => p.SortedCategory).Distinct())} types:{string.Join(",", items.Select(p => p.SortedContainer).Distinct())}");

			// items from saved inventory (critical impact lib)
			items = items.Concat(ConfigurationManager.Config.GetSavedInventoryLocalChar().SelectMany(t => t.Value));
			items = items.Concat(ConfigurationManager.Config.GetSavedInventoryLocalCharsRetainers().SelectMany(t => t.Value));

			items = items.Where(i => !i.IsEmpty && i.Item.ModelMain != 0);

			SavedQuantityCacheMake(items);

			items = items.Where(i =>
					(!ConfigurationManager.Config.filterCurrentRace || i.Item.CanBeEquipedByPlayedRaceGender())
					&& (!ConfigurationManager.Config.filterCurrentJob || i.Item.CanBeEquipedByPlayedJob(ConfigurationManager.Config.filterCurrentJobStrict))
					&& SelectedSlot == i.Item.GlamourPlateSlot()
					&& i.IsFilterDisplayable()
					&& i.IsInFilterLevelRanges()
					&& (!ConfigurationManager.Config.filterRarity.HasValue || i.Item.Rarity == ConfigurationManager.Config.filterRarity)
					&& i.Item.CanBeEquipedByPlayedRaceGender()
					&& i.IsNotInBlackList()
				);


			if (!Search.IsNullOrWhitespace())
				items = items.Where(i =>
					i.FormattedName.Contains(Search, StringComparison.OrdinalIgnoreCase) // search for item name
					|| i.StainName().Contains(Search, StringComparison.OrdinalIgnoreCase) // search for stain name
					|| (i.ModName?.Contains(Search, StringComparison.OrdinalIgnoreCase)??false) // search for mod name
					|| (i.ModAuthor?.Contains(Search, StringComparison.OrdinalIgnoreCase)??false) // search for mod author
					);

			if (!items.Any()) { Items = new List<InventoryItem>(); FinishRecomputeItems(); return; }

			// remove duplicates
			IEnumerable< InventoryItem> uniqueItems;
			if (items.Any(i => i.IsModded())){
				uniqueItems = items;
			} else {
				uniqueItems = items.GroupBy(i => i.GetHashCode()).Select(i => i.First());
			}

			Items = InventoryItemOrder.OrderItems(uniqueItems);

			FinishRecomputeItems();

		}
		private static void FinishRecomputeItems() {

			ItemsCount = Items?.Count() ?? 0;
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
			Vector2 sideBarSize = new(ConfigurationManager.Config.GearBrowserSideBarSize, 0);
			Vector2 available = ImGui.GetContentRegionAvail();
			var isSidebarFitting = available.X > sideBarSize.X;
			if (ConfigurationManager.Config.GearBrowserSideBarHide) isSidebarFitting = false;
			var size = isSidebarFitting ? available - sideBarSize : available;
			ImGui.BeginChildFrame(76, size);
			//ImGui.BeginChildFrame(76, ImGui.GetContentRegionAvail());

			BrowserIndex? selectedItemHash = SelectedInventoryItem == null ? null : (BrowserIndex)SelectedInventoryItem;
			if (Items != null && ItemsCount > 0)
				try {

					bool isTooltipActive = false;
					var i = 0;
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
						var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive, null, ContextMenuBrowser);
						var hoverDown = !selectedInCurrentGear && ImGui.IsItemHovered() && (ImGui.GetIO().KeyCtrl || ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left]);

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


						ImGui.SameLine();
						if (ImGui.GetContentRegionAvail().X < ItemIcon.IconSize.X) {
							if (!rowSizeChecked) {
								rowSizeChecked = true;
								RowSize = i + 1;
							}
							ImGui.NewLine();
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
			if (!ConfigurationManager.Config.PenumbraUseModListCollection && itemInv.IsModded() && ImGui.Selectable("Blacklist this Mod"))
				ConfigWindow.AddModToBlacklist((itemInv.ModDirectory, itemInv.ModName)!);

			if (item.CanTryOn && ImGui.Selectable("Try On") && PluginServices.TryOn.CanUseTryOn)
				PluginServices.TryOn.TryOnItem(item);
			if (item.CanOpenCraftLog && ImGui.Selectable("Open Crafting Log"))
				PluginServices.GameInterface.OpenCraftingLog(item.RowId);

			DrawSameModels(itemInv);

		}
		public static void DrawSameModels(InventoryItem item) {

			var sharedModels = item.Item.GetSharedModels();
			if (sharedModels != null && sharedModels.Any()) {
				ImGui.Spacing();
				ImGui.Text("Shared model:");
				DrawListOfItemIcons(sharedModels);
			}
		}

		private static int DrawListOfItemIconsHoveredIcon = -1;
		public static void DrawListOfItemIcons(List<ItemEx> items)
			=> DrawListOfItemIcons(items.Select(i => InventoryItemExtensions.New(i.RowId, 0)).ToList());
		public static void DrawListOfItemIcons(List<InventoryItem> items) {
			if (!items.Any()) return;
			bool isAnotherTooltipActive = false;
			int iconKey = 0;
			var sizeMod = 0.45f;

			var slot = items.First().Item.GlamourPlateSlot();
			foreach (var item in items) {
				bool isHovering = iconKey == DrawListOfItemIconsHoveredIcon;
				if(ItemIcon.DrawIcon(item, ref isHovering, ref isAnotherTooltipActive, slot, null, sizeMod)) {
					PluginServices.ApplyGearChange.ExecuteBrowserItem(item);
				}
				if (isHovering) DrawListOfItemIconsHoveredIcon = iconKey;
				iconKey++;
				ImGui.SameLine();
				if(iconKey % 5 == 0) {
					ImGui.NewLine();
				}
			}

			if (!isAnotherTooltipActive) DrawListOfItemIconsHoveredIcon = -1;
		}

		private void TestWindow() {
			if (ImGui.Begin("Test Window")) {

				ImageGuiCrop.TestParts();

				// textures
				//var texturePart = PluginServices.ImageGuiCrop.GetPart("character", 17);
				//if (texturePart.ImGuiHandle != IntPtr.Zero) {
				//	if (PluginServices.ImageGuiCrop.Textures.TryGetValue("character", out var tex)) {
				//		ImGui.Text($"s:{tex.Width}*{tex.Height}");
				//		ImGui.Image(texturePart.Item1, new(tex.Width, tex.Height));
				//	}
				//	ImGui.Image(texturePart.ImGuiHandle, ItemIcon.IconSize, texturePart.Item2, texturePart.Item3);
				//	ImGui.SameLine();
				//	ImGui.Image(texturePart.ImGuiHandle, texturePart.Item4, texturePart.Item2, texturePart.Item3);
				//}

				ImGui.End();
			}
		}
	}
}