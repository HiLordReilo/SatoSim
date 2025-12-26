namespace SatoSim.Core.Managers
{
    public static class SettingsManager
    {
        public enum PositionMode
        {
            Synchronized,
            SynchronizedSmoothed,
            Parallel,
        }
        
        public static bool ShowFPS = false;
        public static float FramerateTarget = 300f;
        public static bool AlignGrid = false;
        public static PositionMode ChartPositionMode = PositionMode.SynchronizedSmoothed;
    }
}