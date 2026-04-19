using CriticalCommonLib.Extensions;

using Dresser.Interop.Hooks;
using Dresser.Models;

using System;

using static Dresser.Services.Storage;

using CriticalInventoryCategory = CriticalCommonLib.Models.InventoryCategory;
using CriticalInventoryType = CriticalCommonLib.Enums.InventoryType;

namespace Dresser.Extensions {
	public static class SlotsEnumExtensions {
		public static Penumbra.GameData.Enums.EquipSlot ToPenumbraEquipSlot(this GlamourPlateSlot slot) {
			return slot switch {
				GlamourPlateSlot.MainHand => Penumbra.GameData.Enums.EquipSlot.MainHand,
				GlamourPlateSlot.OffHand => Penumbra.GameData.Enums.EquipSlot.OffHand,
				GlamourPlateSlot.Head => Penumbra.GameData.Enums.EquipSlot.Head,
				GlamourPlateSlot.Body => Penumbra.GameData.Enums.EquipSlot.Body,
				GlamourPlateSlot.Hands => Penumbra.GameData.Enums.EquipSlot.Hands,
				GlamourPlateSlot.Legs => Penumbra.GameData.Enums.EquipSlot.Legs,
				GlamourPlateSlot.Feet => Penumbra.GameData.Enums.EquipSlot.Feet,
				GlamourPlateSlot.Ears => Penumbra.GameData.Enums.EquipSlot.Ears,
				GlamourPlateSlot.Neck => Penumbra.GameData.Enums.EquipSlot.Neck,
				GlamourPlateSlot.Wrists => Penumbra.GameData.Enums.EquipSlot.Wrists,
				GlamourPlateSlot.RightRing => Penumbra.GameData.Enums.EquipSlot.RFinger,
				GlamourPlateSlot.LeftRing => Penumbra.GameData.Enums.EquipSlot.LFinger,
				_ => throw new Exception($"Unidentified GlamourPlateSlot: {slot}")
			};
		}
		public static GlamourPlateSlot ToGlamourPlateSlot(this Penumbra.GameData.Enums.EquipSlot slot) {
			return slot switch {
				Penumbra.GameData.Enums.EquipSlot.MainHand => GlamourPlateSlot.MainHand,
				Penumbra.GameData.Enums.EquipSlot.OffHand => GlamourPlateSlot.OffHand,
				Penumbra.GameData.Enums.EquipSlot.Head => GlamourPlateSlot.Head,
				Penumbra.GameData.Enums.EquipSlot.Body => GlamourPlateSlot.Body,
				Penumbra.GameData.Enums.EquipSlot.Hands => GlamourPlateSlot.Hands,
				Penumbra.GameData.Enums.EquipSlot.Legs => GlamourPlateSlot.Legs,
				Penumbra.GameData.Enums.EquipSlot.Feet => GlamourPlateSlot.Feet,
				Penumbra.GameData.Enums.EquipSlot.Ears => GlamourPlateSlot.Ears,
				Penumbra.GameData.Enums.EquipSlot.Neck => GlamourPlateSlot.Neck,
				Penumbra.GameData.Enums.EquipSlot.Wrists => GlamourPlateSlot.Wrists,
				Penumbra.GameData.Enums.EquipSlot.RFinger => GlamourPlateSlot.RightRing,
				Penumbra.GameData.Enums.EquipSlot.LFinger => GlamourPlateSlot.LeftRing,
				_ => throw new Exception($"Unidentified EquipSlot: {slot}")
			};
		}
		public static string ToFormattedName(this CriticalInventoryType type) {
			if ((InventoryTypeExtra)type >= InventoryTypeExtra.AllItems) return ((InventoryTypeExtra)type).ToString().AddSpaceBeforeCapital();
			return type switch {
				CriticalInventoryType.Bag0 => "Block: [1]",
				CriticalInventoryType.Bag1 => "Block: [2]",
				CriticalInventoryType.Bag2 => "Block: [3]",
				CriticalInventoryType.Bag3 => "Block: [4]",

				//CriticalInventoryType.GearSet0 => "",
				//CriticalInventoryType.GearSet1 => "",

				//CriticalInventoryType.Currency => "",
				//CriticalInventoryType.Crystal => "",
				//CriticalInventoryType.Mail => "",
				//CriticalInventoryType.KeyItem => "",
				//CriticalInventoryType.HandIn => "",
				//CriticalInventoryType.DamagedGear => "",
				//CriticalInventoryType.UNKNOWN_2008 => "",
				//CriticalInventoryType.Examine => "",

				//Custom
				//CriticalInventoryType.Armoire => "",
				//CriticalInventoryType.GlamourChest => "",
				//CriticalInventoryType.FreeCompanyCurrency => "",

				CriticalInventoryType.ArmoryOff => "Off Hand",
				CriticalInventoryType.ArmoryHead => "Head",
				CriticalInventoryType.ArmoryBody => "Body",
				CriticalInventoryType.ArmoryHand => "Hands",
				CriticalInventoryType.ArmoryWaist => "Waist",
				CriticalInventoryType.ArmoryLegs => "Legs",
				CriticalInventoryType.ArmoryFeet => "Feet",
				CriticalInventoryType.ArmoryEar => "Ears",
				CriticalInventoryType.ArmoryNeck => "Neck",
				CriticalInventoryType.ArmoryWrist => "Wrists",
				CriticalInventoryType.ArmoryRing => "Rings",

				CriticalInventoryType.ArmorySoulCrystal => "Soul Crystal",
				CriticalInventoryType.ArmoryMain => "Main Hand",

				CriticalInventoryType.SaddleBag0 => "Block: [1]",
				CriticalInventoryType.SaddleBag1 => "Block: [2]",
				//CriticalInventoryType.PremiumSaddleBag0 => "",
				//CriticalInventoryType.PremiumSaddleBag1 => "",

				CriticalInventoryType.RetainerBag0 => "Block: [1]",
				CriticalInventoryType.RetainerBag1 => "Block: [2]",
				CriticalInventoryType.RetainerBag2 => "Block: [3]",
				CriticalInventoryType.RetainerBag3 => "Block: [4]",
				CriticalInventoryType.RetainerBag4 => "Block: [5]",
				CriticalInventoryType.RetainerBag5 => "Block: [6]",
				CriticalInventoryType.RetainerBag6 => "Block: [7]",
				//CriticalInventoryType.RetainerEquippedGear => "",
				//CriticalInventoryType.RetainerGil => "",
				//CriticalInventoryType.RetainerCrystal => "",
				//CriticalInventoryType.RetainerMarket => "",

				CriticalInventoryType.FreeCompanyBag0 => "Block: [1]",
				CriticalInventoryType.FreeCompanyBag1 => "Block: [2]",
				CriticalInventoryType.FreeCompanyBag2 => "Block: [3]",
				CriticalInventoryType.FreeCompanyBag3 => "Block: [4]",
				CriticalInventoryType.FreeCompanyBag4 => "Block: [5]",
				CriticalInventoryType.FreeCompanyBag5 => "Block: [6]",
				CriticalInventoryType.FreeCompanyBag6 => "Block: [7]",
				CriticalInventoryType.FreeCompanyBag7 => "Block: [8]",
				CriticalInventoryType.FreeCompanyBag8 => "Block: [9]",
				CriticalInventoryType.FreeCompanyBag9 => "Block: [10]",
				CriticalInventoryType.FreeCompanyBag10 => "Block: [11]",
				//CriticalInventoryType.FreeCompanyGil => "",
				//CriticalInventoryType.FreeCompanyCrystal => "",

				_ => type.ToString().AddSpaceBeforeCapital()
			};
		}
		public static string ToFriendlyName(this CriticalInventoryCategory cat) {
			return cat switch {
				CriticalInventoryCategory.CharacterBags => "Inventory",
				CriticalInventoryCategory.RetainerBags => "Retainers Inventory",
				CriticalInventoryCategory.RetainerEquipped => "Retainers Equipped",
				CriticalInventoryCategory.RetainerMarket => "Retainers Market",
				_ => cat.FormattedName()
			};
		}
	}
}
