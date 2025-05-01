using BepInEx;
using System.IO;
using UnityEngine;

namespace AccountManagementPlugin
{
    public class DataUtils
    {
        public static Sprite LoadSprite(string file)
        {
            var path = Path.Combine(GetImagePath(), $"{file}.png");
            if (!File.Exists(path))
            {
                Plugin.Logger.LogError($"File {path} does not exist");
                return null;
            }
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(imageData))
            {
                Plugin.Logger.LogError($"Failed to load image as Texture2d");
                return null;
            }
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            if (sprite != null)
                return sprite;
            return null;
        }

        public static string GetPluginPath()
        {
            return Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_NAME);
        }

        public static string GetImagePath()
        {
            return Path.Combine(GetPluginPath(), "Images");
        }
    }
}