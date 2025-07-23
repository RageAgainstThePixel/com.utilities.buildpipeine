// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Utilities.Editor.BuildPipeline
{
    internal static class TextureExtensions
    {
        public static void SetWSAPlayerIcons(this Texture2D icon)
        {
            // get the icon asset path, and if it is not inside the Assets/ folder, then create a new directory named "Assets/Icons" and put the newly created icons there
            var iconPath = AssetDatabase.GetAssetPath(icon);
            string iconsDirectory;

            if (!iconPath.StartsWith("Assets/"))
            {
                iconsDirectory = "Assets/Icons/WSAPlayer";
            }
            else
            {
                iconsDirectory = Path.GetDirectoryName(iconPath) ?? throw new NullReferenceException(nameof(iconPath));
            }

            if (!Directory.Exists(iconsDirectory))
            {
                Directory.CreateDirectory(iconsDirectory);
            }

            foreach (PlayerSettings.WSAImageType imageType in Enum.GetValues(typeof(PlayerSettings.WSAImageType)))
            {
                foreach (PlayerSettings.WSAImageScale imageScale in Enum.GetValues(typeof(PlayerSettings.WSAImageScale)))
                {
                    try
                    {
                        Vector2Int? size = null;
                        var imagePath = $"{iconsDirectory}/{imageType}{imageScale}.png";

                        switch (imageType)
                        {
                            case PlayerSettings.WSAImageType.SplashScreenImage:
                                switch (imageScale)
                                {
                                    case PlayerSettings.WSAImageScale._100:
                                        size = new Vector2Int(620, 300);
                                        break;
                                    case PlayerSettings.WSAImageScale._200:
                                        size = new Vector2Int(1240, 600);
                                        break;
                                }
                                break;
                            case PlayerSettings.WSAImageType.PackageLogo:
                                switch (imageScale)
                                {
                                    case PlayerSettings.WSAImageScale._100:
                                        size = new Vector2Int(50, 50);
                                        break;
                                    case PlayerSettings.WSAImageScale._125:
                                        size = new Vector2Int(63, 63);
                                        break;
                                    case PlayerSettings.WSAImageScale._150:
                                        size = new Vector2Int(75, 75);
                                        break;
                                    case PlayerSettings.WSAImageScale._200:
                                        size = new Vector2Int(100, 100);
                                        break;
                                    case PlayerSettings.WSAImageScale._400:
                                        size = new Vector2Int(200, 200);
                                        break;
                                }
                                break;
                            case PlayerSettings.WSAImageType.UWPSquare44x44Logo:
                                switch (imageScale)
                                {
                                    case PlayerSettings.WSAImageScale._100:
                                        size = new Vector2Int(44, 44);

                                        break;
                                    case PlayerSettings.WSAImageScale._125:
                                        size = new Vector2Int(55, 55);
                                        break;
                                    case PlayerSettings.WSAImageScale._150:
                                        size = new Vector2Int(66, 66);
                                        break;
                                    case PlayerSettings.WSAImageScale._200:
                                        size = new Vector2Int(88, 88);
                                        break;
                                    case PlayerSettings.WSAImageScale._400:
                                        size = new Vector2Int(176, 176);
                                        break;
                                }
                                break;
                            case PlayerSettings.WSAImageType.UWPSquare71x71Logo:
                                switch (imageScale)
                                {
                                    case PlayerSettings.WSAImageScale._100:
                                        size = new Vector2Int(71, 71);
                                        break;
                                    case PlayerSettings.WSAImageScale._125:
                                        size = new Vector2Int(89, 89);
                                        break;
                                    case PlayerSettings.WSAImageScale._150:
                                        size = new Vector2Int(107, 107);
                                        break;
                                    case PlayerSettings.WSAImageScale._200:
                                        size = new Vector2Int(142, 142);
                                        break;
                                    case PlayerSettings.WSAImageScale._400:
                                        size = new Vector2Int(284, 284);
                                        break;
                                }
                                break;
                            case PlayerSettings.WSAImageType.UWPSquare150x150Logo:
                                switch (imageScale)
                                {
                                    case PlayerSettings.WSAImageScale._100:
                                        size = new Vector2Int(150, 150);
                                        break;
                                    case PlayerSettings.WSAImageScale._125:
                                        size = new Vector2Int(188, 188);
                                        break;
                                    case PlayerSettings.WSAImageScale._150:
                                        size = new Vector2Int(225, 225);
                                        break;
                                    case PlayerSettings.WSAImageScale._200:
                                        size = new Vector2Int(300, 300);
                                        break;
                                    case PlayerSettings.WSAImageScale._400:
                                        size = new Vector2Int(600, 600);
                                        break;
                                }
                                break;
                            case PlayerSettings.WSAImageType.UWPSquare310x310Logo:
                                switch (imageScale)
                                {
                                    case PlayerSettings.WSAImageScale._100:
                                        size = new Vector2Int(310, 310);
                                        break;
                                    case PlayerSettings.WSAImageScale._125:
                                        size = new Vector2Int(388, 388);
                                        break;
                                    case PlayerSettings.WSAImageScale._150:
                                        size = new Vector2Int(465, 465);
                                        break;
                                    case PlayerSettings.WSAImageScale._200:
                                        size = new Vector2Int(620, 620);
                                        break;
                                    case PlayerSettings.WSAImageScale._400:
                                        size = new Vector2Int(1240, 1240);
                                        break;
                                }
                                break;
                            case PlayerSettings.WSAImageType.UWPWide310x150Logo:
                                switch (imageScale)
                                {
                                    case PlayerSettings.WSAImageScale._100:
                                        size = new Vector2Int(310, 150);
                                        break;
                                    case PlayerSettings.WSAImageScale._125:
                                        size = new Vector2Int(388, 188);
                                        break;
                                    case PlayerSettings.WSAImageScale._150:
                                        size = new Vector2Int(465, 225);
                                        break;
                                    case PlayerSettings.WSAImageScale._200:
                                        size = new Vector2Int(620, 300);
                                        break;
                                    case PlayerSettings.WSAImageScale._400:
                                        size = new Vector2Int(1240, 600);
                                        break;
                                }
                                break;
                        }

                        switch (imageScale)
                        {
                            case PlayerSettings.WSAImageScale.Target16:
                                size = new Vector2Int(16, 16);
                                break;
                            case PlayerSettings.WSAImageScale.Target24:
                                size = new Vector2Int(24, 24);
                                break;
                            case PlayerSettings.WSAImageScale.Target32:
                                size = new Vector2Int(32, 32);
                                break;
                            case PlayerSettings.WSAImageScale.Target48:
                                size = new Vector2Int(48, 48);
                                break;
                            case PlayerSettings.WSAImageScale.Target256:
                                size = new Vector2Int(256, 256);
                                break;
                        }

                        if (size == null)
                        {
                            continue;
                        }

                        var fileInfo = new FileInfo(imagePath);

                        if (!fileInfo.Exists)
                        {
                            var texture = new Texture2D(size.Value.x, size.Value.y, TextureFormat.RGBA32, false);
                            icon.CopyTexture(texture);
                            File.WriteAllBytes(imagePath, texture.EncodeToPNG());
                        }

                        PlayerSettings.WSA.SetVisualAssetsImage($"{iconsDirectory}/{imageType}{imageScale}.png", imageType, imageScale);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to set WSA image for {imageType} | {imageScale}: {e}");
                    }
                }
            }
        }

        public static void CopyTexture(this Texture2D source, Texture2D destination)
        {
            // Always use RGBA32 for compatibility with SetPixels
            var iconWidth = destination.width;
            var iconHeight = destination.height;
            var srcAspect = (float)source.width / source.height;
            var dstAspect = (float)iconWidth / iconHeight;

            int targetWidth, targetHeight;

            if (srcAspect > dstAspect)
            {
                // Source is wider than destination
                targetWidth = iconWidth;
                targetHeight = Mathf.RoundToInt(iconWidth / srcAspect);
            }
            else
            {
                // Source is taller than destination
                targetHeight = iconHeight;
                targetWidth = Mathf.RoundToInt(iconHeight * srcAspect);
            }

            // Calculate offsets to center the image
            var xOffset = (iconWidth - targetWidth) / 2;
            var yOffset = (iconHeight - targetHeight) / 2;

            // Resize source texture into a temporary scaled version (always RGBA32)
            var scaled = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            var pixels = source.GetPixels32();
            var scaledPixels = new Color32[targetWidth * targetHeight];

            for (var y = 0; y < targetHeight; y++)
            {
                for (var x = 0; x < targetWidth; x++)
                {
                    var u = (float)x / (targetWidth - 1);
                    var v = (float)y / (targetHeight - 1);
                    var srcX = Mathf.Clamp(Mathf.RoundToInt(u * (source.width - 1)), 0, source.width - 1);
                    var srcY = Mathf.Clamp(Mathf.RoundToInt(v * (source.height - 1)), 0, source.height - 1);
                    scaledPixels[y * targetWidth + x] = pixels[srcY * source.width + srcX];
                }
            }

            scaled.SetPixels32(scaledPixels);
            scaled.Apply();

            var clear = new Color32(0, 0, 0, 0);
            var iconPixels = Enumerable.Repeat(clear, iconWidth * iconHeight).ToArray();
            destination.SetPixels32(iconPixels);

            // Copy scaled image into icon, centered
            for (var y = 0; y < targetHeight; y++)
            {
                for (var x = 0; x < targetWidth; x++)
                {
                    destination.SetPixel(x + xOffset, y + yOffset, scaled.GetPixel(x, y));
                }
            }

            destination.Apply();
        }
    }
}
