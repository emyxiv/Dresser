using System;
using System.Linq;
using System.Numerics;

using AllaganLib.GameSheets.Model;

using CriticalCommonLib;
using CriticalCommonLib.Extensions;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;

using Humanizer;

using ImGuiNET;

using Lumina.Excel.Sheets;

using static Dresser.Services.Storage;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;


namespace Dresser.Windows.Components {
	internal class ItemIcon {
		public static Vector2 IconSize => new Vector2(120) * ConfigurationManager.Config.IconSizeMult;
		public static Vector2 TooltipFramePadding => new Vector2(ImGui.GetFontSize() * 0.2f);
		public static Vector2 TooltipItemSpacing => TooltipFramePadding;
		public static float DyeBorder => 3 * ConfigurationManager.Config.IconSizeMult;

		public static Vector4 ColorGood => ConfigurationManager.Config.ColorGood;
		public static Vector4 ColorGoodLight => ConfigurationManager.Config.ColorGoodLight;
		public static Vector4 ColorBad => ConfigurationManager.Config.ColorBad;
		public static Vector4 ColorGrey => ConfigurationManager.Config.ColorGrey;
		public static Vector4 ColorGreyDark => ConfigurationManager.Config.ColorGreyDark;
		public static Vector4 ColorBronze => ConfigurationManager.Config.ColorBronze;
		public static Vector4 ModdedItemWatermarkColor => ConfigurationManager.Config.ModdedItemWatermarkColor;
		public static Vector4 ModdedItemColor => ConfigurationManager.Config.ModdedItemColor;

		public static InventoryItem? ContexMenuItem = null;
		public static Action<InventoryItem, GlamourPlateSlot?>? ContexMenuAction = null;
		public static GlamourPlateSlot? ContexMenuItemSlot = null;
		public static bool IsHidingTooltip => PluginServices.KeyState[VirtualKey.MENU] || PluginServices.KeyState[VirtualKey.LMENU] || PluginServices.KeyState[VirtualKey.RMENU];

		//public static bool DrawIcon(IDalamudTextureWrap image, Dye? dye, InventoryItem item, byte DyeCount)
		//	=> DrawIcon(image, dye, DyeCount, item, out bool _);

		public static void DrawIcon(InventoryItem? item) {
			bool _ = false;
			bool __ = false;
			DrawIcon(item, ref _, ref __, out __);
		}
		public static bool DrawIcon(InventoryItem? item, ref bool isHovered, ref bool isTooltipActive, out bool clickedMiddle, GlamourPlateSlot? emptySlot = null, Action<InventoryItem, GlamourPlateSlot?>? contextAction = null, float sizeMod = 1) {
			clickedMiddle = false;

			if (PluginServices.Context.LocalPlayer == null
				|| PluginServices.Context.LocalPlayerRace == null
				|| PluginServices.Context.LocalPlayerGender == null
				|| PluginServices.Context.LocalPlayerClass == null
				) return false;

			item ??= Gathering.EmptyItemSlot();

			// item variables
			//var dye = PluginServices.Storage.Dyes!.FirstOrDefault(d => d.RowId == item?.Stain);
			var image = ConfigurationManager.Config.ShowImagesInBrowser ? IconWrapper.Get(item) : null;
			if (image == null && emptySlot == null) emptySlot = item?.Item.GlamourPlateSlot();
			var isEquippableByCurrentClass = item?.Item.CanBeEquipedByPlayedJob();
			// Service.ExcelCache.IsItemEquippableBy(item!.Item.ClassJobCategory.Row, PluginServices.Context.LocalPlayerClass.RowId);
			var isEquippableByGenderRace = item.Item.CanBeEquippedByRaceGender((CharacterRace)PluginServices.Context.LocalPlayerRace, (CharacterSex)PluginServices.Context.LocalPlayerGender);
			var DyeCount = item.Item.Base.DyeCount;
			var isApplicable = !item.IsFadedInBrowser();
			var iconImageFlag = isApplicable ? IconImageFlag.None : IconImageFlag.NotAppliable;

			if (item.ItemId == 0)
				image = null;
			var clicked = DrawImage(image, ref isHovered, out clickedMiddle, iconImageFlag, item,contextAction, emptySlot, sizeMod);
			var isTooltipActive2 = isTooltipActive;

			if (item != null && item.ItemId != 0 && !IsHidingTooltip)
				GuiHelpers.Tooltip(() => {
					if (isTooltipActive2) return;

					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, TooltipFramePadding);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, TooltipItemSpacing);

					isTooltipActive2 = true;
					try {
						var isModdedItem = item.IsModded();
						//if(isModdedItem) GuiHelpers.TextWithFontDrawlist("MODDED ITEM\nMODDED ITEM\nMODDED ITEM\nMODDED ITEM\nMODDED ITEM\nMODDED ITEM", GuiHelpers.Font.Title, ModdedItemWatermarkColor, 7f * ImGui.GetFontSize());

						if (image == null && !ConfigurationManager.Config.ShowImagesInBrowser) image = IconWrapper.Get(item);
						DrawImage(image!, item);

						var rarityColor = RarityColor(item.Item);

						// Side of the icon
						ImGui.SameLine();
						ImGui.BeginGroup();
						Vector2 textInitPos = ImGui.GetCursorScreenPos();
						ImGui.TextColored(rarityColor, $"{item.FormattedName}");
						// Modded
						if (isModdedItem) {
							var nameSize = ImGui.GetItemRectSize();
							var verticalNameMid = nameSize.Y * 0.55f;

							var draw = ImGui.GetWindowDrawList();
							//-new Vector2(0, nameSize.Y / 2)
							//
							draw.AddLine(textInitPos+ new Vector2(0,verticalNameMid), textInitPos + new Vector2(nameSize.X, verticalNameMid), ImGui.ColorConvertFloat4ToU32(ModdedItemColor * new Vector4(1,1,1,0.75f)), ImGui.GetFontSize() * 0.15f);

							ImGui.TextColored(ModdedItemColor, $"{item.ModName}");
							ImGui.SameLine();
							ImGui.TextColored(ColorGreyDark, $"{item.ModVersion}");
							if (!item.ModWebsite.IsNullOrWhitespace()) {
								ImGui.SameLine();
								GuiHelpers.Icon(FontAwesomeIcon.Globe, true, ColorGood);
							}
							ImGui.TextColored(ModdedItemColor, $"by {item.ModAuthor}");
							if (ConfigurationManager.Config.IconTooltipShowDev) {
								ImGui.TextColored(ColorGreyDark, $"[{item.ModModelPath}]");
							}
						}


						if (ConfigurationManager.Config.IconTooltipShowDev) {
							ImGui.TextColored(ColorGreyDark, $"[{item.ItemId} - 0x{item.ItemId:X0}] ({item.FormattedType}) [");
							ImGui.SameLine();
							ImGui.TextColored(rarityColor, $"{item.Item.Base.Rarity}");
							ImGui.SameLine();
							ImGui.TextColored(ColorGreyDark, $"]");

							ImGui.TextColored(ColorGreyDark, $"MM: [{item.Item.Base.ModelMain}] {item.Item.ModelMainItemModel()}");
							ImGui.SameLine();
							ImGui.TextColored(ColorGreyDark, $"MS: [{item.Item.Base.ModelSub}] {item.Item.ModelSubItemModel()}");
						}
						ImGui.TextColored(ColorGreyDark, $"[Patch {item.Item.Patch}]");
						if (item.Item.Base.DyeCount > 0) ImGui.TextColored(item.Stain  != 0 ? ColorGoodLight : ColorGrey, $"{Service.ExcelCache.GameData.Excel.GetSheet<Stain>().First(i => i.RowId == item.Stain ).Name}");
						if (item.Item.Base.DyeCount > 1) ImGui.TextColored(item.Stain2 != 0 ? ColorGoodLight : ColorGrey, $"{Service.ExcelCache.GameData.Excel.GetSheet<Stain>().First(i => i.RowId == item.Stain2).Name}");

						ImGui.EndGroup();

						// type of item (body, legs, etc) under the icon
						ImGui.TextColored(ColorGrey, string.Join(", ",item.Item.EquipSlotCategory?.PossibleSlots.Select(s=>s.Humanize())));
						ImGui.SameLine();
						if ((item.Item.EquipSlotCategory?.BlockedSlots.Count?? 0) > 0) {
							ImGui.TextColored(ColorBad, "Locks: " + string.Join(", ", item.Item.EquipSlotCategory?.BlockedSlots.Select(s => s.Humanize())));
						}
						//PluginLog.Debug($"ui category: {item.ItemUICategory} {item.EquipSlotCategory!.RowId}");
						ImGui.TextColored(isApplicable ? ColorBronze : ColorBad, item.FormattedInventoryCategoryType());

						// Equip Conditions
						ImGui.Separator();

						ImGui.TextColored(PluginServices.Context.LocalPlayerLevel < item.Item.Base.LevelEquip ? ColorBad : ColorGrey, $"lvl: {item.Item.Base.LevelEquip}");
						ImGui.SameLine();
						ImGui.Text($"ilvl: {item.Item.Base.LevelItem.RowId}");

						ImGui.TextColored(isEquippableByCurrentClass.Value ? ColorGood : ColorBad, $"{item.Item.Base.ClassJobCategory.Value.Name}");

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
						var typEx = (InventoryTypeExtra)item.SortedContainer;
						switch (typEx) {
							case InventoryTypeExtra.RelicVendors:
							case InventoryTypeExtra.CalamityVendors:
								ImGui.Text($"Buy (vendor) for {item.BuyFromVendorPrice:n0} gil");
								break;
						}

						// Other info
						//if((InventoryTypeExtra)item.SortedContainer == InventoryTypeExtra.CalamityVendors)
						//ImGui.Text($"Buy (vendor) for {item.BuyFromVendorPrice:n0} gil");
						//var zz = item.Item.Vendors;
						//var dd = zz.Select(shop => shop.Name).Distinct().ToList();
						//var dd = zz.SelectMany(shop => shop.ENpcs.Select(n=>(n.Resident?.Singular ?? "Unknown",shop.Name)).Distinct()).Distinct().ToList();
						//ImGui.Text($"Name: {string.Join(",",dd.Select(t=>$"{t.Item1}:{t.Name}"))}");
						//if(Service.ExcelCache.ItemSpecialShopCostLookup.TryGetValue(item.Item.RowId, out var spsi)) {
						//	foreach(var ddqsdq in spsi) {
						//		ImGui.Text($"currency: {ddqsdq}");
						//	}
						//}

						//var currencies
						//foreach ((var it, var currencyIdOfInteres) in Storage.FilterCurrencyIds) {

						//	if (item.Item.ObtainedWithSpecialShopCurrency(currencyIdOfInteres))
						//		ImGui.Text($"currency");
						//}
						//var specialShopCost = item.Item._specialShopCosts.Select(s => $"{s.Item1?.Value?.Name}+({s.Item2})");
						//ImGui.Text($"specialShopCost ({item.Item._specialShopCosts.Count()}): {string.Join(",", specialShopCost)}");
						//zz.
						// TODO: market price

						//ImGui.Text($"Sell for {item.SellToVendorPrice:n0} gil");
					} catch (Exception ex) {
						PluginLog.Warning($"Error when displaying item ToolTip\n{ex}");
					} finally {
						ImGui.PopStyleVar(2);
					}

				});
			isTooltipActive = isTooltipActive2;



			return clicked;
		}
		private static bool DrawImage(ISharedImmediateTexture image, InventoryItem item, IconImageFlag iconImageFlag = 0) {
			bool _ = false;
			return DrawImage(image, ref _, out _, iconImageFlag, item);
		}
		private static bool DrawImage(ISharedImmediateTexture image, ref bool hovering, out bool clickedMiddle, IconImageFlag iconImageFlag, InventoryItem item,Action<InventoryItem, GlamourPlateSlot?>? contextAction = null, GlamourPlateSlot? emptySlot = null, float sizeMod = 1) {
			ImGui.BeginGroup();
			clickedMiddle = false;
			var iconSize = IconSize * sizeMod;

			bool wasHovered = hovering;
			var clicked = false;
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
			try {

				var initialPosition = ImGui.GetCursorPos();
				var draw = ImGui.GetWindowDrawList();
				var capSize = iconSize * new Vector2(1.17f, 1.16f);
				var difference = capSize - iconSize;
				initialPosition += (new Vector2(0, 3f) * ConfigurationManager.Config.IconSizeMult * sizeMod);


				if (image != null) {
					var colorize = !IsHidingTooltip && iconImageFlag.HasFlag(IconImageFlag.NotAppliable) ? Styler.CollectionColorBackground + new Vector4(0, 0, 0, 0.3f) : Vector4.One;
					ImGui.Image(image.GetWrapOrEmpty().ImGuiHandle, iconSize, Vector2.Zero, Vector2.One, colorize);
				} else if (emptySlot != null) {
					var emptySlotInfo = PluginServices.ImageGuiCrop.GetPart((GlamourPlateSlot)emptySlot);

					// TODO: smaller icons in their slot
					// draw.AddImage(emptySlotInfo.Item1, slotPos, slotPos + slotSize, emptySlotInfo.Item2, emptySlotInfo.Item3);
					if(emptySlotInfo != null) {
						var ddddddddd = ImGui.GetCursorScreenPos();
						ImGui.InvisibleButton("qsdddddsqsdqsd", iconSize);

						var placeholderIconSize = iconSize * 0.75f;
						var placeholderIconOffset = (iconSize - placeholderIconSize) / 2;
						//+(placeholderIconSize / 2)
						draw.AddImage(emptySlotInfo.ImGuiHandle, ddddddddd + placeholderIconOffset, ddddddddd + placeholderIconOffset + placeholderIconSize);
						//ImGui.Image(emptySlotInfo.ImGuiHandle, iconSize * 0.5f);
					}
				}

				clicked = ImGui.IsItemClicked();
				clickedMiddle = ImGui.IsItemClicked(ImGuiMouseButton.Middle);
				hovering = ImGui.IsItemHovered();
				if (ImGui.BeginPopupContextItem($"ContextMenuItemIcon##{((BrowserIndex)item)}##{emptySlot.GetHashCode()}")) {
					contextAction?.Invoke(item, emptySlot);
					ImGui.EndPopup();
				}

				DrawStains(item, sizeMod);

				ImGui.SetCursorPos(initialPosition);
				ImGui.SetCursorPos(ImGui.GetCursorPos() - (difference / 2));

				// item slot
				var itemSlotInfo = PluginServices.ImageGuiCrop.GetPart(UldBundle.MirageSlotNormal);
				if(itemSlotInfo != null) {
					var slotSize = capSize;
					//difference = slotSize - iconSize;
					var slotPos = ImGui.GetCursorScreenPos();
					//var slotPos = ImGui.GetCursorScreenPos() - (difference / 2);
					draw.AddImage(itemSlotInfo.ImGuiHandle, slotPos, slotPos + slotSize);
				}


				// item cap (but no item cap in glam dresser)
				//ImGui.Image(itemCapInfo.Item1, capSize, itemCapInfo.Item2, itemCapInfo.Item3);
				//var capPos = ImGui.GetCursorScreenPos();
				//draw.AddImage(itemCapInfo.Item1, capPos, capPos + capSize, itemCapInfo.Item2, itemCapInfo.Item3);

				// Hover visual
				if (wasHovered) {
					var itemHoveredInfo = PluginServices.ImageGuiCrop.GetPart(UldBundle.SlotHighlight);
					ImGui.SetCursorPos(initialPosition);
					if(itemHoveredInfo != null && itemSlotInfo != null) {
						
						var hoverSize = capSize * (itemHoveredInfo.Size.X / itemSlotInfo.Size.X);
						difference = hoverSize - iconSize;

						var hoverPos = ImGui.GetCursorScreenPos() - (difference / 2);
						draw.AddImage(itemHoveredInfo.ImGuiHandle, hoverPos, hoverPos + hoverSize);
					}
				}
			} catch (Exception ex) {
				PluginLog.Warning($"Error when Drawing item Icon\n{ex}");
			}


			ImGui.PopStyleVar();

			ImGui.EndGroup();
			return clicked;
		}

		public static void DrawStains(InventoryItem item, float sizeMod = 1) {

			if (!item.Item.IsDyeable1()) return;
			var stain1 = Service.ExcelCache.GameData.Excel.GetSheet<Stain>().GetRowOrDefault(item.Stain);
			//if(item.FormattedName == "Thavnairian Bustier") PluginLog.Debug($"DYE: {stain1.Name} {stain1.RowId}");
			if(stain1 == null) return;
			DrawStain(stain1.Value, 1, sizeMod);


			if (!item.Item.IsDyeable2()) return;
			var stain2 = Service.ExcelCache.GameData.Excel.GetSheet<Stain>().GetRowOrDefault(item.Stain2);
			if (stain2 == null) return;
			DrawStain(stain2.Value, 2, sizeMod);
		}
		public static void DrawStain(Stain stain, ushort dyeIndex, float sizeMod = 1) {
			ImGui.SameLine();
			Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();

			var rowOffset = dyeIndex > 1 ? dyeIndex * 1.75f : 1;

			var radius = (ImGui.GetFontSize()) * 0.5f * ConfigurationManager.Config.IconSizeMult * sizeMod;
			var x = cursorScreenPos.X - radius - ImGui.GetStyle().ItemSpacing.X;
			var y = cursorScreenPos.Y + (rowOffset * radius);
			var pos = new Vector2(x, y);
			var color = stain.ColorVector4();

			var draw = ImGui.GetWindowDrawList();
			draw.AddCircleFilled(pos, radius, ImGui.ColorConvertFloat4ToU32(color));
			draw.AddCircle(pos, radius, 0xff000000, 0, DyeBorder * sizeMod);
		}
	}
	[Flags]
	enum IconImageFlag {
		None = 0,
		NotAppliable = 1,
	}
}
