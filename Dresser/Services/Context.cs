using System;
using System.Linq;
using System.Threading;

using AllaganLib.GameSheets.Model;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Config;
using Dalamud.Interface.Windowing;

using Dresser.Interop.Addons;
using Dresser.Interop.Hooks;

using Lumina.Excel.Sheets;

namespace Dresser.Services {
	internal class Context : IDisposable {

		public bool IsGlamingAtDresser = false;
		public bool IsCurrentGearWindowOpen = false;
		public bool IsBrowserWindowOpen = false;
		public bool IsDyePickerPopupOpen = false;
		public bool IsAnyPlateSelectionOpen => GlamourPlates.IsAnyPlateSelectionOpen();
		public ushort? SelectedPlate = null;

		public IPlayerCharacter? LocalPlayer = null;
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
		public bool ChangePostureConfigState = false;
		public uint ChangePostureConfigTime = 0;
		public int PenumbraModCountInApplyCollection = 0;



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

		public bool HasConfirmedApplyIntoDresser = false;
		public bool IsApplyingIntoDresser => PluginServices.ApplyGearChange.DifferencesToApply.Count > 0 && HasConfirmedApplyIntoDresser;

		private bool _lastState_IsGlamingAtDresser = false;
		public ushort? LastState_SelectedPlate = null;
		public delegate void OnChangeGlamingAtDresserDelegate(bool newIsGlamingAtDresser);
		public static event OnChangeGlamingAtDresserDelegate? OnChangeGlamingAtDresser;

		public Window? LastFocusedWindow = null;
		public void Refresh() {
			IsGlamingAtDresser = GlamourPlates.IsGlamingAtDresser();
			if (IsGlamingAtDresser != _lastState_IsGlamingAtDresser) OnChangeGlamingAtDresser?.Invoke(IsGlamingAtDresser);
			_lastState_IsGlamingAtDresser = IsGlamingAtDresser;

			if (IsGlamingAtDresser) {
				SelectedPlate = PluginServices.GlamourPlates.CurrentPlateIndex();
				if(SelectedPlate != null && SelectedPlate != LastState_SelectedPlate) {
					AddonListeners.TriggerPlateChanged(SelectedPlate, LastState_SelectedPlate);
				}
				LastState_SelectedPlate = SelectedPlate;
			} else {
				LastState_SelectedPlate = null;
				SelectedPlate = null;
			}

			try { IsCurrentGearWindowOpen = Plugin.GetInstance()?.IsDresserVisible() ?? false; } catch (Exception) { IsCurrentGearWindowOpen = false; }

			if (IsCurrentGearWindowOpen) {
				IsBrowserWindowOpen = Plugin.GetInstance()?.IsBrowserVisible() ?? false;
				var windowSystem = Plugin.GetInstance().WindowSystem;
				if (windowSystem.HasAnyFocus)
					LastFocusedWindow = windowSystem.Windows.FirstOrDefault(w => w.IsFocused);
			} else {
				LastFocusedWindow = null;
				IsBrowserWindowOpen = false;
			}


			LocalPlayer = PluginServices.ClientState.LocalPlayer;
			if (LocalPlayer != null) {
				LocalPlayerCharacterId = PluginServices.ClientState.LocalContentId;
				LocalPlayerRace = (CharacterRace)LocalPlayer.Customize[(int)CustomizeIndex.Race];
				LocalPlayerGender = LocalPlayer.Customize[(int)CustomizeIndex.Gender] == 0 ? CharacterSex.Male : CharacterSex.Female;
				LocalPlayerClass = LocalPlayer.ClassJob.Value;
				LocalPlayerLevel = LocalPlayer.Level;
			}

		}

		private void ExecutedEvery5Seconds(object? state) {
			if (LocalPlayer == null) return;
			AllaganToolsState = PluginServices.AllaganTools.IsInitialized();
			GlamourerState = PluginServices.Glamourer.IsInitialized();
			PenumbraState = PluginServices.Penumbra.GetEnabledState();
			PenumbraModCountInApplyCollection = PluginServices.Penumbra.CountModsDresserApplyCollection();


			PluginServices.Framework.RunOnFrameworkThread(() => {
				if (PluginServices.GameConfig.TryGet(UiConfigOption.IdleEmoteRandomType, out bool zzz)) {
					ChangePostureConfigState = zzz;
				}
				if (PluginServices.GameConfig.TryGet(UiConfigOption.IdleEmoteTime, out uint zzz1)) {
					ChangePostureConfigTime = zzz1;
				}
			});
		}
		public bool MustGlamourerApply() {
			return
				true
				//GlamourerState
				//&& !ConfigurationManager.Config.ForceStandaloneAppearanceApply
				;
		}
	}
}
