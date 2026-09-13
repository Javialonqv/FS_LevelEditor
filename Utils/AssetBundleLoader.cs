using System.Reflection;
using UnityEngine;

using Object = UnityEngine.Object;

namespace FS_LevelEditor
{
    public static class AssetBundleLoader
    {
        static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

        public static void PreloadEmbeddedBundle(string bundlePath)
        {
            string bundlePathInResources = Assembly.GetExecutingAssembly().GetName().Name + "." + bundlePath.Replace('/', '.');
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(bundlePathInResources);

            if (stream == null)
            {
                Logger.Error("Couldn't find any embedded file in the DLL with name: " + bundlePath + " in: " + bundlePathInResources);
                return;
            }

            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes);

            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes);

            string bundleName = Path.GetFileNameWithoutExtension(bundlePath);
            loadedBundles.Add(bundleName, bundle);
        }

        public static AssetBundle GetLoadedBundle(string bundleName)
        {
            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                Logger.Error($"Couldn't find any loaded bundle with the specified \"{bundleName}\" name!");
                return null;
            }

            return bundle;
        }

        public static T LoadAsset<T>(string assetName, string bundleName, bool throwError = true) where T : Object
        {
            if (!loadedBundles.ContainsKey(bundleName))
            {
                if (throwError)
                    Logger.Error("Couldn't find loaded asset bundle with name:" + bundleName);
                return null;
            }

            T obj = loadedBundles[bundleName].LoadAsset<T>(assetName);
            if (obj == null)
            {
                if (throwError)
                    Logger.Error("Error loading the asset of name: " + assetName);
                return null;
            }

            return obj;
        }
    }
}
