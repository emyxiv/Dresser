using CriticalCommonLib;
using CriticalCommonLib.Comparer;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;

using Dresser.Data;
using Dresser.Data.Excel;

using ImGuiNET;

using ImGuiScene;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static System.Net.Mime.MediaTypeNames;

namespace Dresser.Windows.Components {
	internal class Browse {
		public static float IconSizeMult = 1;
		public static Vector2 IconSize => new Vector2(120) * IconSizeMult;
		public static float DyeBorder => 3 * IconSizeMult;
		private static uint? HoveredItem = null;

		public static Vector4 ColorGood = new Vector4(124, 236, 56, 255) / 255;
		public static Vector4 ColorGoodLight = new Vector4(180, 244, 170, 255) / 255;
		public static Vector4 ColorBad = new Vector4(237, 107, 89, 255) / 255;
		public static Vector4 ColorGrey = new Vector4(199, 198, 197, 255) / 255;
		public static Vector4 ColorGreyDark = ColorGrey / 1.1f;
		public static Vector4 ColorBronze = new Vector4(240, 223, 191, 255) / 255;
		public static void Draw() {

			var localPlayer = Service.ClientState.LocalPlayer;
			if (localPlayer == null) return;
			var localPlayerRace = (CharacterRace)(localPlayer.Customize[(int)CustomizeIndex.Race]);
			var localPlayerGender = (localPlayer.Customize[(int)CustomizeIndex.Gender]) == 0 ? CharacterSex.Male : CharacterSex.Female;
			var localPlayerClass = localPlayer.ClassJob.GameData;
			var localPlayerLevel = localPlayer.Level;

			if (localPlayerClass == null) return;
			
			var items = PluginServices.InventoryMonitor.GetSpecificInventory(PluginServices.CharacterMonitor.ActiveCharacter, InventoryCategory.GlamourChest).Where(f=>!f.IsEmpty);


			ImGui.Text($"{items.Count()}");
			ImGui.SameLine();
			if (ImGui.Button("Clean##dresser##browse"))
				PluginServices.InventoryMonitor.Inventories.Clear();
			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 3);
			ImGui.DragFloat("##IconSize##slider", ref IconSizeMult,0.01f, 0.1f, 4f, "%.2f %");
			ImGui.SameLine();
			ImGui.Text("%");



			ImGui.BeginChildFrame(76, ImGui.GetContentRegionAvail());
			bool isTooltipActive = false;
			bool isIconHovered = false;

			foreach (var item in items) {

				// item variables
				var dye = Storage.Dyes!.FirstOrDefault(d => d.RowId == item.Stain);
				var image = PluginServices.IconStorage.Get(item);
				var isEquippableByCurrentClass = Service.ExcelCache.IsItemEquippableBy(item.Item.ClassJobCategory.Row, localPlayerClass.RowId);
				var isEquippableByGenderRace = item.Item.CanBeEquippedByRaceGender(localPlayerRace, localPlayerGender);
				var isDyeable = item.Item.IsDyeable;


				// icon
				ImGui.BeginGroup();
				var curPos1 = ImGui.GetCursorPos();
				var iconClicked = DrawIcon(image);
				if (ImGui.IsItemHovered()) {
					HoveredItem = item.ItemId;
					isIconHovered |= true;
				}

				DrawStain(dye, isDyeable) ;
				ImGui.EndGroup();

				GuiHelpers.Tooltip(() => {
					if (isTooltipActive) return;
					isTooltipActive = true;

					ImGui.BeginGroup();
					DrawIcon(image);
					DrawStain(dye, isDyeable);
					ImGui.EndGroup();

					// Side of the icon
					ImGui.SameLine();
					ImGui.BeginGroup();
					ImGui.TextColored(ColorGrey, $"{item.FormattedName}");
					ImGui.TextColored(ColorGreyDark, $"[{item.ItemId} - 0x{item.ItemId:X0}] ({item.FormattedType})");
					if (isDyeable) ImGui.TextColored(dye?.RowId != 0 ? ColorGoodLight : ColorGrey, $"{dye?.Name}");

					ImGui.EndGroup();

					// type of item (body, legs, etc) under the icon
					ImGui.TextColored(ColorGrey, item.FormattedUiCategory);

					// Equip Conditions
					ImGui.Separator();

					ImGui.TextColored(localPlayerLevel < item.Item.LevelEquip ? ColorBad : ColorGrey, $"lvl: {item.Item.LevelEquip}");
					ImGui.SameLine();
					ImGui.Text($"ilvl: {item.Item.LevelItem.Row}");

					ImGui.TextColored(isEquippableByCurrentClass ? ColorGood : ColorBad, $"{item.Item.ClassJobCategory.Value?.Name}");

					var genderRaceColor = isEquippableByGenderRace ? ColorBronze : ColorBad;
					if (item.Item.EquippableByGender != CharacterSex.Both || item.Item.EquipRace != CharacterRace.Any) {
						var fitGender = item.Item.EquippableByGender;
						string fitGenderRace = "Fits: ";
						fitGenderRace += item.Item.EquipRace.FormattedName();

						ImGui.TextColored(genderRaceColor, fitGenderRace);
						ImGui.SameLine();
						GuiHelpers.Icon(fitGender == CharacterSex.Male ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus, true, genderRaceColor);

					} else 
						ImGui.TextColored(genderRaceColor,"Fits: Everyone");

					// Acquisition
					ImGui.Separator();

					ImGui.Text($"Buy (vendor) for {item.BuyFromVendorPrice:n0} gil");
					// TODO: market price

					ImGui.Text($"Sell for {item.SellToVendorPrice:n0} gil");
				});

				if (iconClicked) {
					PluginLog.Debug($"clicked item {item.ItemId}");
				}


				ImGui.SameLine();
				if (ImGui.GetContentRegionAvail().X < IconSize.X)
					ImGui.NewLine();
			}
			if (!isIconHovered)
				HoveredItem = null;
			ImGui.EndChildFrame();
		}

		public static bool DrawIcon(TextureWrap image) {

			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);

			ImGui.Image(image.ImGuiHandle, IconSize);
			var clicked = ImGui.IsItemClicked();

			ImGui.PopStyleVar();
			return clicked;
		}
		public static void DrawStain(Dye? dye, bool isDyeable) {
			if (dye == null || !isDyeable) return;

			ImGui.SameLine();
			var color = dye.RowId == 0 ? new Vector4(0, 0, 0, 0) : dye.ColorVector4;

			var draw = ImGui.GetWindowDrawList();
			Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
			var radius = (ImGui.GetFontSize()) * 0.5f * IconSizeMult;
			var x = cursorScreenPos.X - radius - ImGui.GetStyle().ItemSpacing.X;
			var y = cursorScreenPos.Y + radius;
			var pos = new Vector2(x, y);

			draw.AddCircleFilled(pos, radius, ImGui.ColorConvertFloat4ToU32(color));
			draw.AddCircle(pos, radius, 0xff000000, 0, DyeBorder);
		}
	}
}
