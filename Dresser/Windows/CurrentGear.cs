using CriticalCommonLib.Extensions;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

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

namespace Dresser.Windows;

public class CurrentGear : Window, IDisposable {
	private Plugin Plugin;

	public CurrentGear(Plugin plugin) : base(
		"Current Gear",
		ImGuiWindowFlags.AlwaysAutoResize
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoTitleBar
		) {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(10),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};
		this.Plugin = plugin;
	}

	public override void PreDraw()
		=> Styler.PushStyleCollection();
	public override void PostDraw()
		=> Styler.PopStyleCollection();
	public override void PreOpenCheck() {
		if (ConfigurationManager.Config.SelectCurrentGearsetOnOpenCurrentGearWindow) {
			var gearsetPlateNumber = GearSets.CurrentGearsetToPlateNumber();
			PluginLog.Error($"found gearset plate number {gearsetPlateNumber}");
			if (gearsetPlateNumber.HasValue) {
				ConfigurationManager.Config.SelectedCurrentPlate = gearsetPlateNumber.Value;
			}
		}
	}
	public override void OnOpen() {
		base.OnOpen();
		if (GearBrowser.SelectedSlot == null) PluginServices.ApplyGearChange.SelectCurrentSlot(ConfigurationManager.Config.CurrentGearSelectedSlot);
		PluginServices.ApplyGearChange.EnterBrowsingMode();
	}

	public override void OnClose() {
		base.OnClose();
		PluginServices.ApplyGearChange.ExitBrowsingMode();
		ConfigurationManager.SaveAsync();
	}
	public void Dispose() { }

	private static ushort? PlateSlotButtonHovering = null;
	public override void Draw() {

		var draw = TitleBar.Draw(this);

		DrawPlateSelector(draw);
		DrawSlots();


		DrawChildren();
	}

	private static Vector2 SizeGameCircleIcons => Vector2.One * 65 * ConfigurationManager.Config.IconSizeMult;
	private static void DrawBottomButtons() {
		if (PluginServices.Context.LocalPlayer == null) return;

		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

		//if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.ArrowCircleUp, "Apply plate appearance", default))
		//	PluginServices.ApplyGearChange.ApplyCurrentPendingPlateAppearance();
		//ImGui.SameLine();


		if (GuiHelpers.GameButton(UldBundle.CircleLargeQuestionMark, "OpenHelp##CurrentGear", $"Show helps and tricks", SizeGameCircleIcons)) {
			Help.Open();
		}
		ImGui.SameLine();
		if (GuiHelpers.GameButtonCircleToggle(UldBundle.CircleSmallWeapon, ref ConfigurationManager.Config.CurrentGearDisplayWeapon, "Display Sheathed Arms##CurrentGear", "Display Sheathed Arms", SizeGameCircleIcons)) {
			PluginServices.Context.LocalPlayer.RedrawWeapon();
		}
		ImGui.SameLine();
		if (GuiHelpers.GameButtonCircleToggle(UldBundle.CircleSmallHat, ref ConfigurationManager.Config.CurrentGearDisplayHat, "Display Headgear##CurrentGear", "Display Headgear", SizeGameCircleIcons)) {
			PluginServices.Context.LocalPlayer.RedrawHeadGear();
		}
		ImGui.SameLine();
		if (GuiHelpers.GameButtonCircleToggle(UldBundle.CircleSmallVisor, ref ConfigurationManager.Config.CurrentGearDisplayVisor, "Manually adjust visor##CurrentGear", "Manually adjust visor", SizeGameCircleIcons)) {
			PluginServices.Context.LocalPlayer.RedrawHeadGear();
		}
		ImGui.SameLine();
		if (GuiHelpers.GameButtonCircleToggle(UldBundle.CircleSmallDisplayGear, ref ConfigurationManager.Config.CurrentGearDisplayGear, "DisplayGear##CurrentGear", "Display Gear", SizeGameCircleIcons)) {
			PluginServices.ApplyGearChange.ToggleDisplayGear();
		}
		if (PluginServices.Context.IsAnyPlateSelectionOpen) {
			ImGui.SameLine();
			if (GuiHelpers.GameButton(UldBundle.CircleLargeRefresh2, "OverwritePendingWithCurrent##CurrentGear", $"Overwrite portable plate {ConfigurationManager.Config.SelectedCurrentPlate + 1} with the plate you are currently viewing in Glamour Dresser or Plate Selection skill", SizeGameCircleIcons)) {
				PluginServices.ApplyGearChange.OverwritePendingWithCurrentPlate();
			}
		}


		ImGui.PopStyleVar();
		DrawTasks();
	}

	private void DrawPlateSelector(ImDrawListPtr draw) {
		GearSets.FetchGearSets();

		ImGui.BeginGroup();

		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0));

		var radioActive = PluginServices.ImageGuiCrop.GetPart(UldBundle.MiragePlateRadioSelected);
		var radioInActive = PluginServices.ImageGuiCrop.GetPart(UldBundle.MiragePlateRadio);

		var radioOiriginalSize = (radioInActive?.Size ?? Vector2.One) * ConfigurationManager.Config.IconSizeMult;
		var radioSize = radioOiriginalSize * new Vector2(0.75f, 0.85f);
		//var radioSize = radioInActive.Item4 * new Vector2(0.75f, 0.85f);
		var fontSize = 28f * ConfigurationManager.Config.IconSizeMult;
		var textOffset = new Vector2(28f, -36f);
		if (ConfigurationManager.Config.CurrentGearPortablePlateJobIcons) textOffset += new Vector2(-10f, 0);
		var textPlacement = textOffset * ConfigurationManager.Config.IconSizeMult;
		var textPlacementFreePOffset = new Vector2(12.5f, 0f) * ConfigurationManager.Config.IconSizeMult;

		ushort maxPlates = (ushort)(Storage.PlateNumber + ConfigurationManager.Config.NumberOfFreePendingPlates);
		bool anythingHovered = false;
		for (ushort plateNumber = 0; plateNumber < maxPlates; plateNumber++) {
			var isFreePlate = plateNumber + 1 > Storage.PlateNumber;
			var isActive = ConfigurationManager.Config.SelectedCurrentPlate == plateNumber;
			var imageInfo = isActive ? radioActive : radioInActive;
			var plateNumberForHuman = isFreePlate ? (plateNumber + 1 - Storage.PlateNumber) : plateNumber + 1;

			Vector4? roleColor = null;
			Vector4? roleColor1 = null;
			if (ConfigurationManager.Config.CurrentGearPortablePlateJobBgColors || ConfigurationManager.Config.CurrentGearPortablePlateJobIcons) {
				roleColor1 = GearSets.RelatedGearSetClassJobCategoryColor(plateNumber);
				if (ConfigurationManager.Config.CurrentGearPortablePlateJobBgColors && roleColor1 != null) roleColor = roleColor1 + new Vector4(0.6f, 0.6f, 0.6f, -0.1f);
			}

			var tint = PlateSlotButtonHovering == plateNumber ? ConfigurationManager.Config.PlateSelectorHoverColor : roleColor??ConfigurationManager.Config.PlateSelectorRestColor;
			if (isActive) tint = ConfigurationManager.Config.PlateSelectorActiveColor;
			if (imageInfo != null) ImGui.Image(imageInfo.ImGuiHandle, radioSize, Vector2.Zero, Vector2.One, tint);
			var clicked = ImGui.IsItemClicked();
			var hovering = ImGui.IsItemHovered();
			if (ImGui.BeginPopupContextItem($"{plateNumber}##PlateSelector##ContextMenu##CurrentGear")) {
				ContextMenuPlateSelector(plateNumber);
				ImGui.EndPopup();
			}
			GuiHelpers.Tooltip(() => {
				var plateName = $"{(isFreePlate ? "Free " : "")}Plate {plateNumberForHuman}";
				GuiHelpers.TextWithFont(plateName, GuiHelpers.Font.BubblePlateNumber);
				if (!isFreePlate) GearSets.RelatedGearSetNamesImgui(plateNumber);
				ImGui.Spacing();
			});

			var classJobTexture = GearSets.GetClassJobIconTextureForPlate(plateNumber);
			if (ConfigurationManager.Config.CurrentGearPortablePlateJobIcons && classJobTexture != null) {
				var cjt_ratio = 0.8f;
				var cjt_p_min = ImGui.GetCursorScreenPos() + new Vector2(radioSize.X - (radioSize.Y * 0.8f), - radioSize.Y + (radioSize.Y * ((1 - cjt_ratio)/2)));
				var cjt_p_max = cjt_p_min + new Vector2(radioSize.Y * cjt_ratio);

				var jobIconColor1 = !ConfigurationManager.Config.CurrentGearPortablePlateJobBgColors ? roleColor1 ?? new Vector4(0, 0, 0, 1) : new Vector4(1, 1, 1, 1);
				var jobIconColor = jobIconColor1 + new Vector4(0.1f, 0.1f, 0.1f, -0.35f);

				draw.AddImage(classJobTexture.GetWrapOrEmpty().ImGuiHandle, cjt_p_min, cjt_p_max, new(0), new(1), ImGui.ColorConvertFloat4ToU32(jobIconColor));
			}

			var spacer = plateNumberForHuman < 10 ? " " : "";
			GuiHelpers.TextWithFontDrawlist(
				$"{spacer}{plateNumberForHuman}",
				GuiHelpers.Font.Radio,
				ConfigurationManager.Config.PlateSelectorColorRadio,
				fontSize,
				textPlacement + (isFreePlate ? textPlacementFreePOffset : Vector2.Zero));

			if (clicked) {
				// Change selected plate
				PluginServices.ApplyGearChange.UnApplyCurrentPendingPlateAppearance();
				ConfigurationManager.Config.SelectedCurrentPlate = plateNumber;
				PluginServices.ApplyGearChange.ApplyCurrentPendingPlateAppearance();
			}
			if (hovering) {
				// save hovered button for later
				anythingHovered |= true;
				PlateSlotButtonHovering = plateNumber;
			}
			if ((plateNumber + 1) % ConfigurationManager.Config.NumberofPendingPlateNextColumn == 0 && plateNumber + 1 != maxPlates) {
				ImGui.EndGroup();
				ImGui.SameLine();

				var cp = ImGui.GetCursorPos();
				ImGui.SetCursorPosX(cp.X - (radioSize.X * 0.2f));
				ImGui.SetCursorPosY(cp.Y + (radioSize.Y * 0.22f));
				ImGui.BeginGroup();
			}

		}
		if (!anythingHovered) PlateSlotButtonHovering = null;

		ImGui.PopStyleVar();
		ImGui.EndGroup();
		ImGui.SameLine();
	}

	private static GlamourPlateSlot? HoveredSlot = null;
	public static List<GlamourPlateSlot> SlotOrder = new() {
			GlamourPlateSlot.MainHand, GlamourPlateSlot.OffHand,
			GlamourPlateSlot.Head, GlamourPlateSlot.Ears,
			GlamourPlateSlot.Body, GlamourPlateSlot.Neck,
			GlamourPlateSlot.Hands, GlamourPlateSlot.Wrists,
			GlamourPlateSlot.Legs, GlamourPlateSlot.RightRing,
			GlamourPlateSlot.Feet, GlamourPlateSlot.LeftRing,
		};

	private void DrawSlots() {

		if (PluginServices.Storage.DisplayPage != null && ConfigurationManager.Config.DisplayPlateItems.Items.Count == 0) return;
		ImGui.BeginGroup();

		try {
			InventoryItemSet plateItems = new(); ;


			//if (PluginServices.Context.IsGlamingAtDresser && PluginServices.Storage.DisplayPage != null) {
			//	This would be the case where CurrentGear window reflects the changes on actuall dresser, it is only visual and not useful
			//	plateItems = ConfigurationManager.Config.DisplayPlateItems;
			//} else {
			CheckPendingPlateItems();
				if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plateItems2)) {
					plateItems = plateItems2;
				} else plateItems = Gathering.EmptyGlamourPlate();
			//}

			bool isTooltipActive = false;
			int i = 0;
			foreach (var slot in SlotOrder) {
				var item = plateItems.GetSlot(slot);

				bool isHovered = slot == HoveredSlot;
				bool wasHovered = isHovered;
				isHovered |= GearBrowser.SelectedSlot == slot;
				var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive, out bool clickedMiddle, slot, ContextMenuCurrent);
				if (isHovered)
					HoveredSlot = slot;
				else if (!isHovered && wasHovered)
					HoveredSlot = null;
				if (iconClicked) {
					PluginServices.ApplyGearChange.ExecuteCurrentItem(slot);
				}
				if (clickedMiddle && item != null)
					PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveItem(item, slot);

				if (i % 2 == 0)
					ImGui.SameLine();
				i++;
			}

		} catch (Exception ex) {
			PluginLog.Error(ex, "Error in DrawSlots");
		}
		ImGui.EndGroup();
	}

	private static void ContextMenuCurrent(InventoryItem item, GlamourPlateSlot? slot) {
		if (ImGui.Selectable("Remove Item Image from Plate"))
			PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveItem(item,slot);

		if (ImGui.Selectable("Dye")) {


			if (slot.HasValue) PluginServices.ApplyGearChange.SelectCurrentSlot(slot.Value);
			DyePicker.SetSelection(item);
			Plugin.GetInstance().DyePicker.IsOpen = true;
		}

		if (ImGui.Selectable("Remove Dye"))
			PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveDye(item);

	}
	private bool IsCurrentFreePlate = ConfigurationManager.Config.SelectedCurrentPlate + 1 > Storage.PlateNumber;
	private string FormattedNameCurrentPlate()
		=> FormattedPlateName(ConfigurationManager.Config.SelectedCurrentPlate);
	private static string FormattedPlateName(ushort plateNumber) {
		var isTargetFreePlate = plateNumber + 1 > Storage.PlateNumber;
		var prefix = isTargetFreePlate ? "Free p" : "P";
		var number = isTargetFreePlate ? plateNumber - Storage.PlateNumber : plateNumber;
		return $"{prefix}late #{number}";

	}
	private void ContextMenuPlateSelector(ushort plateNumber) {
		ImGui.TextDisabled($"{FormattedPlateName(plateNumber)}");
		ImGui.Spacing();
		if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Broom, $"Remove every items from this plate ({FormattedPlateName(plateNumber)})", default, $"##{plateNumber}##clear##PlateSelector##CurrentGear")) {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(plateNumber, out var set)) {
				set.EmptyAllItemsToNull();
				set.ApplyAppearance();
			}
		}
		ImGui.SameLine();
		if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.ArrowRightArrowLeft, $"Swap contents of current ({FormattedNameCurrentPlate()}) with contents of {FormattedPlateName(plateNumber)}", default, $"##{plateNumber}##swapWithCurrent##PlateSelector##CurrentGear")) {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(plateNumber, out var targetPlateInvItems) && ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlateInvItems)) {
				ConfigurationManager.Config.PendingPlateItemsCurrentChar[ConfigurationManager.Config.SelectedCurrentPlate] = targetPlateInvItems.Copy();
				ConfigurationManager.Config.PendingPlateItemsCurrentChar[plateNumber] = currentPlateInvItems.Copy();
				ConfigurationManager.Config.SelectedCurrentPlate = plateNumber;
			}
		}
	}

	private static void CheckPendingPlateItems() {
		if (ConfigurationManager.Config.PendingPlateItemsCurrentChar == null || !ConfigurationManager.Config.PendingPlateItemsCurrentChar.Any()) {
			ConfigurationManager.Config.PendingPlateItemsCurrentChar = new();
			for (ushort i = 0; i < Storage.PlateNumber; i++) {
				ConfigurationManager.Config.PendingPlateItemsCurrentChar[i] = new();
			}
		}
	}
	public static InventoryItem? SelectedInventoryItem() {
		if(GearBrowser.SelectedSlot != null && ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var set)) {
			return set.GetSlot((GlamourPlateSlot)GearBrowser.SelectedSlot);
		}
		return null;
	}

	public static void DrawTasks() {
		if (PluginServices.ApplyGearChange.TasksOnCurrentPlate.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var taskedItems)) {
			if (taskedItems.Any()) {
				ImGui.BeginGroup();
				var tint = ItemIcon.ColorBad * new Vector4(1.75f, 1.75f, 1.75f, 1);
				ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(-5* ConfigurationManager.Config.IconSizeMult, 0));

				GuiHelpers.GameButton(UldBundle.CircleLargeExclamationMark, "OverwritePendingWithCurrent##CurrentGear", "", SizeGameCircleIcons, tint);
				ImGui.SameLine();

				var tasksText = $"{taskedItems.Count} Task{(taskedItems.Count > 1 ? "s" : "")}";
				GuiHelpers.TextWithFontDrawlist(
					tasksText,
					GuiHelpers.Font.Task,
					ConfigurationManager.Config.PlateSelectorColorRadio,
					SizeGameCircleIcons.Y);

				ImGui.PopStyleVar();
				ImGui.EndGroup();
				GuiHelpers.Tooltip(DrawTasksTooltip);
			}
		}

	}
	private static void DrawTasksTooltip() {
		if (PluginServices.ApplyGearChange.TasksOnCurrentPlate.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var taskedItems)) {
			if(taskedItems.Any()) {
				ImGui.TextDisabled($"Some items are neither in {CriticalCommonLib.Models.InventoryCategory.GlamourChest.FormattedName()} or {CriticalCommonLib.Models.InventoryCategory.Armoire.FormattedName()}");
				ImGui.Spacing();
				if(ImGui.BeginTable("TaskTooltip##CurrentGear", 2)) {

					ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch, 100.0f, 0);
					ImGui.TableSetupColumn("Where", ImGuiTableColumnFlags.WidthStretch, 100.0f, 1);
					ImGui.TableHeadersRow();

					foreach (var taskedItem in taskedItems) {
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.AlignTextToFramePadding();
						ImGui.Text($"{taskedItem.FormattedName}");

						if(taskedItem.ItemSortCategory?.RowId == 11) { // sort category 11 seems to be dyes
							ImGui.SameLine();
							ImGui.Text($" ( {taskedItem.Quantity}");
							ImGui.SameLine();
							ImGui.TextColored(ItemIcon.ColorBad, $"- {taskedItem.QuantityNeeded}");
							ImGui.SameLine();
							ImGui.Text($")");
						}
						ImGui.TableNextColumn();
						ImGui.Text($"{taskedItem.FormattedInventoryCategoryType()}");
					}
					ImGui.EndTable();
				}
			}
		}
	}
	private static void DrawChildren() {
		Styler.PopStyleCollection();

		DrawBottomButtons();


		if (!Plugin.GetInstance().DyePicker.IsOpen && Plugin.GetInstance().DyePicker.MustDraw) Plugin.GetInstance().DyePicker.IsOpen = true;

		Styler.PushStyleCollection();
	}
}
