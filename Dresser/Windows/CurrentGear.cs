using System;
using System.Numerics;

using Dalamud.Interface.Windowing;

using Dresser.Structs.FFXIV;

using ImGuiNET;

using ImGuiScene;

namespace Dresser.Windows;

public class CurrentGear : Window, IDisposable {

	public CurrentGear() : base(
		"Current Gear",
		ImGuiWindowFlags.None
		// | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar
		) {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};
		//Data.Gathering.ParseGlamourPlates();

	}

	public void Dispose() { }

	public override void Draw() =>
		Components.Plates.Draw();
}
