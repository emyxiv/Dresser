using Dalamud.Game.ClientState.Objects.Types;

using Dresser.Logic;
using Dresser.Models;

using Glamourer.Api.Enums;


using Newtonsoft.Json.Linq;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using System;
using System.Collections.Generic;
using System.Linq;


namespace Dresser.Services.Ipc {
	internal partial class  GlamourerService{



		// Track last known state to detect external changes
		private JObject? _lastCachedState;
		private readonly object _cacheLock = new object();

		// Track changes initiated by our app to filter them out
		private HashSet<StateChangeType> _pendingLocalChanges = new();

		/// <summary>
		/// Refresh the cached state to match the provided player state.
		/// Call this when switching contexts (e.g., changing plates, opening windows).
		/// </summary>
		public void RefreshCachedState(JObject currentState) {
			PluginLog.Debug("Refreshing cached state");
			try {
				if (currentState != null) {
					lock (_cacheLock) {
						_lastCachedState = (JObject)currentState.DeepClone();
					}
				}
			} catch (Exception e) {
				PluginLog.Error(e, "Failed to refresh cached state");
			}
		}


		private void OnStateChangedWithType(nint gameObjectPtr, StateChangeType changeType) {
			if (gameObjectPtr != PluginServices.Context.LocalPlayer?.Address) return; // Only track changes for the local player
			if (!Plugin.GetInstance().CurrentGear.IsOpen) return; // Only track changes when our UI is open, to avoid unnecessary processing

			PluginLog.Debug($"StateChangedWithType: ChangeType: {changeType}");
			if (changeType != StateChangeType.Equip
				&& changeType != StateChangeType.Weapon
				&& changeType != StateChangeType.BonusItem
				&& changeType != StateChangeType.Stains
				&& changeType != StateChangeType.EntireCustomize
				) {
				return;
			}


			try {
				// Fetch current state
				var currentState = GetState();
				if (currentState == null) {
					PluginLog.Debug("Could not fetch state for external change");
					return;
				}

				lock (_cacheLock) {
					// Check if this change was initiated by our app
					bool isLocalChange = _pendingLocalChanges.Contains(changeType);
					if (isLocalChange) {
						_pendingLocalChanges.Remove(changeType);
						PluginLog.Verbose($"Ignoring local change: {changeType}");
						// Always update cache to stay in sync, even for local changes
						_lastCachedState = (JObject)currentState.DeepClone();
						return;
					}

					// This is an external change - check if state actually changed
					if (HasStateChanged(currentState, changeType)) {
						ProcessExternalStateChange(currentState, changeType);
						_lastCachedState = (JObject)currentState.DeepClone();
					} else {
						// State didn't actually change, just update cache
						_lastCachedState = (JObject)currentState.DeepClone();
					}
				}
			} catch (Exception e) {
				PluginLog.Error(e, $"Error processing state change: {changeType}");
			}
		}

		/// <summary>
		/// Compares current state with cached state to detect actual changes.
		/// </summary>
		private bool HasStateChanged(JObject currentState, StateChangeType changeType) {
			if (_lastCachedState == null) return true;
			try {
				switch (changeType) {
					case StateChangeType.Equip:
						return HasEquipmentChanged(currentState, _lastCachedState);
					case StateChangeType.Weapon:
						return HasWeaponChanged(currentState, _lastCachedState);
					case StateChangeType.Stains:
						return HasStainsChanged(currentState, _lastCachedState);
					case StateChangeType.BonusItem:
						return HasBonusItemChanged(currentState, _lastCachedState);
					case StateChangeType.EntireCustomize:
						return HasHairstyleChanged(currentState, _lastCachedState);
					default:
						return true;
				}
			} catch (Exception e) {
				PluginLog.Debug($"Error comparing states: {e.Message}");
				return true; // Assume changed on error
			}
		}

		private bool HasEquipmentChanged(JObject current, JObject last) {
			var currentEquip = current["Equipment"];
			var lastEquip = last["Equipment"];
			if (currentEquip == null || lastEquip == null) return false;

			foreach (var slot in Enum.GetValues<EquipSlot>()) {
				var slotName = slot.ToString();
				var curr = currentEquip[slotName];
				var prev = lastEquip[slotName];
				if (curr == null) continue;

				if ((uint?)curr["ItemId"] != (uint?)prev?["ItemId"] ||
					(byte?)curr["Stain"] != (byte?)prev?["Stain"] ||
					(byte?)curr["Stain2"] != (byte?)prev?["Stain2"]) {
					return true;
				}
			}
			return false;
		}

		private bool HasWeaponChanged(JObject current, JObject last) {
			var currentEquip = current["Equipment"];
			var lastEquip = last["Equipment"];
			if (currentEquip == null || lastEquip == null) return false;

			var mainHandCurr = (uint?)currentEquip["MainHand"]?["ItemId"] ?? 0;
			var mainHandLast = (uint?)lastEquip["MainHand"]?["ItemId"] ?? 0;
			var offHandCurr = (uint?)currentEquip["OffHand"]?["ItemId"] ?? 0;
			var offHandLast = (uint?)lastEquip["OffHand"]?["ItemId"] ?? 0;

			return mainHandCurr != mainHandLast || offHandCurr != offHandLast;
		}

		private bool HasStainsChanged(JObject current, JObject last) {
			var currentEquip = current["Equipment"];
			var lastEquip = last["Equipment"];
			if (currentEquip == null || lastEquip == null) return false;

			foreach (var slot in Enum.GetValues<EquipSlot>()) {
				var slotName = slot.ToString();
				var curr = currentEquip[slotName];
				var prev = lastEquip[slotName];
				if (curr == null) continue;

				if ((byte?)curr["Stain"] != (byte?)prev?["Stain"] ||
					(byte?)curr["Stain2"] != (byte?)prev?["Stain2"]) {
					return true;
				}
			}
			return false;
		}

		private bool HasBonusItemChanged(JObject current, JObject last) {
			var currentBonus = (ulong?)current["Equipment"]?["BonusItem"]?["BonusId"] ?? 0;
			var lastBonus = (ulong?)last["Equipment"]?["BonusItem"]?["BonusId"] ?? 0;
			return currentBonus != lastBonus;
		}

		private bool HasHairstyleChanged(JObject current, JObject last) {
			var currentHairstyle = (int?)current["Customize"]?["Hairstyle"]?["Value"];
			var lastHairstyle = (int?)last["Customize"]?["Hairstyle"]?["Value"];
			return currentHairstyle != lastHairstyle;
		}

		private void ProcessExternalStateChange(JObject state, StateChangeType changeType) {
			try {
				switch (changeType) {
					case StateChangeType.Equip:
						ProcessEquipChange(state);
						break;
					case StateChangeType.Weapon:
						ProcessWeaponChange(state);
						break;
					case StateChangeType.Stains:
						ProcessStainChange(state);
						break;
					case StateChangeType.BonusItem:
						ProcessBonusItemChange(state);
						break;
					case StateChangeType.EntireCustomize:
						ProcessCustomizeChange(state);
						break;
				}
			} catch (Exception e) {
				PluginLog.Error(e, $"Error processing {changeType}");
			}
		}

		private void ProcessEquipChange(JObject state) {
			var equipment = state["Equipment"];
			var lastEquipment = _lastCachedState?["Equipment"];

			if (equipment == null || lastEquipment == null) return;

			var changedSlots = GetChangedEquipmentSlots(equipment, lastEquipment);
			if (changedSlots.Count == 0) return;

			PluginLog.Debug("External equipment change detected:");
			foreach (var (slot, itemId, stain, stain2) in changedSlots) {
				PluginLog.Debug($"  {slot}: ItemId={itemId}, Stain={stain}, Stain2={stain2}");
			}
		}

		private List<(string Slot, uint ItemId, byte Stain, byte Stain2)> GetChangedEquipmentSlots(JToken current, JToken last) {
			var changes = new List<(string, uint, byte, byte)>();

			foreach (var slot in Enum.GetValues<EquipSlot>()) {
				var slotName = slot.ToString();
				var currentData = current[slotName];
				var lastData = last[slotName];

				if (currentData == null) continue;

				var currentItemId = (uint?)currentData["ItemId"] ?? 0;
				var currentStain = (byte?)currentData["Stain"] ?? 0;
				var currentStain2 = (byte?)currentData["Stain2"] ?? 0;

				var lastItemId = (uint?)lastData?["ItemId"] ?? 0;
				var lastStain = (byte?)lastData?["Stain"] ?? 0;
				var lastStain2 = (byte?)lastData?["Stain2"] ?? 0;

				// Check if anything changed
				if (currentItemId != lastItemId || currentStain != lastStain || currentStain2 != lastStain2) {
					//PluginLog.Debug($"Detected change in slot {slotName}: ItemId {lastItemId} → {currentItemId}, Stain {lastStain} → {currentStain}, Stain2 {lastStain2} → {currentStain2}");
					changes.Add((slotName, currentItemId, currentStain, currentStain2));
				}
			}

			return changes;
		}

		private void ProcessWeaponChange(JObject state) {
			var equipment = state["Equipment"];
			var lastEquipment = _lastCachedState?["Equipment"];

			if (equipment == null || lastEquipment == null) return;

			var mainHandCurr = (uint?)equipment["MainHand"]?["ItemId"] ?? 0;
			var mainHandLast = (uint?)lastEquipment["MainHand"]?["ItemId"] ?? 0;
			var offHandCurr = (uint?)equipment["OffHand"]?["ItemId"] ?? 0;
			var offHandLast = (uint?)lastEquipment["OffHand"]?["ItemId"] ?? 0;

			if (mainHandCurr == mainHandLast && offHandCurr == offHandLast) return;

			PluginLog.Debug("External weapon change detected:");
			if (mainHandCurr != mainHandLast) {
				PluginLog.Debug($"  MainHand: {mainHandLast} → {mainHandCurr}");
			}
			if (offHandCurr != offHandLast) {
				PluginLog.Debug($"  OffHand: {offHandLast} → {offHandCurr}");
			}
		}

		private void ProcessStainChange(JObject state) {
			
			var equipment = state["Equipment"];
			var lastEquipment = _lastCachedState?["Equipment"];
			if (equipment == null || lastEquipment == null) return;

			var changedStains = GetChangedStains(equipment, lastEquipment);
			if (changedStains.Count == 0) return;

			PluginLog.Debug("External stain change detected:");
			foreach (var (slot, oldStain, newStain, oldStain2, newStain2) in changedStains) {
				if (oldStain != newStain) {
					PluginLog.Debug($"  {slot}: Stain {oldStain} → {newStain}");
				}
				if (oldStain2 != newStain2) {
					PluginLog.Debug($"  {slot}: Stain2 {oldStain2} → {newStain2}");
				}
			}
		}

		private List<(string Slot, byte OldStain, byte NewStain, byte OldStain2, byte NewStain2)> GetChangedStains(JToken current, JToken last) {
			var changes = new List<(string, byte, byte, byte, byte)>();

			foreach (var slot in Enum.GetValues<EquipSlot>()) {
				var slotName = slot.ToString();
				var currentData = current[slotName];
				var lastData = last[slotName];

				if (currentData == null) continue;

				var currentStain = (byte?)currentData["Stain"] ?? 0;
				var currentStain2 = (byte?)currentData["Stain2"] ?? 0;
				var lastStain = (byte?)lastData?["Stain"] ?? 0;
				var lastStain2 = (byte?)lastData?["Stain2"] ?? 0;

				if (currentStain != lastStain || currentStain2 != lastStain2) {
					changes.Add((slotName, lastStain, currentStain, lastStain2, currentStain2));
				}
			}

			return changes;
		}

		private void ProcessBonusItemChange(JObject state) {
			var currentId = (ulong?)state["Equipment"]?["BonusItem"]?["BonusId"] ?? 0;
			var lastId = (ulong?)_lastCachedState?["Equipment"]?["BonusItem"]?["BonusId"] ?? 0;

			if (currentId != lastId) {
				PluginLog.Debug($"External bonus item change detected: {lastId} → {currentId}");
			}
		}

		private void ProcessCustomizeChange(JObject state) {
			var currentHairstyle = (int?)state["Customize"]?["Hairstyle"]?["Value"];
			var lastHairstyle = (int?)_lastCachedState?["Customize"]?["Hairstyle"]?["Value"];

			if (currentHairstyle != null && lastHairstyle != null && currentHairstyle != lastHairstyle) {
				PluginLog.Debug($"External hairstyle change detected: {lastHairstyle} → {currentHairstyle}");
			}
		}
		/// <summary>
		/// Mark that we're initiating a local change of this type,
		/// so we can filter it out when the event fires.
		/// </summary>
		private void MarkLocalChange(StateChangeType changeType) {
			lock (_cacheLock) {
				_pendingLocalChanges.Add(changeType);
			}
		}

		/// <summary>
		/// Wraps SetItem to track local changes.
		/// </summary>
		private void SetItemWithTracking(ICharacter character, EquipSlot slot, CustomItemId itemId, byte stainId, byte stainId2) {
			MarkLocalChange(StateChangeType.Equip);
			SetItem(character, slot, itemId, stainId, stainId2);
		}

		/// <summary>
		/// Wraps SetSet to track local changes.
		/// </summary>
		public bool SetSetWithTracking(ICharacter character, InventoryItemSet set) {
			MarkLocalChange(StateChangeType.Equip);
			MarkLocalChange(StateChangeType.Stains);
			return SetSet(character, set);
		}
	}
}
