using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Report.Services
{
    public class IDEHelperService
    {
        public static IList<string> GetNamespaces(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            return input
                .Split(
                    new[] { ',', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(ns => ns.Trim())
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ============================================================
        // IMPORTANT:
        // Do NOT maintain AutoUsings here.
        //
        // All namespaces must come from the Dynamic Code Editor.
        //
        // Example:
        //
        // System.Web.Mvc
        // System.Collections.Generic
        // System.Linq
        // RSuite.UserInterface.Web.Mvc.Controllers.Transport.CallBack
        // RSuite.Domain.Inventory.Master
        // RSuite.UserInterface.Web.Mvc.Models.Controls
        // RSuite.Infrastructure.Specification.Inventory
        // RSuite.UserInterface.Web.Mvc.AppCode.Common.Factory
        // RSuite.Infrastructure.Core.Helper
        //
        // The user is responsible for adding required namespaces.
        // ============================================================

        public static string PrepareUsingNamespace(
            IList<string> NamespaceList)
        {
            if (NamespaceList == null || NamespaceList.Count == 0)
                return string.Empty;

            var merged = new List<string>();

            foreach (string ns in NamespaceList)
            {
                if (string.IsNullOrWhiteSpace(ns))
                    continue;

                string namespaceName = ns.Trim();

                if (!merged.Contains(
                    namespaceName,
                    StringComparer.OrdinalIgnoreCase))
                {
                    merged.Add(namespaceName);
                }
            }

            return string.Join(
                "",
                merged.Select(ns => "using " + ns + ";\r\n")
            );
        }

        // ============================================================
        // Extract controller + action from a full URL
        //
        // Example:
        // /DynamicCode/Vehicle
        //
        // Result:
        // controller = DynamicCode
        // action     = Vehicle
        // ============================================================

        public static (string controller, string action)
            ExtractControllerAction(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (null, null);

            try
            {
                var uri = new Uri(url);

                var segments = uri.AbsolutePath
                    .Trim('/')
                    .Split(
                        new[] { '/' },
                        StringSplitOptions.RemoveEmptyEntries
                    );

                if (segments.Length == 0)
                    return (null, null);

                string controller =
                    HttpUtility.UrlDecode(segments[0]);

                string action =
                    segments.Length > 1
                        ? HttpUtility.UrlDecode(segments[1])
                        : "";

                return (controller, action);
            }
            catch
            {
                return (null, null);
            }
        }

        // ============================================================
        // Get DLL names required by manually supplied namespaces
        // ============================================================

        public static IList<string> GetDllNamesFromNamespaces(
     IEnumerable<string> namespaces)
        {
            if (namespaces == null || !namespaces.Any())
                return new List<string>();

            var namespaceSet =
                new HashSet<string>(
                    namespaces,
                    StringComparer.OrdinalIgnoreCase
                );

            var result =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            var binNames = GetBinAssemblyNames();

            // Check currently loaded assemblies
            var assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (assembly == null)
                    continue;

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types
                        .Where(t => t != null)
                        .ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null || string.IsNullOrEmpty(type.Namespace))
                        continue;

                    // Support exact namespace OR child namespace
                    bool namespaceMatched =
                        namespaceSet.Any(ns =>
                            string.Equals(
                                type.Namespace,
                                ns,
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            type.Namespace.StartsWith(
                                ns + ".",
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                    if (namespaceMatched)
                    {
                        string assemblyName =
                            assembly.GetName().Name;

                        if (!string.IsNullOrEmpty(assemblyName) &&
                            binNames.Contains(assemblyName))
                        {
                            result.Add(assemblyName);
                        }

                        break;
                    }
                }
            }

            return result.ToList();
        }
        // ============================================================
        // Get simple assembly names from application's bin folder
        // ============================================================

        public static HashSet<string> GetBinAssemblyNames()
        {
            var set = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

            var candidates =
                new List<string>();

            try
            {
                string baseDir =
                    AppDomain.CurrentDomain.BaseDirectory;

                string rel =
                    AppDomain.CurrentDomain.RelativeSearchPath;

                if (!string.IsNullOrEmpty(rel))
                {
                    candidates.Add(
                        System.IO.Path.GetFullPath(
                            System.IO.Path.Combine(
                                baseDir,
                                rel
                            )
                        )
                    );
                }

                var selfDir =
                    System.IO.Path.GetDirectoryName(
                        typeof(IDEHelperService)
                            .Assembly.Location
                    );

                if (!string.IsNullOrEmpty(selfDir))
                {
                    candidates.Add(
                        System.IO.Path.GetFullPath(
                            selfDir
                        )
                    );
                }
            }
            catch
            {
            }

            foreach (var dir in candidates)
            {
                try
                {
                    if (!System.IO.Directory.Exists(dir))
                        continue;

                    foreach (
                        var f in System.IO.Directory.GetFiles(
                            dir,
                            "*.dll"))
                    {
                        set.Add(
                            System.IO.Path.GetFileNameWithoutExtension(f)
                        );
                    }
                }
                catch
                {
                }
            }

            return set;
        }

        // ============================================================
        // Get public classes/interfaces for manually selected
        // namespaces
        // ============================================================

        public static IList<string>
            GetPublicClassesAndInterfacesByNamespace(
                IEnumerable<Assembly> assemblies,
                IList<string> namespaces)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            if (namespaces == null || !namespaces.Any())
                return new List<string>();

            var namespaceSet =
                new HashSet<string>(
                    namespaces,
                    StringComparer.OrdinalIgnoreCase
                );

            var result =
                new List<string>();

            foreach (var assembly in assemblies)
            {
                if (assembly == null)
                    continue;

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types
                        .Where(t => t != null)
                        .ToArray();
                }

                foreach (var type in types)
                {
                    if ((type.IsClass || type.IsInterface) &&
                        (type.IsPublic || type.IsNestedPublic) &&
                        type.Namespace != null &&
                        namespaceSet.Contains(type.Namespace))
                    {
                        result.Add(type.Name);
                    }
                }
            }

            return result
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }
    }
}