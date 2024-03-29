//#define DEBUG_INDEXING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.QuickSearch
{
    class AssetIndexer : ObjectIndexer
    {
        public AssetIndexer(SearchDatabase.Settings settings)
            : base(String.IsNullOrEmpty(settings.name) ? "assets" : settings.name, settings)
        {
        }

        protected override System.Collections.IEnumerator BuildAsync(int progressId, object userData = null)
        {
            var paths = GetDependencies();
            var pathIndex = 0;
            var pathCount = (float)paths.Count;

            Start(clear: true);

            EditorApplication.LockReloadAssemblies();
            lock (this)
            {
                foreach (var path in paths)
                {
                    var progressReport = pathIndex++ / pathCount;
                    ReportProgress(progressId, path, progressReport, false);
                    IndexDocument(path, false);
                    yield return null;
                }
            }
            EditorApplication.UnlockReloadAssemblies();

            Finish();
            while (!IsReady())
                yield return null;

            ReportProgress(progressId, $"Indexing Completed (Documents: {documentCount}, Indexes: {indexCount:n0})", 1f, true);
            yield return null;
        }

        public override IEnumerable<string> GetRoots()
        {
            if (settings.roots == null || settings.roots.Length == 0)
                return new string[] { settings.root };
            return settings.roots.Where(r => Directory.Exists(r));
        }

        public override List<string> GetDependencies()
        {
            string[] roots = GetRoots().ToArray();
            return AssetDatabase.FindAssets(String.Empty, roots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct().Where(path => !SkipEntry(path)).ToList();
        }

        public override Hash128 GetDocumentHash(string path)
        {
            return AssetDatabase.GetAssetDependencyHash(path);
        }

        public override void IndexDocument(string path, bool checkIfDocumentExists)
        {
            var documentIndex = AddDocument(path, checkIfDocumentExists);
            AddDocumentHash(path, GetDocumentHash(path));
            if (documentIndex < 0)
                return;

            IndexWordComponents(documentIndex, path);

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                IndexWord(documentIndex, fileName, fileName.Length, true);

                IndexWord(documentIndex, path, path.Length, exact: true);
                IndexProperty(documentIndex, "id", path, saveKeyword: false, exact: true);

                if (path.StartsWith("Packages/", StringComparison.Ordinal))
                    IndexProperty(documentIndex, "a", "packages", saveKeyword: true, exact: true);
                else
                    IndexProperty(documentIndex, "a", "assets", saveKeyword: true, exact: true);

                if (!String.IsNullOrEmpty(name))
                    IndexProperty(documentIndex, "a", name, saveKeyword: true, exact: true);

                if (settings.options.fstats)
                {
                    var fi = new FileInfo(path);
                    if (fi.Exists)
                    {
                        IndexNumber(documentIndex, "size", (double)fi.Length);
                        IndexProperty(documentIndex, "ext", fi.Extension.Replace(".", "").ToLowerInvariant(), saveKeyword: false);
                        IndexNumber(documentIndex, "age", (DateTime.Now - fi.LastWriteTime).TotalDays);
                        IndexProperty(documentIndex, "dir", fi.Directory.Name.ToLowerInvariant(), saveKeyword: false);
                    }
                }

                var at = AssetDatabase.GetMainAssetTypeAtPath(path);
                var hasCustomIndexers = HasCustomIndexers(at);

                if (settings.options.properties || settings.options.types || hasCustomIndexers)
                {
                    bool wasLoaded = AssetDatabase.IsMainAssetAtPathLoaded(path);
                    var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (!mainAsset)
                        return;

                    if (hasCustomIndexers)
                        IndexCustomProperties(path, documentIndex, mainAsset);

                    if (settings.options.properties || settings.options.types)
                    {
                        if (!String.IsNullOrEmpty(mainAsset.name))
                            IndexWord(documentIndex, mainAsset.name, true);

                        IndexWord(documentIndex, at.Name);
                        while (at != null && at != typeof(Object))
                        {
                            if (at != typeof(GameObject))
                                IndexProperty(documentIndex, "t", at.Name, saveKeyword: true);
                            at = at.BaseType;
                        }

                        var prefabType = PrefabUtility.GetPrefabAssetType(mainAsset);
                        if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
                            IndexProperty(documentIndex, "t", "prefab", saveKeyword: true);

                        var labels = AssetDatabase.GetLabels(mainAsset);
                        foreach (var label in labels)
                            IndexProperty(documentIndex, "l", label, saveKeyword: true);

                        if (settings.options.properties)
                            IndexObject(documentIndex, mainAsset);

                        if (mainAsset is GameObject go)
                        {
                            foreach (var v in go.GetComponents(typeof(Component)))
                            {
                                if (!v || v.GetType() == typeof(Transform))
                                    continue;
                                IndexPropertyComponents(documentIndex, "t", v.GetType().Name);
                                IndexPropertyComponents(documentIndex, "has", v.GetType().Name);

                                if (settings.options.properties)
                                    IndexObject(documentIndex, v, dependencies: settings.options.dependencies);
                            }
                        }
                    }

                    if (!wasLoaded)
                    {
                        if (mainAsset && !mainAsset.hideFlags.HasFlag(HideFlags.DontUnloadUnusedAsset) &&
                            !(mainAsset is GameObject) &&
                            !(mainAsset is Component) &&
                            !(mainAsset is AssetBundle))
                        {
                            Resources.UnloadAsset(mainAsset);
                        }
                    }
                }

                if (settings.options.dependencies)
                {
                    foreach (var depPath in AssetDatabase.GetDependencies(path, true))
                    {
                        if (path == depPath)
                            continue;
                        var depName = Path.GetFileNameWithoutExtension(depPath);
                        IndexProperty(documentIndex, "ref", depName, saveKeyword: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
