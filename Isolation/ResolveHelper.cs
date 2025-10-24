using System.Reflection;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace IsolatedInventorAddin.Isolation;

/// <summary>
/// Used to resolve conflicting versions of a dependency.
/// </summary>
/// <remarks>
/// References:
/// <a href="https://github.com/Nice3point/RevitToolkit/blob/06be21872e7b4b0f0ed0c246f57df79608a90476/source/Nice3point.Revit.Toolkit/Helpers/ResolveHelper.cs">
///	GitHub/RevitToolkit/ResolveHelper.cs
/// </a>
/// </remarks>
public static class ResolveHelper
{
	private static string? _moduleDirectory;
	private static object? _domainResolvers;

	/// <summary>
	/// Subscribes the current domain to resolve dependencies for the type.
	/// </summary>
	/// <typeparam name="T">Type, to search for dependencies in the directory where this type is defined.</typeparam>
	/// <remarks>
	/// Dependencies are searched in a directory of the specified type.
	/// At the time of dependency resolution, all other dependency resolution methods for the domain are disabled,
	/// this requires calling <see cref="EndAssemblyResolve" /> immediately after executing user code where dependency failures occur.
	/// </remarks>
	public static void BeginAssemblyResolve<T>()
	{
		BeginAssemblyResolve(typeof(T));
	}

	/// <summary>
	/// Subscribes the current domain to resolve dependencies for the type.
	/// </summary>
	/// <param name="type">Type, to search for dependencies in the directory where this type is defined.</param>
	/// <remarks>
	/// Dependencies are searched in a directory of the specified type.
	/// At the time of dependency resolution, all other dependency resolution methods for the domain are disabled,
	/// this requires calling <see cref="EndAssemblyResolve" /> immediately after executing user code where dependency failures occur.
	/// </remarks>
	public static void BeginAssemblyResolve(Type type)
	{
		if (_domainResolvers is not null)
			return;
		if (type.Module.FullyQualifiedName == "<Unknown>")
			return;

#if NETCOREAPP
		var loadContextType = typeof(AssemblyLoadContext);
		var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
		var resolvers = resolversField.GetValue(null);
		resolversField.SetValue(null, null);
#else
		var domainType = AppDomain.CurrentDomain.GetType();
		var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
		var resolvers = resolversField.GetValue(AppDomain.CurrentDomain);
		resolversField.SetValue(AppDomain.CurrentDomain, null);
#endif

		_domainResolvers = resolvers;
		_moduleDirectory = System.IO.Path.GetDirectoryName(type.Module.FullyQualifiedName);

		AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
	}

	/// <summary>
	/// Unsubscribes the current domain to resolve dependencies for the type.
	/// </summary>
	public static void EndAssemblyResolve()
	{
		if (_domainResolvers is null)
			return;

#if NETCOREAPP
		var loadContextType = typeof(AssemblyLoadContext);
		var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
		resolversField.SetValue(null, _domainResolvers);
#else
		var domainType = AppDomain.CurrentDomain.GetType();
		var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
		resolversField.SetValue(AppDomain.CurrentDomain, _domainResolvers);
#endif

		_domainResolvers = null;
		_moduleDirectory = null;

		AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
	}

	private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
	{
		string? assemblyName = new AssemblyName(args.Name).Name;

		string assemblyPath = System.IO.Path.Combine(_moduleDirectory!, $"{assemblyName}.dll");
		if (System.IO.File.Exists(assemblyPath) is false)
			return null;

		return Assembly.LoadFrom(assemblyPath);
	}
}