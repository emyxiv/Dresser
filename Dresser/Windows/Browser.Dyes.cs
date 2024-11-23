using System;
using Dalamud.Interface.Windowing;
using Dresser.Logic;

using ImGuiNET;

namespace Dresser.Windows
{
	public partial class GearBrowser : Window, IWindowWithHotkey, IDisposable
	{
		private DyePicker _dyePicker;

		private void DrawDyes() {
			_dyePicker.Draw();
		}
	}
}