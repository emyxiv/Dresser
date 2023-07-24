using Dresser.Interop;
using Dresser.Services;
using Dresser.Structs.Actor;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;
using SubKindsPlayerCharacter = Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter;

namespace Dresser.Extensions {
	public static class PlayerCharacterExtention {

		public unsafe static void Equip(this SubKindsPlayerCharacter playerCharacter, CriticalInventoryItem item) {
			var index = item.Item.EquipIndex();

			if (index != null) {

				var equipment = playerCharacter.EquipmentModels();
				ItemEquip? itemEquipNullable = index switch {
					EquipIndex.Head => equipment.Head,
					EquipIndex.Chest => equipment.Chest,
					EquipIndex.Hands => equipment.Hands,
					EquipIndex.Legs => equipment.Legs,
					EquipIndex.Feet => equipment.Feet,
					EquipIndex.Earring => equipment.Earring,
					EquipIndex.Necklace => equipment.Necklace,
					EquipIndex.Bracelet => equipment.Bracelet,
					EquipIndex.RingLeft => equipment.RingLeft,
					EquipIndex.RingRight => equipment.RingRight,
					_ => null
				};
				if (itemEquipNullable == null) return;
				var itemEquip = (ItemEquip)itemEquipNullable;

				itemEquip.Id = (ushort)item.Item.ModelMain;
				itemEquip.Variant = (byte)(item.Item.ModelMain >> 16);
				itemEquip.Dye = item.Item.IsDyeable ? item.Stain : (byte)0;

				playerCharacter.Equip((EquipIndex)index, itemEquip);
			} else if (item.Item.IsWeapon()) {

				var model = item.Item.ModelSub != 0 ? item.Item.ModelSub : item.Item.ModelMain;

				var weaponSet = (ushort)item.Item.ModelMain;
				var weaponBase = (ushort)(model >> 16);
				var weaponVariant = (ushort)(model >> 32);
				var weaponDye = item.Item.IsDyeable ? item.Stain : (byte)0;

				if (item.Item.EquipSlotCategoryEx?.MainHand == 1) {
					var weapon = playerCharacter.MainHandModels().Equip;
					weapon.Set = weaponSet; weapon.Base = weaponBase; weapon.Variant = weaponVariant; weapon.Dye = weaponDye;

					playerCharacter.Equip(WeaponIndex.MainHand, weapon);
				} else if (item.Item.EquipSlotCategoryEx?.OffHand == 1) {
					var weapon = playerCharacter.OffHandModels().Equip;
					weapon.Set = weaponSet; weapon.Base = weaponBase; weapon.Variant = weaponVariant; weapon.Dye = weaponDye;

					playerCharacter.Equip(WeaponIndex.OffHand, weapon);
				} else return;

			}
		}

		public unsafe static Equipment EquipmentModels(this SubKindsPlayerCharacter playerCharacter)
			=> *(Equipment*)(playerCharacter.Address + Offsets.Equipment);
		public unsafe static ItemEquip EquipmentModel(this SubKindsPlayerCharacter playerCharacter, EquipIndex index)
			=> playerCharacter.EquipmentModels()[index];
		public unsafe static Weapon MainHandModels(this SubKindsPlayerCharacter playerCharacter)
			=> *(Weapon*)(playerCharacter.Address + Offsets.WeaponMainHand);
		public unsafe static Weapon OffHandModels(this SubKindsPlayerCharacter playerCharacter)
			=> *(Weapon*)(playerCharacter.Address + Offsets.WeaponOffHand);

		public static void Equip(this SubKindsPlayerCharacter playerCharacter, EquipIndex index, ItemEquip item) {
			if (Methods.ActorChangeEquip == null) return;
			Methods.ActorChangeEquip(playerCharacter.Address + Offsets.ActorDrawData, index, item);
			if(index == EquipIndex.Head) playerCharacter.DisplayHeadGearIfHidden();
		}

		public static void Equip(this SubKindsPlayerCharacter playerCharacter, WeaponIndex slot, WeaponEquip item) {
			if (Methods.ActorChangeWeapon == null) return;
			Methods.ActorChangeWeapon(playerCharacter.Address + Offsets.ActorDrawData, slot, item, 0, 1, 0, 0);
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
