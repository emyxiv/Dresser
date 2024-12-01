using System.Linq;
using System.Numerics;

using Dresser.Extensions;
using Dresser.Services;
using Dresser.Windows.Components;

using ImGuiNET;

using static Dresser.Services.Storage;


namespace Dresser.Windows
{
    public partial class GearBrowser
    {
        private static bool DrawFilters() {
			bool filterChanged = false;

			if (ImGui.CollapsingHeader($"Owned##Source##GearBrowser", ConfigurationManager.Config.FilterSourceCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) {
				ConfigurationManager.Config.FilterSourceCollapse = true;
				ImGui.Columns(ConfigurationManager.Config.FilterInventoryCategoryColumnNumber is >= 1 and <= 5 ? ConfigurationManager.Config.FilterInventoryCategoryColumnNumber : 2);
				ImGui.BeginGroup();

				int i = 0;
				foreach ((var cat, var willDisplay) in ConfigurationManager.Config.FilterInventoryCategory) {

					var numberOfItems = SavedQuantityCacheGet(cat);
					if (numberOfItems < 1 && ConfigurationManager.Config.GearBrowserSourceHideEmpty) continue;

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
				foreach ((var addItemKind, var option) in PluginServices.Storage.FilterNames) {
					ImGui.TextDisabled(addItemKind.ToString().AddSpaceBeforeCapital());
					foreach ((var inventoryType, var addItemTitle) in option) {
						var numberOfItems = SavedQuantityCacheGet(inventoryType);

						bool isChecked = false;
						ConfigurationManager.Config.FilterInventoryType.TryGetValue(inventoryType, out isChecked);

						if (addItemKind == AdditionalItem.Currency && PluginServices.Storage.FilterCurrencyItemEx.TryGetValue(inventoryType, out var itex) && itex != null && PluginServices.Storage.FilterCurrencyIconTexture.TryGetValue(inventoryType, out var texWrap) && texWrap != null) {
							var savedPosX = ImGui.GetCursorPosX();
							if (ImGui.ImageButton(texWrap.GetWrapOrEmpty().ImGuiHandle, ItemIcon.IconSize / 2, Vector2.Zero, Vector2.One, 0, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg], isChecked ? Styler.ColorIconImageTintEnabled : Styler.ColorIconImageTintDisabled)) {


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

				ImGui.SameLine();
				var strict = ConfigurationManager.Config.filterCurrentJobFilterType == JobFilterType.Strict;
				if (!ConfigurationManager.Config.filterCurrentJob) ImGui.BeginDisabled();
				var strictChanged = ImGui.Checkbox($"S##Current Job Strict##displayCategory", ref strict);
				filterChanged |= strictChanged;
				if (strictChanged) {
					ConfigurationManager.Config.filterCurrentJobFilterType = strict ? JobFilterType.Strict : JobFilterType.None;
				}
				if (!ConfigurationManager.Config.filterCurrentJob) ImGui.EndDisabled();
				GuiHelpers.Tooltip($"Strict\nOnly show items of current job\nHandy to find job gear");

				ImGui.SameLine();
				var relax = ConfigurationManager.Config.filterCurrentJobFilterType == JobFilterType.Relax;
				if (!ConfigurationManager.Config.filterCurrentJob) ImGui.BeginDisabled();

				var relaxChanged = ImGui.Checkbox($"R##Current Job Relax##displayCategory", ref relax);
				filterChanged |= relaxChanged;
				if (relaxChanged) {
					ConfigurationManager.Config.filterCurrentJobFilterType = relax ? JobFilterType.Relax : JobFilterType.None;
				}
				if (!ConfigurationManager.Config.filterCurrentJob) ImGui.EndDisabled();
				GuiHelpers.Tooltip($"Relax\nHides job gear\nHandy to make a role compatible glam ");

				if (columnMode) ImGui.SameLine();
				filterChanged |= ImGui.Checkbox($"Current Race##displayCategory", ref ConfigurationManager.Config.filterCurrentRace);

				var numberInputFrameWidth = ImGui.GetFontSize() * 2;
				// todo: level
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if (filterChanged |= ImGui.DragFloat($"##Min##EquipLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterEquipLevel.X, 1, 1, 90, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
					if (ConfigurationManager.Config.filterEquipLevel.Y < ConfigurationManager.Config.filterEquipLevel.X) ConfigurationManager.Config.filterEquipLevel.Y = ConfigurationManager.Config.filterEquipLevel.X;
				}
				ImGui.SameLine();
				ImGui.TextUnformatted("-");
				ImGui.SameLine();
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if(filterChanged |= ImGui.DragFloat($"##Max##EquipLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterEquipLevel.Y, 1, 1, 200, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
					if (ConfigurationManager.Config.filterEquipLevel.X > ConfigurationManager.Config.filterEquipLevel.Y) ConfigurationManager.Config.filterEquipLevel.X = ConfigurationManager.Config.filterEquipLevel.Y;
				}
				ImGui.SameLine();
				ImGui.Text("Job Level");
				// todo: ilvl
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if (filterChanged |= ImGui.DragFloat($"##Min##ItemLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterItemLevel.X, 1, 1, 1000, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
					if (ConfigurationManager.Config.filterItemLevel.Y < ConfigurationManager.Config.filterItemLevel.X) ConfigurationManager.Config.filterItemLevel.Y = ConfigurationManager.Config.filterItemLevel.X;
				}
				ImGui.SameLine();
				ImGui.TextUnformatted("-");
				ImGui.SameLine();
				ImGui.SetNextItemWidth(numberInputFrameWidth);
				if (filterChanged |= ImGui.DragFloat($"##Max##ItemLevel##Filters##GearBrowser", ref ConfigurationManager.Config.filterItemLevel.Y, 1, 1, 1000, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
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
				if (ImGui.BeginCombo(" Gear Color##Filters##GearBrowser", selectedRarityColor.HasValue ? "" : "Any")) {
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
    }

	public enum JobFilterType : byte {
		None = 0,
		Strict = 1,
		Relax = 2,
	}
}
