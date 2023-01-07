using System;
using System.Linq;
using System.Numerics;

using ImGuiNET;

using Dalamud.Interface.Windowing;
using Dalamud.Logging;

using CriticalCommonLib.Models;

using Dresser.Windows.Components;
using Dresser.Interop.Hooks;
using Dresser.Data;

namespace Dresser.Windows {
	public class GearBrowser : Window, IDisposable {

		public GearBrowser() : base(
			"Gear Browser", ImGuiWindowFlags.None) {
			this.SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};

		}
		public void Dispose() { }


		private static uint? HoveredItem = null;

		public override void Draw() {
			var items = PluginServices.InventoryMonitor.GetSpecificInventory(PluginServices.CharacterMonitor.ActiveCharacter, InventoryCategory.GlamourChest).Where(f => !f.IsEmpty);

			// top "bar" with controls
			ImGui.Text($"{items.Count()}");
			ImGui.SameLine();
			if (ImGui.Button("Clean##dresser##browse"))
				PluginServices.InventoryMonitor.Inventories.Clear();
			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 3);
			ImGui.DragFloat("##IconSize##slider", ref ItemIcon.IconSizeMult, 0.01f, 0.1f, 4f, "%.2f %");
			ImGui.SameLine();
			ImGui.Text("%");



			ImGui.BeginChildFrame(76, ImGui.GetContentRegionAvail());
			bool isTooltipActive = false;

			foreach (var item in items) {

				// icon
				bool isHovered = item.ItemId == HoveredItem;
				bool wasHovered = isHovered;
				var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive);
				if (isHovered)
					HoveredItem = item.ItemId;
				else if (!isHovered && wasHovered)
					HoveredItem = null;

				if (iconClicked) {
					if (GlamourPlates.IsGlaming()) {
						PluginLog.Verbose($"Execute apply item {item.Item.NameString} {item.Item.RowId}");
						PluginServices.GlamourPlates.ModifyGlamourPlateSlot(item);
						Gathering.ParseGlamourPlates();
						// TODO: preview glam on player
					}
				}


				ImGui.SameLine();
				if (ImGui.GetContentRegionAvail().X < ItemIcon.IconSize.X)
					ImGui.NewLine();
			}

			ImGui.EndChildFrame();
		}
	}
}