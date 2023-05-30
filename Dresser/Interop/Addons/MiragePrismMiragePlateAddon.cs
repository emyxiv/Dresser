using Dalamud.Hooking;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using System;

namespace Dresser.Interop.Addons {
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
		//	PluginLog.Debug($"OnShowEvent MiragePrismMiragePlateAddon");
		//	try {
		//		OnShow?.Invoke(this, new IntPtr(agent));
		//	} catch (Exception ex) {
		//		PluginLog.Error(ex, "Something went wrong when the MiragePrismPrismBox Addon was opened");
		//	}
		//	showEventHook!.Original(agent);
		//}
	}
}
