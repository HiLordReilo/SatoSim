using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using SatoSim.Core.Data;
using SatoSim.Core.Utils;

namespace SatoSim.Core.Managers
{
    public class PlayStateManager
    {
        public float Position = -0.15f;

        public NoteBatch[] NoteBatches;
        public NoteBatch[] ActiveHoldNotes;
        public StreamObject[] Streams;

        public int NoteBatchesPassed;
        public int StreamsPassed;

        public bool IsPlaying;
        public float CurrentBPM;
        public float BeatDuration => 1f / (CurrentBPM / 60f);

        private int _maxCombo;
        
        
        public PlayStateManager()
        {
            NoteBatch._altChordColor = false;

            _maxCombo = 0;
            CurrentBPM = GameManager.LoadedChart.InitialBPM;
            
            List<NoteBatch> nbt = new List<NoteBatch>();
            List<StreamObject> str = new List<StreamObject>();

            List<ChartData.ChartObject> unprocessedObjects = GameManager.LoadedChart.ChartObjects.ToList();

            while (unprocessedObjects.Count > 0)
            {
                if (unprocessedObjects[0].ObjectType < 3)
                {
                    nbt.Add(NoteBatch.FromChord(unprocessedObjects[0].ObjectTime,
                        ref GameManager.LoadedChart.ChartObjects));

                    foreach (var note in nbt[^1].Notes) unprocessedObjects.Remove(note);
                    
                    _maxCombo += nbt[^1].Notes.Length;
                }
                else
                {
                    if (unprocessedObjects[0].ObjectType == 3)
                    {
                        str.Add(StreamObject.CreatePrefab(unprocessedObjects[0], BeatDuration)); // TODO: Add support for BPM changes

                        unprocessedObjects.RemoveAt(0);
                    }
                    else if (unprocessedObjects[0].ObjectType == 4)
                    {
                        str.Add(StreamObject.CreateFreeform(unprocessedObjects[0].ObjectID,
                            ref GameManager.LoadedChart.ChartObjects));

                        foreach (var p in str[^1].Points) unprocessedObjects.Remove(p.Note);
                    }
                    
                    _maxCombo += str[^1].Points.Length;
                }
            }

            NoteBatches = nbt.ToArray();
            Streams = str.ToArray();
            ActiveHoldNotes = new NoteBatch[8];
            
            Console.WriteLine("Chart processing finished");
            Console.WriteLine($"- Note batches: {NoteBatches.Length}");
            Console.WriteLine($"- Stream objects: {Streams.Length}");
            Console.WriteLine($"- Total combo: {_maxCombo}");
        }



        public void Update(GameTime gameTime)
        {
            if (IsPlaying)
                Position += gameTime.GetElapsedSeconds();
            
            
        }
        
        
        
        public class NoteBatch
        {
            public float Time;
            public Color Color;
            public ChartData.ChartObject[] Notes;
            public bool[] ProcessedNotes;
            public bool IsProcessed
            {
                get
                {
                    bool result = true;
                    foreach (bool flag in ProcessedNotes) result &= flag;
                    return result;
                }
            }

            internal static bool _altChordColor = false;

            internal NoteBatch()
            {
                
            }
            
            public NoteBatch(ChartData.ChartObject note)
            {
                Time = (float)note.ObjectTime / 1000f;
                Color = note.ObjectType switch
                {
                    0 => PlayfieldUtils.NoteColor_Tap,
                    1 => PlayfieldUtils.NoteColor_Slash,
                    2 => PlayfieldUtils.NoteColor_Ripple,
                    _ => Color.Gray
                };
                Notes = [note];
                ProcessedNotes = new bool[1];
            }

            public static NoteBatch FromChord(int noteTime, ref ChartData.ChartObject[] notes)
            {
                NoteBatch result = new NoteBatch() { Time = (float)noteTime / 1000f };
                List<ChartData.ChartObject> chordedNotes = new List<ChartData.ChartObject>();
                
                foreach (ChartData.ChartObject note in notes)
                {
                    if (note.ObjectTime == noteTime)
                    {
                        if (note.ObjectType < 3)
                        {
                            chordedNotes.Add(note);
                        }
                    }
                }
                
                if (chordedNotes.Count > 1)
                {
                    result.Color = _altChordColor ? PlayfieldUtils.NoteColor_Chord2 : PlayfieldUtils.NoteColor_Chord1;
                    _altChordColor = !_altChordColor;
                }
                else
                {
                    return new NoteBatch(chordedNotes[0]);
                }

                result.Notes = chordedNotes.ToArray();
                result.ProcessedNotes = new bool[chordedNotes.Count];
                
                return result;
            }
        }
        
        public class StreamObject
        {
            public float StartTime;
            public float EndTime;
            public StreamPoint[] Points;
            public float PointsPassed;
            
            public class StreamPoint
            {
                public float Time;
                public Vector2 Position;
                public ChartData.ChartObject Note;
            }

            public static StreamObject CreatePrefab(ChartData.ChartObject note, float beatDuration)
            {
                StreamObject result = new StreamObject();
                
                List<StreamPoint> points = new List<StreamPoint>();
                
                result.StartTime = (float)note.ObjectTime / 1000f;
                result.EndTime = result.StartTime + (float)note.Duration / 1000f;
                
                Vector2[] simplifiedStream = PlayfieldUtils.StreamPrefabs[note.StandardStreamPrefab]
                    .GetSimplifiedPath(result.EndTime - result.StartTime, beatDuration);
                
                for (var i = 0; i < simplifiedStream.Length; i++)
                {
                    var pos = PlayfieldUtils.StreamPrefabs[note.StandardStreamPrefab].PointPositions[i];
                    var pt = new StreamPoint();
                    pt.Time = float.Lerp(result.StartTime, result.EndTime, (float)i / (simplifiedStream.Length - 1));
                    pt.Note = note;
                    pt.Position = simplifiedStream[i];
                    
                    points.Add(pt);
                }

                result.Points = points.ToArray();

                return result;
            }

            public static StreamObject CreateFreeform(short startId, ref ChartData.ChartObject[] notes)
            {
                StreamObject result = new StreamObject();

                List<StreamPoint> points = new List<StreamPoint>();
                short curId = startId;
                while (curId != -1)
                {
                    StreamPoint p = new StreamPoint();
                    
                    p.Note = notes[curId];
                    
                    p.Time = (float)p.Note.ObjectTime / 1000f;

                    if (p.Note.AnchoredStreamPoint)
                        p.Position = PlayfieldUtils.GetPlayfieldAnchorPosition(p.Note.Position1);
                    else
                    {
                        p.Position = new Vector2(100 + 77 * (p.Note.StreamPointPosX - 1),
                            70 * p.Note.StreamPointPosY);

                        if (SettingsManager.AlignGrid)
                            p.Position.Y += 10;
                    }
                    
                    points.Add(p);

                    curId = p.Note.NextID;
                }

                result.Points = points.ToArray();
                result.StartTime = points[0].Time;
                result.EndTime = points[^1].Time;

                return result;
            }
        }
    }
}