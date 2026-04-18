using System.Numerics;

namespace Dresser.Gui
{
	public partial class GearBrowser
	{
		private Vector2 DrawInfoSearchBarDyes(Vector2 posInfoSearchInitial, float darkenAmount) {
			return posInfoSearchInitial;
		}
		private DyePicker _dyePicker;

		private void DrawDyes() {
			_dyePicker.Draw();
		}
	}
}