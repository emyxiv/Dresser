using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dalamud.Interface;
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
using System.Drawing;
using System.Linq;
using System.Numerics;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;


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
	public override void OnOpen() {
		base.OnOpen();
		PluginServices.ApplyGearChange.EnterBrowsingMode();
	}

	public override void OnClose() {
		base.OnClose();
		PluginServices.ApplyGearChange.ExitBrowsingMode();
	}
	public void Dispose() { }

	public static Vector4 CollectionColorTitle = (new Vector4(116, 123, 98, 255) / 255 * 0.3f) + new Vector4(0, 0, 0, 1);
	public static float opacityRadio = 0.70f;
	public static Vector4 CollectionColorRadio = ((new Vector4(116, 123, 98, 255) / 255 * 0.3f) * new Vector4(1, 1, 1, 0)) + new Vector4(0, 0, 0, opacityRadio);
	private static ushort? PlateSlotButtonHovering = null;
	public override void Draw() {

		var draw = TitleBar.Draw(this);

		DrawPlateSelector(draw);
		DrawSlots();

		DrawChildren();
	}

	private static Vector2 SizeGameCircleIcons => Vector2.One * 65 * ConfigurationManager.Config.IconSizeMult;
	private static void DrawBottomButtons() {
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

		//if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.ArrowCircleUp, "Apply plate appearance", default))
		//	PluginServices.ApplyGearChange.ApplyCurrentPendingPlateAppearance();
		//ImGui.SameLine();


		if (GuiHelpers.GameButton("circle_buttons_4", 4, "OpenHelp##CurrentGear", $"Show helps and tricks", SizeGameCircleIcons)) {
			Help.Open();
		}
		ImGui.SameLine();
		if (GuiHelpers.GameButtonCircleToggle(38, ref ConfigurationManager.Config.CurrentGearDisplayGear, "DisplayGear##CurrentGear", "Display Gear", SizeGameCircleIcons)) {
			PluginServices.ApplyGearChange.ToggleDisplayGear();
		}
		if (PluginServices.Context.IsAnyPlateSelectionOpen) {
			ImGui.SameLine();
			if (GuiHelpers.GameButton("circle_buttons_4", 27, "OverwritePendingWithCurrent##CurrentGear", $"Overwrite portable plate {ConfigurationManager.Config.SelectedCurrentPlate + 1} with the plate you are currently viewing in Glamour Dresser or Plate Selection skill", SizeGameCircleIcons)) {
				PluginServices.ApplyGearChange.OverwritePendingWithCurrentPlate();
			}
		}


		ImGui.SameLine();
		ImGui.PopStyleVar();
		DrawTasks();
	}

	private void DrawPlateSelector(ImDrawListPtr draw) {
		ImGui.BeginGroup();

		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0));

		var radioActive = PluginServices.ImageGuiCrop.GetPart("mirage_prism_plate2", 6);
		var radioInActive = PluginServices.ImageGuiCrop.GetPart("mirage_prism_plate2", 5);

		var radioOiriginalSize = radioInActive.Item4 * ConfigurationManager.Config.IconSizeMult;
		var radioSize = radioOiriginalSize * new Vector2(0.75f, 0.85f);
		//var radioSize = radioInActive.Item4 * new Vector2(0.75f, 0.85f);
		var fontSize = 28f * ConfigurationManager.Config.IconSizeMult;
		var textPlacement = new Vector2(28f, -36f) * ConfigurationManager.Config.IconSizeMult;

		Vector4 restColor = new(1, 1, 1, opacityRadio);
		Vector4 hoverColor = new(1, 1, 1, 1);
		Vector4 ActiveColor = new(1, 0.95f, 0.8f, 1);

		ushort maxPlates = (ushort)(Storage.PlateNumber + ConfigurationManager.Config.NumberOfFreePendingPlates);
		bool anythingHovered = false;
		for (ushort plateNumber = 0; plateNumber < maxPlates; plateNumber++) {
			var isFreePlate = plateNumber + 1 > Storage.PlateNumber;
			var isActive = ConfigurationManager.Config.SelectedCurrentPlate == plateNumber;
			var imageInfo = isActive ? radioActive : radioInActive;

			var tint = PlateSlotButtonHovering == plateNumber ? hoverColor : restColor;
			if (isActive) tint = ActiveColor;
			ImGui.Image(imageInfo.Item1, radioSize, imageInfo.Item2, imageInfo.Item3, tint);
			var clicked = ImGui.IsItemClicked();
			var hovering = ImGui.IsItemHovered();
			if (ImGui.BeginPopupContextItem($"{plateNumber}##PlateSelector##ContextMenu##CurrentGear")) {
				ContextMenuPlateSelector(plateNumber);
				ImGui.EndPopup();
			}
			if(!isFreePlate) GuiHelpers.Tooltip(() => {
				var plateName = isFreePlate ? $"Free Plate {plateNumber + 1 - Storage.PlateNumber}" : $"Plate {plateNumber + 1}";
				GuiHelpers.TextWithFont(plateName, GuiHelpers.Font.TrumpGothic_184);
				GearSets.FetchGearSets();
				GearSets.RelatedGearSetNamesImgui(plateNumber);
				ImGui.Spacing();
			});

			if (plateNumber < Storage.PlateNumber) {
				var spacer = plateNumber < 9 ? " " : "";
				draw.AddText(
					PluginServices.Storage.FontRadio.ImFont,
					fontSize,
					ImGui.GetCursorScreenPos() + textPlacement,
					ImGui.ColorConvertFloat4ToU32(CollectionColorRadio),
					$"{spacer}{plateNumber + 1}");
			}

			if (clicked) {
				// Change selected plate
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
				if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plateItems2)) {
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
				var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive, slot, ContextMenuCurrent);
				if (isHovered)
					HoveredSlot = slot;
				else if (!isHovered && wasHovered)
					HoveredSlot = null;
				if (iconClicked) {
					PluginServices.ApplyGearChange.ExecuteCurrentItem(slot);
				}

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
			PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveItem(item);

		if (ImGui.Selectable("Dye")) {


			if (slot.HasValue) PluginServices.ApplyGearChange.SelectCurrentSlot(slot.Value);
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
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(plateNumber, out var set))
				set.EmptyAllItemsToNull();
		}
		ImGui.SameLine();
		if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.ArrowRightArrowLeft, $"Swap contents of current ({FormattedNameCurrentPlate()}) with contents of {FormattedPlateName(plateNumber)}", default, $"##{plateNumber}##swapWithCurrent##PlateSelector##CurrentGear")) {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(plateNumber, out var targetPlateInvItems) && ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlateInvItems)) {
				ConfigurationManager.Config.PendingPlateItems[ConfigurationManager.Config.SelectedCurrentPlate] = targetPlateInvItems.Copy();
				ConfigurationManager.Config.PendingPlateItems[plateNumber] = currentPlateInvItems.Copy();
				ConfigurationManager.Config.SelectedCurrentPlate = plateNumber;
			}
		}
	}

	private static void CheckPendingPlateItems() {
		if (ConfigurationManager.Config.PendingPlateItems == null || !ConfigurationManager.Config.PendingPlateItems.Any()) {
			ConfigurationManager.Config.PendingPlateItems = new();
			for (ushort i = 0; i < Storage.PlateNumber; i++) {
				ConfigurationManager.Config.PendingPlateItems[i] = new();
			}
		}
	}
	public static InventoryItem? SelectedInventoryItem() {
		if(GearBrowser.SelectedSlot != null && ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var set)) {
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

				GuiHelpers.GameButton("circle_buttons_4", 29, "OverwritePendingWithCurrent##CurrentGear", "", SizeGameCircleIcons, tint);
				ImGui.SameLine();

				var tasksText = $"{taskedItems.Count} Tasks";
				GuiHelpers.TextWithFontDrawlist(tasksText, GuiHelpers.Font.Title, ItemIcon.ColorBad, SizeGameCircleIcons.Y);

				ImGui.PopStyleVar();
				ImGui.EndGroup();
				GuiHelpers.Tooltip(DrawTasksTooltip);
			}
		}

	}
	private static void DrawTasksTooltip() {
		if (PluginServices.ApplyGearChange.TasksOnCurrentPlate.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var taskedItems)) {
			if(taskedItems.Any()) {
				ImGui.TextDisabled($"Some items are neither in {InventoryCategory.GlamourChest.FormattedName()} or {InventoryCategory.Armoire.FormattedName()}");
				ImGui.Spacing();
				if(ImGui.BeginTable("TaskTooltip##CurrentGear", 2)) {

					ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch, 100.0f, 0);
					ImGui.TableSetupColumn("Where", ImGuiTableColumnFlags.WidthStretch, 100.0f, 1);
					ImGui.TableHeadersRow();

					foreach (var taskedItem in taskedItems) {
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text($"{taskedItem.FormattedName}");
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
