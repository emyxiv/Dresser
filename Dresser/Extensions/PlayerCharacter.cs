using Dresser.Interop;
using Dresser.Structs.Actor;

using SubKindsPlayerCharacter = Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter;
using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;

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
			}
			else if(item.Item.IsWeapon()) {

				var model = item.Item.ModelSub != 0 ? item.Item.ModelSub : item.Item.ModelMain;

				var weaponSet = (ushort)item.Item.ModelMain;
				var weaponBase = (ushort)(model >> 16);
				var weaponVariant = (ushort)(model >> 32);
				var weaponDye = item.Item.IsDyeable ? item.Stain : (byte)0;

				if (item.Item.EquipSlotCategoryEx?.MainHand == 1) {
					var weapon = playerCharacter.MainHandModels().Equip;
					weapon.Set = weaponSet; weapon.Base = weaponBase; weapon.Variant = weaponVariant; weapon.Dye = weaponDye;

					playerCharacter.Equip(WeaponIndex.OffHand, weapon);
				} else if (item.Item.EquipSlotCategoryEx?.OffHand == 1) {
					var weapon = playerCharacter.OffHandModels().Equip;
					weapon.Set = weaponSet; weapon.Base = weaponBase; weapon.Variant = weaponVariant; weapon.Dye = weaponDye;

					playerCharacter.Equip(WeaponIndex.OffHand, weapon);
				} else return;

			}
		}

		public unsafe static Equipment EquipmentModels(this SubKindsPlayerCharacter playerCharacter)
			=> *(Equipment*)(playerCharacter.Address + Offsets.Equipment);
		public unsafe static Weapon MainHandModels(this SubKindsPlayerCharacter playerCharacter)
			=> *(Weapon*)(playerCharacter.Address + Offsets.WeaponMainHand);
		public unsafe static Weapon OffHandModels(this SubKindsPlayerCharacter playerCharacter)
			=> *(Weapon*)(playerCharacter.Address + Offsets.WeaponOffHand);

		public static void Equip(this SubKindsPlayerCharacter playerCharacter, EquipIndex index, ItemEquip item) {
			if (Methods.ActorChangeEquip == null) return;
			Methods.ActorChangeEquip(playerCharacter.Address + Offsets.ActorDrawData, index, item);
		}

		public static void Equip(this SubKindsPlayerCharacter playerCharacter, WeaponIndex slot, WeaponEquip item) {
			if (Methods.ActorChangeWeapon == null) return;
			Methods.ActorChangeWeapon(playerCharacter.Address + Offsets.ActorDrawData, slot, item, 0, 1, 0, 0);
		}
	}
}
