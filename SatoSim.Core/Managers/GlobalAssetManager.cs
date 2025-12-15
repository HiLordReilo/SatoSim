using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;

namespace SatoSim.Core.Managers
{
    public static class GlobalAssetManager
    {
        public static SpriteFont GeneralFont;
        public static FontSystem VersatileFontSystem;
        public static string[] SplashBlurbs;

        public static Dictionary<string, Texture2D> GlobalTextures = new Dictionary<string, Texture2D>();
    }
}