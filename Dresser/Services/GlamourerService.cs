using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Logic.Glamourer;
using Dresser.Structs.Dresser;

using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;

using Lumina.Excel.Sheets;

using Newtonsoft.Json.Linq;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Dresser.Services {
	internal class GlamourerService : IDisposable {


		private const bool EnableAllApply = true;
		private const bool EnableWeaponsApply = true;
		Throttler<Task> _throttler;

		private ApiVersion ApiVersionSubscriber;
		private SetItem SetItemSubscriber;
		private RevertState RevertStateSubscriber;
		private RevertToAutomation RevertToAutomationSubscriber;

		private GetState GetStateSubscriber;
		private ApplyState ApplyStateSubscriber;

		public GlamourerService(IDalamudPluginInterface pluginInterface) {

			ApiVersionSubscriber = new ApiVersion(pluginInterface);
			SetItemSubscriber = new SetItem(pluginInterface);
			RevertStateSubscriber = new RevertState(pluginInterface);
			RevertToAutomationSubscriber = new RevertToAutomation(pluginInterface);
			GetStateSubscriber = new GetState(pluginInterface);
			ApplyStateSubscriber = new ApplyState(pluginInterface);
			_throttler = new Throttler<Task>(0);

		}
		public void Dispose() {}

		public bool IsInitialized() { try { return ApiVersions().Major >= 0; } catch (Exception) { return false; } }
		public (int Major, int Minor) ApiVersions() { try { return ApiVersionSubscriber.Invoke(); } catch (Exception) { return (-1, -1); } }

		public void RevertCharacter(ICharacter? character) { if (character == null) return; try { RevertStateSubscriber.Invoke(character.ObjectIndex); } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertCharacter"); } }
		public bool RevertToAutomationCharacter(ICharacter? character) { if (character == null) return false; try { return RevertToAutomationSubscriber.Invoke(character.ObjectIndex) == GlamourerApiEc.Success; } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertToAutomation"); return false; } }

		public JObject? GetState() {
			var index = PluginServices.Context.LocalPlayer?.ObjectIndex;
			if(index == null) return null;

			(GlamourerApiEc response, JObject? charaState) result = GetStateSubscriber.Invoke((int)index);
			if(result.response != GlamourerApiEc.Success) return null;

			return result.charaState;
		}
		public Item? GetMainHandItem()
		{
			var state = GetState();
			var equipment = (uint?)(state?["Equipment"]?["MainHand"]?["ItemId"]);
			state?.Remove("Customize");
			state?.Remove("Parameters");

			PluginLog.Debug($"test GetMainHandInfo: {equipment}");

			if (equipment != null)
			{
				return PluginServices.DataManager.GetExcelSheet<Item>().GetRowOrDefault(equipment.Value);
			}

			return null;
		}
		public InventoryItemSet GetSet()
		{
			var set = new InventoryItemSet();
			var state = GetState();
			var equipJson = state?["Equipment"];
			if (equipJson == null) return set;

			foreach (var slot in Enum.GetValues<GlamourPlateSlot>())
			{
				var itemJson = equipJson?[slot.ToPenumbraEquipSlot().ToString()];
				InventoryItem item;

				var itemId = ((((uint?)itemJson?["ItemId"]) ?? 0));
				if (itemId is > 4294967100u or 0) item = InventoryItem.Zero;
				else item = InventoryItemExtensions.New(
					itemId,
					((byte?)itemJson?["Stain"]) ?? 0,
					((byte?)itemJson?["Stain2"]) ?? 0
					);

				if (slot == GlamourPlateSlot.OffHand) {
					var fifif = set.GetSlot(GlamourPlateSlot.MainHand);
					if (fifif != null && !EquipItem.FromOffhand(fifif.Item.Base).Type.AllowsNothing()) {
						continue;
					}
				}

				// PluginLog.Debug($"test GetSet item: {slot.ToPenumbraEquipSlot()}: {itemId} => {item.ItemId}");
				set.SetSlot(slot, item);
			}
			return set;
		}

		private void RecursivelyModifyParam(JToken token, string key, string newValue) {
			if (token.HasValues) {
				foreach (JToken child in token.Children()) {
					RecursivelyModifyParam(child, key, newValue);
				}
			} else if (token.Parent != null && token.ToString() == key) {
				token.Parent[key] = newValue;
			}
		}


		private ulong NothingOrItem(EquipSlot slot, CustomItemId itemId)
		{
				var iid = itemId.Id;
				if (iid == 0) iid = (ulong)Design.NothingId(slot).Id;
				return iid;
		}

		// //////////////////////////////////////
		// Apply stuff
		// //////////////////////////////////////

		private bool ModifyAndSendState(ICharacter character, Func<JObject, JObject?> callback)
		{
			var originalState = GetState();
			if (originalState == null) return false;
			if (!EnableAllApply) return true;

			_throttler.Throttle(() =>
			{
				// PluginLog.Warning($"                         ---        Set State 2     ---                                   \n{new StackTrace()}");
				return PluginServices.Framework.RunOnFrameworkThread(() =>
				{
					var newState = callback.Invoke(originalState);
					if(newState == null) return;
					ApplyMetaDataToState(ref newState,[]);
					ApplyStateSubscriber.Invoke(newState, character.ObjectIndex, 0U, ApplyFlag.Equipment |  ApplyFlag.Customization  | ApplyFlag.Once);
				});
			});
			return true;
		}
		private void SetItem(ICharacter character, EquipSlot slot, CustomItemId itemId, byte stainId, byte stainId2) {
			try {
				_throttler.Throttle(() =>
				{
					// PluginLog.Warning($"                         ---        SetItem      ---                                   \n{new StackTrace()}");
					// if (!EnableAllApply) return;
					return PluginServices.Framework.RunOnFrameworkThread(() =>
					{
						SetItemSubscriber.Invoke(character.ObjectIndex, (ApiEquipSlot)slot, NothingOrItem(slot, itemId), new List<byte>() {stainId, stainId2});
					});

				});
			} catch (Exception e) {
				PluginLog.Error(e, "Failed to contact SetItem");
			}
		}
		public bool SetSet(ICharacter character, InventoryItemSet set)
		{

			return ModifyAndSendState(character, state =>
			{

				var backedUpItems = PluginServices.ApplyGearChange.GetBackedUpAppearance();
				Dictionary<EquipSlot, CustomItemId> customItems = new Dictionary<EquipSlot, CustomItemId>();
				var zffff = Enum.GetValues<GlamourPlateSlot>();
				foreach (var slot in Enum.GetValues<GlamourPlateSlot>())
				{
					if(!EnableWeaponsApply) if(slot is GlamourPlateSlot.MainHand or GlamourPlateSlot.OffHand) continue;
					var item = set.GetSlot(slot);

					if (item == null || item.ItemId == 0)
					{
						// some logic for now we make it all 0
						// todo    change null and 0 to nakey/display
						if (ConfigurationManager.Config.CurrentGearDisplayGear)
						{

							item = backedUpItems?.GetSlot(slot) ?? InventoryItem.Zero;


							// var ofdsf = (backedUpItems?.Items.Select(p => p.Value?.ItemId ?? 0) ?? new List<uint>());
							// var dddsqsdqs = string.Join(",",(ofdsf));
							// PluginLog.Debug($"choose {item.ItemId} ({dddsqsdqs})");
							// PluginLog.Debug($"choose {item.ItemId} ({backedUpItems?.Items.Count})");
						}
						else
						{
							item = InventoryItem.Zero;

						}

					}


					var customeItemIdsForItem = Design.FromInventoryItem(item.Item.Base, slot);
					foreach ((var s,var ci) in customeItemIdsForItem)
					{
						if (!customItems.ContainsKey(s))
						{
							try
							{
								state!["Equipment"]![s.ToString()]!["ItemId"] = NothingOrItem(s, ci).ToString();
								state!["Equipment"]![s.ToString()]!["Apply"] = "true";
								state!["Equipment"]![s.ToString()]!["Stain"] = item.Stain.ToString();
								state!["Equipment"]![s.ToString()]!["Stain2"] = item.Stain2.ToString();
								customItems.Add(s, ci);
							}
							catch (Exception e)
							{
								PluginLog.Error(e, $"Failed create Glamourer entry for {s} ({item.FormattedName})");
							}
						}
					}
				}

				return state;
			});
		}

		public void SetItem(ICharacter character, InventoryItem item, GlamourPlateSlot slot)
		{
			// var ss = FullEquipType.AllowsNothing();
			// var equipItems = item.Item.ToCustomItemId(slot);
			// if(equipItem.Value) return false;
			var customItemIds = item.Item.ToCustomItemId(slot);
			if (customItemIds.Count > 1)
			{
				if(!EnableWeaponsApply) if (customItemIds.Any(p => p.Key is EquipSlot.MainHand or EquipSlot.OffHand)) return;
				_throttler.Throttle(() =>
				{
					// PluginLog.Warning($"                         ---        Set State 1     ---                                   \n{new StackTrace()}");
					if(!EnableAllApply) return new Task(()=>{});

					return PluginServices.Framework.RunOnFrameworkThread(() =>
					{
						var items = customItemIds.ToDictionary(f => f.Key, f => f.Value.Item.Id);
						DesignWithMod(character, items);
					});

				});
				return;

				// if (design == null) return GlamourerApiEc.DesignNotFound;
				// ApplyStateSubscriber.Invoke(design, character.ObjectIndex);

			}

			foreach ((var equipSlot, var equipItem) in customItemIds)
			{
				SetItem(character, equipSlot, equipItem, item.Stain, item.Stain2);
			}
			return;
		}




		public enum MetaData {
			Hat,
			Visor,
			Weapon,
		}
		public bool SetMetaData(ICharacter character, MetaData metaData, bool? forceState = null) {
			return SetMetaData(character, new() { { metaData, forceState } });
		}
		public bool? GetMetaData(MetaData metaData) {
			var state = GetState();
			if (state == null) return null;

			var showFieldName = metaData == MetaData.Visor ? "IsToggled" : "Show";
			return (bool?)state?["Equipment"]?[metaData.ToString()]?[showFieldName];
		}

		public bool SetMetaData(ICharacter character, Dictionary<MetaData, bool?> metaDataStates) {

			return ModifyAndSendState(character, state =>
			{
				// RecursivelyModifyParam(state, "Apply", "true");
				ApplyMetaDataToState(ref state, metaDataStates);

				return state;
			});

		}
		private void ApplyMetaDataToState(ref JObject state, Dictionary<MetaData, bool?> metaDataStates)
		{
			metaDataStates = metaDataStates.Count != 0 ?  metaDataStates : new Dictionary<MetaData, bool?>()
			{
				{MetaData.Weapon, ConfigurationManager.Config.CurrentGearDisplayWeapon},
				{MetaData.Hat, ConfigurationManager.Config.CurrentGearDisplayHat},
				{MetaData.Visor, ConfigurationManager.Config.CurrentGearDisplayVisor},
			};

			foreach ((var metaData, var forceState) in metaDataStates) {

				var showFieldName = metaData == MetaData.Visor ? "IsToggled" : "Show";
				var newValue = forceState ?? !(((bool?)state?["Equipment"]?[metaData.ToString()]?[showFieldName]) ?? false);
				try {
					state!["Equipment"]![metaData.ToString()]![showFieldName] = newValue ? "true" : "false";
					// state!["Equipment"]![metaData.ToString()]!["Apply"] = "true";
				} catch (Exception e) {
					PluginLog.Error(e, $"Failed create Glamourer entry for {metaData}");
				}
			}
		}

		public bool DesignWithMod(ICharacter character, Dictionary<EquipSlot,uint> items)
		{
			// return false;
			return ModifyAndSendState(character, state =>
			{
				// RecursivelyModifyParam(state!, "Apply", "true");

				foreach ((var slot, var itemId) in items)
				{
					try {
						// PluginLog.Debug($"test DesignWithMod {slot.ToString()} =| {state!["Equipment"]![slot.ToString()]!["ItemId"]}");
						state!["Equipment"]![slot.ToString()]!["ItemId"] = itemId.ToString();
						// state!["Equipment"]![slot.ToString()]!["Apply"] = "true";
						// PluginLog.Debug($"TO   DesignWithMod {slot.ToString()} => {itemId}");
						// PluginLog.Debug($"TO   DesignWithMod {slot.ToString()} => {state!["Equipment"]![slot.ToString()]!["ItemId"]}");
					} catch (Exception e) {
						PluginLog.Error(e, $"Failed create Glamourer entry for {slot} ({itemId})");
					}
				}
				return state;
			});
		}
	}
}
