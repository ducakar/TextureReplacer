using System.IO;
using UnityEditor;

public class BuildBundles
{
  [MenuItem("TextureReplacer/Build Bundles")]
  static void BuildForAllPlatforms()
  {
    Directory.CreateDirectory("Bundles");

    Directory.CreateDirectory("Bundles/Linux");
    BuildPipeline.BuildAssetBundles("Bundles/Linux", BuildAssetBundleOptions.None, BuildTarget.StandaloneLinuxUniversal);

    Directory.CreateDirectory("Bundles/Windows");
    BuildPipeline.BuildAssetBundles("Bundles/Windows", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

    Directory.CreateDirectory("Bundles/OSX");
    BuildPipeline.BuildAssetBundles("Bundles/OSX", BuildAssetBundleOptions.None, BuildTarget.StandaloneOSXIntel);
  }
}
