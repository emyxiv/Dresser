using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dresser {
	internal class EventManager {


		public delegate void OnGearSelectionOpen();
		public static OnGearSelectionOpen? GearSelectionOpen = null;

		public delegate void OnGearSelectionClose();
		public static OnGearSelectionClose? GearSelectionClose = null;


	}
}
