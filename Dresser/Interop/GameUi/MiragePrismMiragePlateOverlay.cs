using System.Collections.Generic;
using System.Numerics;

using Dresser.Interop.GameUi;
using Dresser;
using System.Linq;
using Dalamud.Logging;

namespace InventoryTools.GameUi {
	public class MiragePrismMiragePlateOverlay : AtkMiragePrismMiragePlate, IAtkOverlayState {
		public bool HasState { get; set; }
		public bool NeedsStateRefresh { get; set; }

		public Dictionary<ushort, Dictionary<Vector2, Vector4?>> ChestInventoryColours = new();
		public Dictionary<uint, Vector4?> TabColours = new();
		public Dictionary<uint, Vector4?> EmptyTabs = new() { { 0, null }, { 1, null }, { 2, null }, { 3, null }, { 4, null }, { 5, null }, { 6, null }, { 7, null }, { 8, null }, { 9, null }, { 10, null }, { 11, null }, { 12, null }, { 13, null }, { 14, null }, { 15, null }, { 16, null }, { 17, null }, { 18, null }, { 19, null } };
		public Dictionary<Vector2, Vector4?> EmptyDictionary = new();

		public ushort? _storedTab = null;


		public delegate void PlateChangedDelegate(ushort? newPlateIndex, ushort? oldPlateIndex);
		public static event PlateChangedDelegate? OnPlateChanged;



		public override void Update() {
			//PluginLog.Debug($" Update === = = = = {!HasState} || {!HasAddon}");
			if (!HasState || !HasAddon) {
				return;
			}
			var currentTab = CurrentPlate;
			//PluginLog.Debug($"plate status === {currentTab} was {_storedTab} ");

			if (currentTab != -1 && currentTab != _storedTab) {
				var previousTab = _storedTab;
				_storedTab = (ushort)currentTab;
				NeedsStateRefresh = true;
				PluginServices.Context.SelectedPlate = _storedTab;
				PluginLog.Error($"plate changed from {previousTab} to {_storedTab} ");
				OnPlateChanged?.Invoke(_storedTab, previousTab);
			}
		}



		public override bool Draw() {
			if (!HasState || !HasAddon) {
				return false;
			}
			var atkUnitBase = AtkUnitBase;
			if (atkUnitBase != null) {

				this.SetTabColors(TabColours);
				return true;
			}
			return false;
		}

		public void UpdateState(HighlighterState? newState) {
			if (PluginServices.CharacterMonitor.ActiveCharacter == 0) {
				return;
			}
			if (newState != null && HasAddon && newState.Value.PlatesToHighlight.Any()) {
				HasState = true;

				TabColours = EmptyTabs;
				foreach ((var plateIndex, var color)  in newState.Value.PlatesToHighlight) {
					TabColours[plateIndex] = color;
				}

				Draw();
				return;
			}

			if (HasState) {

				Clear();
			}

			HasState = false;
		}

		public override void Setup() {
			for (int x = 0; x < 10; x++) {
				for (int y = 0; y < 5; y++) {
					EmptyDictionary.Add(new Vector2(x, y), null);
				}
			}

		}

		public void Clear() {
			var atkUnitBase = AtkUnitBase;
			if (atkUnitBase != null) {
				this.SetTabColors(EmptyTabs);
			}
		}
	}
}