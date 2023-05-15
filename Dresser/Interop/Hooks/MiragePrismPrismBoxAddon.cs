using System;

using Dalamud.Hooking;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dresser.Interop.Hooks {
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
}
