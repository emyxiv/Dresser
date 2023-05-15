using System;

namespace Dresser.Interop.Hooks {
	public class AddonListeners {
		public static void Init() {
			PluginServices.AddonManager = new AddonManager();
			PluginServices.ClientState.Login += OnLogin;
			PluginServices.ClientState.Logout += OnLogout;

			var MiragePrismMiragePlate = PluginServices.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent += OnGlamourPlatesReceiveEvent;
			MiragePrismMiragePlate.OnShow += OnGlamourPlatesShow;
			var MiragePrismPrismBox = PluginServices.AddonManager.Get<MiragePrismPrismBoxAddon>();
			MiragePrismPrismBox.ReceiveEvent += OnMiragePrismPrismBoxReceiveEvent;

			OnLogin(null!, null!);
		}

		public static void Dispose() {
			PluginServices.AddonManager.Dispose();
			PluginServices.ClientState.Logout -= OnLogout;
			PluginServices.ClientState.Login -= OnLogin;

			var MiragePrismMiragePlate = PluginServices.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent -= OnGlamourPlatesReceiveEvent;
			MiragePrismMiragePlate.OnShow -= OnGlamourPlatesShow;
			var MiragePrismPrismBox = PluginServices.AddonManager.Get<MiragePrismPrismBoxAddon>();
			MiragePrismPrismBox.ReceiveEvent += OnMiragePrismPrismBoxReceiveEvent;


			OnLogout(null!, null!);
		}

		// Various event methods
		private static void OnLogin(object? sender, EventArgs e) {
			//Sets.Init();
		}
		private static void OnLogout(object? sender, EventArgs e) {
			//Sets.Dispose();
		}


		private unsafe static void OnGlamourPlatesShow(object? sender, IntPtr ptr) {
			Data.Gathering.DelayParseGlamPlatesAndComparePending();

		}
		private unsafe static void OnGlamourPlatesReceiveEvent(object? sender, ReceiveEventArgs e) {
			//e.PrintData();

			if (
				e.SenderID == 0 && (
				e.EventArgs->Int == 18 // used "Close" button, the (X) button, Close UI Component keybind, Cancel Keybind. NOT when using the "Glamour Plate" toggle skill to close it.
				|| e.EventArgs->Int == 17 // Change Glamour Plate Page
				)) {
				Data.Gathering.DelayParseGlamPlates();
			}
			//if (e.SenderID == 0 && e.EventArgs->Int == -2)
			//	EventManager.GearSelectionClose?.Invoke();

		}
		private unsafe static void OnMiragePrismPrismBoxReceiveEvent(object? sender, ReceiveEventArgs e) {
			//e.PrintData();

			if (
				e.SenderID == 0 && (
				e.EventArgs->Int == 2 // used "Close" button, the (X) button, Close UI Component keybind, Cancel Keybind. NOT when using the "Glamour Plate" toggle skill to close it.
				//|| e.EventArgs->Int == 17 // Change Glamour Plate Page
				)) {
				Data.Gathering.DelayParseGlamPlates();
			}
			if (e.SenderID == 0 && e.EventArgs->Int == 5) {
				//EventManager.GearSelectionOpen?.Invoke();
				Data.Gathering.DelayParseGlamPlates();
			}

			//if (e.SenderID == 0 && e.EventArgs->Int == -2)
			//	EventManager.GearSelectionClose?.Invoke();
		}
	}



}
