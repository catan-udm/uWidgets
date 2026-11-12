using System.Reflection;
using System.Resources;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Models.Attributes;

namespace uWidgets.Core.Services;
 
/// <inheritdoc />
public class AssemblyProvider : IAssemblyProvider
{
    private readonly Dictionary<string, AssemblyLoadContext> loadedContexts = new();
    private ILookup<string, AssemblyInfo> assemblyCache;
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public AssemblyProvider(IServiceProvider serviceProvider)
    {
        assemblyCache = LoadAssemblyInfos(Const.WidgetsFolder);
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public ILookup<string, AssemblyInfo> GetAssemblyInfos(string directoryPath)
    {
        if (assemblyCache != null &&
            string.Equals(Path.GetFullPath(directoryPath), Path.GetFullPath(Const.WidgetsFolder), StringComparison.OrdinalIgnoreCase))
        {
            return assemblyCache;
        }

        return LoadAssemblyInfos(directoryPath);
    }

    private ILookup<string, AssemblyInfo> LoadAssemblyInfos(string directoryPath)
    {
        var assemblies = Directory.Exists(directoryPath)
            ? Directory
                .GetFiles(directoryPath, "*.dll")
                .Where(IsCandidatePluginDll)
                .Select(GetAssemblyInfo)
                .Where(info => info != null)
                .Cast<AssemblyInfo>()
            : [];
        
        return assemblies
            .ToLookup(assembly => assembly.AssemblyName);
    }

    private static bool IsCandidatePluginDll(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return !fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
               !fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) &&
               !fileName.StartsWith("WinRT.", StringComparison.OrdinalIgnoreCase) &&
               !fileName.StartsWith("TimeZone", StringComparison.OrdinalIgnoreCase) &&
               !fileName.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase) &&
               !fileName.StartsWith("uWidgets", StringComparison.OrdinalIgnoreCase) &&
               !fileName.StartsWith("lib", StringComparison.OrdinalIgnoreCase) &&
               !fileName.StartsWith("av_", StringComparison.OrdinalIgnoreCase);
    }

    private AssemblyInfo? GetAssemblyInfo(string filePath)
    {
        AssemblyLoadContext? context = null;
        try
        {
            context = new PluginLoadContext(filePath);
            var assembly = context.LoadFromAssemblyPath(filePath);
            var localeAttribute = assembly.GetCustomAttributes<LocaleAttribute>().FirstOrDefault();
            var companyAttribute = assembly.GetCustomAttributes<AssemblyCompanyAttribute>().FirstOrDefault();
            var widgetAttributes = assembly.GetCustomAttributes<WidgetInfoAttribute>();

            if (!widgetAttributes.Any()) return null;

            var assemblyName = assembly.GetName().Name!;
            var version = assembly.GetName().Version!;
            var company = companyAttribute?.Company ?? "";
            var locale = GetLocaleResourceManager(assembly);
            var displayName = locale?.GetString(localeAttribute?.DisplayName ?? "") ?? assemblyName;

            return new AssemblyInfo(filePath, assemblyName, displayName, company, version, localeAttribute?.IconData ?? "");
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            context?.Unload();
        }
    }
    
    /// <inheritdoc />
    public Assembly LoadAssembly(string name)
    {
        if (loadedContexts.TryGetValue(name, out var context))
            return context.Assemblies.Single(assembly => 
                assembly.ManifestModule.Name == $"{name}.dll");
        
        var filePath = GetAssemblyPath(name);
        context = new PluginLoadContext(filePath);
        loadedContexts[name] = context;

        return context.LoadFromAssemblyPath(filePath);
    }
    
    /// <inheritdoc />
    public void UnloadAssembly(string name)
    {
        if (!loadedContexts.TryGetValue(name, out var context))
            throw new InvalidOperationException($"Assembly {name} is not loaded");

        context.Unload();
        loadedContexts.Remove(name);
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <inheritdoc />
    public object Activate(Type type, params object[] args)
    {
        try
        {
            return ActivatorUtilities.CreateInstance(serviceProvider, type, args);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to create an instance of {type.Name}", e);
        }
    }

    /// <inheritdoc />
    public ResourceManager? GetLocaleResourceManager(Assembly assembly)
    {
        return assembly
            .DefinedTypes
            .FirstOrDefault(type => type.Name == "Locale")?
            .GetProperty(nameof(ResourceManager), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?
            .GetValue(null) as ResourceManager;
    }
    
    private string GetAssemblyPath(string name, bool updateCache = false)
    {
        if (updateCache) 
            assemblyCache = LoadAssemblyInfos(Const.WidgetsFolder);
        
        var assemblyInfo = assemblyCache[name]
            .MaxBy(assembly => assembly.Version);

        if (assemblyInfo != default) 
            return assemblyInfo.FilePath;

        if (!updateCache)
            return GetAssemblyPath(name, true);
        
        throw new FileNotFoundException($"Assembly {name} not found");
    }

    private class PluginLoadContext(string pluginPath) : AssemblyLoadContext(true)
    {
        private static readonly object SharedLoadLock = new();
        private static readonly HashSet<string> SharedWinRtAssemblies = new(StringComparer.OrdinalIgnoreCase)
        {
            "WinRT.Runtime",
            "Microsoft.Windows.SDK.NET"
        };

        private readonly AssemblyDependencyResolver resolver = new(pluginPath);
        private readonly string pluginDirectory = Path.GetDirectoryName(pluginPath) ?? Const.WidgetsFolder;

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assembly = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            if (assembly != null)
            {
                return assembly;
            }

            var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath == null && !string.IsNullOrEmpty(assemblyName.Name))
            {
                var localPath = Path.Combine(pluginDirectory, $"{assemblyName.Name}.dll");
                if (File.Exists(localPath))
                {
                    assemblyPath = localPath;
                }
            }

            if (assemblyPath == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(assemblyName.Name) && SharedWinRtAssemblies.Contains(assemblyName.Name))
            {
                return LoadSharedWinRtAssembly(assemblyName.Name, assemblyPath);
            }

            return LoadFromAssemblyPath(assemblyPath);
        }

        private static Assembly LoadSharedWinRtAssembly(string assemblyName, string assemblyPath)
        {
            lock (SharedLoadLock)
            {
                var loaded = Default.Assemblies.FirstOrDefault(a =>
                    string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
                if (loaded != null)
                {
                    return loaded;
                }

                // Ensure WinRT.Runtime is loaded into Default before Microsoft.Windows.SDK.NET triggers resolution
                if (assemblyName.Equals("Microsoft.Windows.SDK.NET", StringComparison.OrdinalIgnoreCase) &&
                    Default.Assemblies.All(a => !string.Equals(a.GetName().Name, "WinRT.Runtime", StringComparison.OrdinalIgnoreCase)))
                {
                    var winrtPath = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "WinRT.Runtime.dll");
                    if (File.Exists(winrtPath))
                    {
                        Default.LoadFromAssemblyPath(winrtPath);
                    }
                }

                return Default.LoadFromAssemblyPath(assemblyPath);
            }
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
    
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}