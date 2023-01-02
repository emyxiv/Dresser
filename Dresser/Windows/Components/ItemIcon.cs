using System.Linq;
using System.Numerics;

using ImGuiNET;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Lumina.Excel.GeneratedSheets;

using CriticalCommonLib;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dresser.Data;
using Dresser.Data.Excel;
using ImGuiScene;

namespace Dresser.Windows.Components {
	internal class ItemIcon {
		public static float IconSizeMult = 1;
		public static Vector2 IconSize => new Vector2(120) * IconSizeMult;
		public static float DyeBorder => 3 * IconSizeMult;

		public static Vector4 ColorGood = new Vector4(124, 236, 56, 255) / 255;
		public static Vector4 ColorGoodLight = new Vector4(180, 244, 170, 255) / 255;
		public static Vector4 ColorBad = new Vector4(237, 107, 89, 255) / 255;
		public static Vector4 ColorGrey = new Vector4(199, 198, 197, 255) / 255;
		public static Vector4 ColorGreyDark = ColorGrey / 1.1f;
		public static Vector4 ColorBronze = new Vector4(240, 223, 191, 255) / 255;

		public static PlayerCharacter? LocalPlayer = null;
		public static CharacterRace? LocalPlayerRace = null;
		public static CharacterSex? LocalPlayerGender = null;
		public static ClassJob? LocalPlayerClass = null;
		public static byte LocalPlayerLevel = 0;

		public static void Init() {
			LocalPlayer = Service.ClientState.LocalPlayer;
			if (LocalPlayer == null) return;
			LocalPlayerRace = (CharacterRace)(LocalPlayer.Customize[(int)CustomizeIndex.Race]);
			LocalPlayerGender = (LocalPlayer.Customize[(int)CustomizeIndex.Gender]) == 0 ? CharacterSex.Male : CharacterSex.Female;
			LocalPlayerClass = LocalPlayer.ClassJob.GameData;
			LocalPlayerLevel = LocalPlayer.Level;
		}
		public static void Dispose() {
			LocalPlayer = null;
			LocalPlayerRace = null;
			LocalPlayerGender = null;
			LocalPlayerClass = null;
			LocalPlayerLevel = 0;
		}

		//public static bool DrawIcon(TextureWrap image, Dye? dye, InventoryItem item, bool isDyeable)
		//	=> DrawIcon(image, dye, isDyeable, item, out bool _);

		public static void DrawIcon(InventoryItem item) {
			bool _ = false;
			bool __ = false;
			DrawIcon(item, ref _, ref __);
		}
		public static bool DrawIcon(InventoryItem item, ref bool isHovered, ref bool isTooltipActive) {

			if (LocalPlayer == null) Init();
			if (LocalPlayer == null
				|| LocalPlayerRace == null
				|| LocalPlayerGender == null
				|| LocalPlayerClass == null
				) return false;


			// item variables
			var dye = Storage.Dyes!.FirstOrDefault(d => d.RowId == item.Stain);
			var image = PluginServices.IconStorage.Get(item);
			var isEquippableByCurrentClass = Service.ExcelCache.IsItemEquippableBy(item.Item.ClassJobCategory.Row, LocalPlayerClass.RowId);
			var isEquippableByGenderRace = item.Item.CanBeEquippedByRaceGender((CharacterRace)LocalPlayerRace, (CharacterSex)LocalPlayerGender);
			var isDyeable = item.Item.IsDyeable;


			var clicked = DrawImage(image, dye, isDyeable, ref isHovered);

			var isTooltipActive2 = isTooltipActive;
			GuiHelpers.Tooltip(() => {
				if (isTooltipActive2) return;
				isTooltipActive2 = true;

				DrawImage(image, dye, isDyeable);

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

				ImGui.TextColored(LocalPlayerLevel < item.Item.LevelEquip ? ColorBad : ColorGrey, $"lvl: {item.Item.LevelEquip}");
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
					ImGui.TextColored(genderRaceColor, "Fits: Everyone");

				// Acquisition
				ImGui.Separator();

				ImGui.Text($"Buy (vendor) for {item.BuyFromVendorPrice:n0} gil");
				// TODO: market price

				ImGui.Text($"Sell for {item.SellToVendorPrice:n0} gil");
			});
			isTooltipActive = isTooltipActive2;



			return clicked;
		}
		private static bool DrawImage(TextureWrap image, Dye? dye, bool isDyeable) {
			bool _ = false;
			return DrawImage( image,dye, isDyeable, ref _);
		}
		private static bool DrawImage(TextureWrap image, Dye? dye, bool isDyeable, ref bool hovering) {
			ImGui.BeginGroup();

			bool wasHovered = hovering;
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);

			ImGui.Image(image.ImGuiHandle, IconSize);
			var clicked = ImGui.IsItemClicked();
			hovering = ImGui.IsItemHovered();

			// TODO: find a way to get a part of textures for:

			// Empty item
			// ring: 28 // bracer: 27 // necklace: 26 // earring: 25 // feet: 24 // legs: 23 // hands: 21 // body: 20 // head: 19 // head: 19 // main weapon: 17 // off weapon: 18
			//if(DataStorage.EmptyEquipTexture != null)
			//	ImGui.Image(DataStorage.EmptyEquipTexture.ImGuiHandle,new Vector2(DataStorage.EmptyEquipTexture.Width, DataStorage.EmptyEquipTexture.Height) * IconSize);

			// Embed-like slot visual

			// Hover visual
			// (wasHovered)

			ImGui.PopStyleVar();

			DrawStain(dye, isDyeable);
			ImGui.EndGroup();
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
