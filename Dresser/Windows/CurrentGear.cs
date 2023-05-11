using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using CriticalCommonLib.Models;

using Dresser.Data;
using Dresser.Logic;
using Dresser.Structs.FFXIV;
using Dresser.Windows.Components;
using Dresser.Extensions;
using CriticalCommonLib;
using Dresser.Interop.Hooks;

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
		=> GearBrowser.PushStyleCollection();
	public override void PostDraw()
		=> GearBrowser.PopStyleCollection();
	public void Dispose() { }

	public static Vector4 CollectionColorTitle = (new Vector4(116, 123, 98, 255) / 255 * 0.3f) + new Vector4(0, 0, 0, 1);
	public static float opacityRadio = 0.70f;
	public static Vector4 CollectionColorRadio = ((new Vector4(116, 123, 98, 255) / 255 * 0.3f) * new Vector4(1, 1, 1, 0) ) + new Vector4(0, 0, 0, opacityRadio);
	private static ushort? PlateSlotButtonHovering = null;
	public override void Draw() {
		//if (Storage.DisplayPage == null) return;
		var draw = ImGui.GetWindowDrawList();

		draw.AddText(
			PluginServices.Storage.FontTitle.ImFont,
			80 * Plugin.PluginConfiguration.IconSizeMult,
			ImGui.GetCursorScreenPos() + new Vector2(0, -10),
			ImGui.ColorConvertFloat4ToU32(CollectionColorTitle),
			"Plate Creation");
		ImGui.NewLine();
		ImGui.Spacing();

		var avail = ImGui.GetContentRegionAvail();
		var start = ImGui.GetCursorScreenPos();
		var end = new Vector2(start.X + avail.X, start.Y);
		draw.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(CollectionColorTitle), 5);
		ImGui.Spacing();


		ImGui.BeginGroup();
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0));

		var radioActive = ImageGuiCrop.GetPart("mirage_prism_plate2", 6);
		var radioInActive = ImageGuiCrop.GetPart("mirage_prism_plate2", 5);

		var radioSize = radioInActive.Item4 * new Vector2(0.75f, 0.85f);
		var fontSize = 28f;
		var textPlacement = new Vector2(28f, -36f);

		Vector4 restColor = new(1, 1, 1, opacityRadio);
		Vector4 hoverColor = new(1, 1, 1, 1);

		bool anythingHovered = false;
		for (ushort plateNumber = 0; plateNumber < Storage.PlateNumber; plateNumber++) {
			var imageInfo = ConfigurationManager.Config.SelectedCurrentPlate == plateNumber ? radioActive : radioInActive;

			var tint = PlateSlotButtonHovering == plateNumber ? hoverColor : restColor;
			ImGui.Image(imageInfo.Item1, radioSize, imageInfo.Item2, imageInfo.Item3, tint);
			var clicked = ImGui.IsItemClicked();
			var hovering = ImGui.IsItemHovered();

			var spacer = plateNumber < 9 ? " " : "";
			draw.AddText(
				PluginServices.Storage.FontRadio.ImFont,
				fontSize,
				ImGui.GetCursorScreenPos() + textPlacement,
				ImGui.ColorConvertFloat4ToU32(CollectionColorRadio),
				$"{spacer}{plateNumber+1}");

			if (clicked) {
				// Change selected plate
				ConfigurationManager.Config.SelectedCurrentPlate = plateNumber;
			}
			if (hovering) {
				// save hovered button for later
				anythingHovered |= true;
				PlateSlotButtonHovering = plateNumber;
			}
		}
		if (!anythingHovered) PlateSlotButtonHovering = null;

		ImGui.PopStyleVar();


		ImGui.EndGroup();
		ImGui.SameLine(radioInActive.Item4.X);
		ImGui.BeginGroup();
		DrawSlots();
		ImGui.EndGroup();

		DrawChildren();
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

	public void DrawSlots() {
		if (Storage.DisplayPage != null && Plugin.PluginConfiguration.DisplayPlateItems.Count == 0) return;
		var isGlamingAtDresser = GlamourPlates.IsGlamingAtDresser();

		try {
			Dictionary<GlamourPlateSlot, InventoryItem> plateItems = new(); ;

			if (isGlamingAtDresser && Storage.DisplayPage != null) {
				plateItems = Plugin.PluginConfiguration.DisplayPlateItems;
			} else {
				CheckPendingPlateItems();
				ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plateItems2);
				if (plateItems2 != null) plateItems = plateItems2;
				else plateItems = Gathering.EmptyGlamourPlate();
			}
			bool isTooltipActive = false;
			int i = 0;
			foreach (var slot in SlotOrder) {
				plateItems.TryGetValue(slot, out var item);
				item ??= Gathering.EmptyItemSlot();

				bool isHovered = slot == HoveredSlot;
				bool wasHovered = isHovered;
				isHovered |= GearBrowser.SelectedSlot == slot;
				var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive, slot, ContextMenuCurrent);
				if (isHovered)
					HoveredSlot = slot;
				else if (!isHovered && wasHovered)
					HoveredSlot = null;
				if (iconClicked) {
					GearBrowser.SelectedSlot = slot;
					this.Plugin.OpenGearBrowserIfClosed();
				}

				if (i % 2 == 0)
					ImGui.SameLine();
				i++;
			}

		} catch(Exception ex) {
			PluginLog.Error(ex, ex.ToString());
		}

	}
	private static void ContextMenuCurrent(InventoryItem item) {
		if (ImGui.Selectable("Remove Item Image from Plate")) {
			item.Clear();
			Service.ClientState.LocalPlayer?.Equip(item);
		}
		if (ImGui.Selectable("Dye"))
			PluginLog.Debug("TODO: open dye picker");
		if (ImGui.Selectable("Remove Dye"))
			item.Stain = 0;

	}

	private static void CheckPendingPlateItems() {
		if(ConfigurationManager.Config.PendingPlateItems == null || !ConfigurationManager.Config.PendingPlateItems.Any()) {
			ConfigurationManager.Config.PendingPlateItems = new();
			for (ushort i = 0; i < Storage.PlateNumber; i++) {
				ConfigurationManager.Config.PendingPlateItems[i] = new();
			}
		}
	}
	private static void DrawChildren() {
		ItemIcon.DrawContextMenu();
	}
}
