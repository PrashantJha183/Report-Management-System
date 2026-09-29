using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Report.Services;

namespace Report.Controllers
{
    public class NameSpaceController : Controller
    {
        private static NamespaceNode Root = new NamespaceNode();
        private static bool _loaded = false;

        [ChildActionOnly]
        public ActionResult NamespaceEditor(string textareaId = "txtNamespaces", string value = "")
        {
            if (!_loaded)
            {
                LoadNamespacesFromReferenceDLL();
                _loaded = true;
            }

            ViewBag.TextareaId = textareaId;
            ViewBag.Value = value;

            return PartialView("_NamespaceEditor");
        }

        [HttpGet]
        public JsonResult Suggest(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return Json(new List<string>(), JsonRequestBehavior.AllowGet);

                term = term.Trim();
                var parts = term.Split('.');

                NamespaceNode current = Root;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string part = parts[i];
                    if (!current.Children.ContainsKey(part))
                        return Json(new List<string>(), JsonRequestBehavior.AllowGet);
                    current = current.Children[part];
                }

                string lastPart = parts.Last();

                var results = current.Children
                    .Where(x => x.Key.StartsWith(lastPart, StringComparison.OrdinalIgnoreCase))
                    .Select(x =>
                    {
                        if (parts.Length == 1)
                            return x.Key;
                        return string.Join(".",
                            parts.Take(parts.Length - 1).Concat(new[] { x.Key }));
                    })
                    .OrderBy(x => x)
                    .Take(20)
                    .ToList();

                return Json(results, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }

        private static void LoadNamespacesFromReferenceDLL()
        {
            var allNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

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
                    if (!string.IsNullOrWhiteSpace(type.Namespace))
                        allNamespaces.Add(type.Namespace);
                }
            }

            foreach (var ns in allNamespaces.OrderBy(x => x))
                AddNamespace(ns);
        }

        private static void AddNamespace(string ns)
        {
            var parts = ns.Split('.');
            NamespaceNode current = Root;

            foreach (var part in parts)
            {
                if (!current.Children.ContainsKey(part))
                {
                    current.Children[part] = new NamespaceNode { Name = part };
                }
                current = current.Children[part];
            }
        }
    }

    public class NamespaceNode
    {
        public string Name { get; set; }
        public Dictionary<string, NamespaceNode> Children { get; set; }

        public NamespaceNode()
        {
            Children = new Dictionary<string, NamespaceNode>();
        }
    }
}