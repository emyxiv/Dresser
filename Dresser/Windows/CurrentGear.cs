using CriticalCommonLib.Models;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;

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

	private static void DrawBottomButtons() {
		//if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.ArrowCircleUp, "Apply plate appearance", default))
		//	PluginServices.ApplyGearChange.ApplyCurrentPendingPlateAppearance();
		//ImGui.SameLine();

		if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.QuestionCircle, "OpenHelp##CurrentGear", $"Show helps and tricks")) {
			Help.Open();
		}
		ImGui.SameLine();
		if (GuiHelpers.IconButtonNoBg(ConfigurationManager.Config.CurrentGearDisplayGear ? FontAwesomeIcon.Church : FontAwesomeIcon.Peace, "DisplayGear##CurrentGear", "Display Gear")) {
			ConfigurationManager.Config.CurrentGearDisplayGear = !ConfigurationManager.Config.CurrentGearDisplayGear;
			PluginServices.ApplyGearChange.ToggleDisplayGear();
		}
		ImGui.SameLine();
		if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.FileImport, "OverwritePendingWithCurrent##CurrentGear", $"Overwrite portable plate {ConfigurationManager.Config.SelectedCurrentPlate + 1} with the plate you are currently viewing in Glamour Dresser or Plate Selection skill")) {
			PluginServices.ApplyGearChange.OverwritePendingWithCurrentPlate();
		}
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
			var isActive = ConfigurationManager.Config.SelectedCurrentPlate == plateNumber;
			var imageInfo = isActive ? radioActive : radioInActive;

			var tint = PlateSlotButtonHovering == plateNumber ? hoverColor : restColor;
			if (isActive) tint = ActiveColor;
			ImGui.Image(imageInfo.Item1, radioSize, imageInfo.Item2, imageInfo.Item3, tint);
			var clicked = ImGui.IsItemClicked();
			var hovering = ImGui.IsItemHovered();

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

			if (PluginServices.Context.IsGlamingAtDresser && PluginServices.Storage.DisplayPage != null) {
				plateItems = ConfigurationManager.Config.DisplayPlateItems;
			} else {
				CheckPendingPlateItems();
				if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plateItems2)) {
					plateItems = plateItems2;
				} else plateItems = Gathering.EmptyGlamourPlate();
			}
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
					if (SlotSelectDye != null) SlotSelectDye = slot;
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

	public static GlamourPlateSlot? SlotSelectDye = null;
	private static void ContextMenuCurrent(InventoryItem item, GlamourPlateSlot? slot) {
		if (ImGui.Selectable("Remove Item Image from Plate"))
			PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveItem(item);

		if (ImGui.Selectable("Dye")) {
			SlotSelectDye = slot;
			//PluginServices.ApplyGearChange.ExecuteCurrentContextDye(item);
		}

		if (ImGui.Selectable("Remove Dye"))
			PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveDye(item);

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
	private static void DrawChildren() {
		Styler.PopStyleCollection();

		DrawBottomButtons();

		if (SlotSelectDye != null) {
			try {
				DyePicker.DrawDyePicker((GlamourPlateSlot)SlotSelectDye);

			} catch (Exception ex) {
				PluginLog.Error(ex, "Error in DrawDyePicker");
			}
		}

		Styler.PushStyleCollection();
	}
}
