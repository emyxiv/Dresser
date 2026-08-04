using System;
using System.Collections.Generic;
using System.Linq;

using Dresser.Logic;
using Dresser.Logic.Ipc.Glamourer;
using Dresser.Models;

using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;

using Newtonsoft.Json.Linq;

namespace Dresser.Services.Ipc {
	internal partial class GlamourerService {
        private AddDesign _addDesignSubscriber;
        private GetDesignList _getDesignListSubscriber;
        private GetDesignJObject _getDesignJObjectSubscriber;
        

        public GlamourerApiEc AddDesign(JObject designData, string designName, out Guid createdGuid)
            => _addDesignSubscriber.Invoke(Design.ShareBase64(designData), designName, out createdGuid); 
        public Dictionary<Guid, string> GetDesignList()
            => _getDesignListSubscriber.Invoke();
        public JObject? GetDesignJObject(Guid designId)
            => _getDesignJObjectSubscriber.Invoke(designId);


        // todo: add designs from InventoryItemSet, including mods for modded items.
        public void AddDesignFromItemSet(InventoryItemSet itemSet, string designName, out Guid createdGuid) {
            var designData = CreateDesignDataFromItemSet(itemSet, designName);
            var result = this.AddDesign(designData, designName, out createdGuid);
            if (result != GlamourerApiEc.Success) {
                PluginLog.Error($"Failed to add design from item set with name {designName}. Error code: {result}");
            }

        }

        private static JObject CreateDesignDataFromItemSet(InventoryItemSet itemSet, string designName) {
            
            var design = FakeDesign();
            design["Name"] = designName;
            design["Description"] = $"Some [{designName}]";

            PluginLog.Debug($"Created design from itemSet with identifier {design["Identifier"]}");
            Design.SerializeEquipment(ref design, itemSet);
            if (itemSet.HasModdedItem()) {
                var mods = itemSet.Items.Values.DistinctBy(f=>f?.ModDirectory).Select(i => {
                    if(i == null || !i.IsModded()) return null;
                    var settings = PluginServices.Penumbra.GetCurrentSettingsForMod(i);
                    if(settings == null) return null;
                    var jObjectSettings = JObject.FromObject(settings.Value.Options);

                    return new JObject {
                        ["Name"] =  i.ModName,
                        ["Directory"] =  i.ModDirectory,
                        ["Enabled"] =  true,
                        ["Priority"] =  0,
                        ["Settings"] =  jObjectSettings,
                    };
                }).Where(j=>j != null).Distinct();
                design["Mods"] = new JArray(mods);
            } else {
                design["Mods"] = new JArray();
            }

            return design;
        }

        private static JObject EmptyDesign() {
            var design = new JObject();
            return design;
        }

        private static JObject FakeDesign() {
       
            PluginLog.Debug("about to import design from stub");
            // design stub, it's a generic male midlander, stripped of all unnecessary info
            string stubDesign = "Bh+LCAAAAAAAAArEWF1v4jgU/SuVXzcgJ3E+39oCLTt02i1MuxLiwU1uwGpIWNtplUX895WTNA0lE9Bq2H0j5vice6+vj51s0YjF8ARcsDRBvqGhcQiJZBEDjnyEHaoH1LJ7VhiQHnFA77lR5PRwBHZECMH6CyANXXOgkqXJgEpAPjKwYfew1TPMmYF90/JN3Hccy3Jt8zeMfYyRhiZUyGHI5CHc9ondt0xCTEuv4d/pWjFfXl0PhqMbrHuu7rkEaWgAIuBso9SRj6bpGi7mX1ALpKFRygMIHyHk9B35EY0FaOgRBMjL8I0mAYSDHATycTU6g/Um5ZTnU5CSJUvRmPQG/HDWdRqnqmJIQ39kLHgdgGDLBPmSZ6ChGVUM84WGnjmT8MBTCYGEsGYd/pWxzRoSifwtuqMsuaVJqH6PJazHIfJ103GsotJC1rMuN5s433+aSsqS/aH9ORUAV78M5OOdhu6j6D9WvAW6J0cMj3i2o9vkfJpXaZi3appnzJMmoWgVNc4nOoHlnqaFXVf/iVzZoU21xkirmGfsq40A5L6aYeNfpvYltSHlreW03POV8zsEr62azvk0nzkTsj1T+3yqjyOWLJX1t8ie0Qwm/4/sLS36drpK3/f6sJq209ATA04/Wq4TJ5T9b9FYzNLlMlbW3gp8BrpRR1UHW2FUSVZI3sRUCHXEbMsxVRmXEGIQj9ge9nSXHM6+zoRM1+xvKA6TNIRYTVNHGw2KsScaZ4B8/SC6G0jCchkqCD6AKA+d5Zt2HpVN4e1suZKdNNcxTTpDGR2L9ZYyLmQeH0Gx5SpW0YjOcKavLKmO8A7UMIcC9Hg0PRXccb7P4I5jRzRgNB4BlRkH/XSocTrUPB1KTodap0Pt06FOJ3QCSxrkMyplmnYCS8hJK//C03dxUntMIOrujmEO0xXd20MtLbmmcTzmrFvzeyq6aX5XF96O/+/STK66i8k2QrLyBOwCHa/iXSaCGO6o6M5pRll8vD5XmZDTyuS6egYeKCvv1cdRxeVeKPM+BX084WeQCexl23KGKct+oJyuQUJ50NQKP97usliyTVy8jn3aXL8jqh9v91EkyhvZR2D9n63FLE2KAj4ADyCRdFmgScuGiuSErV9oPE4kJILJ/HDaoUhhlP9inrLjAYuirGzuR7UiuO8RD+ueraEbDqBO875HdN2xLQ1dVXl6uukY6srQ4seHhOoKXnOZtq4Tw/vkMrDpuRiTVrLauht0ju0YtkkanMQxTbe4mtakpkUcx3BaKzzMm9FZxPQ83WnyuRYxdK8Ro2vprunZdnvlfyFfZbt1y3+k7NjEwc0lqUc+KOuBQ8c4XBHcYMKfHKpD4s2KtnfLAAIaf43MM7CpCv/ZK9VA3SrVc01seYZu2d7h5ryjEjijsdqbhWOqF7r59uOjxDf2zi56F/P7TEZMisWUrTfK5XLDVB8oGIdApjxHPnqtkGmFFAVyXSGHCX1pXBwfOEt5sV/UrbX+DLFFV1TAhfpso6JoPi1KU7woXXGOvnGaSeB/TtFit1uoiievJQNEKVeQhYYuI6m8Zb7Y7f4BAAD//w==";
            var design = Design.FromBase64v6(stubDesign);
            design["Identifier"] = Guid.NewGuid();
            design["CreationDate"] = new DateTimeOffset();
            design["LastEdit"] = new DateTimeOffset();
            design["WriteProtected"] = false;

            // remove all applies, we only want the new ones
            Design.TurnOffAllApplies(ref design);

            return design;
        }
    }
}

