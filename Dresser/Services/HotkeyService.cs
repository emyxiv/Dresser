using System;
using System.Collections.Generic;
using CriticalCommonLib.Services;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using System.Linq;
using Dresser.Logic;
using Dalamud.Plugin.Services;

namespace Dresser.Services;

public class HotkeyService : IDisposable {
	private IFramework _frameworkService;
	private IKeyState _keyStateService;
	private List<Hotkey> _hotKeys;
	public HotkeyService(IFramework framework, IKeyState keyState) {
		_hotKeys = new List<Hotkey>();
		_frameworkService = framework;
		_keyStateService = keyState;
		_frameworkService.Update += FrameworkServiceOnUpdate;
	}

	public void AddHotkey(Hotkey hotkey) {
		_hotKeys.Add(hotkey);
	}
	public void ClearHotkey()
		=> _hotKeys.Clear();

	private void FrameworkServiceOnUpdate(IFramework framework) {

		foreach (var hotkey in _hotKeys) {
			var hotkeyVirtualKeys = hotkey.VirtualKeys;
			if (hotkeyVirtualKeys != null && HotkeyPressed(hotkeyVirtualKeys)) {
				if (hotkey.OnHotKey() && !hotkey.PassToGame) {
					foreach (var k in hotkeyVirtualKeys) {
						_keyStateService[(int)k] = false;
					}
				}
			}
		}
	}

	private bool HotkeyPressed(VirtualKey[] keys) {
		if (keys.Length == 1 && keys[0] == VirtualKey.NO_KEY) {
			return false;
		}

		bool hotKeyPressed = keys.Length != 0;
		foreach (var vk in keys) {
			if (!_keyStateService[vk]) {
				hotKeyPressed = false;
			}
		}
		return hotKeyPressed;
	}

	private bool _disposed;
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) {
		if (!_disposed && disposing) {
			_frameworkService.Update -= FrameworkServiceOnUpdate;
		}
		_disposed = true;
	}


	~HotkeyService() {
#if DEBUG
		if (_disposed == false) {
			PluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType().Name));
		}
#endif
		Dispose(true);
	}
}