using Inventor;

namespace IsolatedInventorAddin.Isolation;

/// <summary>
/// <see cref="ApplicationAddInServer" /> is the standard entry point of the Inventor addin.
/// This class is extended to have fully isolated addin dependency container.
/// Inherit this class and add custom logic to overrides of <see cref="OnActivate" /> or <see cref="OnDeactivate" />.
/// </summary>
/// <remarks>
/// Must set 'Private' to 'False' on 'Autodesk.Inventor.Interop' project file reference.
/// References:
/// <a href="https://github.com/Nice3point/RevitToolkit/blob/06be21872e7b4b0f0ed0c246f57df79608a90476/source/Nice3point.Revit.Toolkit/External/ExternalApplication.cs">
///	GitHub/RevitToolkit/ExternalApplication.cs
/// </a>
/// </remarks>
public abstract class IsolatedApplicationAddInServer : ApplicationAddInServer
{
#if NETCOREAPP
	private object? _isolatedInstance;
#endif

	public object? Automation { get; set; } = null;

	/// <summary>
	///	Reference to the parameter in <see cref="ApplicationAddInServer.Activate" />.
	/// </summary>
	public ApplicationAddInSite ApplicationAddInSite { get; private set; } = default!;

	/// <summary>
	///	Reference to the parameter in <see cref="ApplicationAddInServer.Activate" />.
	/// </summary>
	public bool FirstTime { get; private set; } = default!;

	public void Activate(ApplicationAddInSite applicationAddInSite, bool firstTime)
	{
		Type currentType = GetType();

#if NETCOREAPP
		if (AddinLoadContext.CheckIfCustomContext(currentType) is false)
		{
			AddinLoadContext dependenciesProvider = AddinLoadContext.GetDependenciesProvider(currentType);
			_isolatedInstance = dependenciesProvider.CreateAssemblyInstance(currentType);

			AddinLoadContext.Invoke(_isolatedInstance, nameof(Activate), applicationAddInSite, firstTime);
			return;
		}
#endif

		ApplicationAddInSite = applicationAddInSite;
		FirstTime = firstTime;

#if NETCOREAPP
		OnActivate();
#else
		try
		{
			ResolveHelper.BeginAssemblyResolve(currentType);
			OnActivate();
		}
		finally
		{
			ResolveHelper.EndAssemblyResolve();
		}
#endif
	}

	public void Deactivate()
	{
		Type currentType = GetType();

#if NETCOREAPP
		if (AddinLoadContext.CheckIfCustomContext(currentType) is false)
		{
			AddinLoadContext.Invoke(_isolatedInstance!, nameof(Deactivate));
			return;
		}

		OnDeactivate();
#else
		try
		{
			ResolveHelper.BeginAssemblyResolve(currentType);
			OnDeactivate();
		}
		finally
		{
			ResolveHelper.EndAssemblyResolve();
		}
#endif
	}

	[Obsolete("Deprecated in the Inventor API. Required for legacy compatibility.")]
	public void ExecuteCommand(int CommandID) { }

	/// <summary>
	///	Overload this method to execute custom logic when the Inventor addin is loaded and <see cref="ApplicationAddInServer.Activate" /> method is executed.
	/// </summary>
	public abstract void OnActivate();

	/// <summary>
	///	Overload this method to execute custom logic when the Inventor addin is unloaded and <see cref="ApplicationAddInServer.Deactivate" /> method is executed.
	/// </summary>
	public abstract void OnDeactivate();
}