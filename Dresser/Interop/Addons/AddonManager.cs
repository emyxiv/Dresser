using System;
using System.Collections.Generic;
using System.Linq;

namespace Dresser.Interop.Addons {
	// The classes AddonManager and ReceiveEventArgs are from DailyDuty plugin.
	// MiragePrismMiragePlateAddon class is strongly inspired from the different addons managed in DailyDuty.
	// Thank you MidoriKami <3

	// their role is to manage events for any kind of AgentInterface
	internal class AddonManager : IDisposable {
		private readonly List<IDisposable> addons = new()
		{
			new MiragePrismMiragePlateAddon(),
			new MiragePrismPrismBoxAddon(),
		};

		public void Dispose() {
			foreach (var addon in addons) {
				addon.Dispose();
			}
		}

		public T Get<T>() {
			return addons.OfType<T>().First();
		}
	}

}
