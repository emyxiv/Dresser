using System;
using System.Numerics;

using Dalamud.Interface.Windowing;

using FabulousDresser.Structs.FFXIV;

using ImGuiNET;

using ImGuiScene;

namespace FabulousDresser.Windows;

public class MainWindow : Window, IDisposable {
	private Plugin Plugin;

	public MainWindow(Plugin plugin, TextureWrap goatImage) : base(
		"Fabulous Dresser", ImGuiWindowFlags.None) {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};

		this.Plugin = plugin;
	}

	public void Dispose() {

	}

	public override void Draw() {
		ImGui.Text("start");
		Components.Plates.Draw();
		ImGui.Text("end");

	}
}
