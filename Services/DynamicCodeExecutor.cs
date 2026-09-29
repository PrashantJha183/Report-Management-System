//using System;
//using System.CodeDom.Compiler;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using Microsoft.CSharp;

//namespace Report.Services
//{
//    public class DynamicCodeExecutor
//    {
//        private static readonly string[] CoreAssemblies = new[]
//        {
//            "System.dll",
//            "System.Core.dll",
//            "System.Data.dll",
//            "System.Web.dll",
//            "Microsoft.CSharp.dll",
//            "System.Net.Http.dll",
//            "System.Xml.dll",
//            "System.Xml.Linq.dll",
//            "System.Web.Extensions.dll",
//            "System.Web.Mvc.dll"
//        };

//        public CompileResult CompileCode(string keyName, string code,
//            IList<string> gacAssemblies, IList<string> privateAssemblies,
//            string namespaceString)
//        {
//            var results = CompileAssembly(keyName, code, gacAssemblies, privateAssemblies, namespaceString);

//            if (results.Errors.HasErrors)
//            {
//                var errors = results.Errors
//                    .Cast<CompilerError>()
//                    .Select(e => $"Line {e.Line}: {e.ErrorText}")
//                    .ToList();

//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.Log($"Compile FAILED: {string.Join("; ", errors)}");
//                return new CompileResult { Success = false, Errors = errors };
//            }

//            // Execution logging disabled (execution runs in Sumeet/ERP).
//            //StaticLogger.Log($"Compile SUCCESS for keyName={keyName}");
//            return new CompileResult { Success = true };
//        }

//        // ── Compile + execute the wrapped code, returning the ExecuteDynamicCode result ──
//        public ExecuteResult ExecuteCode(string keyName, string code,
//            object[] inputParameters,
//            IList<string> gacAssemblies, IList<string> privateAssemblies,
//            string namespaceString)
//        {
//            var results = CompileAssembly(keyName, code, gacAssemblies, privateAssemblies, namespaceString);

//            if (results.Errors.HasErrors)
//            {
//                var errors = results.Errors
//                    .Cast<CompilerError>()
//                    .Select(e => $"Line {e.Line}: {e.ErrorText}")
//                    .ToList();

//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.Log($"ExecuteCode compile FAILED for {keyName}: {string.Join("; ", errors)}");
//                return new ExecuteResult { Success = false, Errors = errors };
//            }

//            try
//            {
//                var assembly = results.CompiledAssembly;
//                var type = assembly?.GetType("DynamicCode.DynamicClass");
//                if (type == null)
//                    return new ExecuteResult { Success = false, Errors = new List<string> { "DynamicClass type not found." } };

//                var instance = Activator.CreateInstance(type);
//                var method = type.GetMethod("ExecuteDynamicCode", BindingFlags.Public | BindingFlags.Instance);
//                if (method == null)
//                    return new ExecuteResult { Success = false, Errors = new List<string> { "ExecuteDynamicCode method not found." } };

//                object[] callArgs = inputParameters ?? new object[0];
//                object returnVal = method.Invoke(instance, new object[] { callArgs });

//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.Log($"ExecuteCode SUCCESS for {keyName}; return type = {returnVal?.GetType().Name ?? "null"}");
//                return new ExecuteResult { Success = true, ReturnVal = returnVal };
//            }
//            catch (Exception ex)
//            {
//                string message = ex is TargetInvocationException && ex.InnerException != null
//                    ? ex.InnerException.Message
//                    : ex.Message;

//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.LogError(ex, $"DynamicCodeExecutor.ExecuteCode {keyName}");
//                return new ExecuteResult { Success = false, Errors = new List<string> { message } };
//            }
//        }

//        private CompilerResults CompileAssembly(string keyName, string code,
//            IList<string> gacAssemblies, IList<string> privateAssemblies,
//            string namespaceString)
//        {
//            var wrappedCode =
//                "namespace DynamicCode { public class DynamicClass { " +
//                "public object ExecuteDynamicCode(params object[] Parameters) { " +
//                code + " } } }";

//            var fullCode = namespaceString + "\r\n" + wrappedCode;

//            // Execution logging disabled (execution runs in Sumeet/ERP).
//            //StaticLogger.Log($"CompileCode: keyName={keyName}, namespaceString length={namespaceString?.Length ?? 0}, code length={code?.Length ?? 0}");

//            var provider = new CSharpCodeProvider();
//            var parameters = new CompilerParameters();

//            var referenced = new List<string>();
//            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//            var binNames = IDEHelperService.GetBinAssemblyNames();

//            void AddReference(string reference)
//            {
//                if (string.IsNullOrWhiteSpace(reference)) return;

//                string resolved = ResolveAssemblyPath(reference);

//                // Only reference assemblies physically present in the app's bin\ folder.
//                // Framework/GAC assemblies (System, System.Core, System.Web, etc.) are not
//                // collected from here; the framework core set is referenced explicitly below.
//                if (string.IsNullOrEmpty(resolved))
//                    return;

//                string simpleName = System.IO.Path.GetFileNameWithoutExtension(resolved);
//                if (!binNames.Contains(simpleName))
//                    return;

//                if (seen.Add(resolved))
//                    referenced.Add(resolved);
//            }

//            // Framework references required for dynamic/MVC code (ViewBag, ViewResult...).
//            // The compiler here does not auto-import these from csc.rsp, so they are added
//            // explicitly; they resolve to the same framework paths csc uses, so no duplicates.
//            foreach (var asm in CoreAssemblies)
//            {
//                string r = ResolveAssemblyPath(asm);
//                if (!string.IsNullOrEmpty(r) && seen.Add(r))
//                    referenced.Add(r);
//            }

//            // Add Report.dll if it exists
//            try
//            {
//                var reportAssembly = typeof(DynamicCodeExecutor).Assembly;
//                if (!string.IsNullOrEmpty(reportAssembly.Location))
//                {
//                    AddReference(reportAssembly.Location);
//                    // Execution logging disabled (execution runs in Sumeet/ERP).
//                    //StaticLogger.Log($"Added Report.dll: {reportAssembly.Location}");
//                }
//            }
//            catch (Exception)
//            {
//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.LogError(ex, "DynamicCodeExecutor.AddReportAssembly");
//            }

//            // Add all AppDomain assemblies that are not dynamic
//            try
//            {
//                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
//                {
//                    if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location))
//                        continue;

//                    AddReference(asm.Location);
//                }
//            }
//            catch (Exception)
//            {
//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.LogError(ex, "DynamicCodeExecutor.AddAppDomainAssemblies");
//            }

//            if (gacAssemblies != null)
//            {
//                foreach (var asm in gacAssemblies)
//                    AddReference(asm);
//            }

//            if (privateAssemblies != null)
//            {
//                foreach (var asmName in privateAssemblies)
//                {
//                    try
//                    {
//                        var asm = Assembly.Load(asmName);
//                        if (!string.IsNullOrEmpty(asm.Location))
//                            AddReference(asm.Location);
//                    }
//                    catch { }
//                }
//            }

//            foreach (var r in referenced)
//                parameters.ReferencedAssemblies.Add(r);

//            parameters.GenerateInMemory = true;

//            return provider.CompileAssemblyFromSource(parameters, fullCode);
//        }

//        private static string ResolveAssemblyPath(string asmFile)
//        {
//            try
//            {
//                if (System.IO.File.Exists(asmFile))
//                    return System.IO.Path.GetFullPath(asmFile);

//                string name = asmFile;

//                if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
//                    name = name.Substring(0, name.Length - 4);

//                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
//                {
//                    if (string.Equals(asm.GetName().Name, name, StringComparison.OrdinalIgnoreCase)
//                        && !asm.IsDynamic
//                        && !string.IsNullOrEmpty(asm.Location)
//                        && System.IO.File.Exists(asm.Location))
//                        return asm.Location;
//                }

//                try
//                {
//                    var loaded = Assembly.Load(name);
//                    if (!loaded.IsDynamic && !string.IsNullOrEmpty(loaded.Location) && System.IO.File.Exists(loaded.Location))
//                        return loaded.Location;
//                }
//                catch { }

//                string binDir = AppDomain.CurrentDomain.RelativeSearchPath;
//                if (string.IsNullOrEmpty(binDir))
//                {
//                    var codeBase = typeof(DynamicCodeExecutor).Assembly.Location;
//                    if (!string.IsNullOrEmpty(codeBase))
//                        binDir = System.IO.Path.GetDirectoryName(codeBase);
//                }

//                if (!string.IsNullOrEmpty(binDir))
//                {
//                    string candidate = System.IO.Path.Combine(binDir, asmFile);
//                    if (System.IO.File.Exists(candidate))
//                        return candidate;
//                }
//            }
//            catch (Exception)
//            {
//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.Log("ResolveAssemblyPath: " + asmFile + " -> " + ex.Message);
//            }

//            return null;
//        }
//    }

//    public class CompileResult
//    {
//        public bool Success { get; set; }
//        public List<string> Errors { get; set; } = new List<string>();
//    }

//    public class ExecuteResult
//    {
//        public bool Success { get; set; }
//        public List<string> Errors { get; set; } = new List<string>();
//        public object ReturnVal { get; set; }
//    }
//}
