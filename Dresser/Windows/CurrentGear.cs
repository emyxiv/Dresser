using System;
using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;
using Dalamud.Interface.Windowing;

using Dresser.Data;
using Dresser.Structs.FFXIV;
using Dresser.Windows.Components;
using Dalamud.Logging;

namespace Dresser.Windows;

public class CurrentGear : Window, IDisposable {

	public CurrentGear() : base(
		"Current Gear",
		ImGuiWindowFlags.AlwaysAutoResize
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoTitleBar
		) {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(10),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};
	}

	public override void PreDraw()
		=> GearBrowser.PushStyleCollection();
	public override void PostDraw()
		=> GearBrowser.PopStyleCollection();
	public void Dispose() { }

	public static Vector4 CollectionColorTitle = (new Vector4(116, 123, 98, 255) / 255 * 0.3f) + new Vector4(0, 0, 0, 1);

	public override void Draw() {
		if (Storage.DisplayPage == null) return;
		var draw = ImGui.GetWindowDrawList();

		draw.AddText(
			PluginServices.Storage.FontTitle.ImFont,
			80 * ItemIcon.IconSizeMult,
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

		DrawSlots();
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

	public static void DrawSlots() {
		try {
			bool isTooltipActive = false;
			int i = 0;
			foreach (var slot in SlotOrder) {
				Configuration.SlotInventoryItems.TryGetValue(slot, out var item);
				bool isHovered = slot == HoveredSlot;
				bool wasHovered = isHovered;
				var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive, slot);
				if (isHovered)
					HoveredSlot = slot;
				else if (!isHovered && wasHovered)
					HoveredSlot = null;

				if (i % 2 == 0)
					ImGui.SameLine();
				i++;
			}

		} catch(Exception ex) {
			PluginLog.Error(ex.ToString());
		}

	}
}
