using Dresser.Interop;
using Dresser.Services;
using Dresser.Structs.Actor;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using DresserInventoryItem = Dresser.Structs.Dresser.InventoryItem;
using SubKindsPlayerCharacter = Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter;

namespace Dresser.Extensions {
	public static class PlayerCharacterExtention {

		public unsafe static void Equip(this SubKindsPlayerCharacter playerCharacter, DresserInventoryItem item) {

			var index = item.Item.EquipIndex();
			if (index != null) {
				playerCharacter.Equip((EquipIndex)index, item.ToItemEquip());

			} else if (item.Item.IsWeapon()) {

				var weaponIndex = item.Item.EquipSlotCategoryEx?.OffHand == 1 ? WeaponIndex.OffHand : WeaponIndex.MainHand;
				playerCharacter.Equip(weaponIndex, item.ToWeaponEquip(weaponIndex));

				if (weaponIndex == WeaponIndex.MainHand)
					playerCharacter.Equip(WeaponIndex.OffHand, item.ToWeaponEquip(WeaponIndex.OffHand));
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
			var drawData = ((Character*)playerCharacter.Address)->DrawData;
			bool mustResetHat = !drawData.IsHatHidden != ConfigurationManager.Config.CurrentGearDisplayHat;
			if(mustResetHat) {
				drawData.HideHeadgear(0, !ConfigurationManager.Config.CurrentGearDisplayHat);
			}
			// drawData.IsVisorToggled doesn't seem to react properly so we can't use the reset
			// as it is done for hats
			drawData.SetVisor(ConfigurationManager.Config.CurrentGearDisplayVisor);
		}
		public static void RedrawHeadGear(this SubKindsPlayerCharacter playerCharacter)
			=> playerCharacter.Equip(EquipIndex.Head, playerCharacter.EquipmentModel(EquipIndex.Head));
		public unsafe static void DisplayWeaponIfHidden(this SubKindsPlayerCharacter playerCharacter) {
			var drawData = ((Character*)playerCharacter.Address)->DrawData;
			drawData.HideWeapons(!ConfigurationManager.Config.CurrentGearDisplayWeapon);
		}
		public static void RedrawWeapon(this SubKindsPlayerCharacter playerCharacter) {
			playerCharacter.Equip(WeaponIndex.MainHand, playerCharacter.MainHandModels().Equip);
			playerCharacter.Equip(WeaponIndex.OffHand, playerCharacter.OffHandModels().Equip);
		}


	}
}
