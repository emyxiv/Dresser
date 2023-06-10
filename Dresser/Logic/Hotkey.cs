using Dalamud.Game.ClientState.Keys;

using System.Collections.Generic;

namespace Dresser.Logic {
	public class Hotkey {

		private VirtualKey[]? _keys;
		private HotkeyPurpose _hotkeyPurpose;
		public bool PassToGame { get; set; }
		public Hotkey(HotkeyPurpose hotkeyPurpose, VirtualKey[]? keys, bool passToGame) {
			_hotkeyPurpose = hotkeyPurpose;
			_keys = keys;
			PassToGame = passToGame;
		}

		public VirtualKey[]? VirtualKeys => _keys;

		public bool OnHotKey() {
			return _hotkeyPurpose switch {
				HotkeyPurpose.Up or HotkeyPurpose.Down or HotkeyPurpose.Left or HotkeyPurpose.Right => OnWindowFocusedHotkey(),
				_ => false,
			};
		}
		private bool OnWindowFocusedHotkey() {

			var focusedWindow = PluginServices.Context.LastFocusedWindow;
			if (focusedWindow == null) return false;

			// make sure this window is of isIWindowWithHotkey interface
			var isIWindowWithHotkey = focusedWindow.GetType().GetInterface(nameof(IWindowWithHotkey)) != null;
				//.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWindowWithHotkey<>));
			if (!isIWindowWithHotkey) return false;

			return ((IWindowWithHotkey)focusedWindow).OnHotkey(_hotkeyPurpose);
		}
	}


	public enum HotkeyPurpose {
		Up,
		Down,
		Left,
		Right,

	}
	public static class HotkeySetup {
		public static List<Hotkey> config = new() {
			new Hotkey(HotkeyPurpose.Up   , new VirtualKey[]{VirtualKey.UP   }, false),
			new Hotkey(HotkeyPurpose.Down , new VirtualKey[]{VirtualKey.DOWN }, false),
			new Hotkey(HotkeyPurpose.Left , new VirtualKey[]{VirtualKey.LEFT }, false),
			new Hotkey(HotkeyPurpose.Right, new VirtualKey[]{VirtualKey.RIGHT}, false),
		};

		public static void Init() {
			foreach (var key in config) {
				PluginServices.HotkeyService.AddHotkey(key);
			}
		}
	}


}
