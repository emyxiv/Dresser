using Dresser.Interop;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Actor;
using Dresser.Structs.Dresser;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using System.Diagnostics;

using SubKindsPlayerCharacter = Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter;

namespace Dresser.Extensions {
	public static class PlayerCharacterExtention {


		// apply appearance of set items
		public unsafe static void EquipSet(this SubKindsPlayerCharacter playerCharacter, Structs.Dresser.InventoryItemSet set) {
			if (PluginServices.Context.MustGlamourerApply())
				playerCharacter.EquipGlamourer(set);
			else {
				//PluginLog.Debug($"dddv => {set.Items.Count}");

				foreach ((var slot, var item) in set.Items) {
					//PluginLog.Debug($"dddv => {slot}, {(item == null?"null":item.ItemId)}");
					if (item == null) continue;
					playerCharacter.EquipStandalone(item, slot);
				}
			}
		}

		// apply appearance of single item
		public unsafe static void Equip(this SubKindsPlayerCharacter playerCharacter, InventoryItem item, GlamourPlateSlot slot) {
			if (PluginServices.Context.MustGlamourerApply()) {
				//PluginLog.Debug($"Apply Item set with EquipGlamourer through Equip {slot}, {item.FormattedName}");
				//playerCharacter.EquipGlamourer(new Structs.Dresser.InventoryItemSet(slot, item));
				PluginServices.Glamourer.SetItem(playerCharacter, item, slot);
			} else {
				playerCharacter.EquipStandalone(item, slot);
			}
		}
		public unsafe static void EquipGlamourer(this SubKindsPlayerCharacter playerCharacter, Structs.Dresser.InventoryItemSet set) {
			foreach ((var slot, var item) in set.Items) {
				if (item == null) playerCharacter.Equip(InventoryItem.Zero, slot);
				else playerCharacter.Equip(item, slot);
			}
		}
		public unsafe static void EquipStandalone(this SubKindsPlayerCharacter playerCharacter, InventoryItem item, GlamourPlateSlot slot) {
			PluginLog.Debug($"Apply Item with EquipStandalone");
			if (slot.IsWeapon()) {
				var weaponIndex = slot.ToWeaponIndex();
				if (weaponIndex != null) {
					playerCharacter.Equip(weaponIndex.Value, item.ToWeaponEquip(weaponIndex.Value));
					//PluginLog.Debug($"item {item.DebugName} = {item.Item.ToFullEquipType(true)}");
					if (slot == Interop.Hooks.GlamourPlateSlot.MainHand && !item.Item.IsMainModelOnOffhand()) { // TODO: if item is not a shield or tool, don't do that (also equip offhand sub)
						playerCharacter.Equip(WeaponIndex.OffHand, item.ToWeaponEquipSub());
					}
				}
			} else {
				var index = slot.ToEquipIndex();
				//PluginLog.Debug($"stda armor Equip => {item.ItemId} => {index} => {item.ToItemEquip().Id}");

				if (index != null) playerCharacter.Equip(index.Value, item.ToItemEquip());
			}
		}

		public unsafe static Equipment EquipmentModels(this SubKindsPlayerCharacter playerCharacter)
			=> *(Equipment*)(playerCharacter.Address + Offsets.Equipment);
		public unsafe static ItemEquip EquipmentModel(this SubKindsPlayerCharacter playerCharacter, EquipIndex index)
			=> playerCharacter.EquipmentModels()[index];
		public unsafe static Weapon WeaponModels(this SubKindsPlayerCharacter playerCharacter, WeaponIndex index)
			=> *(Weapon*)(playerCharacter.Address + (index == WeaponIndex.OffHand ? Offsets.WeaponOffHand : Offsets.WeaponMainHand));
		public static Weapon MainHandModels(this SubKindsPlayerCharacter playerCharacter)
			=> playerCharacter.WeaponModels(WeaponIndex.MainHand);
		public static Weapon OffHandModels(this SubKindsPlayerCharacter playerCharacter)
			=> playerCharacter.WeaponModels(WeaponIndex.OffHand);

		public static void Equip(this SubKindsPlayerCharacter playerCharacter, EquipIndex index, ItemEquip item) {
			if (Methods.ActorChangeEquip == null) return;
			Methods.ChangeEquip(playerCharacter.Address + Offsets.ActorDrawData, index, item);
			if(index == EquipIndex.Head) playerCharacter.DisplayHeadGearIfHidden();
		}

		public static void Equip(this SubKindsPlayerCharacter playerCharacter, WeaponIndex slot, WeaponEquip item) {
			if (Methods.ActorChangeWeapon == null) return;
			Methods.ChangeWeapon(playerCharacter.Address + Offsets.ActorDrawData, slot, item);
			//playerCharacter.DisplayWeaponIfHidden();
		}

		public unsafe static void DisplayHeadGearIfHidden(this SubKindsPlayerCharacter playerCharacter) {
			if (PluginServices.Context.MustGlamourerApply()) return;
			var drawData = ((Character*)playerCharacter.Address)->DrawData;
			bool mustResetHat = !drawData.IsHatHidden != ConfigurationManager.Config.CurrentGearDisplayHat;
			if(mustResetHat) {
				drawData.HideHeadgear(0, !ConfigurationManager.Config.CurrentGearDisplayHat);
			}
			// drawData.IsVisorToggled doesn't seem to react properly so we can't use the reset
			// as it is done for hats
			drawData.SetVisor(ConfigurationManager.Config.CurrentGearDisplayVisor);
		}
		public static void RedrawHeadGear(this SubKindsPlayerCharacter playerCharacter) {
			ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out InventoryItemSet plate);

			if (PluginServices.Context.MustGlamourerApply() && !plate.HasModdedItem()) {
				playerCharacter.EquipGlamourer(new());
				return;
			}

			var currentHat = plate.GetSlot(GlamourPlateSlot.Head);
			if (currentHat != null) PluginServices.ApplyGearChange.ApplyItemAppearanceOnPlayerWithMods(currentHat, GlamourPlateSlot.Head);
		}
		public unsafe static void DisplayWeaponIfHidden(this SubKindsPlayerCharacter playerCharacter) {
			var drawData = ((Character*)playerCharacter.Address)->DrawData;
			drawData.HideWeapons(!ConfigurationManager.Config.CurrentGearDisplayWeapon);
		}
		public static void RedrawWeapon(this SubKindsPlayerCharacter playerCharacter) {

			var didGetValue = ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out InventoryItemSet plate);

			if (PluginServices.Context.MustGlamourerApply() && !plate.HasModdedItem()) {
				playerCharacter.EquipGlamourer(new());
				return;
			}
			// Todo toggle hide weapon glamourer


			//var drawData = ((Character*)playerCharacter.Address)->DrawData;
			//var weaponData = *(WeaponModelId*)drawData.WeaponData;
			//drawData.LoadWeapon(DrawDataContainer.WeaponSlot.MainHand, weaponData,1,)



			if (didGetValue) {
				if (!ConfigurationManager.Config.CurrentGearDisplayWeapon) {
					playerCharacter.Equip(WeaponIndex.MainHand, WeaponEquip.Empty);
					playerCharacter.Equip(WeaponIndex.OffHand, WeaponEquip.Empty);
				} else {
					var itemMain = plate.GetSlot(GlamourPlateSlot.MainHand);
					if (itemMain != null) PluginServices.ApplyGearChange.ApplyItemAppearanceOnPlayerWithMods(itemMain, GlamourPlateSlot.MainHand);
					var itemOff = plate.GetSlot(GlamourPlateSlot.OffHand);
					if(itemOff != null)	PluginServices.ApplyGearChange.ApplyItemAppearanceOnPlayerWithMods(itemOff, GlamourPlateSlot.OffHand);
				}


			}


			//playerCharacter.Equip(WeaponIndex.MainHand, playerCharacter.MainHandModels().Equip);
			//playerCharacter.Equip(WeaponIndex.OffHand, playerCharacter.OffHandModels().Equip);
		}


	}
}
