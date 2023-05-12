﻿using CriticalCommonLib;
using CriticalCommonLib.Extensions;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dresser.Interop.Hooks;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dresser.Logic {
	internal class Context : IDisposable {

		public bool IsGlamingAtDresser = false;

		public PlayerCharacter? LocalPlayer = null;
		public CharacterRace? LocalPlayerRace = null;
		public CharacterSex? LocalPlayerGender = null;
		public ClassJob? LocalPlayerClass = null;
		public byte LocalPlayerLevel = 0;



		public Context() {
			Refresh();
		}
		public void Dispose() {
			LocalPlayer = null;
			LocalPlayerRace = null;
			LocalPlayerGender = null;
			LocalPlayerClass = null;
			LocalPlayerLevel = 0;
		}

		public void Refresh() {
			IsGlamingAtDresser = GlamourPlates.IsGlamingAtDresser();

			LocalPlayer = Service.ClientState.LocalPlayer;
			if (LocalPlayer == null) return;
			LocalPlayerRace = (CharacterRace)(LocalPlayer.Customize[(int)CustomizeIndex.Race]);
			LocalPlayerGender = (LocalPlayer.Customize[(int)CustomizeIndex.Gender]) == 0 ? CharacterSex.Male : CharacterSex.Female;
			LocalPlayerClass = LocalPlayer.ClassJob.GameData;
			LocalPlayerLevel = LocalPlayer.Level;
		}
	}
}
