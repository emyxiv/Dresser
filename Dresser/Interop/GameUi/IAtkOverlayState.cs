using CriticalCommonLib.Services.Ui;

using System.Collections.Generic;
using System.Numerics;

namespace Dresser.Interop.GameUi {
	public interface IAtkOverlayState : IAtkOverlay, AtkState<HighlighterState?> {

	}

	public interface AtkState<T> {
		public bool HasState { get; set; }
		public bool NeedsStateRefresh { get; set; }
		public void UpdateState(T newState);
		public void Clear();
	}

	public struct HighlighterState {
		public Dictionary<ushort, Vector4?> PlatesToHighlight;
		public bool SaveButtonHighlight;
	}
}
