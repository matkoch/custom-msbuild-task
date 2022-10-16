using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Utilities;
#if NETCOREAPP
using System.Runtime.Loader;
using Microsoft.Build.Framework;
#endif

namespace CustomTasks;

public abstract class ContextAwareTask : Task
{
    protected virtual string ManagedDllDirectory => Path.GetDirectoryName(new Uri(GetType().GetTypeInfo().Assembly.CodeBase).LocalPath);

    protected virtual string UnmanagedDllDirectory => null;

    public sealed override bool Execute()
    {
#if NETCOREAPP
        var taskAssemblyPath = new Uri(GetType().GetTypeInfo().Assembly.CodeBase).LocalPath;
        var context = new CustomAssemblyLoader(this);
        var inContextAssembly = context.LoadFromAssemblyPath(taskAssemblyPath);
        var innerTaskType = inContextAssembly.GetType(GetType().FullName);
        var innerTask = Activator.CreateInstance(innerTaskType);

        var outerProperties = GetType().GetRuntimeProperties().ToDictionary(i => i.Name);
        var innerProperties = innerTaskType.GetRuntimeProperties().ToDictionary(i => i.Name);
        var propertiesDiscovery =
            from outerProperty in outerProperties.Values
            where outerProperty.SetMethod != null && outerProperty.GetMethod != null
            let innerProperty = innerProperties[outerProperty.Name]
            select new { outerProperty, innerProperty };

        var propertiesMap = propertiesDiscovery.ToArray();
        var outputPropertiesMap = propertiesMap.Where(pair => pair.outerProperty.GetCustomAttribute<OutputAttribute>() != null).ToArray();

        foreach (var propertyPair in propertiesMap)
        {
            var outerPropertyValue = propertyPair.outerProperty.GetValue(this);
            propertyPair.innerProperty.SetValue(innerTask, outerPropertyValue);
        }

        var executeInnerMethod = innerTaskType.GetMethod(nameof(ExecuteInner), BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (bool) executeInnerMethod.Invoke(innerTask, new object[0]);

        foreach (var propertyPair in outputPropertiesMap)
            propertyPair.outerProperty.SetValue(this, propertyPair.innerProperty.GetValue(innerTask));

        return result;
#else
        // On .NET Framework (on Windows), we find native binaries by adding them to our PATH.
        if (UnmanagedDllDirectory != null)
        {
            var pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            var searchPaths = pathEnvVar.Split(Path.PathSeparator);
            if (!searchPaths.Contains(UnmanagedDllDirectory, StringComparer.OrdinalIgnoreCase))
            {
                pathEnvVar += Path.PathSeparator + UnmanagedDllDirectory;
                Environment.SetEnvironmentVariable("PATH", pathEnvVar);
            }
        }

        return ExecuteInner();
#endif
    }

    protected abstract bool ExecuteInner();

#if NETCOREAPP
    private class CustomAssemblyLoader : AssemblyLoadContext
    {
        private readonly ContextAwareTask loaderTask;

        internal CustomAssemblyLoader(ContextAwareTask loaderTask)
        {
            this.loaderTask = loaderTask;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var assemblyPath = Path.Combine(loaderTask.ManagedDllDirectory, assemblyName.Name) + ".dll";
            return File.Exists(assemblyPath)
                ? LoadFromAssemblyPath(assemblyPath)
                : Default.LoadFromAssemblyName(assemblyName);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var unmanagedDllPath = Directory.EnumerateFiles(
                    loaderTask.UnmanagedDllDirectory,
                    $"{unmanagedDllName}.*").Concat(
                    Directory.EnumerateFiles(
                        loaderTask.UnmanagedDllDirectory,
                        $"lib{unmanagedDllName}.*"))
                .FirstOrDefault();

            return unmanagedDllPath != null
                ? LoadUnmanagedDllFromPath(unmanagedDllPath)
                : base.LoadUnmanagedDll(unmanagedDllName);
        }
    }
#endif
}
