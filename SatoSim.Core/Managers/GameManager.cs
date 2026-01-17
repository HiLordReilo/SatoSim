using FmodForFoxes;
using Microsoft.Xna.Framework.Graphics;
using SatoSim.Core.Data;

namespace SatoSim.Core.Managers
{
    public static class GameManager
    {
        public static string GameDirectory;
        public static ChartData LoadedChart;
        public static string LoadedMD5;
        public static ChartMetadata LoadedMetadata;
        public static Texture2D LoadedJacket;
        public static Sound LoadedSong;
        public static PlayerData ActivePlayer = new PlayerData();
        public static bool UseTouch;
    }
}