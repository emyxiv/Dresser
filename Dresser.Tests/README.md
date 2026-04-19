# Dresser.Tests — Unit Testing Guide

## Prerequisites

- .NET 10 SDK
- Dalamud dev libraries installed at `%APPDATA%\XIVLauncher\addon\Hooks\dev\`
  (automatically installed when you run FFXIV with Dalamud in dev mode at least once)

## Running Tests

### Terminal (any IDE)

From the solution root (`Dresser/`):

```powershell
# Run all tests
dotnet test Dresser.Tests/Dresser.Tests.csproj

# Verbose output (see each test name and result)
dotnet test Dresser.Tests/Dresser.Tests.csproj --verbosity normal

# Filter by test class
dotnet test Dresser.Tests/Dresser.Tests.csproj --filter "InventoryItemOrder"

# Filter by single test name
dotnet test Dresser.Tests/Dresser.Tests.csproj --filter "Defaults_ReturnsNonEmptyList"
```

### VS Code

1. Install the **C# Dev Kit** extension (if not already installed).
2. Open the **Testing** panel — beaker icon in the sidebar, or `Ctrl+Shift+P` → `Testing: Focus on Test Explorer View`.
3. Tests are discovered automatically. Click the play button next to any test or class to run it.
4. To debug a test, click the debug icon next to it — breakpoints work normally.

### Visual Studio

1. Open `Dresser.sln`.
2. Go to **Test → Test Explorer** (`Ctrl+E, T`).
3. Click **Run All** or right-click individual tests to run/debug them.
4. Failed tests show the assertion message and a clickable stack trace.

### JetBrains Rider

1. Open `Dresser.sln`.
2. Tests appear in the **Unit Tests** tool window (or in the gutter next to each `[Fact]`/`[Theory]`).
3. Click the green play icon next to any test, class, or the project node.
4. Use **Run with Coverage** for a coverage report.

## Reading Test Output

| Status      | Meaning |
|-------------|---------|
| **Passed**  | All assertions held. |
| **Failed**  | An `Assert` didn't hold, or an unhandled exception was thrown. The output shows which assertion failed and the exact line number. |
| **Skipped** | The test has `Skip = "reason"` — it was intentionally excluded (e.g. requires game runtime). |

A healthy run:
```
Test Run Successful.
     Passed: 6
    Skipped: 2
```

A failure shows the exact mismatch:
```
Assert.Equal() Failure
Expected: Descending
Actual:   Ascending
   at InventoryItemOrderTests.Defaults_StartsWithItemLevelDescending() in ...Tests.cs:line 19
```

## Writing New Tests

### File & naming conventions

- One file per class being tested: `{ClassName}Tests.cs`
- Test method naming: `MethodName_Scenario_ExpectedResult`
- Place test files directly in `Dresser.Tests/` (flat structure is fine for now)

### Minimal test

```csharp
namespace Dresser.Tests;

public class MyFeatureTests {

    [Fact]
    public void MyMethod_WhenCalledWithX_ReturnsY() {
        // Arrange
        var input = 42;

        // Act
        var result = MyClass.DoSomething(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Parameterized tests

Use `[Theory]` + `[InlineData]` to run the same logic with different inputs:

```csharp
[Theory]
[InlineData("Ilvl & Lvl", 2)]
[InlineData("Newer first", 2)]
public void DefaultSets_PresetHasExpectedCount(string key, int expectedCount) {
    var sets = InventoryItemOrder.DefaultSets();
    Assert.Equal(expectedCount, sets[key].Count);
}
```

### Common assertions

```csharp
Assert.Equal(expected, actual);                      // value equality
Assert.True(condition);                              // bool check
Assert.False(condition);
Assert.NotNull(obj);
Assert.Null(obj);
Assert.Empty(collection);
Assert.NotEmpty(collection);
Assert.Contains(item, collection);
Assert.DoesNotContain(item, collection);
Assert.Throws<SomeException>(() => SomeMethod());   // expect a specific exception
Assert.InRange(value, low, high);                    // numeric range
```

### What CAN be tested (no game runtime needed)

- Pure logic methods that take inputs and return outputs
- Enums, constants, default values
- Static helper methods that don't touch `PluginServices`
- Data structures and their initialization

### What CANNOT be tested (requires game runtime)

Anything that accesses these at construction time or in the method body will crash:

- `PluginServices.*` (static service locator — requires Dalamud injection)
- `ConfigurationManager.Config` (populated by Dalamud)
- `unsafe` code / game memory pointers (`FFXIVClientStructs`)
- ImGui rendering calls
- IPC calls to other plugins (Glamourer, Penumbra, etc.)

If you need to test logic that currently depends on `PluginServices`, the path forward is:
1. Extract the pure logic into a method that takes parameters instead of reading statics
2. Or inject dependencies via constructor (the DI container supports this)
3. Mark tests that require runtime with `[Fact(Skip = "Requires game runtime — reason")]`

## Release builds

The test project **never** ships with the plugin:

- `Dresser.Tests` references `Dresser`, not the other way around
- `<IsPackable>false</IsPackable>` prevents NuGet packaging
- DalamudPackager only packages the `Dresser` project output
- `dotnet build Dresser/Dresser.csproj -c Release` produces a clean plugin with no test dependencies

Building the full solution (`dotnet build Dresser.sln`) compiles the test project but does **not** run tests — tests only execute via `dotnet test`.
