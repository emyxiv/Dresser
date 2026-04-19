using Dresser.Interop.Agents;
using Dresser.Services.Ipc;
using Dresser.Models;

using SubKindsPlayerCharacter = Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter;

namespace Dresser.Extensions {
	public static class PlayerCharacterExtention {


		// apply appearance of single item
		public unsafe static void Equip(this SubKindsPlayerCharacter playerCharacter, InventoryItem item, GlamourPlateSlot slot) {
			if (PluginServices.Context.MustGlamourerApply()) {
				//PluginLog.Debug($"Apply Item set with EquipGlamourer through Equip {slot}, {item.FormattedName}");
				//playerCharacter.EquipGlamourer(new Structs.Dresser.InventoryItemSet(slot, item));
				PluginServices.Glamourer.SetItem(playerCharacter, item, slot);
			} else {
				// playerCharacter.EquipStandalone(item, slot);
			}
		}
		public static void SetWeaponVisibility(this SubKindsPlayerCharacter playerCharacter) {
			PluginServices.Glamourer.SetMetaData(playerCharacter, GlamourerService.MetaData.Weapon, ConfigurationManager.Config.CurrentGearDisplayWeapon);
		}
		public static void SetHatVisibility(this SubKindsPlayerCharacter playerCharacter) {
			PluginServices.Glamourer.SetMetaData(playerCharacter, GlamourerService.MetaData.Hat, ConfigurationManager.Config.CurrentGearDisplayHat);
		}
		public static void SetVisorVisibility(this SubKindsPlayerCharacter playerCharacter) {
			PluginServices.Glamourer.SetMetaData(playerCharacter, GlamourerService.MetaData.Visor, ConfigurationManager.Config.CurrentGearDisplayVisor);
		}
		public static void SetMetaVisibility(this SubKindsPlayerCharacter playerCharacter) {
			PluginServices.Glamourer.SetMetaData(playerCharacter, new() {
				{ GlamourerService.MetaData.Weapon, ConfigurationManager.Config.CurrentGearDisplayWeapon},
				{ GlamourerService.MetaData.Hat, ConfigurationManager.Config.CurrentGearDisplayHat},
				{ GlamourerService.MetaData.Visor, ConfigurationManager.Config.CurrentGearDisplayVisor},
			});
		}


	}
}
