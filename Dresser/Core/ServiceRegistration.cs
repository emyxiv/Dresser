using AllaganLib.GameSheets.Service;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Dresser.Interop.Agents;
using Dresser.Services;
using Dresser.Services.Ipc;

using Microsoft.Extensions.DependencyInjection;

namespace Dresser.Core;

internal static class ServiceRegistration
{
	internal static ServiceProvider ConfigureServices(IDalamudPluginInterface pluginInterface, Plugin plugin)
	{
		var services = new ServiceCollection();

		// Dalamud services (pre-existing instances — NOT disposed by the container)
		services.AddSingleton(pluginInterface);
		services.AddSingleton(PluginServices.CommandManager);
		services.AddSingleton(PluginServices.ClientState);
		services.AddSingleton(PluginServices.PlayerState);
		services.AddSingleton(PluginServices.DataManager);
		services.AddSingleton(PluginServices.TargetManager);
		services.AddSingleton(PluginServices.TextureProvider);
		services.AddSingleton(PluginServices.SigScanner);
		services.AddSingleton(PluginServices.KeyState);
		services.AddSingleton(PluginServices.DalamudGameGui);
		services.AddSingleton(PluginServices.PluginLog);
		services.AddSingleton(PluginServices.Framework);
		services.AddSingleton(PluginServices.ChatGui);
		services.AddSingleton(PluginServices.Objects);
		services.AddSingleton(PluginServices.GameConfig);
		services.AddSingleton(PluginServices.Condition);
		services.AddSingleton(PluginServices.GameInteropProvider);

		// Plugin instance (pre-existing — NOT disposed by the container)
		services.AddSingleton(plugin);

		// Core services
		services.AddSingleton<SheetManager>(sp =>
		{
			var dm = sp.GetRequiredService<IDataManager>();
			return new SheetManager(pluginInterface, dm.GameData, new SheetManagerStartupOptions
			{
				BuildNpcLevels = true,
				BuildNpcShops = true,
				BuildItemInfoCache = true,
				CalculateLookups = true,
				PersistInDataShare = true,
				CacheInDataShare = true,
			});
		});
		services.AddSingleton<InventoryItemFactory>();
		services.AddSingleton<Context>();

		// Services
		services.AddSingleton<HotkeyService>();
		services.AddSingleton<ImageGuiCrop>();
		services.AddSingleton<UldPartResolver>();
		services.AddSingleton<PenumbraIpc>();
		services.AddSingleton<Storage>();
		services.AddSingleton<ModdedIconStorage>();
		services.AddSingleton<ItemVendorLocation>();
		services.AddSingleton<AllaganToolsService>();
		services.AddSingleton<GlamourerService>();
		services.AddSingleton<MiragePlateAgent>();
		services.AddSingleton<ApplyGearChange>();
		services.AddSingleton<Actions>();

		return services.BuildServiceProvider();
	}
}
