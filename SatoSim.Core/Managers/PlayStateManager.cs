using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using SatoSim.Core.Data;
using SatoSim.Core.Screens;
using SatoSim.Core.Utils;
using static SatoSim.Core.Utils.PlayfieldUtils;
using static SatoSim.Core.Utils.TimingUtils;
using static SatoSim.Core.Utils.Utility;

namespace SatoSim.Core.Managers
{
    public class PlayStateManager
    {
        public float Position = -2.5f;
        private float _lastSongPos = 0f;
        private float _songOffset;
        
        public float Progress => Position / (GameManager.LoadedChart.SongLength / 1000f);

        public readonly PlayerState PlayerState;
        
        public readonly NoteBatch[] NoteBatches;
        public readonly NoteBatch[] SuspendedNotes;
        public readonly int[] SuspendedJudgments;
        public readonly float[] SuspensionTimes;
        public readonly TouchLocation?[] SuspendedSlashTouchLoc;
        public readonly StreamObject[] Streams;
        
        public int NoteBatchesPassed;
        public int StreamsPassed;

        public bool IsPlaying = true;
        public float CurrentBPM;
        public float BeatDuration => 1f / (CurrentBPM / 60f);
        
        private readonly bool[] _hoveredReceptors;

        private readonly GameplayScreen _scene;
        
        
        public PlayStateManager(GameplayScreen scene, float songOffset)
        {
            _scene = scene;
            _songOffset = songOffset;
            NoteBatch._altChordColor = false;
            
            CurrentBPM = GameManager.LoadedChart.InitialBPM;
            
            List<NoteBatch> nbt = new List<NoteBatch>();
            List<StreamObject> str = new List<StreamObject>();

            List<ChartData.ChartObject> unprocessedObjects = GameManager.LoadedChart.ChartObjects.ToList();

            int pointCount = 0;
            int noteCount = 0;
            
            while (unprocessedObjects.Count > 0)
            {
                if (unprocessedObjects[0].ObjectType < 3)
                {
                    nbt.Add(NoteBatch.FromChord(unprocessedObjects[0].ObjectTime,
                        ref GameManager.LoadedChart.ChartObjects));

                    foreach (var note in nbt[^1].Notes) unprocessedObjects.Remove(note);
                    
                    noteCount += nbt[^1].Notes.Length;
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
                    
                    pointCount += str[^1].Points.Length;
                }
            }

            NoteBatches = nbt.ToArray();
            Streams = str.ToArray();
            SuspendedNotes = new NoteBatch[8];
            SuspendedJudgments = [-1, -1, -1, -1, -1, -1, -1, -1];
            SuspensionTimes = new float[8];
            SuspendedSlashTouchLoc = new TouchLocation?[8];

            _hoveredReceptors = new bool[15];
            
            Console.WriteLine("Chart processing finished");
            Console.WriteLine($"- Note batches: {NoteBatches.Length}");
            Console.WriteLine($"- Stream objects: {Streams.Length}");
            Console.WriteLine($"- Total combo: {noteCount + pointCount}");
            Console.WriteLine($"- Total objects: {noteCount + Streams.Length}");
            
            PlayerState = new PlayerState(noteCount, Streams.Length);
        }



        public void Update(GameTime gameTime)
        {
            // Update hovered receptors
            for (int i = 0; i < 15; i++)
            {
                if (GameManager.UseTouch)
                {
                    if (Game1.TouchListener.CurrentState.Count == 0)
                    {
                        _hoveredReceptors[i] = false;
                        continue;
                    }
                    
                    // Touch check
                    foreach (TouchLocation location in Game1.TouchListener.CurrentState)
                    {
                        float dist = Vector2.Distance(
                            Game1.ScreenToCanvasSpace(location.Position.ToPoint()).ToVector2(),
                            GetPlayfieldAnchorPosition(i));

                        if (dist < ReceptorRadius)
                        {
                            _hoveredReceptors[i] = true;
                            break;
                        }
                        else
                        {
                            _hoveredReceptors[i] = false;
                        }
                    }

                }
                else
                {
                    // Mouse check
                    if (Vector2.Distance(Game1.ScreenToCanvasSpace(MouseExtended.GetState().Position).ToVector2(),
                            GetPlayfieldAnchorPosition(i)) < ReceptorRadius)
                        _hoveredReceptors[i] = MouseExtended.GetState().IsButtonDown(MouseButton.Left);
                    else
                        _hoveredReceptors[i] = false;
                }
            }
            
            if (IsPlaying)
            {
                if (!_scene.IsSongPlaying)
                    Position += gameTime.GetElapsedSeconds();
                else
                {
                    if (_lastSongPos == _scene.GetSongTime())
                        Position += gameTime.GetElapsedSeconds();
                    else
                    {
                        Position = _scene.GetSongTime() - _songOffset;
                        _lastSongPos = _scene.GetSongTime();
                    }
                }
                
                // Check if current note batch was missed
                if(NoteBatchesPassed < NoteBatches.Length)
                    if (NoteBatches[NoteBatchesPassed].Time - Position < -JUDGE_FINE)
                    {
                        for (int i = 0; i < NoteBatches[NoteBatchesPassed].NoteCount; i++)
                        {
                            if (NoteBatches[NoteBatchesPassed].ProcessedNotes[i]) continue;
                            
                            PlayerState.ProcessJudgment(JUDGE_ID_L_MISS);
                            _scene.TriggerHitEffect(NoteBatches[NoteBatchesPassed].Notes[i].Position1, JUDGE_ID_L_MISS,
                                NoteBatches[NoteBatchesPassed].Notes[i].ObjectType, 0f);
                            NoteBatches[NoteBatchesPassed].ProcessedNotes[i] = true;
                        }

                        NoteBatchesPassed++;
                    }
                
                // Update streams
                foreach (StreamObject stream in Streams)
                {
                    // Stream is too far ahead - break out
                    if(stream.StartTime - Position > JUDGE_FANTASTIC) break;
                    
                    if(stream.IsFinished) continue;
                    
                    for (int i = stream.PointsProcessed; i < stream.Points.Length - 1; i++)
                    {
                        if(i + 1 > stream.PointsPassed) break;
                        
                        PlayerState.ProcessStreamPoint(stream.PointScoreWeight);
                        stream.PointsProcessed++; 
                        stream.IsFinished = stream.PointsProcessed >= stream.Points.Length - 1;
                    }
                    
                    if(stream.IsFinished) continue;
                    
                    // Stream have ended (either timed out or reached its end)
                    if (stream.EndTime - Position <= -JUDGE_FINE || stream.PointsPassed >= stream.Points.Length - 1)
                    {
                        PlayerState.AddMissedStream();
                        stream.IsFinished = true;
                        continue;
                    }
                    
                    stream.PointsPassed += stream.Inertia * gameTime.GetElapsedSeconds();
                    stream.Inertia /= 1.005f;
                    
                    if (stream.PointsPassed > stream.Points.Length - 1)
                    {
                        stream.PointsPassed = stream.Points.Length - 1;
                        stream.Inertia = 0f;
                    }
                }

                
                // Update suspended notes
                for (int i = 0; i < 8; i++)
                {
                    // If suspended judgment is invalid/empty - skip
                    if(SuspendedJudgments[i] == -1) continue;
                    
                    // Long note
                    if (SuspendedNotes[i].Notes[0].HoldFlag)
                    {
                        float endTime =
                            (SuspendedNotes[i].Notes[0].ObjectTime + SuspendedNotes[i].Notes[0].Duration) /
                            1000f;
                        
                        // If note have ended - release the note and continue onto the next one
                        if (endTime - Position <= 0)
                        {
                            ReleaseSuspendedNote(i);
                            continue;
                        }
                        
                        // If note have not ended yet, but the receptor isn't hovered - release the note
                        if (!_hoveredReceptors[i])
                        {
                            // If note is close to ending - release it normally
                            if (endTime - Position <= JUDGE_FINE)
                                ReleaseSuspendedNote(i);
                            //Otherwise - cause a long break
                            else 
                                 ReleaseBrokenSuspendedLongNote(i);
                        }
                    }
                    
                    // Slash note
                    if (SuspendedNotes[i].Notes[0].SlashFlag)
                    {
                        // If the note is still suspended, but passed the miss judgment threshold - release the note,
                        // cause slash hold and continue onto the next note
                        if (SuspendedNotes[i].Time - Position < -JUDGE_FINE)
                        {
                            ReleaseHeldSuspendedSlashNote(i);
                        }
                        
                        // The rest of Slash note logic is done in ProcessSwipe and ProcessRelease
                    }
                }
            }
        }



        public void ProcessTap(TouchLocation input)
        {
            // Skip processing if no batches left
            if(NoteBatchesPassed >= NoteBatches.Length) return;
            
            // Skip processing if the note batch is too far
            if(NoteBatches[NoteBatchesPassed].Time - Position > JUDGE_MISS) return;
            
            List<Tuple<int, float, float>> activeReceptorDist = new List<Tuple<int, float, float>>();

            Point inputLocation = Game1.ScreenToCanvasSpace(input.Position.ToPoint());
            
            // Loop through all receptors and get the distance between it and the touch location
            // We also check if the tap overlaps the receptor
            for (int i = 0; i < 15; i++)
            {
                float dist = Vector2.Distance(inputLocation.ToVector2(), GetPlayfieldAnchorPosition(i));

                bool isCloseEnough = false;
                float closestTime = NoteBatches[NoteBatchesPassed].Time - Position;
                int batch = NoteBatchesPassed;
                while (closestTime < JUDGE_MISS)
                {
                    for (int n = 0; n < NoteBatches[batch].NoteCount; n++)
                    {
                        if (NoteBatches[batch].Notes[n].Position1 == i)
                        {
                            isCloseEnough = true;
                            closestTime = NoteBatches[batch].Time - Position;
                            break;
                        }
                    }

                    if(isCloseEnough) break;
                    
                    batch++;
                    if(batch >= NoteBatches.Length) break;
                    closestTime = NoteBatches[batch].Time - Position;
                }
                
                if (dist <= ReceptorRadius && isCloseEnough)
                    activeReceptorDist.Add(new Tuple<int, float, float>(i, dist, closestTime));
            }
            
            // If no receptors are triggered, we skip the processing
            if(activeReceptorDist.Count == 0) return;

            bool processed = false;

            foreach (var recDist in activeReceptorDist.OrderBy(tuple => tuple.Item3))
            {
                // If we processed the note already - break out
                if(processed) break;
                
                // Check whole octagon-worth of batches to avoid note lock 
                for (int b = 0; b < 8; b++)
                {
                    // If we processed the note already - break out
                    if(processed) break;
                    
                    // No batches left - skip
                    if (NoteBatchesPassed + b >= NoteBatches.Length) continue;
                    
                    // We check time distance of the batch again. If it's too far - break out 
                    if(NoteBatches[NoteBatchesPassed + b].Time - Position > JUDGE_MISS) break;
                    
                    for (int n = 0; n < NoteBatches[NoteBatchesPassed + b].NoteCount; n++)
                    {
                        // The note is already processed - skip
                        if(NoteBatches[NoteBatchesPassed + b].ProcessedNotes[n]) continue;
                        
                        // The note is in the wrong location - skip
                        if(NoteBatches[NoteBatchesPassed + b].Notes[n].Position1 != recDist.Item1) continue;

                        // Now we process the note and get its judgment ID
                        int judge = NoteBatches[NoteBatchesPassed + b].ProcessNote(n, Position);
                        
                        // If the note is Long or Slash, we suspend the note and judgment
                        if (NoteBatches[NoteBatchesPassed + b].Notes[n].SlashFlag ||
                            NoteBatches[NoteBatchesPassed + b].Notes[n].HoldFlag)
                        {
                            SuspendedNotes[recDist.Item1] = new NoteBatch(NoteBatches[NoteBatchesPassed + b].Notes[n],
                                NoteBatches[NoteBatchesPassed + b].Color);

                            SuspendedJudgments[recDist.Item1] = judge;

                            SuspensionTimes[recDist.Item1] = Position;
                            
                            // If it's a Slash note, we'd like to also keep the touch location
                            if (NoteBatches[NoteBatchesPassed + b].Notes[n].SlashFlag)
                                SuspendedSlashTouchLoc[recDist.Item1] = input;
                        }
                        else // Otherwise, we immediately process it
                        {
                            PlayerState.ProcessJudgment(judge);
                            _scene.TriggerHitEffect(recDist.Item1, judge,
                                NoteBatches[NoteBatchesPassed + b].Notes[n].ObjectType, 0f);
                        }
                        
                        processed = true;
                    }
                    
                    // If the batch is fully processed now, increase the passed batch counter
                    if (NoteBatches[NoteBatchesPassed].IsProcessed)
                    {
                        b--; // also reduce this to avoid skipping unprocessed batches
                        NoteBatchesPassed++;
                    }
                }
            }
        }

        public void ProcessSwipe(Point inputLocation, Vector2 delta)
        {
            Vector2 prevInputLoc = inputLocation.ToVector2() - delta;
            float swipeDir = float.Atan2(delta.Y, delta.X);
            //Console.WriteLine(MathHelper.ToDegrees(swipeDirection));
            
            // Process slashes
            for (int i = 0; i < 8; i++)
            {
                // If no note is suspended - skip
                if(SuspendedJudgments[i] == -1) continue;
                
                // If note is not Slash - skip
                if(!SuspendedNotes[i].Notes[0].SlashFlag) continue;
                
                // If previous input location was not on this receptor - skip 
                if(Vector2.Distance(prevInputLoc, GetPlayfieldAnchorPosition(i)) > ReceptorRadius) continue;

                if (delta != Vector2.Zero)
                {
                    ReleaseSuspendedNote(i, swipeDir);
                }
            }
            
            // Process streams
            MoveStreams(inputLocation.ToVector2(), prevInputLoc);
        }

        public void ProcessRelease(TouchLocation input)
        {
            for (int i = 0; i < 8; i++)
            {
                if(SuspendedJudgments[i] == -1) continue;
                
                // Slash note
                if(SuspendedNotes[i].Notes[0].SlashFlag)
                    if (SuspendedSlashTouchLoc[i].HasValue &&
                        input.Id == SuspendedSlashTouchLoc[i].Value.Id)
                    {
                        Vector2 delta = input.Position - SuspendedSlashTouchLoc[i].Value.Position;
                        float swipeDir = float.Atan2(delta.Y, delta.X);
                        
                        if (delta == Vector2.Zero)
                            ReleaseTapSuspendedSlashNote(i);
                        else
                            ReleaseSuspendedNote(i, swipeDir);
                    }
            }

            input.TryGetPreviousLocation(out var lastLoc);
            // Process streams
            MoveStreams(Game1.ScreenToCanvasSpace(input.Position.ToPoint()).ToVector2(),
                Game1.ScreenToCanvasSpace(lastLoc.Position.ToPoint()).ToVector2());
        }

        private void MoveStreams(Vector2 touchPos, Vector2 lastPos)
        {
            // Process streams
            foreach (StreamObject stream in Streams)
            {
                // Stream have ended (either timed out or reached its end) or is already finished - skip
                if (stream.EndTime - Position <= 0f || stream.PointsPassed >= stream.Points.Length - 1 ||
                    stream.IsFinished) continue;
                
                // Stream is too far ahead - break out
                if(stream.StartTime - Position > JUDGE_FANTASTIC) break;
                
                int p1 = (int)stream.PointsPassed;
                int p2 = (int)stream.PointsPassed + 1;
                Vector2 pos1 = stream.Points[p1].Position;
                Vector2 pos2 = stream.Points[p2].Position;
                float segLen = Vector2.Distance(pos1, pos2);
                
                Vector2 handlePos = Vector2.Lerp(pos1, pos2, stream.PointsPassed % 1f);

                if (Vector2.Distance(handlePos, touchPos) < ReceptorRadius ||
                    Vector2.Distance(handlePos, lastPos) < ReceptorRadius)
                {
                    Vector2 tPointOnLine = NearestPointOnLine(pos1, pos2 - pos1, touchPos);
                    float tPointDist = Vector2.Distance(Vector2.Zero, tPointOnLine - pos1);

                    float swipeVal = tPointDist / segLen;
                    float newVal = (int)stream.PointsPassed + swipeVal;
                    
                    if (stream.PointsPassed == 0f)
                    {
                        PlayerState.ProcessStreamPoint(stream.PointScoreWeight);
                    }

                    if (newVal > stream.PointsPassed)
                    {
                        stream.Inertia = float.Max(stream.Inertia, Vector2.Distance(touchPos, lastPos) * 0.2f);
                        stream.PointsPassed = newVal;
                    }
                    
                    //stream.IsFinished = stream.PointsPassed >= stream.Points.Length - 1;
                }
            }
        }

        #region Suspended notes release methods
        
        private void ReleaseSuspendedNote(int lane, float hitEffectRotation = 0f)
        {
            _scene.TriggerHitEffect(lane, SuspendedJudgments[lane], SuspendedNotes[lane].Notes[0].ObjectType, hitEffectRotation);
            PlayerState.ProcessJudgment(SuspendedJudgments[lane]);
            SuspendedJudgments[lane] = -1;
        }

        private void ReleaseBrokenSuspendedLongNote(int lane)
        {
            Console.WriteLine("Long break");
            
            _scene.TriggerHitEffect(lane, JUDGE_ID_E_MISS, SuspendedNotes[lane].Notes[0].ObjectType, 0f);
            PlayerState.AddLongBreak();
            SuspendedJudgments[lane] = -1;
        }
        
        private void ReleaseTapSuspendedSlashNote(int lane)
        {
            Console.WriteLine("Slash tapped");
            
            _scene.TriggerHitEffect(lane, JUDGE_ID_E_FINE, SuspendedNotes[lane].Notes[0].ObjectType, 0f);
            PlayerState.AddTappedSlash();
            SuspendedJudgments[lane] = -1;
        }
        
        private void ReleaseHeldSuspendedSlashNote(int lane)
        {
            Console.WriteLine("Slash held");
            
            _scene.TriggerHitEffect(lane, JUDGE_ID_L_MISS, SuspendedNotes[lane].Notes[0].ObjectType, 0f);
            PlayerState.AddHeldSlash();
            SuspendedJudgments[lane] = -1;
        }
        
        #endregion
        
        public void DebugDraw(SpriteBatch batch)
        {
            for(int i = 0; i < 15; i++)
                batch.DrawCircle(GetPlayfieldAnchorPosition(i), ReceptorRadius, 32, _hoveredReceptors[i] ? Color.LimeGreen : Color.Red, 2f, 1f);
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
            public int NoteCount => Notes.Length;

            internal static bool _altChordColor;

            private NoteBatch()
            {
                
            }

            private NoteBatch(ChartData.ChartObject note)
            {
                Time = note.ObjectTime / 1000f;
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
            
            public NoteBatch(ChartData.ChartObject note, Color noteColor)
            {
                Time = note.ObjectTime / 1000f;
                Color = noteColor;
                Notes = [note];
                ProcessedNotes = new bool[1];
            }

            public int ProcessNote(int noteInBatch, float timePosition)
            {
                ProcessedNotes[noteInBatch] = true;
                return GetJudgmentId(Time - timePosition);
            }
            
            public static NoteBatch FromChord(int noteTime, ref ChartData.ChartObject[] notes)
            {
                NoteBatch result = new NoteBatch() { Time = noteTime / 1000f };
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
            public float Inertia;
            public float PointScoreWeight;
            public int PointsProcessed;
            public bool IsFinished;
            
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
                
                result.StartTime = note.ObjectTime / 1000f;
                result.EndTime = result.StartTime + note.Duration / 1000f;
                
                Vector2[] simplifiedStream = StreamPrefabs[note.StandardStreamPrefab]
                    .GetSimplifiedPath(result.EndTime - result.StartTime, beatDuration);
                
                for (var i = 0; i < simplifiedStream.Length; i++)
                {
                    var pt = new StreamPoint
                    {
                        Time = float.Lerp(result.StartTime, result.EndTime, (float)i / (simplifiedStream.Length - 1)),
                        Note = note,
                        Position = simplifiedStream[i]
                    };

                    points.Add(pt);
                }

                result.Points = points.ToArray();

                result.PointScoreWeight = 1f / points.Count;
                
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
                    
                    p.Time = p.Note.ObjectTime / 1000f;

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

                result.PointScoreWeight = 1f / points.Count;
                
                return result;
            }
        }
    }
}