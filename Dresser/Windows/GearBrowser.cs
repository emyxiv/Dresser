using System;
using System.Numerics;

using Dalamud.Interface.Windowing;

using Dresser.Structs.FFXIV;

using ImGuiNET;

using ImGuiScene;

namespace Dresser.Windows;

public class GearBrowser : Window, IDisposable {

	public GearBrowser() : base(
		"Gear Browser", ImGuiWindowFlags.None) {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};

	}
	public void Dispose() { }

	public override void Draw() =>
		Components.Browse.Draw();
}
