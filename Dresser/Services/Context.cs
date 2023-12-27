using CriticalCommonLib;
using CriticalCommonLib.Extensions;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;

using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Windows;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Linq;
using System.Threading;

namespace Dresser.Services {
	internal class Context : IDisposable {

		public bool IsGlamingAtDresser = false;
		public bool IsCurrentGearWindowOpen = false;
		public bool IsBrowserWindowOpen = false;
		public bool IsDyePickerPopupOpen = false;
		public bool IsAnyPlateSelectionOpen => GlamourPlates.IsAnyPlateSelectionOpen();
		public ushort? SelectedPlate = null;

		public PlayerCharacter? LocalPlayer = null;
		public CharacterRace? LocalPlayerRace = null;
		public CharacterSex? LocalPlayerGender = null;
		public ClassJob? LocalPlayerClass = null;
		public byte LocalPlayerLevel = 0;
		public ulong LocalPlayerCharacterId = 0;

		private Timer Every5seconds;

		// Stuff executed every 5s
		public bool AllaganToolsState = false;
		public bool GlamourerState = false;
		public bool PenumbraState = false;



		public Context() {
			Every5seconds = new Timer(ExecutedEvery5Seconds, null, 0, 5000); // set a 5 seconds interval
			//Refresh();
		}
		public void Dispose() {
			LocalPlayer = null;
			LocalPlayerRace = null;
			LocalPlayerGender = null;
			LocalPlayerClass = null;
			LocalPlayerLevel = 0;
			Every5seconds.Dispose();
			Every5seconds = null!;
		}

		private bool _lastState_IsGlamingAtDresser = false;
		public delegate void OnChangeGlamingAtDresserDelegate(bool newIsGlamingAtDresser);
		public static event OnChangeGlamingAtDresserDelegate? OnChangeGlamingAtDresser;

		public Window? LastFocusedWindow = null;
		public void Refresh() {
			IsGlamingAtDresser = GlamourPlates.IsGlamingAtDresser();
			if (IsGlamingAtDresser != _lastState_IsGlamingAtDresser) OnChangeGlamingAtDresser?.Invoke(IsGlamingAtDresser);
			_lastState_IsGlamingAtDresser = IsGlamingAtDresser;

			try { IsCurrentGearWindowOpen = Plugin.GetInstance()?.IsDresserVisible() ?? false; } catch (Exception) { IsCurrentGearWindowOpen = false; }

			if (IsCurrentGearWindowOpen) {
				IsBrowserWindowOpen = Plugin.GetInstance()?.IsBrowserVisible() ?? false;
				IsDyePickerPopupOpen = Plugin.GetInstance().DyePicker.IsOpen;
				var windowSystem = Plugin.GetInstance().WindowSystem;
				if (windowSystem.HasAnyFocus)
					LastFocusedWindow = windowSystem.Windows.FirstOrDefault(w => w.IsFocused);
			} else {
				LastFocusedWindow = null;
				IsBrowserWindowOpen = false;
				IsDyePickerPopupOpen = false;
			}


			LocalPlayer = Service.ClientState.LocalPlayer;
			if (LocalPlayer != null) {
				LocalPlayerCharacterId = PluginServices.CharacterMonitor?.ActiveCharacterId ?? 0;
				LocalPlayerRace = (CharacterRace)LocalPlayer.Customize[(int)CustomizeIndex.Race];
				LocalPlayerGender = LocalPlayer.Customize[(int)CustomizeIndex.Gender] == 0 ? CharacterSex.Male : CharacterSex.Female;
				LocalPlayerClass = LocalPlayer.ClassJob.GameData;
				LocalPlayerLevel = LocalPlayer.Level;
			}

		}

		private void ExecutedEvery5Seconds(object? state) {
			if (LocalPlayer == null) return;
			AllaganToolsState = PluginServices.AllaganTools.IsInitialized();
			GlamourerState = PluginServices.Glamourer.IsInitialized();
			PenumbraState = PluginServices.Penumbra.GetEnabledState();
		}
		public bool MustGlamourerApply() {
			return GlamourerState && !ConfigurationManager.Config.ForceStandaloneAppearanceApply;
		}
	}
}
