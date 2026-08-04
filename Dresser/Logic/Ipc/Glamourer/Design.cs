using System;
using System.Collections.Generic;
using System.Linq;

using Dresser.Extensions;
using Dresser.Interop.Agents;
using Dresser.Models;

using Lumina.Excel.Sheets;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Dresser.Logic.Ipc.Glamourer;

public static class Design {


	public static string PrepareDesign(JObject design, InventoryItemSet set) {

		design["Name"] = "DresserAnywhere Auto Apply";
		design["Description"] = "DresserAnywhere Auto Apply";

		design["Identifier"] = Guid.NewGuid();
		design["CreationDate"] = new DateTimeOffset();
		design["LastEdit"] = new DateTimeOffset();
		design["WriteProtected"] = false;

		TurnOffAllApplies(ref design);
		SerializeEquipment(ref design, set);

		return ShareBase64(design);
	}

	public static void SerializeEquipment(ref JObject design, InventoryItemSet set) {
		var ret = (JObject?)design["Equipment"];
		if (ret == null) return;

		bool overwritesOffhand = false;

		//var mainhandOfSet = set.GetSlot(GlamourPlateSlot.MainHand);
		//if (mainhandOfSet == null || !mainhandOfSet.Item.CanBeEquipedByPlayedJob()) {
		//	set.SetSlot(GlamourPlateSlot.MainHand,new(CriticalCommonLib.Enums.InventoryType.Bag0,Service.ExcelCache.AllItems.First(i => i.Value.CanBeEquipedByPlayedJob()).Value.RowId));
		//}

		foreach ((var slot, var item) in set.Items) {
			if (item == null) continue; // if null, leave empty to let it be filled with empty + not apply
			if (slot == GlamourPlateSlot.OffHand && overwritesOffhand) continue;

			// if item id == 0, make it empty and apply
			// else display the item
			var itemRow = item.ToItemRow();

			CustomItemId? mainItem = null;
			if (item.ItemId == 0) mainItem = NothingId(slot.ToPenumbraEquipSlot()).Id;
			else {
				var designDict = FromInventoryItem(itemRow, slot, ret);

				if(designDict.Count > 0) {
					var designCustomIdPair = designDict.First();
					// var ddsq = designCustomIdPair.Value;
					// var dddd = ddsq.Item.Id;
					mainItem = designCustomIdPair.Value.Id;
					
				} else mainItem = NothingId(slot.ToPenumbraEquipSlot()).Id;
			}

			//var hackedId = mainItem.Id | 1ul << 48;

			//var ddd = Service.ExcelCache.AllItems.Where(p => p.Value.RowId == 38081).First().Value;
			//PluginLog.Debug($"bsqd=> {ddd.NameString} => {ddd.ModelMain} <> {mainItem.Id}");
			//mainItem = 38081;


			if (mainItem != null) {
				ret[slot.ToPenumbraEquipSlot().ToString()] = SerializeItem(mainItem.Value, item.Stain, false, true, true, false);
				if (slot == GlamourPlateSlot.MainHand) {
					//&& !item.Item.IsMainModelOnOffhand()
					var sameItemOnOffhand = item.Item.ToFullEquipType(false).IsOffhandType();

					if (sameItemOnOffhand) {
						ret[EquipSlot.OffHand.ToString()] = SerializeItem(EquipItem.FromOffhand(itemRow).Id, item.Stain, false, true, true, false);
						overwritesOffhand = true;
					}
				}

			}

		}

		ret["Hat"] = SerializeToggles("Show", ConfigurationManager.Config.CurrentGearDisplayHat, true);
		ret["Visor"] = SerializeToggles("IsToggled", ConfigurationManager.Config.CurrentGearDisplayVisor, true);
		ret["Weapon"] = SerializeToggles("Show", ConfigurationManager.Config.CurrentGearDisplayWeapon, true);

		design["Equipment"] = ret;

	}

	public static Dictionary<EquipSlot,CustomItemId> FromInventoryItem(Item item, GlamourPlateSlot slot, JObject? equipmentDesign = null) {
		var equipItem = slot switch {
			GlamourPlateSlot.MainHand  => EquipItem.FromMainhand(item),
			GlamourPlateSlot.OffHand => EquipItem.FromOffhand(item),
			_ => EquipItem.FromArmor(item),
		};
		var penumbraEquipSlot = slot.ToPenumbraEquipSlot();
		var returningItemIds = new Dictionary<EquipSlot, CustomItemId>();


		// if (equipItem.Type.AllowsNothing()) return null;

		if (slot == GlamourPlateSlot.OffHand && item.RowId == 0)
		{
			// prevent inserting an item in offhand if it is not allowed
			var mainhandItem = PluginServices.Glamourer.GetMainHandItem();
			if (mainhandItem != null)
			{
				var mainHandEquipItem = EquipItem.FromMainhand(mainhandItem.Value);
				var validOffhand = mainHandEquipItem.Type.Offhand().AllowsNothing();
				if (!validOffhand) return new();
			}
		}

		CustomItemId? possibleOffhand = null;
		if (slot == GlamourPlateSlot.MainHand && item.RowId != 0)
		{
			var mainHandEquipItem = EquipItem.FromMainhand(item);
			PluginLog.Debug($"Check if MainHand is equippable {mainHandEquipItem.Type}");
			// verify if item is compatible with job
			if(!item.CanBeEquipedByPlayedJob()) return new();
			// also put offhand if it is including an offhand
			PluginLog.Debug("Yes");

			PluginLog.Debug("Check if OffHand should be added");
			var offHandEquipItem = mainHandEquipItem.Type.Offhand();
			if (offHandEquipItem.IsOffhandType())
			{
				PluginLog.Debug("Yes");
				possibleOffhand = item.RowId;
				// possibleOffhand = new CustomItemId(equipItem.PrimaryId, equipItem.SecondaryId, equipItem.Variant, equipItem.Type);
			}
		}

		// if (slot == GlamourPlateSlot.OffHand || slot == GlamourPlateSlot.MainHand)
		// {
		//
		// 	PluginLog.Debug($"tried to equip {equipItem.Type.ToName()} on {slot} with {item.RowId}");
		// 	return new ();
		// }

		//if (slot == GlamourPlateSlot.MainHand) {
		//	PluginLog.Debug($"mainhand => {equipItem.ModelId}, {equipItem.WeaponType}, {equipItem.Variant}, {equipItem.Type}");
		//}

		//var manualUlong = equipItem.ModelId.Id | (ulong)equipItem.WeaponType.Id << 16 | (ulong)equipItem.Variant.Id << 32;
		//PluginLog.Debug($"weapon => {manualUlong}");
		//manualUlong |= (ulong)equipItem.Type << 42;
		//PluginLog.Debug($"weapon + type => {manualUlong}");
		returningItemIds.Add(penumbraEquipSlot,
			item.RowId
			// item.RowId == 0 ? NothingId(penumbraEquipSlot) : new CustomItemId(equipItem.PrimaryId, equipItem.SecondaryId, equipItem.Variant, equipItem.Type)
			);
		if(possibleOffhand != null) returningItemIds.Add(EquipSlot.OffHand, possibleOffhand.Value);
		return returningItemIds;
		//mainItem = new CustomItemId(manualUlong);

		//if (slot == GlamourPlateSlot.MainHand) {
		//	PluginLog.Debug($"mainhand ({slot})>({penumbraEquipSlot}) => {mainItem.Id} > {mainItem.Item.Id}, {(mainItem.IsItem ? "is": "not")} an item");
		//}
		//PluginLog.Debug($"new  = > jr:{equipItem.JobRestrictions}");



		// below is attempt to check if weapon should be set or now, depending on item's weapon type and current's glamourer weapon type
		if (equipmentDesign != null && equipmentDesign.TryGetValue(penumbraEquipSlot.ToString(), out var slotObject)) {

			var id = (ulong)(((JObject?)slotObject)?["ItemId"] ?? 0);
			var cId = new CustomItemId(id);
			//var eIt = EquipItem.FromId(cId);

			//ulong CustomFlag = 1ul << 48;

			//var issss = cId.Id < CustomFlag;
			//var dddsqs = ((SetId)cId.Id, (WeaponType)(cId.Id >> 16), (Variant)(cId.Id >> 32), (FullEquipType)(cId.Id >> 40));


			//var foundItemOld = InventoryItem.FromModelMain(id, slot);


			//PluginLog.Debug($"old = >  id:{id} | {cId.Item}");
			//PluginLog.Debug($"old = > jr:{eIt.JobRestrictions}  cId:{cId} => {(issss?"YesItem":"notItem")} {dddsqs} <>>>>>>>> {foundItemOld?.ModelMain} :> {foundItemOld?.NameString}");
			//var fd = eIt
			//if (cId)


			//EquipItem.FromId().Type == EquipSlot.MainHand

		}

		return returningItemIds;

	}

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


	public static string ShareBase64(JObject jObject) {
		var json = jObject.ToString(Formatting.None);
		var compressed = json.Compress(6);
		return Convert.ToBase64String(compressed);
	}
	public static JObject FromBase64(string base64String) {
		byte[] byteArray = Convert.FromBase64String(base64String);
		byteArray.DecompressToString(out var json);
		return JObject.Parse(json);
	}
	public static JObject FromBase64v6(string base64) {
		var bytes = System.Convert.FromBase64String(base64);

		var version1 = bytes[0];
		PluginLog.Debug($"Detected glamourer design version {version1}");
		// if(version1 == 5) {
		// 	var Base64SizeV4 = 95;
		// 	bytes   = bytes[Base64SizeV4..];
		// }
		var version2 = bytes.DecompressToString(out var decompressed);
		// PluginLog.Debug($"json:\n{decompressed}");
		// PluginLog.Debug($"Detected glamourer design version {version1} => {version2}");
		var jObj2 = JObject.Parse(decompressed);
		return jObj2;
	}
	public static void TurnOffAllApplies(ref JObject json) {

		foreach (var property in json.Properties()) {
			if (property.Name == "Apply" || property.Name == "ApplyStain" || property.Name == "ApplyCrest") {
				property.Value = false;
			}

			if (property.Value.Type == JTokenType.Object) {
				var value = (JObject)property.Value;
				TurnOffAllApplies(ref value);
			} else if (property.Value.Type == JTokenType.Array) {
				foreach (var item in (JArray)property.Value) {
					if (item.Type == JTokenType.Object) {
						var value = (JObject)item;
						TurnOffAllApplies(ref value);
					}
				}
			}
		}
	}

	public static ItemId NothingId(EquipSlot slot)
	=> slot switch  {
		EquipSlot.MainHand => 1601,
		EquipSlot.OffHand => 4294966874,
		_ => uint.MaxValue - 128 - (uint)slot.ToSlot(),
	};
	//  slot == EquipSlot.MainHand ? 1601 : uint.MaxValue - 128 - (uint)slot.ToSlot();

	public static ItemId SmallclothesId(EquipSlot slot)
		=> uint.MaxValue - 256 - (uint)slot.ToSlot();

	public static ItemId NothingId(FullEquipType type)
		=> uint.MaxValue - 384 - (uint)type;


	public static uint DesignIdToItemId(ulong key, GlamourPlateSlot slot) {
		EquipSlot? equipSlot;
		try {
			equipSlot = slot.ToPenumbraEquipSlot();
		} catch (Exception) {
			equipSlot = null;
		}
		return DesignIdToItemId(key, equipSlot);
	}
	public static uint DesignIdToItemId(ulong key, EquipSlot? slot = null) {

		// kinda known to be empty, for offhands?
		
		var customId = (CustomItemId) key;
		var itemId = customId.Item.StripModifiers.Id;

		// TODO: there seems to be a bug, maybe in glamourer
		//    I managed to get this head item id  "ItemId": 282574488338433,
		//    By doing the manipulation (with Glamourer alone) use design > restore to automaton
		//    It shows the head item as "Unknown (1-0)" and when Dresser tries to parse the huge number, it produces errors
		//
		//    The slot should be empty anyway
		//    And casting it to CustomItemId seems to put the id to 0, which is what we want


		// seems like these are empty slots, maybe for different items
		if(key > 4294966000ul && key < 4294999999ul) {
			if (ConfigurationManager.Config.EnableVerboseGlamourerIpc) {
				var offset = (long)key - uint.MaxValue;
				PluginLog.Debug($"[Design Item ID Debug] found near max value with offset: {offset}");
			}
			itemId = 0;
		}


		if(ConfigurationManager.Config.EnableVerboseGlamourerIpc && (uint)key != itemId) {
			PluginLog.Debug($"[Design Item ID Debug] Converted {key} => {itemId} (split: {customId.Split})");			
		}
		return itemId;
	}
	public static ulong ItemIdToDesignId(Item item, GlamourPlateSlot slot) {
		if(slot == GlamourPlateSlot.MainHand) return IdentificationListWeapons.ToKey(EquipItem.FromMainhand(item));
		if(slot == GlamourPlateSlot.OffHand) return IdentificationListWeapons.ToKey(EquipItem.FromOffhand(item));
		return IdentificationListEquipment.ToKey(EquipItem.FromArmor(item));
	}

}
