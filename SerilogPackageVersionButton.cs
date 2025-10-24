using Inventor;
using System.Runtime.InteropServices;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace IsolatedInventorAddin;

internal class SerilogPackageVersionButton(Inventor.Application inventorApplication)
	 : Button(
		  inventorApplication: inventorApplication,
		  displayName: $"{nameof(Serilog)}\nVersion",
		  internalName: $"Autodesk:{nameof(IsolatedInventorAddin)}:{nameof(SerilogPackageVersionButton)}",
		  commandType: CommandTypesEnum.kNonShapeEditCmdType,
		  clientId: AddinServer.PluginClientId,
		  description: $"Show {nameof(Serilog)} assembly version.",
		  tooltip: $"Show {nameof(Serilog)} assembly version.",
		  standardImage: Properties.Resources.Info32x32,
		  largeImage: Properties.Resources.Info32x32,
		  buttonDisplayType: ButtonDisplayEnum.kDisplayTextInLearningMode)
{
	override protected void ButtonDefinition_OnExecute(NameValueMap context)
	{
		try
		{
			string serilogVersionInfo = GetAssemblyVersionInfo(nameof(Serilog), typeof(Serilog.Log));
			MessageBox.Show(serilogVersionInfo);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
		}
	}

	private static string GetAssemblyVersionInfo(string targetAssemblyName, Type targetAssemblyType)
	{
		try
		{
			var stringBuilder = new System.Text.StringBuilder();

			// Requesting add-in assembly.
			var addinAssembly = typeof(SerilogPackageVersionButton).Assembly;
#if NETCOREAPP
			var addinAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(addinAssembly);
			var addinAssemblyLoadContextName = addinAssemblyLoadContext?.Name ?? "<default>";
#else
			var addinAppDomain = AppDomain.CurrentDomain;
			var addinAppDomainName = addinAppDomain?.FriendlyName ?? "<default>";
#endif

			stringBuilder.AppendLine("****  Requesting Add-In Assembly  ****");
			AppendKeyValuePair(stringBuilder, "Name", addinAssembly.GetName().Name);
#if NETCOREAPP
			AppendKeyValuePair(stringBuilder, "AssemblyLoadContext", addinAssemblyLoadContextName);
#else
			AppendKeyValuePair(stringBuilder, "AppDomain", addinAppDomainName);
#endif
			AppendKeyValuePair(stringBuilder, "Version", addinAssembly.GetName().Version?.ToString());
			AppendKeyValuePair(stringBuilder, "Path", addinAssembly.Location);
			stringBuilder.AppendLine();

			// Assembly used for target type.
			var usedTargetAssembly = targetAssemblyType.Assembly;
#if NETCOREAPP
			var usedTargetAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(usedTargetAssembly);
			var usedTargetAssemblyLoadContextName = usedTargetAssemblyLoadContext?.Name ?? "<default>";
#else
			var usedTargetAppDomain = AppDomain.CurrentDomain;
			var usedTargetAppDomainName = addinAppDomain?.FriendlyName ?? "<default>";
#endif

			stringBuilder.AppendLine($"****  Target '{targetAssemblyName}' Assembly Actually Used  ****");
			AppendKeyValuePair(stringBuilder, "Name", usedTargetAssembly.GetName().Name);
#if NETCOREAPP
			AppendKeyValuePair(stringBuilder, "AssemblyLoadContext", usedTargetAssemblyLoadContextName);
#endif
			AppendKeyValuePair(stringBuilder, "Version", usedTargetAssembly.GetName().Version?.ToString());
			AppendKeyValuePair(stringBuilder, "Path", usedTargetAssembly.Location);
			stringBuilder.AppendLine();

			// All loaded target assemblies.
#if NETCOREAPP
			stringBuilder.AppendLine($"****  All Loaded \"{targetAssemblyName}\" Assemblies (by AssemblyLoadContext)  ****");
			var groups = AppDomain.CurrentDomain
				 .GetAssemblies()
				 .Where(assembly => string.Equals(assembly.GetName().Name, targetAssemblyName, StringComparison.Ordinal))
				 .Select(assembly => new { Context = AssemblyLoadContext.GetLoadContext(assembly), Assembly = assembly })
				 .GroupBy(x => x.Context?.Name ?? "<default>")
				 .OrderBy(group => group.Key, StringComparer.Ordinal);

			foreach (var group in groups)
			{
				AppendKeyValuePair(stringBuilder, "AssemblyLoadContext", group.Key);
				foreach (var assembly in group)
				{
					AppendKeyValuePair(stringBuilder, "Version", assembly.Assembly.GetName().Version?.ToString());
					AppendKeyValuePair(stringBuilder, "Path", assembly.Assembly.Location);
				}
				stringBuilder.AppendLine();
			}
#else
			stringBuilder.AppendLine($"****  All Loaded '{targetAssemblyName}' Assemblies  ****");
			var assemblies = AppDomain.CurrentDomain
				 .GetAssemblies()
				 .Where(assembly => string.Equals(assembly.GetName().Name, targetAssemblyName, StringComparison.Ordinal))
				 .OrderBy(assembly => assembly.GetName().Version);

			foreach (var assembly in assemblies)
			{
				AppendKeyValuePair(stringBuilder, "Version", assembly.GetName().Version?.ToString());
				AppendKeyValuePair(stringBuilder, "Path", assembly.Location);
				stringBuilder.AppendLine();
			}
#endif

			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			return @$"Error getting assembly version info for '{targetAssemblyName}' with type '{targetAssemblyType}'.
Exception Message: {ex.Message}
Exception Stack Trace: {ex.StackTrace}";
		}
	}

	private static void AppendKeyValuePair(System.Text.StringBuilder stringBuilder, string key, string? value)
		 => stringBuilder.Append($"     {key}").Append(": ").AppendLine(value ?? "<n/a>");
}


internal abstract class Button : IDisposable
{
	private ButtonDefinition? _buttonDefinition;
	private readonly ButtonDefinitionSink_OnExecuteEventHandler? _buttonDefinition_OnExecuteEventDelegate;
	private bool _disposed;

	public Inventor.ButtonDefinition? ButtonDefinition => _buttonDefinition;

	public Button(
		 Inventor.Application inventorApplication,
		 string displayName,
		 string internalName,
		 CommandTypesEnum commandType,
		 string clientId,
		 string description,
		 string tooltip,
		 Image standardImage,
		 Image largeImage,
		 ButtonDisplayEnum buttonDisplayType)
	{
		try
		{
			stdole.IPictureDisp standardImageDisp = PictureConverter.ImageToPictureDisp(standardImage);
			stdole.IPictureDisp largeImageDisp = PictureConverter.ImageToPictureDisp(largeImage);

			_buttonDefinition = inventorApplication!.CommandManager.ControlDefinitions.AddButtonDefinition(
				 displayName,
				 internalName,
				 commandType,
				 clientId,
				 description,
				 tooltip,
				 standardImageDisp,
				 largeImageDisp,
				 buttonDisplayType);

			_buttonDefinition.Enabled = true;

			_buttonDefinition_OnExecuteEventDelegate = new ButtonDefinitionSink_OnExecuteEventHandler(ButtonDefinition_OnExecute);
			_buttonDefinition.OnExecute += _buttonDefinition_OnExecuteEventDelegate;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
		}
	}

	public void Dispose()
	{
		if (_disposed) return;

		try
		{
			if (_buttonDefinition is not null && _buttonDefinition_OnExecuteEventDelegate is not null)
				_buttonDefinition.OnExecute -= _buttonDefinition_OnExecuteEventDelegate;
		}
		catch { }
		finally
		{
			if (_buttonDefinition is not null)
				Marshal.ReleaseComObject(_buttonDefinition);

			_buttonDefinition = null;
			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}

	abstract protected void ButtonDefinition_OnExecute(NameValueMap context);

	/// <summary>
	/// Gets the standard sized icon (32px x 32px) as stdole.IPictureDisp.
	/// Requires stdole Nuget package (https://www.nuget.org/packages/stdole/).
	/// </summary>
	/// <remarks>
	/// Add the image to the project Resources.resx designer in the Properties folder, which will add it to the project resources.
	/// 'Build Action' property on the image must be set to 'EmbeddedResource'.
	/// 'Copy To Output Directory' property on the image must be set to 'Do Not Copy'.
	/// </remarks>
	private class PictureConverter : AxHost
	{
		private PictureConverter() : base(string.Empty) { }

		public static stdole.IPictureDisp ImageToPictureDisp(Image image) => (stdole.IPictureDisp)GetIPictureDispFromPicture(image);
	}
}