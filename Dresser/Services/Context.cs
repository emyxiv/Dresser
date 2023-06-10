using CriticalCommonLib;
using CriticalCommonLib.Extensions;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;

using Dresser.Interop.Hooks;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Linq;

namespace Dresser.Services {
	internal class Context : IDisposable {

		public bool IsGlamingAtDresser = false;
		public bool IsCurrentGearWindowOpen = false;
		public bool IsBrowserWindowOpen = false;
		public ushort? SelectedPlate = null;

		public PlayerCharacter? LocalPlayer = null;
		public CharacterRace? LocalPlayerRace = null;
		public CharacterSex? LocalPlayerGender = null;
		public ClassJob? LocalPlayerClass = null;
		public byte LocalPlayerLevel = 0;
		public ulong LocalPlayerCharacterId = 0;



		public Context() {
			//Refresh();
		}
		public void Dispose() {
			LocalPlayer = null;
			LocalPlayerRace = null;
			LocalPlayerGender = null;
			LocalPlayerClass = null;
			LocalPlayerLevel = 0;
		}

		private bool _lastState_IsGlamingAtDresser = false;
		public delegate void OnChangeGlamingAtDresserDelegate(bool newIsGlamingAtDresser);
		public static event OnChangeGlamingAtDresserDelegate? OnChangeGlamingAtDresser;

		public Window? LastFocusedWindow = null;
		public void Refresh() {
			IsGlamingAtDresser = GlamourPlates.IsGlamingAtDresser();
			if (IsGlamingAtDresser != _lastState_IsGlamingAtDresser) OnChangeGlamingAtDresser?.Invoke(IsGlamingAtDresser);
			_lastState_IsGlamingAtDresser = IsGlamingAtDresser;

			IsCurrentGearWindowOpen = Plugin.GetInstance()?.IsDresserVisible() ?? false;
			IsBrowserWindowOpen = Plugin.GetInstance()?.IsBrowserVisible() ?? false;
			if (IsCurrentGearWindowOpen) {
				var windowSystem = Plugin.GetInstance().WindowSystem;
				if (windowSystem.HasAnyFocus)
					LastFocusedWindow = windowSystem.Windows.FirstOrDefault(w => w.IsFocused);
			}
			else
				LastFocusedWindow = null;


			LocalPlayer = Service.ClientState.LocalPlayer;
			if (LocalPlayer == null) return;

			LocalPlayerCharacterId = PluginServices.CharacterMonitor?.ActiveCharacterId ?? 0;
			LocalPlayerRace = (CharacterRace)LocalPlayer.Customize[(int)CustomizeIndex.Race];
			LocalPlayerGender = LocalPlayer.Customize[(int)CustomizeIndex.Gender] == 0 ? CharacterSex.Male : CharacterSex.Female;
			LocalPlayerClass = LocalPlayer.ClassJob.GameData;
			LocalPlayerLevel = LocalPlayer.Level;
		}
	}
}
