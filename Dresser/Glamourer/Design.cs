using CriticalCommonLib;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using System;
using System.Linq;

namespace Glamourer.Designs;

public class Design {


	public static string PrepareDesign(InventoryItemSet set)
		=> ShareBase64(JsonSerialize(set));
	public static JObject JsonSerialize(InventoryItemSet set) {
		var ret = new JObject() {
			["FileVersion"] = 1,
			["Identifier"] = new Guid(),
			["CreationDate"] = new DateTimeOffset(),
			["LastEdit"] = new DateTimeOffset(),
			["Name"] = "DresserAnywhere Auto Apply",
			["Description"] = "DresserAnywhere Auto Apply",
			["Color"] = string.Empty,
			//["Tags"] = Array.Empty<string>(),
			["WriteProtected"] = false,
			["Equipment"] = SerializeEquipment(set),
			["Customize"] = SerializeCustomize(),
			//["Mods"] = SerializeMods(),
		};
		return ret;
	}
	protected static JObject SerializeCustomize() {
		var ret = new JObject();
		foreach (var idx in Enum.GetValues<CustomizeIndex>()) {
			ret[idx.ToString()] = new JObject() {
				["Value"] = Default(idx),
				["Apply"] = false,
			};
		}
		return ret;
	}
	private static int Default(CustomizeIndex index) {
		return index switch {
			CustomizeIndex.Race => 1,
			CustomizeIndex.Clan => 1,
			CustomizeIndex.Face => 1,
			CustomizeIndex.Hairstyle => 1,
			_ => 0,
		};
	}
	protected static JObject SerializeEquipment(InventoryItemSet set) {
		var ret = new JObject();

		foreach((var slot, var item) in set.Items) {
			if(item == null) continue; // if null, leave empty to let it be filled with empty + not apply


			// if item id == 0, make it empty and apply
			// else display the item

			CustomItemId mainItem;
			if (item.ItemId == 0) mainItem = NothingId(slot.ToPenumbraEquipSlot()).Id;
			else {
				var equipItem = slot switch {
					GlamourPlateSlot.MainHand or GlamourPlateSlot.OffHand => EquipItem.FromMainhand(item.Item),
					_ => EquipItem.FromArmor(item.Item),
				};

				mainItem = new CustomItemId(equipItem.ModelId, equipItem.WeaponType, equipItem.Variant, equipItem.Type);
			}

			//var hackedId = mainItem.Id | 1ul << 48;

			//var ddd = Service.ExcelCache.AllItems.Where(p => p.Value.RowId == 38081).First().Value;
			//PluginLog.Debug($"bsqd=> {ddd.NameString} => {ddd.ModelMain} <> {mainItem.Id}");
			//mainItem = 38081;
			ret[slot.ToPenumbraEquipSlot().ToString()] = SerializeItem(mainItem, item.Stain, true, true, true, true);
			if (slot == GlamourPlateSlot.MainHand && !item.Item.IsMainModelOnOffhand()) {
				ret[EquipSlot.OffHand.ToString()] = SerializeItem(EquipItem.FromOffhand(item.Item).Id, item.Stain, false, true, true, false);
			}

		}

		//PluginLog.Debug($"ggg => {ret.Count}");

		// fill empty stuff with empty and not apply
		foreach (var slotz in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand)) {
			if (!ret.ContainsKey(slotz.ToString())) {
				ret[slotz.ToString()] = SerializeItem(slotz == EquipSlot.OffHand ? NothingId(FullEquipType.Shield) : NothingId(slotz), 0, false, false, false, false);
			}
		}



		ret["Hat"] = SerializeToggles("Show", ConfigurationManager.Config.CurrentGearDisplayHat, true);
		ret["Visor"] = SerializeToggles("IsToggled", ConfigurationManager.Config.CurrentGearDisplayVisor, true);
		ret["Weapon"] = SerializeToggles("Show", ConfigurationManager.Config.CurrentGearDisplayWeapon, true);

		return ret;

	}




	/*private JArray SerializeMods() {
		var ret = new JArray();
		foreach (var (mod, settings) in AssociatedMods) {
			var obj = new JObject() {
				["Name"] = mod.Name,
				["Directory"] = mod.DirectoryName,
				["Enabled"] = settings.Enabled,
			};
			if (settings.Enabled) {
				obj["Priority"] = settings.Priority;
				obj["Settings"] = JObject.FromObject(settings.Settings);
			}

			ret.Add(obj);
		}

		return ret;
	}*/

	//protected static JObject SerializeEquipment() {

		//EquipSlotExtensions.EqdpSlots
		//var ret = new JObject();
		//foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand)) {
		//	var item = _designData.Item(slot);
		//	var stain = _designData.Stain(slot);
		//	var crestSlot = slot.ToCrestFlag();
		//	var crest = _designData.Crest(crestSlot);
			//ret[slot.ToString()] = Serialize(item.Id, stain, crest, DoApplyEquip(slot), DoApplyStain(slot), DoApplyCrest(crestSlot));
		//}


		//ret["Hat"] = SerializeToggles("Show", true, true);
		//ret["Visor"] = SerializeToggles("IsToggled", true, true);
		//ret["Weapon"] = SerializeToggles("Show", true, true);


		//return ret;
	//}
	public static JObject SerializeItem(CustomItemId id, StainId stain, bool crest, bool apply, bool applyStain, bool applyCrest)
	=> new() {
		["ItemId"] = id.Id,
		["Stain"] = stain.Id,
		["Crest"] = crest,
		["Apply"] = apply,
		["ApplyStain"] = applyStain,
		["ApplyCrest"] = applyCrest,
	};
	public static JObject SerializeToggles(string key, bool state, bool apply)
	=> new() {
		[key] = state,
		["Apply"] = apply,
	};


	private static string ShareBase64(JObject jObject) {
		var json = jObject.ToString(Formatting.None);
		var compressed = json.Compress(6);
		return System.Convert.ToBase64String(compressed);
	}

	public enum CustomizeIndex : byte {
		Race,
		Gender,
		BodyType,
		Height,
		Clan,
		Face,
		Hairstyle,
		Highlights,
		SkinColor,
		EyeColorRight,
		HairColor,
		HighlightsColor,
		FacialFeature1,
		FacialFeature2,
		FacialFeature3,
		FacialFeature4,
		FacialFeature5,
		FacialFeature6,
		FacialFeature7,
		LegacyTattoo,
		TattooColor,
		Eyebrows,
		EyeColorLeft,
		EyeShape,
		SmallIris,
		Nose,
		Jaw,
		Mouth,
		Lipstick,
		LipColor,
		MuscleMass,
		TailShape,
		BustSize,
		FacePaint,
		FacePaintReversed,
		FacePaintColor,
	}

	public static ItemId NothingId(EquipSlot slot)
	=> uint.MaxValue - 128 - (uint)slot.ToSlot();

	public static ItemId SmallclothesId(EquipSlot slot)
		=> uint.MaxValue - 256 - (uint)slot.ToSlot();

	public static ItemId NothingId(FullEquipType type)
		=> uint.MaxValue - 384 - (uint)type;


}
