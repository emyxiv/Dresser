using AllaganLib.GameSheets.Caches;
using AllaganLib.GameSheets.Model;

using Dalamud.Bindings.ImGui;
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

using Lumina.Excel.Sheets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
			bool __ = false;
			bool ___ = false;
			DrawIcon(item, ref ___, ref __, out var _, out var _);
		}
		public static bool DrawIcon(InventoryItem? item, ref bool isHovered, ref bool isTooltipActive, out bool clickedMiddle, out bool clickedStain, GlamourPlateSlot? emptySlot = null, Action<InventoryItem, GlamourPlateSlot?>? contextAction = null, float sizeMod = 1) {
			clickedMiddle = false;
			clickedStain = false;

			if (PluginServices.Context.LocalPlayer == null
				|| PluginServices.Context.LocalPlayerRace == null
				|| PluginServices.Context.LocalPlayerGender == null
				|| PluginServices.Context.LocalPlayerClass == null
				) return false;

			item ??= Gathering.EmptyItemSlot();

			// item variables
			//var dye = PluginServices.Storage.Dyes!.FirstOrDefault(d => d.RowId == item?.Stain);
			var image = ConfigurationManager.Config.ShowImagesInBrowser ? IconWrapper.Get(item) : null;
			if (image == null && emptySlot == null) emptySlot = item.Item.GlamourPlateSlot();
			var isEquippableByCurrentClass = true;
			// Service.ExcelCache.IsItemEquippableBy(item!.Item.ClassJobCategory.Row, PluginServices.Context.LocalPlayerClass.RowId);
			var isEquippableByGenderRaceGc = item.Item.CanBeEquipedByPlayedRaceGenderGc();
			var DyeCount = item.Item.Base.DyeCount;
			var isApplicable = !item.IsFadedInBrowser();
			var iconImageFlag = isApplicable ? IconImageFlag.None : IconImageFlag.NotAppliable;

			if (item.ItemId == 0)
				image = null;
			var clicked = DrawImage(image, ref isHovered, out clickedMiddle, out clickedStain, iconImageFlag, item,contextAction, emptySlot, sizeMod);
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

						// START ICON SIDE
						//
						ImGui.SameLine();
						ImGui.BeginGroup();
						Vector2 textInitPos = ImGui.GetCursorScreenPos();
						ImGui.TextColored(rarityColor, $"{item.FormattedName}");
						var nameSize = ImGui.GetItemRectSize();
						// Glamour Set Outfit indicator
						if (item.Item.IsPartOfGlamourSet()) {
							ImGui.SameLine();
							GuiHelpers.GameImage(UldBundle.ItemDetail_GlamourSetItem, new Vector2(ImGui.GetFontSize()));
						}

						// Modded
						if (isModdedItem) {
							var verticalNameMid = nameSize.Y * 0.55f;

							var draw = ImGui.GetWindowDrawList();
							// strike through the item name
							draw.AddLine(textInitPos + new Vector2(0, verticalNameMid), textInitPos + new Vector2(nameSize.X, verticalNameMid), ImGui.ColorConvertFloat4ToU32(ModdedItemColor * new Vector4(1, 1, 1, 0.75f)), ImGui.GetFontSize() * 0.15f);

							// add the mod name and version instead of the item name
							ImGui.TextColored(ModdedItemColor, $"{item.ModName}");
							ImGui.SameLine();
							ImGui.TextColored(ColorGreyDark, $"{item.ModVersion}");

							// this says if there is a website for the mod, usually for heliosphere mods
							if (!item.ModWebsite.IsNullOrWhitespace()) {
								ImGui.SameLine();
								GuiHelpers.Icon(FontAwesomeIcon.Globe, true, ColorGood);
							}
						}

						// LINE BREAK

						// level
						ImGui.TextColored(PluginServices.Context.LocalPlayerLevel < item.Item.Base.LevelEquip ? ColorBad : ColorGrey, $"\uE06A{item.Item.Base.LevelEquip}");
						// ilvl
						ImGui.SameLine();
						ImGui.TextColored(ColorGrey, $"\uE033{item.Item.Base.LevelItem.RowId}");
						ImGui.SameLine();
						ImGui.TextColored(ColorGreyDark, $"[{item.Item.Patch}]");


						// LINE BREAK
						// dyes stains
						DrawStainsTooltip(item, sizeMod);

						// END ICON SIDE
						//
						ImGui.EndGroup();

						// LINE BREAK
						// various debug and id info
						if (ConfigurationManager.Config.IconTooltipShowDev) {
							ImGui.TextColored(ColorGreyDark, $"[{item.ItemId} - 0x{item.ItemId:X0}] ({item.FormattedType}) [");
							ImGui.SameLine();
							ImGui.TextColored(rarityColor, $"{item.Item.Base.Rarity}");
							ImGui.SameLine();
							ImGui.TextColored(ColorGreyDark, $"]");

							// LINE BREAK
							ImGui.TextColored(ColorGreyDark, $"MM: [{item.Item.Base.ModelMain}] {item.Item.ModelMainItemModel()}");
							ImGui.SameLine();
							ImGui.TextColored(ColorGreyDark, $"MS: [{item.Item.Base.ModelSub}] {item.Item.ModelSubItemModel()}");
						}

						// LINE BREAK
						// mod author
						if (isModdedItem) {

							ImGui.TextColored(ModdedItemColor, $"by {item.ModAuthor}");
							if (ConfigurationManager.Config.IconTooltipShowDev) {
								// LINE BREAK
								ImGui.TextColored(ColorGreyDark, $"[{item.ModModelPath}]");
							}
						}

						// LINE BREAK


						// type of item (body, legs, etc) under the icon
						ImGui.TextColored(ColorGrey, string.Join(", ",item.Item.EquipSlotCategory?.PossibleSlots.Select(s=>s.Humanize()) ?? []));
						ImGui.SameLine();
						if ((item.Item.EquipSlotCategory?.BlockedSlots.Count?? 0) > 0) {
							ImGui.TextColored(ColorBad, "Locks: " + string.Join(", ", item.Item.EquipSlotCategory?.BlockedSlots.Select(s => s.Humanize()) ?? []));
						}
						// inventory where the item is if owned (only new line if there is a "Locks")
						ImGui.TextColored(isApplicable ? ColorBronze : ColorBad, item.FormattedInventoryCategoryType());

						// job/class allowed
						ImGui.TextColored(isEquippableByCurrentClass ? ColorGood : ColorBad, $"{item.Item.Base.ClassJobCategory.Value.Name}");

						// gender/race/gc
						var fitGender = item.Item.EquippableByGender;
						var fitRace = item.Item.EquipRace;
						bool requiresSpecificGender = fitGender != CharacterSex.Both;
						bool requiresSpecificRace = fitRace != CharacterRace.Any;
						bool requiresSpecificGc = item.Item.Base.GrandCompany.RowId != 0;
						if(requiresSpecificGender || requiresSpecificRace || requiresSpecificGc) {
							ImGui.TextColored(isEquippableByGenderRaceGc ? ColorBronze : ColorBad, "Fits: ");
						}
						if (requiresSpecificRace) {
							ImGui.SameLine();
							ImGui.TextColored(fitRace == PluginServices.Context.LocalPlayerRace ? ColorBronze : ColorBad, fitRace.Humanize());
						}
						if (requiresSpecificGender) {
							ImGui.SameLine();
							GuiHelpers.Icon(fitGender == CharacterSex.Male ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus, true, fitGender == PluginServices.Context.LocalPlayerGender ? ColorBronze : ColorBad);
						}
						if (requiresSpecificGc) {
							ImGui.SameLine();
							ImGui.TextColored(item.Item.Base.GrandCompany.RowId == PluginServices.Context.LocalPlayerGrandCompany?.RowId ? ColorGood : ColorBad, item.Item.Base.GrandCompany.Value.Name.ToString());
						}


						DrawItemSource(item);

						// Acquisition
						//ImGui.Separator();
						//var typEx = (InventoryTypeExtra)item.SortedContainer;
						//switch (typEx) {
						//	case InventoryTypeExtra.RelicVendors:
						//	case InventoryTypeExtra.CalamityVendors:
						//		ImGui.Text($"Buy (vendor) for {item.BuyFromVendorPrice:n0} gil");
						//		break;
						//}

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

		public static void DrawItemSource(InventoryItem inventoryItem) {
			// Quick config checks
			if (!ConfigurationManager.Config.ShowItemTooltipsSources) return;
			if (ConfigurationManager.Config.ShowItemTooltipsSourcesNotObtained && inventoryItem.IsObtained()) return;

			var item = inventoryItem.Item;
			var sources = item.Sources;
			if (sources == null || sources.Count == 0) return;

			// Deduplicate sources:
			// - Key by (Type, primary cost item id)
			// - For each group keep the source with the lowest total cost (sum of CostItem.Count)
			// Rationale: "same item id" interpreted as the primary cost item's RowId (first cost item), and
			// when multiple cost entries exist compare their total cost and keep the minimal one.
			var deduped = sources
				.Select(s => new {
					Source = s,
					PrimaryCostItemId = s.CostItems?.FirstOrDefault()?.ItemRow?.RowId ?? 0,
					TotalCost = s.CostItems?.Sum(ci => (ci.Count ?? 0)) ?? 0
				})
				.GroupBy(x => (x.Source.Type, x.PrimaryCostItemId))
				.Select(g => g.OrderBy(x => x.TotalCost).ThenBy(x => (x.Source.CostItems?.Count ?? 0)).First().Source);

			// Draw
			ImGui.Text($"Sources:");
			foreach (var itemSource in deduped) {

				// print source type
				ImGui.BulletText($"{itemSource.Type.Humanize()}");

				// followed by costs if available
				int costItemCount = 0;
				var costItems = itemSource.CostItems;
				if (costItems != null && costItems.Count != 0) {
					ImGui.SameLine();
					foreach (var costItem in costItems) {
						costItemCount++;
						var icon = costItem.ItemRow.Base.Icon;
						GuiHelpers.GameIcon(icon, new Vector2(ImGui.GetFontSize()));
						if (costItem.Count != null) {
							ImGui.SameLine();
							ImGui.TextColored(ColorBronze, costItem.Count?.ToString());
						}
						if (costItemCount < costItems.Count) {
							ImGui.SameLine();
						}
					}
				}
			}
		}
		private static bool DrawImage(ISharedImmediateTexture image, InventoryItem item, IconImageFlag iconImageFlag = 0)
		{
			bool __ = false;
			return DrawImage(image, ref __, out var _, out var _, iconImageFlag, item);
		}
		private static bool DrawImage(ISharedImmediateTexture image, ref bool hovering, out bool clickedMiddle, out bool clickedStain, IconImageFlag iconImageFlag, InventoryItem item,Action<InventoryItem, GlamourPlateSlot?>? contextAction = null, GlamourPlateSlot? emptySlot = null, float sizeMod = 1) {
			ImGui.BeginGroup();
			clickedMiddle = false;
			clickedStain = false;
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
					// ImGui.Image(image.GetWrapOrEmpty().Handle, iconSize, Vector2.Zero, Vector2.One, colorize);
					var pos = ImGui.GetCursorScreenPos();
					ImGui.GetWindowDrawList().AddImageRounded(image.GetWrapOrEmpty().Handle,pos,pos +  iconSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(colorize),iconSize.X * 0.13f);
					ImGui.InvisibleButton($"##invisibleItemIcon##", iconSize);
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
						draw.AddImage(emptySlotInfo.Handle, ddddddddd + placeholderIconOffset, ddddddddd + placeholderIconOffset + placeholderIconSize);
						//ImGui.Image(emptySlotInfo.Handle, iconSize * 0.5f);
					}
				}

				clicked = ImGui.IsItemClicked();
				clickedMiddle = ImGui.IsItemClicked(ImGuiMouseButton.Middle);
				hovering = ImGui.IsItemHovered();
				if (ImGui.BeginPopupContextItem($"ContextMenuItemIcon##{((BrowserIndex)item)}##{emptySlot.GetHashCode()}")) {
					contextAction?.Invoke(item, emptySlot);
					ImGui.EndPopup();
				}

				clickedStain = DrawStains(item, sizeMod);

				ImGui.SetCursorPos(initialPosition);
				ImGui.SetCursorPos(ImGui.GetCursorPos() - (difference / 2));

				// item slot
				var itemSlotInfo = PluginServices.ImageGuiCrop.GetPart(UldBundle.MirageSlotNormal);
				if(itemSlotInfo != null) {
					var slotSize = capSize;
					//difference = slotSize - iconSize;
					var slotPos = ImGui.GetCursorScreenPos();
					//var slotPos = ImGui.GetCursorScreenPos() - (difference / 2);
					draw.AddImage(itemSlotInfo.Handle, slotPos, slotPos + slotSize);
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
						draw.AddImage(itemHoveredInfo.Handle, hoverPos, hoverPos + hoverSize);
					}
				}
			} catch (Exception ex) {
				PluginLog.Warning($"Error when Drawing item Icon\n{ex}");
			}


			ImGui.PopStyleVar();

			ImGui.EndGroup();
			return clicked;
		}


		public static void DrawStainsTooltip(InventoryItem item, float sizeMod = 1) {

			if (!item.Item.IsDyeable1()) return;
			var stain1 = item.StainEntry;
			if (stain1 == null) return; // should not happen
			var pos1 = ImGui.GetCursorScreenPos();
			var color1 = item.Stain != 0 ? ColorGoodLight : ColorGrey;
			ImGui.TextColored(color1, $"{stain1.Value.Name}");
			pos1 += ImGui.GetItemRectSize() + new Vector2(ImGui.GetStyle().ItemSpacing.X * 0.5f, 0);
			DrawStain(stain1.Value, 1, sizeMod * 0.75f,out var radius1, ImGui.ColorConvertFloat4ToU32(color1), pos1, false);

			if (!item.Item.IsDyeable2()) return;
			var stain2 = item.StainEntry;
			if (stain2 == null) return; // should not happen
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (radius1 * 2) + ImGui.GetStyle().ItemSpacing.X);
			var pos2 = ImGui.GetCursorScreenPos();
			var color2 = item.Stain2 != 0 ? ColorGoodLight : ColorGrey;
			ImGui.TextColored(color2, $"{stain2.Value.Name}");

			pos2 += ImGui.GetItemRectSize() + new Vector2(ImGui.GetStyle().ItemSpacing.X * 0.5f, 0);
			DrawStain(stain2.Value, 2, sizeMod * 0.75f, out var radius2, ImGui.ColorConvertFloat4ToU32(color2), pos2, false);
			ImGui.SetCursorScreenPos(pos2);
			ImGui.InvisibleButton("##spacelockerStain2##DrawStainsTooltip",new Vector2(radius2*2)); // lock space on the right
		}
		public static bool DrawStains(InventoryItem item, float sizeMod = 1)
		{

			bool wasClicked = false;
			if (!item.Item.IsDyeable1()) return wasClicked;
			var stain1 = item.StainEntry;
			if(stain1 == null) return wasClicked;
			wasClicked |= DrawStain(stain1.Value, 1, sizeMod);

			if (!item.Item.IsDyeable2()) return wasClicked;
			var stain2 = item.Stain2Entry;
			if (stain2 == null) return wasClicked;
			wasClicked |= DrawStain(stain2.Value, 2, sizeMod);
			return wasClicked;
		}
		public static bool DrawStain(Stain stain, ushort dyeIndex, float sizeMod = 1)
			=> DrawStain(stain, dyeIndex, sizeMod, out var _, 0xff000000, null, true);
		public static bool DrawStain(Stain stain, ushort dyeIndex, float sizeMod, out float radius, uint borderColor, Vector2? pos = null, bool forceSameLineBefore = true) {
			if(forceSameLineBefore) ImGui.SameLine();

			var rowOffset = dyeIndex > 1 ? dyeIndex * 1.75f : 1;

			radius = (ImGui.GetFontSize()) * 0.5f * ConfigurationManager.Config.IconSizeMult * sizeMod;
			if (pos == null) {
				Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
				var x = cursorScreenPos.X - radius - ImGui.GetStyle().ItemSpacing.X;
				var y = cursorScreenPos.Y + (rowOffset * radius);
				pos = new Vector2(x, y);
			} else {
				pos += new Vector2(radius, -radius - (ImGui.GetFontSize() * 0.075f)); // set anchor to bottom left corner 
			}
			var color = stain.ColorVector4();

			var draw = ImGui.GetWindowDrawList();
			draw.AddCircleFilled(pos.Value, radius, ImGui.ColorConvertFloat4ToU32(color));
			draw.AddCircle(pos.Value, radius, borderColor, 0, DyeBorder * sizeMod);

			var posSquare = pos.Value - new Vector2(radius);
			var wasClicked = ImGui.IsMouseHoveringRect(posSquare, posSquare + new Vector2(radius * 2)) && ImGui.IsItemClicked();

			if (wasClicked)DyePicker.CircleIndex(dyeIndex);
			return wasClicked;
		}
	}
	[Flags]
	enum IconImageFlag {
		None = 0,
		NotAppliable = 1,
	}
}
