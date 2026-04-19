namespace Dresser.Tests;

/// <summary>
/// Configuration cannot currently be tested outside game runtime because
/// field initializers at line 161-162 access PluginServices.Storage.MaxEquipLevel/MaxItemLevel.
/// To make this testable, those field initializers would need to use lazy evaluation
/// or be set in Load() instead of the constructor.
/// </summary>
public class ConfigurationTests {

	[Fact(Skip = "Requires game runtime — Configuration field initializers access PluginServices.Storage")]
	public void Constructor_CreatesWithDefaults() {
		var config = new Configuration();

		Assert.Equal(0, config.Version);
		Assert.False(config.Debug);
		Assert.False(config.EnablePenumbraModding);
		Assert.True(config.ForceStandaloneAppearanceApply);
		Assert.False(config.DebugDisplayModedInTitleBar);
	}

	[Fact(Skip = "Requires game runtime — Configuration field initializers access PluginServices.Storage")]
	public void Constructor_DisplayPlateItemsIsNotNull() {
		var config = new Configuration();

		Assert.NotNull(config.DisplayPlateItems);
	}
}
