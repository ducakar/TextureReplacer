/**
 * Kerbal Visual Enhancements is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Kerbal Visual Enhancements is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 * Copyright © 2016 RangeMachine
 */

using System.IO;

using UnityEditor;

public class BuildShaders
{
    [MenuItem("TextureReplacer/Build Shaders")]
    private static void BuildForAllPlatforms()
    {
        // Cleanup
        File.Delete("Bundles/DirectX.bundle");
        File.Delete("Bundles/OpenGL.bundle");

        // DirectX build
        BuildPipeline.BuildAssetBundles("Bundles", BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);
        File.Move("Bundles/shaders", "Bundles/DirectX.bundle");
        File.Delete("Bundles/shaders.manifest");

        // OpenGL build
        BuildPipeline.BuildAssetBundles("Bundles", BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneOSXUniversal);
        File.Move("Bundles/shaders", "Bundles/OpenGL.bundle");
        File.Delete("Bundles/shaders.manifest");

        // Cleanup
        File.Delete("Bundles/Bundles");
        File.Delete("Bundles/Bundles.manifest");
    }
}
