using System;
using System.Linq;

using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;

using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using Lumina.Excel.Sheets;

using Newtonsoft.Json;

namespace Dresser.Structs.Dresser
{
    public partial class InventoryItem
    {
        public static InventoryItem FromNumeric(ulong[] serializedItem)
        {
            var gearSetLengh = serializedItem.Length - 25;
            var gearSets = gearSetLengh > 0 ? new ArraySegment<ulong>(serializedItem, 25, serializedItem.Length - 25).Select(i => (uint)i).ToArray() : null;

            var inventoryItem = new InventoryItem {
                Container = (InventoryType)serializedItem[0],
                Slot = (short)serializedItem[1],
                ItemId = (uint)serializedItem[2],
                Quantity = (uint)serializedItem[3],
                Spiritbond = (ushort)serializedItem[4],
                Condition = (ushort)serializedItem[5],
                Flags = (FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags)serializedItem[6],
                Materia0 = (ushort)serializedItem[7],
                Materia1 = (ushort)serializedItem[8],
                Materia2 = (ushort)serializedItem[9],
                Materia3 = (ushort)serializedItem[10],
                Materia4 = (ushort)serializedItem[11],
                MateriaLevel0 = (byte)serializedItem[12],
                MateriaLevel1 = (byte)serializedItem[13],
                MateriaLevel2 = (byte)serializedItem[14],
                MateriaLevel3 = (byte)serializedItem[15],
                MateriaLevel4 = (byte)serializedItem[16],
                Stain = (byte)serializedItem[17],
                Stain2 = (byte)serializedItem[18],
                GlamourId = (uint)serializedItem[19],
                SortedContainer = (InventoryType)serializedItem[20],
                SortedCategory = (InventoryCategory)serializedItem[21],
                SortedSlotIndex = (int)serializedItem[22],
                RetainerId = serializedItem[23],
                RetainerMarketPrice = (uint)serializedItem[24],
                GearSets = gearSets,
            };

            return inventoryItem;
        }
    }
}