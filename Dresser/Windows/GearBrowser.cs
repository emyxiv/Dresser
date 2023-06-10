using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using Dalamud.Interface.Windowing;
using Dalamud.Logging;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;
using Dresser.Windows.Components;

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


		public static Vector4 CollectionColorBackground = new Vector4(113, 98, 119, 200) / 255;
		public static Vector4 CollectionColorBorder = (new Vector4(116, 123, 98, 255) / 255 * 0.4f) + new Vector4(0, 0, 0, 1);
		public static Vector4 CollectionColorScrollbar = (new Vector4(116, 123, 98, 255) / 255 * 0.2f) + new Vector4(0, 0, 0, 1);
		public static Vector4 ColorIconImageTintDisabled = new(1, 1, 1, 0.5f);
		public static Vector4 ColorIconImageTintEnabled = Vector4.One;

		private static int? HoveredItem = null;
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
					if (HotkeyNextSelect < 0) HotkeyNextSelect = 0;
					return true;
				case HotkeyPurpose.Down:
					HotkeyNextSelect = HoveredIncrement + RowSize;
					if (HotkeyNextSelect > ItemsCount) HotkeyNextSelect = ItemsCount - 1;
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
			ImGui.SameLine();

			ImGui.BeginGroup();
			ImGui.BeginChildFrame(84, ImGui.GetContentRegionAvail() - new Vector2(0, 0));
			if (DrawFilters()) RecomputeItems();
			ImGui.EndChildFrame();
			ImGui.EndGroup();

		}
		private void DrawSearchBar() {
			if (ImGui.InputTextWithHint("##SearchByName##GearBrowser", "Search", ref Search, 100))
				RecomputeItems();
			ImGui.SameLine();
			ImGui.Text($"Found: {ItemsCount}");

			ImGui.SameLine();
			if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog)) {
				this.Plugin.DrawConfigUI();
			}


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
					ImGui.TextDisabled(AddItemKind.ToString());
					foreach ((var inventoryType, var addItemTitle) in option) {
						var numberOfItems = SavedQuantityCacheGet(inventoryType);

						bool isChecked = false;
						ConfigurationManager.Config.FilterInventoryType.TryGetValue(inventoryType, out isChecked);

						if (AddItemKind == AdditionalItem.Currency && PluginServices.Storage.FilterCurrencyItemEx.TryGetValue(inventoryType, out var itex) && itex != null && PluginServices.Storage.FilterCurrencyIconTexture.TryGetValue(inventoryType, out var texWrap) && texWrap != null) {
							var savedPosX = ImGui.GetCursorPosX();
							if (ImGui.ImageButton(texWrap.ImGuiHandle, ItemIcon.IconSize / 2, Vector2.Zero, Vector2.One, 0, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg], isChecked ? ColorIconImageTintEnabled : ColorIconImageTintDisabled)) {


								filterChanged = true;
								ConfigurationManager.Config.FilterInventoryType[inventoryType] = !isChecked;
							}
							GuiHelpers.Tooltip($"{itex.NameString} ({numberOfItems})");

							ImGui.SameLine();
							var itemSize = ImGui.GetCursorPosX() - savedPosX + ImGui.GetStyle().ItemSpacing.X;
							if (ImGui.GetContentRegionAvail().X < itemSize) ImGui.NewLine();
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
				if (columnMode) ImGui.SameLine();
				filterChanged |= ImGui.Checkbox($"Current Race##displayCategory", ref ConfigurationManager.Config.filterCurrentRace);

				// todo: level
				// todo: ilvl
				// todo: rarity
				// todo: dyeable only / not dyeable / all
				// todo: dyed with
			} else
				ConfigurationManager.Config.FilterAdvancedCollapse = false;

			if (ImGui.CollapsingHeader($"Sort##Source##GearBrowser", ConfigurationManager.Config.FilterAdditionalCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) {
				ConfigurationManager.Config.FilterSortCollapse = true;
			} else
				ConfigurationManager.Config.FilterSortCollapse = false;


			return filterChanged;
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
					if (inventoryType == (InventoryType)InventoryTypeExtra.AllItems) break;
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
					&& (!ConfigurationManager.Config.filterCurrentJob || i.Item.CanBeEquipedByPlayedJob())
					&& SelectedSlot == i.Item.GlamourPlateSlot()
					&& i.IsFilterDisplayable()
					&& i.Item.CanBeEquipedByPlayedRaceGender()
					&& (
						//!Search.IsNullOrWhitespace() &&
						i.FormattedName.Contains(Search, StringComparison.OrdinalIgnoreCase)
						)

					);


			Items = items
				// remove duplicates
				.GroupBy(i => i.GetHashCode()).Select(i => i.First())

				// sort the items
				//.OrderBy(i => i.Item.EquipSlotCategoryEx)
				.OrderByDescending(i => i.Item.LevelEquip)
				//.OrderBy(i => i.Item.LevelItem)
				.ToList()
				;

			ItemsCount = Items.Count();
			JustRecomputed = true;
		}


		public static InventoryItem? SelectedInventoryItem {
			get{
				var selectedItemCurrentGear = CurrentGear.SelectedInventoryItem();
				if (selectedItemCurrentGear == null) return null;
				return Items?.Where(i => i.ItemId == selectedItemCurrentGear?.ItemId).FirstOrDefault();
			}
		}

		public int RowSize = 1;
		public int? HoveredIncrement = null;
		public int? HotkeyNextSelect = null;
		public void DrawItems() {
			PushStyleCollection();
			Vector2 sideBarSize = new(ConfigurationManager.Config.GearBrowserSideBarSize, 0);
			Vector2 available = ImGui.GetContentRegionAvail();
			var size = available.X > sideBarSize.X ? available - sideBarSize : available;
			ImGui.BeginChildFrame(76, size);
			//ImGui.BeginChildFrame(76, ImGui.GetContentRegionAvail());

			var selectedItemHash = SelectedInventoryItem?.GetHashCode();

			if (Items != null && ItemsCount > 0)
				try {

					bool isTooltipActive = false;
					var i = 0;
					bool rowSizeChecked = false;
					bool hotkeySelected = false;

					foreach (var item in Items) {

						// icon
						var itemHash = item.GetHashCode();
						bool isHovered = itemHash == HoveredItem;
						bool wasHovered = isHovered;

						var selectedInCurrentGear = itemHash == selectedItemHash;
						if (selectedInCurrentGear) HoveredIncrement = i;
						isHovered |= selectedInCurrentGear;
						var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive);
						if (JustRecomputed && selectedInCurrentGear) ImGui.SetScrollHereY();
						if (isHovered)
							HoveredItem = itemHash;
						else if (!isHovered && wasHovered)
							HoveredItem = null;

						// execute when clicked
						if (iconClicked) {
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
			PopStyleCollection();
		}

		public static void PushStyleCollection() {
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ItemIcon.IconSize / 5f);
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemIcon.IconSize / 8f);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 3 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 7 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(CollectionColorBackground));
			ImGui.PushStyleColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(CollectionColorBorder));
			ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, ImGui.ColorConvertFloat4ToU32(CollectionColorScrollbar));


			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ItemIcon.IconSize / 8f);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 3 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.ColorConvertFloat4ToU32(CollectionColorBackground));
		}
		public static void PopStyleCollection() {
			ImGui.PopStyleColor(4);
			ImGui.PopStyleVar(8);
		}


		private void TestWindow() {
			if (ImGui.Begin("Test Window")) {


				// textures
				var texturePart = PluginServices.ImageGuiCrop.GetPart("character", 17);
				if (texturePart.Item1 != IntPtr.Zero) {
					if (PluginServices.ImageGuiCrop.Textures.TryGetValue("character", out var tex)) {
						ImGui.Text($"s:{tex.Width}*{tex.Height}");
						ImGui.Image(texturePart.Item1, new(tex.Width, tex.Height));
					}
					ImGui.Image(texturePart.Item1, ItemIcon.IconSize, texturePart.Item2, texturePart.Item3);
					ImGui.SameLine();
					ImGui.Image(texturePart.Item1, texturePart.Item4, texturePart.Item2, texturePart.Item3);
				}

				ImGui.End();
			}
		}
	}
}