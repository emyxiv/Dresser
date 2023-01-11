using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dresser.Interop.Hooks {
	public class AddonListeners {
		public static void Init() {
			PluginServices.AddonManager = new AddonManager();
			PluginServices.ClientState.Login += OnLogin;
			PluginServices.ClientState.Logout += OnLogout;

			var MiragePrismMiragePlate = PluginServices.AddonManager.Get<MiragePrismMiragePlateAddon>();
			MiragePrismMiragePlate.ReceiveEvent += OnGlamourPlatesReceiveEvent;
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

		private unsafe static void OnGlamourPlatesReceiveEvent(object? sender, ReceiveEventArgs e) {
			//e.PrintData();

			if (
				e.SenderID == 0 && (
				e.EventArgs->Int == 18 // used "Close" button, the (X) button, Close UI Component keybind, Cancel Keybind. NOT when using the "Glamour Plate" toggle skill to close it.
				|| e.EventArgs->Int == 17 // Change Glamour Plate Page
				)) {
				Data.Gathering.DelayParseGlamPlates();
			}
			if (e.SenderID == 0 && e.EventArgs->Int == -2)
				EventManager.GearSelectionClose?.Invoke();

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
				EventManager.GearSelectionOpen?.Invoke();
				Data.Gathering.DelayParseGlamPlates();
			}

			if (e.SenderID == 0 && e.EventArgs->Int == -2)
				EventManager.GearSelectionClose?.Invoke();
		}
	}


	// The classes AddonManager and ReceiveEventArgs are from DailyDuty plugin.
	// MiragePrismMiragePlateAddon class is strongly inspired from the different addons managed in DailyDuty.
	// Thank you MidoriKami <3

	// their role is to manage events for any kind of AgentInterface
	internal class AddonManager : IDisposable {
		private readonly List<IDisposable> addons = new()
		{
			new MiragePrismMiragePlateAddon(),
			new MiragePrismPrismBoxAddon(),
		};

		public void Dispose() {
			foreach (var addon in addons) {
				addon.Dispose();
			}
		}

		public T Get<T>() {
			return addons.OfType<T>().First();
		}
	}

	// This is Glamour Plate event hook
	// If adding new agents, it may be a good idea to move them in their own files
	internal unsafe class MiragePrismMiragePlateAddon : IDisposable {
		public event EventHandler<ReceiveEventArgs>? ReceiveEvent;
		//public event EventHandler<IntPtr>? OnShow;

		private delegate void* AgentReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender);
		//private delegate void AgentShow(AgentInterface* agent);

		private readonly Hook<AgentReceiveEvent>? receiveEventHook;
		//private readonly Hook<AgentShow>? showEventHook;

		internal unsafe static AgentInterface* AgentInterface = Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);
		public MiragePrismMiragePlateAddon() {
			receiveEventHook ??= Hook<AgentReceiveEvent>.FromAddress(new IntPtr(AgentInterface->VTable->ReceiveEvent), OnReceiveEvent);
			//showEventHook ??= Hook<AgentShow>.FromAddress(new IntPtr(AgentInterface->VTable->Show), OnShowEvent);

			receiveEventHook?.Enable();
			//showEventHook?.Enable();

		}

		public void Dispose() {
			receiveEventHook?.Dispose();
			//showEventHook?.Dispose();
		}

		private void* OnReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender) {
			try {
				ReceiveEvent?.Invoke(this, new ReceiveEventArgs(agent, rawData, eventArgs, eventArgsCount, sender));
			} catch (Exception ex) {
				PluginLog.Error(ex, "Something went wrong when the MiragePrismMiragePlates Addon was opened");
			}

			return receiveEventHook!.Original(agent, rawData, eventArgs, eventArgsCount, sender);
		}
		//private void OnShowEvent(AgentInterface* agent) {
		//	PluginLog.Debug($"OnShowEvent");
		//	try {
		//		//Data.Gathering.DelayParseGlamPlates();

		//		//EventManager.GearSelectionOpen?.Invoke();
		//		OnShow?.Invoke(this, new IntPtr(agent));
		//	} catch (Exception ex) {
		//		PluginLog.Error(ex, "Something went wrong when the MiragePrismPrismBox Addon was opened");
		//	}
		//}
	}
	internal unsafe class MiragePrismPrismBoxAddon : IDisposable {
		public event EventHandler<ReceiveEventArgs>? ReceiveEvent;
		//public event EventHandler<IntPtr>? OnShow;
		//public event EventHandler<IntPtr>? OnHide;

		private delegate void* AgentReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender);
		//private delegate void AgentShow(AgentInterface* agent);
		//private delegate void AgentHide(AgentInterface* agent);

		private readonly Hook<AgentReceiveEvent>? receiveEventHook;
		//private readonly Hook<AgentShow>? showEventHook;
		//private readonly Hook<AgentHide>? hideEventHook;

		internal unsafe static AgentInterface* AgentInterface = Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismPrismBox);

		public MiragePrismPrismBoxAddon() {
			receiveEventHook ??= Hook<AgentReceiveEvent>.FromAddress(new IntPtr(AgentInterface->VTable->ReceiveEvent), OnReceiveEvent);
			//showEventHook ??= Hook<AgentShow>.FromAddress(new IntPtr(MiragePrismPrismBoxAgentInterface->VTable->Show), OnShowEvent);
			//hideEventHook ??= Hook<AgentHide>.FromAddress(new IntPtr(MiragePrismPrismBoxAgentInterface->VTable->Hide), OnHideEvent);

			receiveEventHook?.Enable();
			//showEventHook?.Enable();
			//hideEventHook?.Enable();
		}

		public void Dispose() {
			receiveEventHook?.Dispose();
			//showEventHook?.Dispose();
			//hideEventHook?.Dispose();
		}
		public static bool IsVisible() {
			var MiragePrismPrismBox = PluginServices.AddonManager.Get<MiragePrismPrismBoxAddon>();
			return Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismPrismBox)->IsAgentActive();
		}

		private void* OnReceiveEvent(AgentInterface* agent, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong sender) {
			try {
				ReceiveEvent?.Invoke(this, new ReceiveEventArgs(agent, rawData, eventArgs, eventArgsCount, sender));
			} catch (Exception ex) {
				PluginLog.Error(ex, "Something went wrong when the MiragePrismPrismBoxs Addon was opened");
			}

			return receiveEventHook!.Original(agent, rawData, eventArgs, eventArgsCount, sender);
		}
		//private void OnShowEvent(AgentInterface* agent) {
		//	PluginLog.Debug($"OnShowEvent");
		//	try {
		//		EventManager.GearSelectionOpen?.Invoke();
		//		OnShow?.Invoke(this, new IntPtr(agent));
		//	} catch (Exception ex) {
		//		PluginLog.Error(ex, "Something went wrong when the MiragePrismPrismBox Addon was opened");
		//	}

		//	showEventHook!.Original(agent);
		//}
		//private void OnHideEvent(AgentInterface* agent) {
		//	PluginLog.Debug($"OnHideEvent");
		//	try {
		//		EventManager.GearSelectionClose?.Invoke();
		//		OnHide?.Invoke(this, new IntPtr(agent));
		//	} catch (Exception ex) {
		//		PluginLog.Error(ex, "Something went wrong when the MiragePrismPrismBox Addon was opened");
		//	}

		//	hideEventHook!.Original(agent);
		//}
	}
	internal unsafe class ReceiveEventArgs : EventArgs {
		public ReceiveEventArgs(AgentInterface* agentInterface, void* rawData, AtkValue* eventArgs, uint eventArgsCount, ulong senderID) {
			AgentInterface = agentInterface;
			RawData = rawData;
			EventArgs = eventArgs;
			EventArgsCount = eventArgsCount;
			SenderID = senderID;
		}

		public AgentInterface* AgentInterface;
		public void* RawData;
		public AtkValue* EventArgs;
		public uint EventArgsCount;
		public ulong SenderID;

		public void PrintData() {
			PluginLog.Verbose("ReceiveEvent Argument Printout --------------");
			PluginLog.Verbose($"AgentInterface: {(IntPtr)AgentInterface:X8}");
			PluginLog.Verbose($"RawData: {(IntPtr)RawData:X8}");
			PluginLog.Verbose($"EventArgs: {(IntPtr)EventArgs:X8}");
			PluginLog.Verbose($"EventArgsCount: {EventArgsCount}");
			PluginLog.Verbose($"SenderID: {SenderID}");

			for (var i = 0; i < EventArgsCount; i++) {
				PluginLog.Verbose($"[{i}] {EventArgs[i].Int}, {EventArgs[i].Type}");
			}

			PluginLog.Verbose("End -----------------------------------------");
		}
	}
}
