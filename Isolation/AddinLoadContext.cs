#if NETCOREAPP
using Inventor;
using System.Reflection;
using System.Runtime.Loader;

namespace IsolatedInventorAddin.Isolation;

/// <summary>
/// Isolated addin dependency container.
/// </summary>
/// <remarks>
/// <a href="https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support">
///	Microsoft/Tutorials/Plugins
/// </a>,
/// <a href="https://github.com/dotnet/coreclr/blob/v2.1.0/Documentation/design-docs/assemblyloadcontext.md">
///	GitHub/DotNet/CoreCLR
/// </a>
/// </remarks>
internal sealed class AddinLoadContext : AssemblyLoadContext
{
	/// <summary>
	/// Addins contexts storage.
	/// </summary>
	private static readonly Dictionary<string, AddinLoadContext> _dependenciesProviders = new(1);

	private readonly AssemblyDependencyResolver _resolver;

	private const BindingFlags _methodSearchFlags = BindingFlags.Public | BindingFlags.Instance;

	private AddinLoadContext(Type type, string addinName) : base(addinName)
	{
		string addinLocation = type.Assembly.Location;

		_resolver = new AssemblyDependencyResolver(addinLocation);
	}

	/// <summary>
	/// Resolve and load dependency any time one is loaded if it exists in the isolated addin dependency container.
	/// </summary>
	protected override Assembly? Load(AssemblyName assemblyName)
	{
		string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

		return assemblyPath is not null // && System.IO.File.Exists(assemblyPath)
			 ? LoadFromAssemblyPath(assemblyPath)
			 : null;
	}

	/// <summary>
	/// Resolve and load unmanaged native dependency any time one is loaded if it exists in the isolated addin dependency container.
	/// </summary>
	protected override IntPtr LoadUnmanagedDll(string assemblyName)
	{
		string? assemblyPath = _resolver.ResolveUnmanagedDllToPath(assemblyName);

		return assemblyPath is not null // && System.IO.File.Exists(assemblyPath)
			 ? LoadUnmanagedDllFromPath(assemblyPath)
			 : IntPtr.Zero;
	}

	/// <summary>
	///     Determine if the <see cref="AssemblyLoadContext" /> is custom or still the default context.
	/// </summary>
	public static bool CheckIfCustomContext(Type type)
	{
		AssemblyLoadContext? currentContext = GetLoadContext(type.Assembly);

		return currentContext != Default;
	}

	/// <summary>
	/// Get or create a new isolated context for the type.
	/// </summary>
	public static AddinLoadContext GetDependenciesProvider(Type type)
	{
		// Assembly location used as context name and the unique provider key.
		string addinRoot = System.IO.Path.GetDirectoryName(type.Assembly.Location)!;
		if (_dependenciesProviders.TryGetValue(addinRoot, out var provider))
		{
			return provider;
		}

		string addinName = System.IO.Path.GetFileName(addinRoot);
		provider = new AddinLoadContext(type, addinName);
		_dependenciesProviders.Add(addinRoot, provider);

		return provider;
	}

	/// <summary>
	/// Create new instance in the separated context.
	/// </summary>
	public object CreateAssemblyInstance(Type type)
	{
		string assemblyLocation = type.Assembly.Location;
		Assembly assembly = LoadFromAssemblyPath(assemblyLocation);

		return assembly.CreateInstance(type.FullName!)!;
	}

	/// <summary>
	/// Execute <see cref="ApplicationAddInServer.Activate" /> method in the isolated context.
	/// </summary>
	/// <remarks>
	/// Matches parameter format of <see cref="ApplicationAddInServer.Activate" /> method.
	/// </remarks>
	public static void Invoke(object instance, string methodName, ApplicationAddInSite application, bool firstTime)
	{
		Type instanceType = instance.GetType();

		Type[] methodParameterTypes =
		[
			  typeof(ApplicationAddInSite),
					 typeof(bool)
		];

		object[] methodParameters =
		[
			  application,
					 firstTime
		];

		MethodInfo method = instanceType.GetMethod(methodName, _methodSearchFlags, null, methodParameterTypes, null)!;

		_ = method.Invoke(instance, methodParameters)!;
	}

	/// <summary>
	/// Execute <see cref="ApplicationAddInServer.Deactivate" /> method in the isolated context.
	/// </summary>
	/// <remarks>
	/// Matches parameter format of <see cref="ApplicationAddInServer.Deactivate" /> method.
	/// </remarks>
	public static void Invoke(object instance, string methodName)
	{
		Type instanceType = instance.GetType();

		MethodInfo method = instanceType.GetMethod(methodName, _methodSearchFlags, null, [], null)!;

		_ = method.Invoke(instance, [])!;
	}
}
#endif