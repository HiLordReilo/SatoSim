using System.Collections.Generic;

namespace SatoSim.Core.Data
{
    public class PlayerData
    {
        public string Name = "GUEST";

        public Dictionary<string, object> Options = new Dictionary<string, object>()
        {
            { "PLAY_NoteSpeed", 1.7f },
            { "PLAY_BackgroundDim", 0.5f },
            { "SOUND_SongOffset", 0.25f },
            { "VISUAL_ExtraEffects", true },
        };
}
}