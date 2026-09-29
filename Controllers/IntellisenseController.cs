using Report.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    // ── Test class for IntelliSense verification (top-level, like RSuiteTools) ──
    public class Vehicle
    {
        public string VehicleNo { get; set; }
        public string Color { get; set; }
        public void Validate(string VehicleId) { }
        public void Validate(int VehicleId) { }
        public IList<string> Items { get; set; }
    }

    public class IntellisenseController : Controller
    {
        private static readonly Dictionary<string, Type> _typeCache =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> _memberCache =
            new Dictionary<Type, Dictionary<string, MemberInfo>>();

        private static Assembly[] LoadedAssemblies = new Assembly[0];

        private static readonly Dictionary<string, Type> SymbolTable =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        private static bool _isCacheWarm = false;

        public static void PreCacheMembers(Type type)
        {
            if (_memberCache.ContainsKey(type)) return;

            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .GroupBy(m => m.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase
                );

            _memberCache[type] = members;
        }

        // ── Get ALL assemblies from AppDomain (no filtering) ──
        private static Assembly[] GetAllCandidateAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToArray();
        }

        private static void WarmUpCaches()
        {
            if (_isCacheWarm) return;

            var assemblies = GetAllCandidateAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in GetTypesSafely(assembly))
                {
                    _typeCache[type.Name] = type;
                    if (type.FullName != null)
                        _typeCache[type.FullName] = type;
                    PreCacheMembers(type);
                }
            }

            _isCacheWarm = true;
        }

        // ── Find assemblies containing specific namespaces ──
        private static Assembly[] GetAssembliesFromNamespaces(IEnumerable<string> namespaces)
        {
            if (namespaces == null || !namespaces.Any())
                return Array.Empty<Assembly>();

            var result = new HashSet<Assembly>();
            var namespaceSet = new HashSet<string>(namespaces, StringComparer.OrdinalIgnoreCase);
            var assemblies = GetAllCandidateAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type.Namespace != null && namespaceSet.Contains(type.Namespace))
                    {
                        result.Add(assembly);
                        break;
                    }
                }
            }

            return result.ToArray();
        }

        public static IList<string> LoadAssemblyFromNamespaces(IList<string> NamespaceList)
        {
            var assemblies = GetAssembliesFromNamespaces(NamespaceList);
            var allCandidates = GetAllCandidateAssemblies();
            var combined = new HashSet<Assembly>(allCandidates);
            foreach (var a in assemblies)
                combined.Add(a);
            LoadedAssemblies = combined.ToArray();  

            IList<string> ClassList = IDEHelperService.GetPublicClassesAndInterfacesByNamespace(
                LoadedAssemblies, NamespaceList);
            return ClassList;
        }

        // ── Namespaces whose DLL could not be loaded / found ──
        public static IList<string> GetMissingNamespaces(IList<string> namespaceList)
        {
            var result = new List<string>();
            if (namespaceList == null || namespaceList.Count == 0)
                return result;

            var requested = new HashSet<string>(namespaceList, StringComparer.OrdinalIgnoreCase);
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblies = GetAllCandidateAssemblies();

            foreach (var assembly in assemblies)
            {
                if (assembly == null) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch (Exception)
                {
                    // DLL present but unusable (e.g. missing dependency)
                    types = Array.Empty<Type>();
                }

                foreach (var type in types)
                {
                    if (type.Namespace != null && requested.Contains(type.Namespace))
                        covered.Add(type.Namespace);
                }
            }

            foreach (var ns in namespaceList)
                if (!covered.Contains(ns))
                    result.Add(ns);

            return result;
        }

        [ChildActionOnly]
        public ActionResult CodeEditor(string editorId = "editorContainer", string hiddenFieldId = "hdnCode", string code = "")
        {
            IList<string> namespaceList = new List<string>();
            LoadAssemblyFromNamespaces(namespaceList);
            IList<Assembly> asmList = LoadedAssemblies.ToList();
            IList<string> classList = GetPublicClassesAndInterfaces(asmList);

            ViewBag.ClassList = classList;
            ViewBag.EditorId = editorId;
            ViewBag.HiddenFieldId = hiddenFieldId;
            ViewBag.Code = code;

            return PartialView("_CodeEditor");
        }

        public static IList<string> GetPublicClassesAndInterfaces(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            var result = new List<string>();

            foreach (var assembly in assemblies)
            {
                if (assembly == null) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if ((type.IsClass || type.IsInterface) && (type.IsPublic || type.IsNestedPublic))
                        result.Add(type.Name);
                }
            }

            return result
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        private static Type InferTypeFromLiteral(string literal)
        {
            literal = literal.Trim();

            if (literal.StartsWith("\"") && literal.EndsWith("\""))
                return typeof(string);

            if (int.TryParse(literal, out _))
                return typeof(int);

            if (float.TryParse(literal, out _))
                return typeof(float);

            if (double.TryParse(literal, out _))
                return typeof(double);

            if (literal == "true" || literal == "false")
                return typeof(bool);

            return null;
        }

        [HttpPost]
        public JsonResult RegisterVariables(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false });

            var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var cleanLine = line.Trim();

                if (string.IsNullOrWhiteSpace(cleanLine) || cleanLine.StartsWith("//"))
                    continue;

                if (cleanLine.Contains("var") && cleanLine.Contains("new"))
                {
                    var parts = cleanLine.Split(new[] { ' ', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        string varName = parts[1];
                        string typeName = parts[3].Replace("()", "");
                        var type = FindType(typeName);
                        if (type != null)
                            SymbolTable[varName] = type;
                    }
                }
                else if (cleanLine.Contains("=") && cleanLine.Contains("new"))
                {
                    var parts = cleanLine.Split(new[] { ' ', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        string typeName = parts[0];
                        string varName = parts[1];
                        var type = FindType(typeName);
                        if (type != null)
                            SymbolTable[varName] = type;
                    }
                }
                else if (cleanLine.Contains("="))
                {
                    var parts = cleanLine.Split(new[] { ' ', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        string typeName = parts[0];
                        string varName = parts[1];
                        string value = parts[2];

                        Type type = null;
                        if (typeName == "var")
                            type = InferTypeFromLiteral(value);
                        else
                            type = FindType(typeName);

                        if (type != null)
                            SymbolTable[varName] = type;
                    }
                }
            }

            return Json(new { success = true, registered = SymbolTable.Keys.ToList() }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Completions(string fullcode, string expression)
        {
            RegisterVariables(fullcode);
            if (string.IsNullOrWhiteSpace(expression))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            expression = ExtractLastExpression(expression);
            Type finalType = ResolveExpressionType(expression);
            if (finalType != null)
                return Json(GetTypeMembers(finalType), JsonRequestBehavior.AllowGet);
            var nsTypes = FindTypesInNamespace(expression);
            if (nsTypes.Any())
            {
                var items = nsTypes
                    .Select(t => new { label = t.Name, kind = t.IsInterface ? "Interface" : "Class" })
                    .OrderBy(x => x.label)
                    .ToList();
                return Json(items, JsonRequestBehavior.AllowGet);
            }
            var partialMatches = FindTypesByPartialName(expression);
            if (partialMatches.Any())
            {
                var items = partialMatches
                    .Select(t => new { label = t.Name, kind = t.IsInterface ? "Interface" : "Class" })
                    .OrderBy(x => x.label)
                    .ToList();
                return Json(items, JsonRequestBehavior.AllowGet);
            }
            return Json(new List<object>(), JsonRequestBehavior.AllowGet);
        }

        private object GetTypeMembers(Type type)
        {
            IEnumerable<MemberInfo> rawMembers;
            if (type.IsInterface)
                rawMembers = GetAllInterfaceMembers(type);
            else
                rawMembers = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);

            var members = rawMembers
                .Where(m => m.MemberType == MemberTypes.Method || m.MemberType == MemberTypes.Property)
                .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                .GroupBy(m => m.Name)
                .Select(g => g.First())
                .Select(m => new
                {
                    label = m.Name,
                    kind = m.MemberType == MemberTypes.Method ? "Method" : "Property"
                })
                .OrderBy(m => m.label)
                .ToList();

            // Add LINQ extension methods for IList<T>
            if (type.IsGenericType && (
                type.GetGenericTypeDefinition() == typeof(IList<>) ||
                type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))))
            {
                var elementType = type.GetGenericArguments().FirstOrDefault();

                if (elementType != null)
                {
                    var linqMethods = typeof(System.Linq.Enumerable)
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                        .Where(m =>
                        {
                            var parameters = m.GetParameters();
                            if (parameters.Length == 0) return false;
                            var firstParam = parameters[0].ParameterType;
                            if (!typeof(IEnumerable).IsAssignableFrom(firstParam)) return false;
                            if (firstParam.IsGenericType)
                            {
                                var genDef = firstParam.GetGenericTypeDefinition();
                                if (genDef != typeof(IEnumerable<>))
                                {
                                    var genericArg = firstParam.GetGenericArguments().FirstOrDefault();
                                    return genericArg.IsAssignableFrom(elementType);
                                }
                                return true;
                            }
                            return true;
                        })
                        .ToList();

                    var linqMemberObjects = linqMethods
                        .GroupBy(m => new
                        {
                            m.Name,
                            Signature = string.Join(",", m.GetParameters().Select(p => p.ParameterType.ToString())
                                                        .Concat(m.IsGenericMethod ? m.GetGenericArguments().Select(t => t.ToString()) : Enumerable.Empty<string>()))
                        })
                        .Select(g => g.First())
                        .Select(m => new { label = m.Name, kind = "Method" });

                    members.AddRange(linqMemberObjects);
                    members = members
                        .GroupBy(m => m.label)
                        .Select(g => g.First())
                        .OrderBy(m => m.label)
                        .ToList();
                }
            }

            return members;
        }

        // ── Find all types within a given namespace ──
        private static List<Type> FindTypesInNamespace(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
                return new List<Type>();

            var assemblies = GetAllCandidateAssemblies();
            var result = new List<Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in GetTypesSafely(assembly))
                {
                    if (string.Equals(type.Namespace, ns, StringComparison.OrdinalIgnoreCase) && type.IsPublic)
                        result.Add(type);
                }
            }

            return result.OrderBy(t => t.Name).ToList();
        }

        // ── Find types whose name starts with the given partial string ──
        private static List<Type> FindTypesByPartialName(string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return new List<Type>();

            var assemblies = GetAllCandidateAssemblies();
            var result = new List<Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in GetTypesSafely(assembly))
                {
                    if (type.IsPublic && type.Name.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                        result.Add(type);
                }
            }

            return result.OrderBy(t => t.Name).ToList();
        }

        private static IEnumerable<MemberInfo> GetAllInterfaceMembers(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                return Enumerable.Empty<MemberInfo>();

            var members = new List<MemberInfo>();
            members.AddRange(interfaceType.GetMembers(BindingFlags.Public | BindingFlags.Instance));
            foreach (var baseInterface in interfaceType.GetInterfaces())
                members.AddRange(baseInterface.GetMembers(BindingFlags.Public | BindingFlags.Instance));
            return members;
        }

        private static IEnumerable<MethodInfo> GetAllInterfaceMethods(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                return Enumerable.Empty<MethodInfo>();

            var methods = new List<MethodInfo>();
            methods.AddRange(interfaceType.GetMethods());
            foreach (var baseInterface in interfaceType.GetInterfaces())
                methods.AddRange(baseInterface.GetMethods());
            return methods;
        }

        [HttpPost]
        public JsonResult Signatures(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return Json(new { signatures = new List<object>() }, JsonRequestBehavior.AllowGet);

            var methodName = expression.Trim();
            var lastDot = methodName.LastIndexOf('.');
            if (lastDot < 0)
                return Json(new { signatures = new List<object>() }, JsonRequestBehavior.AllowGet);

            string typeExpr = methodName.Substring(0, lastDot);
            string method = methodName.Substring(lastDot + 1);

            typeExpr = ExtractLastExpression(typeExpr);
            var type = ResolveExpressionType(typeExpr);

            if (type == null)
                return Json(new { signatures = new List<object>() }, JsonRequestBehavior.AllowGet);

            IEnumerable<MethodInfo> methods;
            if (type.IsInterface)
                methods = GetAllInterfaceMethods(type);
            else
                methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var results = methods
                .Where(m => m.Name.Equals(method, StringComparison.OrdinalIgnoreCase))
                .Select(m => new
                {
                    name = m.Name,
                    returnType = m.ReturnType.Name,
                    parameters = m.GetParameters()
                                  .Select(p => new { name = p.Name, type = p.ParameterType.Name })
                })
                .ToList();

            return Json(new { signatures = results }, JsonRequestBehavior.AllowGet);
        }

        // ── Helpers ──

        private string ExtractLastExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return expression;
            var tokens = expression.Split(new[] { ' ', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Last();
        }

        private static Type ResolveExpressionType(string expression)
        {
            expression = expression.Trim();

            // First check if the whole expression is a known variable
            if (SymbolTable.TryGetValue(expression, out var directType))
                return directType;

            // Try dotted path: x.Property.SubProperty
            var parts = expression.Split('.');
            if (parts.Length == 0) return null;

            if (!SymbolTable.TryGetValue(parts[0], out var currentType))
            {
                // Maybe expression is a type name itself (e.g. "Vehicle")
                var asType = FindType(expression);
                if (asType != null) return asType;

                return null;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                var member = currentType.GetMember(parts[i], BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (member is PropertyInfo pi) currentType = pi.PropertyType;
                else if (member is MethodInfo mi) currentType = mi.ReturnType;
                else return null;
            }

            return currentType;
        }

        private static Type FindType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            if (_typeCache.TryGetValue(typeName, out var cachedType))
                return cachedType;

            var assemblies = GetAllCandidateAssemblies();
            Type result = null;
            try
            {
                result = assemblies
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t =>
                        t.FullName != null && t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                        t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                result = assemblies
                    .SelectMany(a => GetTypesSafely(a))
                    .FirstOrDefault(t =>
                        t.FullName != null && t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                        t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            }

            if (result != null)
            {
                _typeCache[typeName] = result;
                if (!string.Equals(result.Name, typeName, StringComparison.OrdinalIgnoreCase))
                    _typeCache[result.Name] = result;
                if (result.FullName != null && !string.Equals(result.FullName, typeName, StringComparison.OrdinalIgnoreCase))
                    _typeCache[result.FullName] = result;
            }

            return result;
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}