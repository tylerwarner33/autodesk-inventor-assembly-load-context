# IsolatedInventorAddin

A minimal Autodesk Inventor add-in that demonstrates safe dependency isolation. It allows you to use NuGet package/assembly versions that differ from the ones bundled with Inventor by loading your add-in’s dependencies into a custom load context.

## Why

Inventor ships with specific assembly versions. Referencing newer (or different) versions in an add-in often causes binding conflicts. This sample avoids those conflicts by isolating your add-in’s dependencies so they do not collide with Inventor’s copies.

## How it works

- .NET 8 / Inventor 2025+:
  - `Isolation/AddinLoadContext.cs` defines a dedicated `AssemblyLoadContext` and uses `AssemblyDependencyResolver` so your add-in resolves managed and native dependencies from its own output folder first.
- .NET Framework 4.8 / Inventor 2023–2024:
  - The add-in runs in the default `AppDomain`. The sample still reports assembly identity so you can compare behavior across targets.
- The Interop assembly (`Autodesk.Inventor.Interop`) is referenced with `<Private>False</Private>` so it’s always loaded from Inventor, while your NuGet dependencies (e.g., `Serilog`) are copied beside the add-in and loaded in isolation.
- The `Serilog Version` button (`SerilogPackageVersionButton`) shows the loaded Serilog version and the load context/app domain to verify isolation.
- The project uses MSBuild logic to pick the Inventor version and target framework based on the build configuration.

## Build and run

1. Choose a configuration: `Debug-2023`, `Debug-2024`, `Debug-2025`, `Debug` (2026) (or the corresponding `Release-*`).
2. Build the solution. The post-build step creates a bundle and copies it to:
   - `$(AppData)\Autodesk\Inventor $(InventorVersion)\Addins\IsolatedInventorAddin.bundle\Contents\`
3. Start Inventor (F5 launches `Inventor.exe` as configured in the project).
4. Use the `Serilog Version` button to display the version/context of the loaded Serilog assembly.

## Switching Inventor versions

- Pick the matching build configuration (`Debug-2023`, `Debug-2024`, `Debug-2025`, `Debug` (2026)).

## Notable settings/packages

- `CopyLocalLockFileAssemblies=true` ensures NuGet dependencies are copied next to the add-in.
- NuGet: `Serilog` (demonstration of isolated package versions).

## What to look at

- `Isolation/AddinLoadContext.cs` - custom `AssemblyLoadContext` and resolution.
- `SerilogPackageVersionButton.cs` - UI command showing assembly version and load context.
- `IsolatedInventorAddin.csproj` - multi-target/version selection and packaging.

## Resources

- Microsoft Learn: [System.Runtime.Loader.AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- Microsoft GitHub: [AssemblyLoadContext](https://github.com/dotnet/coreclr/blob/v2.1.0/Documentation/design-docs/assemblyloadcontext.md)
