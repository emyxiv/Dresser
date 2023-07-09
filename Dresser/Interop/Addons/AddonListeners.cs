using Dalamud.Logging;

using Dresser.Interop.GameUi;
using Dresser.Services;

using System;

namespace Dresser.Interop.Addons {
	public class AddonListeners {
		public static void Init() {
			PluginServices.AddonManager = new AddonManager();
			PluginServices.ClientState.Login += OnLogin;
			PluginServices.ClientState.Logout += OnLogout;

			var MiragePrismMiragePlate = PluginServices.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent += OnGlamourPlatesReceiveEvent;
			//MiragePrismMiragePlate.OnShow += OnGlamourPlatesShow;
			Context.OnChangeGlamingAtDresser += OnGlamourPlatesShow2;
			var MiragePrismPrismBox = PluginServices.AddonManager.Get<MiragePrismPrismBoxAddon>();
			MiragePrismPrismBox.ReceiveEvent += OnMiragePrismPrismBoxReceiveEvent;

			MiragePrismMiragePlateOverlay.OnPlateChanged += OnPlateChanged;

			OnLogin(null!, null!);
		}

		public static void Dispose() {
			PluginServices.AddonManager.Dispose();
			PluginServices.ClientState.Logout -= OnLogout;
			PluginServices.ClientState.Login -= OnLogin;

			var MiragePrismMiragePlate = PluginServices.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent -= OnGlamourPlatesReceiveEvent;
			//MiragePrismMiragePlate.OnShow -= OnGlamourPlatesShow;
			Context.OnChangeGlamingAtDresser += OnGlamourPlatesShow2;

			var MiragePrismPrismBox = PluginServices.AddonManager.Get<MiragePrismPrismBoxAddon>();
			MiragePrismPrismBox.ReceiveEvent -= OnMiragePrismPrismBoxReceiveEvent;
			MiragePrismMiragePlateOverlay.OnPlateChanged -= OnPlateChanged;


			OnLogout(null!, null!);
		}

		// Various event methods
		private static void OnLogin(object? sender, EventArgs e) {
			//Sets.Init();
		}
		private static void OnLogout(object? sender, EventArgs e) {
			//Sets.Dispose();
		}


		private static void OnGlamourPlatesShow(object? sender, IntPtr ptr) {
			Logic.Gathering.DelayParseGlamPlatesAndComparePending();

		}
		private static void OnGlamourPlatesShow2(bool isShowing) {
			if (isShowing) {
				Logic.Gathering.DelayParseGlamPlatesAndComparePending();
				PluginServices.ApplyGearChange.OpenGlamourDresser();
			}
		}
		private unsafe static void OnGlamourPlatesReceiveEvent(object? sender, ReceiveEventArgs e) {
			//e.PrintData();

			if (e.SenderID == 0 && e.EventArgs->Int == 18) {
				// used "Close" button, the (X) button, Close UI Component keybind, Cancel Keybind. NOT when using the "Glamour Plate" toggle skill to close it.
				Logic.Gathering.DelayParseGlamPlates();
				PluginServices.ApplyGearChange.Popup_AllDone();
			}
			if (e.SenderID == 0 && e.EventArgs->Int == 17) {
				// Change Glamour Plate Page
				Logic.Gathering.DelayParseGlamPlates();
			}
			if (e.SenderID == 0 && e.EventArgs->Int == -2) {

				//	EventManager.GearSelectionClose?.Invoke();
			}
			if (e.SenderID == 1 && e.EventArgs->Int == 0) {
				// if change plate + discards
				PluginServices.OverlayService.RefreshOverlayStates();
			}
			if (e.SenderID == 4 && e.EventArgs->Int == 0) {
				// click "yes" when asked to saving changes
				PluginServices.ApplyGearChange.ExecuteSavingPlateChanges();
			}
		}
		private unsafe static void OnMiragePrismPrismBoxReceiveEvent(object? sender, ReceiveEventArgs e) {
			//e.PrintData();

			if (
				e.SenderID == 0 &&
				e.EventArgs->Int == 2 // used "Close" button, the (X) button, Close UI Component keybind, Cancel Keybind. NOT when using the "Glamour Plate" toggle skill to close it.
									  //|| e.EventArgs->Int == 17 // Change Glamour Plate Page
				) {
				Logic.Gathering.DelayParseGlamPlates();
			}
			if (e.SenderID == 0 && e.EventArgs->Int == 5) {
				//EventManager.GearSelectionOpen?.Invoke();
				Logic.Gathering.DelayParseGlamPlates();
			}

			//if (e.SenderID == 0 && e.EventArgs->Int == -2)
			//	EventManager.GearSelectionClose?.Invoke();
		}
		private static void OnPlateChanged(ushort? newPlateIndex, ushort? oldPlateIndex) {
			PluginLog.Warning($"OnPlateChanged >>{newPlateIndex ?? -1}<< OnPlateChanged OnPlateChanged");

			// close the failed popup on changing plate
			var dialogs = Plugin.GetInstance().Dialogs;
			if (dialogs != null && dialogs.DialogInfo?.Label == "FailedSomeAskWhatToDo") dialogs.IsOpen = false;
			PluginServices.ApplyGearChange.CheckIfLeavingPlateWasApplied(oldPlateIndex);
			PluginServices.ApplyGearChange.ExecuteChangesOnSelectedPlate();
		}
	}



}
