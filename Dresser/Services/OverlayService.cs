using CriticalCommonLib;
using CriticalCommonLib.Services.Ui;
using CriticalCommonLib.Services;

using Dalamud.Game;
using Dresser.Logic;
using Dalamud.Plugin.Services;

using Dresser.Interop.GameUi;

using System;
using System.Collections.Generic;

namespace Dresser.Services {
	public class OverlayService : IDisposable {
		private GameUiManager _gameUiManager;

		public OverlayService(GameUiManager gameUiManager) {
			_gameUiManager = gameUiManager;
			PluginServices.GameUi.UiVisibilityChanged += GameUiOnUiVisibilityChanged;
			//PluginServices.GameUi.UiUpdated += GameUiOnUiUpdated;
			AddOverlay(new MiragePrismMiragePlateOverlay());

			PluginServices.Framework.Update += FrameworkOnUpdate;
			PluginServices.OnPluginLoaded += PluginServiceOnOnPluginLoaded;
		}

		private void PluginServiceOnOnPluginLoaded() {
			RefreshOverlayStates();
		}


		public void RefreshOverlayStates() {
			var plateHighlight = PluginServices.ApplyGearChange.HighlightPlatesRadio;
			var saveButton = PluginServices.ApplyGearChange.HighlightSaveButton;

			if (plateHighlight != null || saveButton != null) {
				UpdateState(new HighlighterState() { PlatesToHighlight = plateHighlight ?? new(), SaveButtonHighlight = saveButton ?? false });
			} else {
				UpdateState(null);
			}
		}

		private void FrameworkOnUpdate(IFramework framework) {
			foreach (var overlay in _overlays) {
				if (overlay.Value.NeedsStateRefresh) {
					overlay.Value.UpdateState(_lastState);
					overlay.Value.NeedsStateRefresh = false;
				}

				if (overlay.Value.HasAddon) {
					overlay.Value.Update();
				}
			}
		}

		private Dictionary<WindowName, IAtkOverlayState> _overlays = new();
		private HashSet<WindowName> _setupHooks = new();
		private Dictionary<WindowName, DateTime> _lastUpdate = new();
		private HighlighterState? _lastState;

		public HighlighterState? LastState => _lastState;

		public Dictionary<WindowName, IAtkOverlayState> Overlays {
			get => _overlays;
		}

		public void UpdateState(HighlighterState? highlighterState) {
			foreach (var overlay in _overlays) {
				overlay.Value.UpdateState(highlighterState);
				_lastState = highlighterState;
			}
		}

		public void SetupUpdateHook(IAtkOverlayState overlayState) {
			if (_setupHooks.Contains(overlayState.WindowName)) {
				return;
			}
			var result = PluginServices.GameUi.IsWindowVisible(overlayState.WindowName);
			if (result) {
				_setupHooks.Add(overlayState.WindowName);
			}
		}

		public void AddOverlay(IAtkOverlayState overlayState) {
			if (!Overlays.ContainsKey(overlayState.WindowName)) {
				Overlays.Add(overlayState.WindowName, overlayState);
				overlayState.Setup();
				overlayState.Draw();
			} else {
				PluginLog.Error("Attempted to add an overlay that is already registered.");
			}
		}

		public void RemoveOverlay(WindowName windowName) {
			if (Overlays.ContainsKey(windowName)) {
				Overlays[windowName].Clear();
				Overlays.Remove(windowName);
			}
		}

		public void RemoveOverlay(IAtkOverlayState overlayState) {
			if (Overlays.ContainsKey(overlayState.WindowName)) {
				Overlays.Remove(overlayState.WindowName);
				overlayState.Clear();
			}
		}

		public void ClearOverlays() {
			foreach (var overlay in _overlays) {
				RemoveOverlay(overlay.Value);
			}
		}
		private void GameUiOnUiVisibilityChanged(WindowName windowname, bool? windowstate) {
			if (PluginServices.PluginLoaded) {
				if (windowstate == true) {
					RefreshOverlayStates();
				}

				if (_overlays.ContainsKey(windowname)) {
					var overlay = _overlays[windowname];
					if (windowstate.HasValue && windowstate.Value) {
						SetupUpdateHook(overlay);
						if (_lastState != null && !overlay.HasState) {
							overlay.UpdateState(_lastState);
						}
					}

					if (windowstate.HasValue && !windowstate.Value) {
						overlay.UpdateState(null);
					}

					overlay.Draw();
				}
			}
		}

		/*
		private void GameUiOnUiUpdated(WindowName windowname) {
			if (PluginServices.PluginLoaded) {
				if (_overlays.ContainsKey(windowname)) {
					var overlay = _overlays[windowname];
					if (!_lastUpdate.ContainsKey(windowname)) {
						_lastUpdate[windowname] = DateTime.Now.AddMilliseconds(50);
						if (_lastState != null && !overlay.HasState) {
							overlay.UpdateState(_lastState);
						} else {
							overlay.Draw();
						}
					} else if (_lastUpdate[windowname] <= DateTime.Now) {
						if (_lastState != null && !overlay.HasState) {
							overlay.UpdateState(_lastState);
						} else {
							overlay.Draw();
						}

						_lastUpdate[windowname] = DateTime.Now.AddMilliseconds(50);
					}
				}
			}
		}
		*/

		private bool _disposed;
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (!_disposed && disposing) {
				PluginServices.Framework.Update -= FrameworkOnUpdate;
				ClearOverlays();
				PluginServices.GameUi.UiVisibilityChanged -= GameUiOnUiVisibilityChanged;
				//PluginServices.GameUi.UiUpdated -= GameUiOnUiUpdated;
				PluginServices.OnPluginLoaded -= PluginServiceOnOnPluginLoaded;
			}
			_disposed = true;
		}


		~OverlayService() {
#if DEBUG
			// In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
			// disposed by the programmer.

			if (_disposed == false) {
				PluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + GetType().Name);
			}
#endif
			Dispose(true);
		}
	}
}
