using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Services;
using Dresser.Windows.Components;

using Lumina.Excel.Sheets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using static Dresser.Services.Storage;


namespace Dresser.Windows
{
    public partial class GearBrowser
    {
		private static bool FilterActive = false;
        private static bool DrawFilters() {
			bool filterChanged = false;
			bool filterActiveBefore = FilterActive;
			bool? filterActiveAfter = null;

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

				filterChanged |= ImGui.Checkbox($"Hide Cash Shop", ref ConfigurationManager.Config.filterHideCashShop);
				GuiHelpers.Tooltip("Hide cash shop (sq store) items.\nNote: This will also hide the items that can be obtained by other means than the cash shop (e.g. with event's currency). However, these are usually from past event so these currencies should now be unobtainable.");
				filterChanged |= ImGui.Checkbox($"Hide No Source", ref ConfigurationManager.Config.filterHideNoSource);
				GuiHelpers.Tooltip("Hide unobtained items that have no known source.\nNote: This is not purely a \"hide unobtainable\" checkbox, as some unobtainable future event items have known sources, and in opposition, some sources are not known but this plugin.");

				int i = 0;
				foreach ((var addItemKind, var option) in PluginServices.Storage.FilterNames) {
					ImGui.TextDisabled(addItemKind.ToString().AddSpaceBeforeCapital());
					foreach ((var inventoryType, var addItemTitle) in option) {
						var numberOfItems = SavedQuantityCacheGet(inventoryType);

						bool isChecked = false;
						ConfigurationManager.Config.FilterInventoryType.TryGetValue(inventoryType, out isChecked);

						if (addItemKind == AdditionalItem.Currency && PluginServices.Storage.FilterCurrencyItemEx.TryGetValue(inventoryType, out var itex) && itex != null && PluginServices.Storage.FilterCurrencyIconTexture.TryGetValue(inventoryType, out var texWrap) && texWrap != null) {
							var savedPosX = ImGui.GetCursorPosX();
							if (ImGui.ImageButton(texWrap.GetWrapOrEmpty().Handle, ItemIcon.IconSize / 2, Vector2.Zero, Vector2.One, 0, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg], isChecked ? Styler.ColorIconImageTintEnabled : Styler.ColorIconImageTintDisabled)) {


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

			if(filterActiveBefore) {
				ImGui.PushStyleColor(ImGuiCol.Header, Styler.FilterIndicatorFrameColor);
				ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Styler.FilterIndicatorFrameHoveredColor);
				ImGui.PushStyleColor(ImGuiCol.HeaderActive, Styler.FilterIndicatorFrameActiveColor);
			}
			var filterCollaspingHeaderIsOpen = ImGui.CollapsingHeader($"Filters##Source##GearBrowser", ConfigurationManager.Config.FilterAdvancedCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
			if (filterActiveBefore) ImGui.PopStyleColor(3);

			if (filterCollaspingHeaderIsOpen) {
				filterActiveAfter ??= false;

				ConfigurationManager.Config.FilterAdvancedCollapse = true;
				var columnMode = !ConfigurationManager.Config.GearBrowserDisplayMode.HasFlag(DisplayMode.Vertical);

				filterChanged |= JobCategoryFilter(out var filterActiveJobs);
				filterActiveAfter |= filterActiveJobs;

				//filterChanged |= ImGui.Checkbox($"Current Job##displayCategory", ref ConfigurationManager.Config.filterCurrentJob);

				var enumJftValues = Enum.GetValues<JobFilterType>().Cast<JobFilterType>();
				var maxWidthJftCombo = enumJftValues.Select(jft => ImGui.CalcTextSize(jft.ToString()).X).Max() + (ImGui.GetFontSize() * 2);
				ImGui.SetNextItemWidth(maxWidthJftCombo);
				if(ImGui.BeginCombo("##Current Job Filter Type##displayCategory", ConfigurationManager.Config.filterCurrentJobFilterType.ToString())) {

					foreach(var enumValue in enumJftValues) {
						var isSelected = ConfigurationManager.Config.filterCurrentJobFilterType == enumValue;
						var clickedJobFilterTypeCombo = ImGui.Selectable($"{enumValue.ToString().AddSpaceBeforeCapital()}##Current Job Filter Type##displayCategory", isSelected);
						if (clickedJobFilterTypeCombo) {
							ConfigurationManager.Config.filterCurrentJobFilterType = enumValue;
							filterChanged = true;
						}
						GuiHelpers.Tooltip(enumValue.Tooltip());
					}
					ImGui.EndCombo();
				}
				ImGui.SameLine();
				ImGui.TextUnformatted("Job filter Type");

				if (columnMode) ImGui.SameLine();
				filterChanged |= ImGui.Checkbox($"Current Race/Gender/GC##displayCategory", ref ConfigurationManager.Config.filterCurrentRace);

				filterChanged |= ImGui.Checkbox($"Glamour Outfits Only", ref ConfigurationManager.Config.filterGlamourSetsOnly);

				filterChanged |= ConfigControls.ConfigFloatFromTo(nameof(ConfigurationManager.Config.filterEquipLevel), $"Job Level##Filters##GearBrowser", out bool jobLevelfilterActive);
				filterActiveAfter |= jobLevelfilterActive;
				filterChanged |= ConfigControls.ConfigFloatFromTo(nameof(ConfigurationManager.Config.filterItemLevel), $"Item Level##Filters##GearBrowser", out bool itemLevelfilterActive);
				filterActiveAfter |= itemLevelfilterActive;

				// todo: rarity
				Vector4? selectedRarityColor = null;
				var hoveredAlphaMod = new Vector4(1, 1, 1, 0.5f);
				if (ConfigurationManager.Config.filterRarity.HasValue && ConfigurationManager.Config.filterRarity.Value < PluginServices.Storage.RarityColors.Count)
					if (PluginServices.Storage.RarityColors.TryGetValue(ConfigurationManager.Config.filterRarity.Value, out var selectedRarityColor2))
						selectedRarityColor = selectedRarityColor2;
				bool rarityFilterActive = selectedRarityColor.HasValue;
				filterActiveAfter |= rarityFilterActive;
				if (selectedRarityColor.HasValue) {
					ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered]);
					ImGui.PushStyleColor(ImGuiCol.FrameBg, selectedRarityColor.Value * hoveredAlphaMod);
					ImGui.PushStyleColor(ImGuiCol.Button, selectedRarityColor.Value * hoveredAlphaMod);
				}

				var numberInputFrameWidth = ImGui.GetFontSize() * 2;
				ImGui.SetNextItemWidth(numberInputFrameWidth * 2 + ImGui.GetStyle().ItemSpacing.X);
				var rarityComboStartPos = ImGui.GetCursorScreenPos();
				var isRarityComboOpen = ImGui.BeginCombo("##Item Rarity##Filters##GearBrowser", selectedRarityColor.HasValue ? "" : "Any");
				var rarityComboSize = ImGui.GetItemRectSize();

				if (isRarityComboOpen) {
					ImGui.GetItemRectSize();
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
				if (rarityFilterActive) {
					ImGui.GetWindowDrawList().AddRect(rarityComboStartPos, rarityComboStartPos + rarityComboSize, ImGui.GetColorU32(Styler.FilterIndicatorFrameColor), ImGui.GetStyle().FrameRounding, Styler.BigButtonBorderThickness);
				}
				ImGui.SameLine();
				ImGui.TextColored(rarityFilterActive ? Styler.FilterIndicatorFrameActiveColor : ImGui.GetStyle().Colors[(int)ImGuiCol.Text], " Item Rarity");

				// todo: dyeable only / not dyeable / all
				// todo: dyed with
			} else
				ConfigurationManager.Config.FilterAdvancedCollapse = false;

			if (ImGui.CollapsingHeader($"Sort##Source##GearBrowser", ConfigurationManager.Config.FilterAdditionalCollapse ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) {
				ConfigurationManager.Config.FilterSortCollapse = true;

				filterChanged |= DrawSort();

			} else
				ConfigurationManager.Config.FilterSortCollapse = false;

			if(filterActiveAfter != null) {
				FilterActive = filterActiveAfter.Value;
			}
			return filterChanged;
		}
		private static bool JobCategoryFilter(out bool filterActive) {
			filterActive = false;
			var classJobs = PluginServices.SheetManager.GetSheet<ClassJobSheet>();
			if (classJobs == null || classJobs.Count() == 0) return false;

			var classJobsSorted = SortClassJobs(classJobs);

			bool changed = false;


			// Calculate sizes and positions
			var selectedFrameSize = new Vector2(ImGui.GetContentRegionAvail().X, (ItemIcon.IconSize.Y / 2));
			var selectedFrameOrigin = ImGui.GetCursorPos();

			ImGui.SetNextItemWidth(Math.Max(120, ImGui.GetContentRegionAvail().X / 3));
			var isPreviewClicked = ImGui.InvisibleButton($"##summonSelectJobs##JobCategoryPreview", selectedFrameSize);
			var isPreviewHovered = ImGui.IsItemHovered();
			if (isPreviewClicked) {
				ImGui.OpenPopup("JobCategoryPopup##JobCategory");
			}

			ImGui.SetCursorPos(selectedFrameOrigin);

			// Preview / control
			//
			var selectedIds = ConfigurationManager.Config.FilterClassJobCategories ?? new List<uint>();
			var selectedJobNames = classJobsSorted.Where(c => selectedIds.Contains(c.RowId));

			if (!selectedJobNames.Any()) {
				ImGui.TextUnformatted(" Select to filter Job(s)");
			} else {
				int drawnCount = 0;
				foreach (var selectedClassJob in selectedJobNames) {
					if(ImGui.GetContentRegionAvail().X < (ItemIcon.IconSize.X * 1.0f)) break;
					bool _ = false;
					DrawClassJob(selectedClassJob, false, ref _);
					ImGui.SameLine();
					drawnCount++;
				}
				if(drawnCount < selectedJobNames.Count()) {
					GuiHelpers.TextWithFont($"+{selectedJobNames.Count() - drawnCount}", GuiHelpers.Font.Title);
				}
				ImGui.NewLine();
			}

			filterActive |= selectedIds.Count > 0;

			Vector4 borderColor;
			if (selectedIds.Count > 0)
				borderColor = isPreviewHovered ? Styler.FilterIndicatorFrameActiveColor : Styler.FilterIndicatorFrameColor; // going for active color when hovering instead of "hovered" color to make it more visible
			else
				borderColor = ImGui.GetStyle().Colors[isPreviewHovered ? (int)ImGuiCol.ButtonActive: (int)ImGuiCol.Button];
			var windowsMinPos = ImGui.GetWindowPos();
			ImGui.GetWindowDrawList().AddRect(windowsMinPos + selectedFrameOrigin, windowsMinPos + selectedFrameOrigin + selectedFrameSize, ImGui.GetColorU32(borderColor), Styler.BigButtonRounding, Styler.BigButtonBorderThickness);

			ImGui.SetCursorPosY(selectedFrameOrigin.Y + (ItemIcon.IconSize.Y / 2));
			ImGui.Spacing();


			// Draw Popup for class selection
			//
			if (ImGui.BeginPopup("JobCategoryPopup##JobCategory")) {

				// some buttons
				if (GuiHelpers.IconButtonNoBg(Dalamud.Interface.FontAwesomeIcon.Times, "##closeButton##JobCategoryPopup##JobCategory")) ImGui.CloseCurrentPopup();
				ImGui.SameLine();
				if (ImGui.Button("Clear##JobCategoryPopup##JobCategory")) {
					ConfigurationManager.Config.FilterClassJobCategories?.Clear();
					changed = true;
				}
				ImGui.SameLine();
				if (ImGui.Button("Select All##JobCategoryPopup##JobCategory")) {
					ConfigurationManager.Config.FilterClassJobCategories = classJobsSorted.Select(cj=>cj.RowId).ToList();
					changed = true;
				}
				ImGui.SameLine();
				if (PluginServices.Context.LocalPlayerClass != null && GuiHelpers.IconButtonTooltip(Dalamud.Interface.FontAwesomeIcon.Crosshairs, "Toggle Current Job", default, "Toggle Current Job##JobCategoryPopup##JobCategory")) {
					if(ConfigurationManager.Config.FilterClassJobCategories?.Contains(PluginServices.Context.LocalPlayerClass.Value.RowId) ?? false)
						ConfigurationManager.Config.FilterClassJobCategories?.Remove(PluginServices.Context.LocalPlayerClass.Value.RowId);
					else
						ConfigurationManager.Config.FilterClassJobCategories?.Add(PluginServices.Context.LocalPlayerClass.Value.RowId);
					changed = true;
				}
				ImGui.SameLine();
				if (PluginServices.Context.LocalPlayerClass != null && GuiHelpers.IconButtonTooltip(Dalamud.Interface.FontAwesomeIcon.Crosshairs, "Target Current Job", default, "Target Current Job##JobCategoryPopup##JobCategory")) {
					ConfigurationManager.Config.FilterClassJobCategories = [PluginServices.Context.LocalPlayerClass.Value.RowId];
					changed = true;
				}

				// Job type buttons line
				if (GuiHelpers.GameButton(UldBundle.CharacterClass_Tank,"SelectTankJobs##JobCategoryPopup##JobCategory","",new Vector2(ImGui.GetFontSize()*1.5f))) {
					ConfigurationManager.Config.FilterClassJobCategories = [.. PluginServices.DataManager.Excel.GetSheet<ClassJob>().Where(cj=> cj.IsTypeTank()).Select(cj=>cj.RowId)];
					changed = true;
				}
				ImGui.SameLine();
				if (GuiHelpers.GameButton(UldBundle.CharacterClass_Healer,"SelectHealJobs##JobCategoryPopup##JobCategory","",new Vector2(ImGui.GetFontSize()*1.5f))) {
					ConfigurationManager.Config.FilterClassJobCategories = [.. PluginServices.DataManager.Excel.GetSheet<ClassJob>().Where(cj=> cj.IsTypeHealer()).Select(cj=>cj.RowId)];
					changed = true;
				}
				ImGui.SameLine();
				if (GuiHelpers.GameButton(UldBundle.CharacterClass_DpsMelee, "SelectDpsMeleeJobs##JobCategoryPopup##JobCategory", "",new Vector2(ImGui.GetFontSize()*1.5f))) {
					ConfigurationManager.Config.FilterClassJobCategories = [.. PluginServices.DataManager.Excel.GetSheet<ClassJob>().Where(cj=> cj.IsTypeDpsMelee()).Select(cj=>cj.RowId)];
					changed = true;
				}
				ImGui.SameLine();
				if (GuiHelpers.GameButton(UldBundle.CharacterClass_DpsPhysicalRanged, "SelectDpsPhysicalRangedJobs##JobCategoryPopup##JobCategory", "",new Vector2(ImGui.GetFontSize()*1.5f))) {
					ConfigurationManager.Config.FilterClassJobCategories = [.. PluginServices.DataManager.Excel.GetSheet<ClassJob>().Where(cj=> cj.IsTypeDpsPhysicalRanged()).Select(cj=>cj.RowId)];
					changed = true;
				}
				ImGui.SameLine();
				if (GuiHelpers.GameButton(UldBundle.CharacterClass_DpsMagicalRanged, "SelectDpsMagicalRangedJobs##JobCategoryPopup##JobCategory", "",new Vector2(ImGui.GetFontSize()*1.5f))) {
					ConfigurationManager.Config.FilterClassJobCategories = [.. PluginServices.DataManager.Excel.GetSheet<ClassJob>().Where(cj=> cj.IsTypeDpsMagicalRanged()).Select(cj=>cj.RowId)];
					changed = true;
				}
				ImGui.SameLine();
				if (GuiHelpers.GameButton(UldBundle.CharacterClass_DisciplesOfTheHand, "SelectDisciplesOfTheHandJobs##JobCategoryPopup##JobCategory", "",new Vector2(ImGui.GetFontSize()*1.5f))) {
					ConfigurationManager.Config.FilterClassJobCategories = [.. PluginServices.DataManager.Excel.GetSheet<ClassJob>().Where(cj=> cj.IsTypeDisciplesOfTheHand()).Select(cj=>cj.RowId)];
					changed = true;
				}
				ImGui.SameLine();
				if (GuiHelpers.GameButton(UldBundle.CharacterClass_DisciplesOfTheLand, "SelectDisciplesOfTheLandJobs##JobCategoryPopup##JobCategory", "",new Vector2(ImGui.GetFontSize()*1.5f))) {
					ConfigurationManager.Config.FilterClassJobCategories = [.. PluginServices.DataManager.Excel.GetSheet<ClassJob>().Where(cj=> cj.IsTypeDisciplesOfTheLand()).Select(cj=>cj.RowId)];
					changed = true;
				}
				ImGui.NewLine();

				// render the icons
				ClassJobRow? previousRow = null;
				foreach (var row in classJobsSorted) {
					DrawClassJobNewLineOrSpacing(row, previousRow);
					previousRow = row;

					var id = row.RowId;

					var isSelected = selectedIds.Contains(id);

					if (DrawClassJob(row, true, ref isSelected)) {
						if (isSelected) ConfigurationManager.Config.FilterClassJobCategories?.Remove(id);
						else ConfigurationManager.Config.FilterClassJobCategories?.Add(id);
						changed = true;
					}
				}


				ImGui.EndPopup();
			}

			return changed;
		}

		/// <summary>
		/// Sort class jobs for filter display, to match the "Search Info" ordering
		/// </summary>
		/// <param name="classJobs"></param>
		/// <returns></returns>
		private static IOrderedEnumerable<ClassJobRow> SortClassJobs(ClassJobSheet classJobs) {
			return classJobs
				.Where(c => !c.Base.Abbreviation.ToString().IsNullOrWhitespace()) // filter out filling empty rows
				.OrderBy(c => c.Base.Role == 0 ? byte.MaxValue : c.Base.Role switch {  // sort them by role first, put doh/dol last
					4 => 2, // healer
					2 => 3, // melee dps
					3 => 4, // ranged dps and magic dps
					_ => c.Base.Role,
				})
				.ThenBy(c => { // for ranged dps, sort by job type (physical ranged, magic), put classes at the end
					if (c.Base.Role != 3) return byte.MinValue;
					if (c.Base.JobType == 0) return c.Base.Abbreviation.ToString() switch { // recreate the job type for classes
						"ARC" => 4, // BRD ranged physical
						"THM" => 5, // BLM ranged magical
						"ACN" => 5, // SMN ranged magical
						_ => byte.MaxValue,
					};
					return c.Base.JobType;
					})
				.ThenBy(c => {
					if(c.Base.JobIndex == 0) return int.MaxValue;
					if(c.Base.IsLimitedJob) return int.MaxValue - 1; // put limited jobs after regular jobs but before classes
					return c.Base.JobIndex;
				}) // put job first and in their right order, put the classes at the end
				.ThenBy(c => c.Base.BattleClassIndex) // order by class index
				.ThenBy(c => c.Base.BattleClassIndex == 0 ? int.MaxValue : c.Base.BattleClassIndex)
				.ThenBy(c => {
					if (c.Base.DohDolJobIndex == -1) return byte.MinValue;
					if (c.Base.JobType == 0 && c.Base.IsTypeDisciplesOfTheHand()) return byte.MinValue; // put crafting classes at the start
					if (c.Base.JobType == 0 && c.Base.IsTypeDisciplesOfTheLand()) return byte.MaxValue; // put gathering classes at the end
					return c.Base.JobType;
				}) // for ranged dps, sort by job type (physical ranged, magic), put classes at the end

				.ThenBy(c => c.Base.DohDolJobIndex)
				;
		}

		private static int _rowCounter = 0;
		/// <summary>
		/// Break line/space to match the "Search Info" display
		/// </summary>
		/// <param name="current"></param>
		/// <param name="previous"></param>
		private static void DrawClassJobNewLineOrSpacing(ClassJobRow? current, ClassJobRow? previous) {
			_rowCounter++;
			if (previous == null || current == null) {
				//ImGui.SameLine();
				return;
			}
			// new line for new role
			if (previous.Base.Role != current.Base.Role) {
				_rowCounter = 0;
				//ImGui.NewLine();
				return;
			}
			// new line for new job type (ranged physical / ranged magical)
			if ((current.Base.Role == 3 && current.Base.JobType != 0) &&
				previous.Base.JobType != current.Base.JobType) {
				_rowCounter = 0;
				return;
			}
			// spacing for new job (melee dps and healer only)
			if ((current.Base.JobIndex == 0 && previous.Base.JobIndex != 0)
				//&& (current.Base.BattleClassIndex != 0 && previous.Base.JobIndex != 0)
				) {

				var spacesToAdd = 7 - _rowCounter;
				ImGui.SameLine();
				var size = (ItemIcon.IconSize.X / 2) * spacesToAdd
					+ ImGui.GetStyle().ItemSpacing.X * (spacesToAdd - 1)
					;
				ImGui.Dummy(new Vector2(size, 0));
				ImGui.SameLine();
				return;
			}

			// new line for dol after doh jobs
			if(_rowCounter > 0 && current.Base.DohDolJobIndex == 0) {
				_rowCounter = 0;
				return;
			}

			// else same line
			ImGui.SameLine();
		}

		private static bool DrawClassJob(ClassJobRow classJob, bool isInteractive, ref bool isSelected) {
			var isClicked = false;

			//ImGui.TextUnformatted(classJob.Base.Abbreviation.ToString());
			//ImGui.SameLine();
			var size = ItemIcon.IconSize / 2;
			if (isInteractive) {
				var tooltip = $"{classJob.Base.Abbreviation}";
				if(ConfigurationManager.Config.IconTooltipShowDev) {
					tooltip += $"\nID: {classJob.RowId}" +
						$"\nJob Index: {classJob.Base.JobIndex}" +
						$"\nBattleClassIndex: {classJob.Base.BattleClassIndex}" +
						$"\nDohDolJobIndex: {classJob.Base.DohDolJobIndex}" +
						$"\nRole: {classJob.Base.Role}" +
						$"\nJobType: {classJob.Base.JobType}" +
						$"\nIsLimitedJob: {classJob.Base.IsLimitedJob}";
				}
				if (GuiHelpers.GameIconButtonToggle((uint)classJob.Icon, ref isSelected, $"ClassJobSelector##{classJob.RowId}", tooltip, size)) {
					isSelected = !isSelected;
					isClicked = true;
				}
			} else {
				var icon = PluginServices.TextureProvider.GetFromGameIcon(new GameIconLookup((uint)classJob.Icon));
				ImGui.Image(icon.GetWrapOrEmpty().Handle, size);
			}
			return isClicked;
		}
	}

	public enum JobFilterType : byte {
		All = 0,
		Job = 1,
		Type = 2,
		NoJob = 3,
	}
	public static class JobFilterTypeExtensions {
		public static string Tooltip(this JobFilterType status) {
			return status switch {
				JobFilterType.All  => $"All compatible\nShow any items including the selected job(s)",
				JobFilterType.Job  => $"Job Gear Only\nOnly show items of selected job(s)\nHandy to find job gear",
				JobFilterType.Type => $"Job Type\nOnly show items of job types the selected job is included in\nE.g. Selected PLD, the gear wearable by \"PLD WAR DRK GNB\" or \"Disciple of War\".",
				JobFilterType.NoJob => $"All but Not Job Gear\nShow only gear that can be equiped by multiple jobs, hides job gear\nHandy to make a role compatible glam (became useless with 7.4)",
				_ => "",
			};
		}
	}
}
