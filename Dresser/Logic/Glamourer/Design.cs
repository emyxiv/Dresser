using System;
using System.Collections.Generic;

using Dresser.Extensions;
using Dresser.Interop.Hooks;

using Lumina.Excel.Sheets;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Dresser.Logic.Glamourer;

public static class Design {

/*
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

			CustomItemId? mainItem = null;
			if (item.ItemId == 0) mainItem = NothingId(slot.ToPenumbraEquipSlot()).Id;
			else mainItem = FromInventoryItem(item.Item, slot, ret);

			//var hackedId = mainItem.Id | 1ul << 48;

			//var ddd = Service.ExcelCache.AllItems.Where(p => p.Value.RowId == 38081).First().Value;
			//PluginLog.Debug($"bsqd=> {ddd.NameString} => {ddd.ModelMain} <> {mainItem.Id}");
			//mainItem = 38081;


			if (mainItem != null) {
				ret[slot.ToPenumbraEquipSlot().ToString()] = SerializeItem(mainItem.Value, item.Stain, false, true, true, false);
				if (slot == GlamourPlateSlot.MainHand) {
					//&& !item.Item.IsMainModelOnOffhand()
					var sameItemOnOffhand = item.Item.ToFullEquipType2(false).IsOffhandType();

					if (sameItemOnOffhand) {
						ret[EquipSlot.OffHand.ToString()] = SerializeItem(EquipItem.FromOffhand(item.Item).Id, item.Stain, false, true, true, false);
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
*/
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


	private static string ShareBase64(JObject jObject) {
		var json = jObject.ToString(Formatting.None);
		var compressed = json.Compress(6);
		return Convert.ToBase64String(compressed);
	}
	public static JObject FromBase64(string base64String) {
		byte[] byteArray = Convert.FromBase64String(base64String);
		byteArray.DecompressToString(out var json);
		return JObject.Parse(json);
	}
	static void TurnOffAllApplies(ref JObject json) {

		var propertyName = "Apply";
		foreach (var property in json.Properties()) {
			if (property.Name == propertyName) {
				property.Value = false;
			}

			if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array) {
				var value = (JObject)property.Value;
				TurnOffAllApplies(ref value);
			}
		}
	}

	public static ItemId NothingId(EquipSlot slot)
	=> uint.MaxValue - 128 - (uint)slot.ToSlot();

	public static ItemId SmallclothesId(EquipSlot slot)
		=> uint.MaxValue - 256 - (uint)slot.ToSlot();

	public static ItemId NothingId(FullEquipType type)
		=> uint.MaxValue - 384 - (uint)type;


}
