using System;
using System.Linq;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using Lumina.Excel.Sheets;

using Action = Lumina.Excel.Sheets.Action;

namespace Dresser.Services
{
    internal unsafe class Actions : IDisposable
    {


        private const ushort ChangePostureEmoteId = 90;
        public Actions() {
            // _actionManager = ActionManager.Instance();
            
            // _actionManager->
            // ChangePostureEmoteId = PluginServices.DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Emote>().Where(e => e.TextCommand == "/changepose");

        }

        public void ExecuteChangePosture()
            => PluginServices.Framework.RunOnFrameworkThread(() => { AgentEmote.Instance()->ExecuteEmote(ChangePostureEmoteId, null, false, false); });
        
        public void ExcuteAction()
        {
            PluginServices.Framework.RunOnFrameworkThread(() =>
            {
                // var controlInstance = Control.Instance();
                // if(controlInstance == null) return;
                // var localPlayer = controlInstance->LocalPlayer;

                // var actionManager = ActionManager.Instance();
                // if (actionManager == null) return;
                
                                

                // var zzz = ActionManager.Instance()->UseAction(ActionType.Action,1,);
                
                // localPlayer->EmoteController.

            });
            // _actionManager->UseAction(ActionType.GeneralAction,)
        }
        public byte GetAvailablePoses() => EmoteController.GetAvailablePoses(EmoteController.PoseType.Idle);
        public EmoteController? GetEmoteController() {
            var controlInstance = Control.Instance();
            if(controlInstance == null) return null;
            var localPlayer = controlInstance->LocalPlayer;
            return localPlayer->EmoteController;
        }
        public EmoteController.PoseType? GetPoseKind() {
            var zz = GetEmoteController();
            if(zz == null) return null;
            
            return (EmoteController.PoseType)zz.Value.GetPoseKind();
        }

        public void EmoteList() {
            var emotes = PluginServices.DataManager.Excel.GetSheet<Emote>();
            foreach (var emote in emotes) {
                var actionTl = string.Join(",",emote.ActionTimeline.Select(a=>a.RowId).ToArray());
                var textCommand = emote.TextCommand.ValueNullable;
                PluginLog.Debug($"{emote.RowId}: {emote.Name} {textCommand?.Command}" +
                                $" {textCommand?.Alias} {textCommand?.ShortAlias} {textCommand?.ShortCommand}" +
                                $" {actionTl} {emote.Unknown0} {emote.Unknown1} {emote.Unknown2} {emote.Unknown3} {emote.Unknown4} {emote.Unknown5} {emote.Unknown6}");
            }
        }
        public void ActionList() {
            var actions = PluginServices.DataManager.Excel.GetSheet<Action>();
            foreach (var action in actions.Where(a=>a.Name.ToString().Contains("Change"))) {
                PluginLog.Debug($"{action.RowId}: {action.Name} {action.ActionCategory.Value.Name}");
            }
        }

        public ushort[] EmoteHistory() => EmoteHistoryModule.Instance()->History.ToArray();
        public ushort[] EmoteFavorites() => EmoteHistoryModule.Instance()->Favorites.ToArray();

        public void Dispose()
        {
            
        }
    }
}