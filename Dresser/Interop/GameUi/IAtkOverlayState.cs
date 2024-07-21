using CriticalCommonLib.Services.Ui;

using Dresser.Logic;

using System;
using System.Collections.Generic;
using System.Numerics;

using static Lumina.Data.BaseFileHandle;

namespace Dresser.Interop.GameUi {
	public interface IAtkOverlayState : AtkState<HighlighterState?>  {

	}

	public interface IGameOverlay : IAtkOverlayState, IDisposable {
		bool ShouldDraw { get; set; }
		public WindowName WindowName { get; }
		public HashSet<WindowName>? ExtraWindows { get; }

		public bool HasAddon { get; }

		public bool Enabled { get; set; }

		bool Draw();
		void Setup();
		void Update();
	}

	public abstract class GameOverlay<T> : IGameOverlay where T : AtkOverlay {
		public GameOverlay(T overlay) {
			AtkOverlay = overlay;
			AtkOverlay.AtkUpdated += AtkOverlayOnAtkUpdated;
		}

		private void AtkOverlayOnAtkUpdated() {
			PluginLog.Verbose("ATK overlay event received, requesting state refresh.");
			NeedsStateRefresh = true;
		}

		public T AtkOverlay { get; }

		public abstract bool ShouldDraw { get; set; }

		public virtual WindowName WindowName => AtkOverlay.WindowName;
		public virtual HashSet<WindowName>? ExtraWindows => AtkOverlay.ExtraWindows;
		public virtual bool HasAddon => AtkOverlay.HasAddon;

		public abstract bool Draw();
		public abstract void Setup();

		public virtual void Update() {
			AtkOverlay.Update();
		}

		public bool Enabled { get; set; } = true;
		public abstract bool HasState { get; set; }
		public abstract bool NeedsStateRefresh { get; set; }
		public abstract void UpdateState(HighlighterState? newState);
		public abstract void Clear();

		public void Dispose() {
			AtkOverlay.AtkUpdated -= AtkOverlayOnAtkUpdated;
		}
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
