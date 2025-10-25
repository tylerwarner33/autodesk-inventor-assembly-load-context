# Isolated Inventor Add-in Sample

![Inventor Versions](https://img.shields.io/badge/Inventor-2023--2026-blue.svg)
[![.NET Versions](https://img.shields.io/badge/.NET-4.8--8.0-blue.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

A minimal Autodesk Inventor add-in sample that demonstrates safe dependency isolation. Inventor ships with specific assembly versions. Referencing newer (or different) versions in an add-in often causes binding conflicts. This sample allows the use of NuGet package/assembly versions that differ from the ones bundled with Inventor by isolating the add-in’s dependencies in a custom load context.

![SerilogVersion](Images/Ribbon_SerilogVersion.png)
![Inventor 2026.1](Images/Dialog_Inventor2026.1.png)
![Inventor 2023.6](Images/Dialog_Inventor2023.6.png)

## Setup

### Run Locally

1. Clone this project or download it. It's recommended to install [GitHub Desktop](https://desktop.github.com/). To clone it via command line, use the following:
```
git clone https://github.com/tylerwarner33/autodesk-inventor-assembly-load-context.git
```
2. Choose a configuration: `Debug-2026`, `Debug-2025`, `Debug-2024`, `Debug-2023` (or the corresponding `Release-*`) to work with that Inventor version.
3. Build the solution. The post-build step creates a bundle and copies it to (replace `<version>` and paste in file explorer):
   - `%AppData%\Autodesk\Inventor <version>\Addins\IsolatedInventorAddin.bundle\Contents\`
4. Run the solution (Inventor will launch automatically using the selected version, as configured in the project file).
5. Use the `Serilog Version` button to display the version/context of the loaded Serilog assembly.

### How It Works

- `AddinServer.cs`
  - __The main entry point for the add-in using the `OnActivate()` and `OnDeactivate()` methods. Custom add-in logic added here.__
  - Inherits `Isolation/IsolatedApplicationAddInServer.cs` which implements `Inventor.ApplicationAddInServer`.
- .NET 8 / Inventor 2025+:
  - `Isolation/AddinLoadContext.cs` defines a dedicated `AssemblyLoadContext` and uses `AssemblyDependencyResolver` so the add-in resolves managed and native dependencies from its own output folder first.
- .NET Framework 4.8 / Inventor 2023–2024:
  - The add-in runs in the default `AppDomain`. The sample still reports assembly identity to compare behavior across targets.
- The Interop assembly (`Autodesk.Inventor.Interop`) is referenced with `<Private>False</Private>` so it’s always loaded from Inventor, while the NuGet dependencies (ex. `Serilog`) are copied beside the add-in and loaded in isolation.
- The `Serilog Version` button (`SerilogPackageVersionButton`) shows the loaded Serilog version and the load context/app domain to verify isolation.
- The project uses MSBuild logic to pick the Inventor version and target framework based on the build configuration.

## Resources

- Microsoft Learn: [System.Runtime.Loader.AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- Microsoft GitHub: [AssemblyLoadContext](https://github.com/dotnet/coreclr/blob/v2.1.0/Documentation/design-docs/assemblyloadcontext.md)

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for more details.
