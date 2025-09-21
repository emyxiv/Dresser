using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;

using System;
using System.Numerics;

namespace Dresser.Windows {
	internal class Dialogs : Window, IDisposable {
		private Plugin Plugin;


		public Dialogs(Plugin plugin) : base(
				"Dialogs",
				ImGuiWindowFlags.AlwaysAutoResize
				| ImGuiWindowFlags.NoScrollbar
				| ImGuiWindowFlags.NoTitleBar
				) {
			this.SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(10),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};
			this.PositionCondition = ImGuiCond.Appearing;
			this.Position = ImGui.GetIO().DisplaySize * new Vector2(0.25f, 0.3f);
			this.Plugin = plugin;
		}
		public void Dispose() { }






		public override void OnClose() {
			base.OnClose();
			if (DialogInfo?.Choice == -1) DialogInfo.Choice = DialogInfo.ChoiceWhenForceClose;
			DialogInfo?.Quit(DialogInfo.Choice);
			DialogInfo = null;
		}
		public override bool DrawConditions() {
			return DialogInfo != null;
		}

		public DialogInfo? DialogInfo = null;


		public override void Draw() {
			if (DialogInfo != null) {
				DialogInfo.Choice = DialogInfo.Contents();
				if (DialogInfo.Choice != -1) {
					this.IsOpen = false;
				}
			}
		}

		public static int GenericButtonClose(string? closeText = null) {
			if (ImGui.Button($"{closeText ?? "Close"}##Dialog##Dresser")) {
				return 1;
			}
			return -1;
		}
		public static int GenericButtonConfirmCancel(string? confirmText = null, string? cancelText = null) {

			if (ImGui.Button($"{confirmText ?? "Confirm"}##Dialog##Dresser")) {
				return 1;
			}
			ImGui.SameLine();
			if (ImGui.Button($"{cancelText ?? "Cancel"}##Dialog##Dresser")) {
				return 2;
			}
			return -1;
		}
	}

	public class DialogInfo {
		public string Label = "";
		public Func<int> Contents;
		public Action<int> Quit;
		public int Choice = -1;
		public int ChoiceWhenForceClose;
		public DialogInfo(string label, Func<int> contents, Action<int> onClose, int choiceWhenForceClose = -1) {
			this.Label = label;
			this.Contents = contents;
			this.Quit = onClose;
			this.ChoiceWhenForceClose = choiceWhenForceClose;
		}
	}
}
