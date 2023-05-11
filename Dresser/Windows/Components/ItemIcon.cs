using System;
using System.Linq;
using System.Numerics;

using ImGuiNET;
using ImGuiScene;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Lumina.Excel.GeneratedSheets;

using CriticalCommonLib;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dresser.Data;
using Dresser.Data.Excel;
using Dresser.Extensions;
using Dresser.Structs.FFXIV;
using Dresser.Logic;
using Dalamud.Logging;
using Dalamud.Interface.Components;

namespace Dresser.Windows.Components {
	internal class ItemIcon {
		public static Vector2 IconSize => new Vector2(120) * Plugin.PluginConfiguration.IconSizeMult;
		public static Vector2 TooltipFramePadding => new Vector2(ImGui.GetFontSize() * 0.2f);
		public static Vector2 TooltipItemSpacing => TooltipFramePadding;
		public static float DyeBorder => 3 * Plugin.PluginConfiguration.IconSizeMult;

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
		public static InventoryItem? ContexMenuItem = null;
		public static Action<InventoryItem>? ContexMenuAction = null;
		public static bool IsHidingTooltip => PluginServices.KeyState[VirtualKey.MENU] || PluginServices.KeyState[VirtualKey.LMENU] || PluginServices.KeyState[VirtualKey.RMENU];

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

		public static void DrawIcon(InventoryItem? item) {
			bool _ = false;
			bool __ = false;
			DrawIcon(item, ref _, ref __);
		}
		public static bool DrawIcon(InventoryItem? item, ref bool isHovered, ref bool isTooltipActive, GlamourPlateSlot? emptySlot = null, System.Action<InventoryItem>? contextAction = null) {

			if (LocalPlayer == null) Init();
			if (LocalPlayer == null
				|| LocalPlayerRace == null
				|| LocalPlayerGender == null
				|| LocalPlayerClass == null
				) return false;


			// item variables
			var dye = Storage.Dyes!.FirstOrDefault(d => d.RowId == item?.Stain);
			var image = ConfigurationManager.Config.ShowImagesInBrowser ? PluginServices.IconStorage.Get(item) : null;
			if (image == null && emptySlot == null) emptySlot = item?.Item.GlamourPlateSlot();
			var isEquippableByCurrentClass = Service.ExcelCache.IsItemEquippableBy(item!.Item.ClassJobCategory.Row, LocalPlayerClass.RowId);
			var isEquippableByGenderRace = item.Item.CanBeEquippedByRaceGender((CharacterRace)LocalPlayerRace, (CharacterSex)LocalPlayerGender);
			var isDyeable = item.Item.IsDyeable;
			var isApplicable = item.IsGlamourPlateApplicable();
			var iconImageFlag = isApplicable ? IconImageFlag.None : IconImageFlag.NotAppliable;

			if (item.ItemId == 0)
				image = null;
			var clicked = DrawImage(image, dye, isDyeable, ref isHovered, iconImageFlag, out bool rightClicked, emptySlot);
			if (contextAction != null && rightClicked) {
				PluginLog.Debug("right clicked");
				ContexMenuAction = contextAction;
				ContexMenuItem = item;
				ImGui.OpenPopup("ContextMenuItemDresser");
			}
			var isTooltipActive2 = isTooltipActive;

			if (item != null && item.ItemId != 0 && !IsHidingTooltip)
			GuiHelpers.Tooltip(() => {
				if (isTooltipActive2) return;

				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, TooltipFramePadding);
				ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, TooltipItemSpacing);

				isTooltipActive2 = true;

				if(image == null && !ConfigurationManager.Config.ShowImagesInBrowser) image = PluginServices.IconStorage.Get(item);
				DrawImage(image!, dye, isDyeable);

				var rarityColor = Storage.RarityColor(item.Item);

				// Side of the icon
				ImGui.SameLine();
				ImGui.BeginGroup();
				ImGui.TextColored(rarityColor, $"{item.FormattedName}");
				ImGui.TextColored(ColorGreyDark, $"[{item.ItemId} - 0x{item.ItemId:X0}] ({item.FormattedType}) [");
				ImGui.SameLine();
				ImGui.TextColored(rarityColor, $"{item.Item.Rarity}");
				ImGui.SameLine();
				ImGui.TextColored(ColorGreyDark, $"]");
				if (isDyeable) ImGui.TextColored(dye?.RowId != 0 ? ColorGoodLight : ColorGrey, $"{dye?.Name}");

				ImGui.EndGroup();

				// type of item (body, legs, etc) under the icon
				ImGui.TextColored(ColorGrey, item.FormattedUiCategory);
				//PluginLog.Debug($"ui category: {item.ItemUICategory} {item.EquipSlotCategory!.RowId}");
				ImGui.TextColored(isApplicable ? ColorBronze : ColorBad, item.Container.ToString());

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

				// Other info
				var sameModelItems = Service.ExcelCache.AllItems.Where(i =>
					(i.Value.ModelMain == item.Item.ModelMain && i.Value.ModelMain != 0
					//|| (i.Value.ModelSub == item.Item.ModelSub && i.Value.ModelSub != 0)
					//|| (i.Value.ModelMain == item.Item.ModelSub && i.Value.ModelSub != 0)
					//|| (i.Value.ModelSub == item.Item.ModelMain && i.Value.ModelSub != 0)
					) && i.Value.RowId != item.Item.RowId
					&& i.Value.EquipSlotCategory.Value == item.Item.EquipSlotCategory.Value
				).Select(i => i.Value);

				if (sameModelItems != null && sameModelItems.Any()) {
					ImGui.Text($"Same model [{item.Item.ModelMain} - {item.Item.ModelSub}]:");

					foreach (var sameModelItem in sameModelItems.OrderBy(i => i.LevelEquip))
						ImGui.TextColored(Storage.RarityColor(sameModelItem), sameModelItem.NameString);
				}
				ImGui.Text($"Buy (vendor) for {item.BuyFromVendorPrice:n0} gil");
				// TODO: market price

				ImGui.Text($"Sell for {item.SellToVendorPrice:n0} gil");

				ImGui.PopStyleVar(2);
			});
			isTooltipActive = isTooltipActive2;



			return clicked;
		}
		private static bool DrawImage(TextureWrap image, Dye? dye, bool isDyeable, IconImageFlag iconImageFlag = 0) {
			bool _ = false;
			return DrawImage( image,dye, isDyeable, ref _, iconImageFlag, out var _);
		}
		private static bool DrawImage(TextureWrap? image, Dye? dye, bool isDyeable, ref bool hovering, IconImageFlag iconImageFlag, out bool rightClicked, GlamourPlateSlot? emptySlot = null) {
			ImGui.BeginGroup();

			bool wasHovered = hovering;
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);

			var initialPosition = ImGui.GetCursorPos();
			var draw = ImGui.GetWindowDrawList();
			var capSize = IconSize * new Vector2(1.17f, 1.16f);
			var difference = capSize - IconSize;
			initialPosition += (new Vector2(0, 3f) * Plugin.PluginConfiguration.IconSizeMult);


			if (image != null) {
				var colorize = !IsHidingTooltip && iconImageFlag.HasFlag(IconImageFlag.NotAppliable) ? GearBrowser.CollectionColorBackground + new Vector4(0, 0, 0, 0.3f) : Vector4.One;
				ImGui.Image(image.ImGuiHandle, IconSize, Vector2.Zero, Vector2.One, colorize);
			} else if(emptySlot != null) {
				var emptySlotInfo = ImageGuiCrop.GetPart((GlamourPlateSlot)emptySlot);

				// TODO: smaller icons in their slot
				// draw.AddImage(emptySlotInfo.Item1, slotPos, slotPos + slotSize, emptySlotInfo.Item2, emptySlotInfo.Item3);
				ImGui.Image(emptySlotInfo.Item1, IconSize, emptySlotInfo.Item2, emptySlotInfo.Item3);
			}

			var clicked = ImGui.IsItemClicked();
			hovering = ImGui.IsItemHovered();
			rightClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);


			DrawStain(dye, isDyeable);

			ImGui.SetCursorPos(initialPosition);
			ImGui.SetCursorPos(ImGui.GetCursorPos() - (difference / 2));

			var itemCapInfo = ImageGuiCrop.GetPart("icon_a_frame", 1);

			// item slot
			var itemSlotInfo = ImageGuiCrop.GetPart("mirage_prism_box", 3);
			var slotSize = capSize * (itemSlotInfo.Item4.X / itemCapInfo.Item4.X);
			//difference = slotSize - IconSize;
			var slotPos = ImGui.GetCursorScreenPos();
			//var slotPos = ImGui.GetCursorScreenPos() - (difference / 2);
			draw.AddImage(itemSlotInfo.Item1, slotPos, slotPos + slotSize, itemSlotInfo.Item2, itemSlotInfo.Item3);


			// item cap (but no item cap in glam dresser)
			//ImGui.Image(itemCapInfo.Item1, capSize, itemCapInfo.Item2, itemCapInfo.Item3);
			//var capPos = ImGui.GetCursorScreenPos();
			//draw.AddImage(itemCapInfo.Item1, capPos, capPos + capSize, itemCapInfo.Item2, itemCapInfo.Item3);

			// Hover visual
			if (wasHovered) {
				var itemHoveredInfo = ImageGuiCrop.GetPart("icon_a_frame", 16);
				ImGui.SetCursorPos(initialPosition);
				var hoverSize = capSize * (itemHoveredInfo.Item4.X / itemCapInfo.Item4.X);
				difference = hoverSize - IconSize;

				var hoverPos = ImGui.GetCursorScreenPos() - (difference / 2);
				draw.AddImage(itemHoveredInfo.Item1, hoverPos, hoverPos + hoverSize, itemHoveredInfo.Item2, itemHoveredInfo.Item3);
			}

			ImGui.PopStyleVar();

			ImGui.EndGroup();
			return clicked;
		}

		public static void DrawStain(Dye? dye, bool isDyeable) {
			if (dye == null || !isDyeable) return;

			ImGui.SameLine();
			var color = dye.RowId == 0 ? new Vector4(0, 0, 0, 0) : dye.ColorVector4;

			var draw = ImGui.GetWindowDrawList();
			Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
			var radius = (ImGui.GetFontSize()) * 0.5f * Plugin.PluginConfiguration.IconSizeMult;
			var x = cursorScreenPos.X - radius - ImGui.GetStyle().ItemSpacing.X;
			var y = cursorScreenPos.Y + radius;
			var pos = new Vector2(x, y);

			draw.AddCircleFilled(pos, radius, ImGui.ColorConvertFloat4ToU32(color));
			draw.AddCircle(pos, radius, 0xff000000, 0, DyeBorder);
		}
		public static void DrawContextMenu() {
			if(ContexMenuItem != null && ContexMenuAction != null) {
				if (ImGui.BeginPopupContextWindow("ContextMenuItemDresser")) {
					ContexMenuAction?.Invoke(ContexMenuItem);
					ImGui.EndPopup();
				}
			}
		}
	}
	[Flags]
	enum IconImageFlag {
		None = 0,
		NotAppliable = 1,
	}
}
