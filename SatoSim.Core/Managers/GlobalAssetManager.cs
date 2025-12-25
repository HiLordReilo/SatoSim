using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using static SatoSim.Core.Utils.Utility;

namespace SatoSim.Core.Managers
{
    public static class GlobalAssetManager
    {
        public static SpriteFont GeneralFont;
        public static FontSystem VersatileFontSystem;
        public static FontSystem MainFontSystem;
        public static MonospaceFont DifficultyRatingFont;
        public static string[] SplashBlurbs;

        public static Dictionary<string, Texture2D> GlobalTextures = new Dictionary<string, Texture2D>();
    }
}