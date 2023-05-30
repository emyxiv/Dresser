using Dalamud.Interface.Windowing;

using ImGuiNET;

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
	}

	public class DialogInfo {
		public Func<int> Contents;
		public Action<int> Quit;
		public int Choice = -1;
		public DialogInfo(Func<int> contents, Action<int> onClose) {
			this.Contents = contents;
			this.Quit = onClose;
		}
	}
}
