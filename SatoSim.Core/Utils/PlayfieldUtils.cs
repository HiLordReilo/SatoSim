using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SatoSim.Core.Utils
{
    public static class PlayfieldUtils
    {
        public static readonly Color NoteColor_Tap = Color.Magenta;
        public static readonly Color NoteColor_Slash = Color.LimeGreen;
        public static readonly Color NoteColor_Ripple = Color.LimeGreen;
        public static readonly Color NoteColor_Chord1 = Color.CornflowerBlue;
        public static readonly Color NoteColor_Chord2 = Color.Yellow;
        public static readonly Color StreamStartColor = Color.Magenta;
        public static readonly Color StreamEndColor = Color.CornflowerBlue;

        public static List<PrefabStreamPath> StreamPrefabs = new List<PrefabStreamPath>();
        
        private static readonly Vector2 _screenCenter = new Vector2(1280f, 720f) / 2f;
        private const int R_OFFSET_CENTER = 130;
        private const int R_OFFSET_RIPPLE_EDGE = 248;
        private const int R_OFFSET_RIPPLE_MIDDLE = 400;

        public static Vector2 GetPlayfieldAnchorPosition(int location)
        {
            return location switch
            {
                // Center
                < 8 => // Octagon
                    _screenCenter + Utility.CreateFromAngle(360f / 8f * -location + 180f) * R_OFFSET_CENTER,
                8 => // Center ripple
                    _screenCenter,
                
                // Right ripples
                9 => // Top
                    _screenCenter + Vector2.UnitX * R_OFFSET_RIPPLE_EDGE + Vector2.UnitY * -R_OFFSET_RIPPLE_EDGE,
                10 => // Middle
                    _screenCenter + Vector2.UnitX * R_OFFSET_RIPPLE_MIDDLE,
                11 => // Bottom
                    _screenCenter + Vector2.UnitX * R_OFFSET_RIPPLE_EDGE + Vector2.UnitY * R_OFFSET_RIPPLE_EDGE,
                
                // Left ripples
                12 => // Top
                    _screenCenter + Vector2.UnitX * -R_OFFSET_RIPPLE_EDGE + Vector2.UnitY * -R_OFFSET_RIPPLE_EDGE,
                13 => // Middle
                    _screenCenter + Vector2.UnitX * -R_OFFSET_RIPPLE_MIDDLE,
                14 => // Bottom
                    _screenCenter + Vector2.UnitX * -R_OFFSET_RIPPLE_EDGE + Vector2.UnitY * R_OFFSET_RIPPLE_EDGE,
                
                // Invalid location: just put it in the middle of the screen
                _ => _screenCenter
            };
        }
        
        public class PrefabStreamPath
        {
            public readonly Vector2[] PointPositions;

            public PrefabStreamPath(string pathData)
            {
                string[] segments = pathData.Split(';');
                PointPositions = new Vector2[segments.Length];
                
                for (int s = 0; s < segments.Length; s++)
                {
                    string[] coords = segments[s].Split(':');
                    
                    PointPositions[s] = new Vector2(float.Parse(coords[0]), float.Parse(coords[1]));
                    PointPositions[s] += _screenCenter;
                }
            }

            public Vector2[] GetSimplifiedPath(float streamDuration, float beatDuration)
            {
                int pointCount = (int)float.Round(streamDuration / beatDuration);

                Vector2[] result = new Vector2[pointCount + 2];

                result[0] = PointPositions[0];
                result[^1] = PointPositions[^1];

                int pointStep = (PointPositions.Length / (1 + pointCount));
                
                for (int i = 1; i < pointCount + 1; i++)
                {
                    result[i] = PointPositions[pointStep * i];
                }

                return result;
            }
        }
    }
}