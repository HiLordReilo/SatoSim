using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using MonoGame.Extended.Screens;
using SatoSim.Core.Data;
using SatoSim.Core.Managers;
using SatoSim.Core.Utils;
using static SatoSim.Core.Utils.PlayfieldUtils;

namespace SatoSim.Core.Screens
{
    public class GameplayScreen : GameScreen
    {
        private SpriteBatch _spriteBatch;
        
        private Texture2D _tex_octagonReceptor;
        private Texture2D _tex_rippleReceptor;
        private Texture2D _tex_guideLamp;
        private Texture2D _tex_gradient;
        private Texture2D _tex_interlacer;

        private PlayStateManager _stateManager;
        
        public GameplayScreen(Game game) : base(game)
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _stateManager = new PlayStateManager();
        }

        public override void Initialize()
        {
            
            base.Initialize();
        }

        public override void LoadContent()
        {
            _tex_octagonReceptor = Content.Load<Texture2D>("Graphics/Gameplay/Objects/r_Center");
            _tex_rippleReceptor = Content.Load<Texture2D>("Graphics/Gameplay/Objects/r_Ripple");
            _tex_guideLamp = Content.Load<Texture2D>("Graphics/Gameplay/Objects/g_guideLamp");
            _tex_gradient = Content.Load<Texture2D>("Graphics/General/gradient");
            _tex_interlacer = Content.Load<Texture2D>("Graphics/Gameplay/interlacer");
            
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            _stateManager.Update(gameTime);

            if (KeyboardExtended.GetState().WasKeyPressed(Keys.Space))
                _stateManager.IsPlaying = !_stateManager.IsPlaying;
            if(MouseExtended.GetState().DeltaScrollWheelValue < 0)
                _stateManager.Position -= gameTime.GetElapsedSeconds() * 10f;
            if(MouseExtended.GetState().DeltaScrollWheelValue > 0)
                _stateManager.Position += gameTime.GetElapsedSeconds() * 10f;
                
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White * 0.5f);
            
            _spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
            {
                // Draw background gradient and interlacer
                _spriteBatch.Draw(_tex_gradient, GraphicsDevice.ScissorRectangle, null, Color.Black * 0.5f,
                    0f, Vector2.Zero, SpriteEffects.None, 0.1f);
                _spriteBatch.Draw(_tex_interlacer, GraphicsDevice.ScissorRectangle, null, Color.Black,
                    0f, Vector2.Zero, SpriteEffects.None, 0.11f);

                // Draw receptors
                // Octagon
                for (int i = 0; i < 8; i++)
                {
                    _spriteBatch.Draw(_tex_octagonReceptor, GetPlayfieldAnchorPosition(i), null, Color.White,
                        MathHelper.ToRadians(360f / 8f * i), _tex_octagonReceptor.Bounds.Center.ToVector2(),
                        Vector2.One, SpriteEffects.None, 0.3f);
                }

                // Ripples
                for (int i = 8; i < 15; i++)
                {
                    _spriteBatch.Draw(_tex_rippleReceptor, GetPlayfieldAnchorPosition(i), null, Color.White,
                        0, _tex_rippleReceptor.Bounds.Center.ToVector2(),
                        Vector2.One, SpriteEffects.None, 0.3f);
                }

                // Draw notes
                if (_stateManager.NoteBatches.Length > 0)
                {
                    for (var b = _stateManager.NoteBatchesPassed; b < _stateManager.NoteBatches.Length; b++)
                    {
                        var batch = _stateManager.NoteBatches[b];

                        for (int i = 0; i < batch.Notes.Length; i++)
                        {
                            if (!batch.ProcessedNotes[i])
                            {
                                if (batch.Notes[i].ObjectType < 2)
                                    DrawNote(batch.Notes[i], batch.Color);
                                if (batch.Notes[i].ObjectType == 2)
                                    DrawRipple(batch.Notes[i], batch.Color);
                            }
                        }
                    }
                }

                // Draw held notes
                if (_stateManager.ActiveHoldNotes.Length > 0)
                {
                    for (int b = 0; b < _stateManager.ActiveHoldNotes.Length; b++)
                    {
                        var batch = _stateManager.ActiveHoldNotes[b];

                        // TODO: Implement this
                    }
                }

                // Draw streams
                for (int s = _stateManager.StreamsPassed; s < _stateManager.Streams.Length; s++)
                {
                    DrawStream(_stateManager.Streams[s]);
                }
            }
            _spriteBatch.End();
        }

        
        
        private void DrawNote(ChartData.ChartObject note, Color coreColor)
        {
            float rAng = 360f / 8f * note.Position1 + 180f;
            float rRad = MathHelper.ToRadians(rAng) + Single.Pi;

            float timeToNote = (float)note.ObjectTime / 1000f - _stateManager.Position;
            
            Vector2 posOffset = Utils.Utility.CreateFromAngle(-rAng)
                                * 512f // TODO: Replace with adjustable scroll speed
                                * timeToNote;

            if (timeToNote <= 0) posOffset *= 0.5f;
            
            Vector2 position = GetPlayfieldAnchorPosition(note.Position1) + posOffset;
            
            float noteScale = 0.75f;
            
            // Skip rendering of outside of timing threshold
            if(timeToNote > 3f) return;
            
            // DEBUG: Hide notes
            if(timeToNote <= -0f) return;

            // Guide lamp
            if (timeToNote <= _stateManager.BeatDuration * 4f)
            {
                _spriteBatch.Draw(_tex_guideLamp, GetPlayfieldAnchorPosition(note.Position1), null, Color.White,
                    rRad, _tex_guideLamp.Bounds.Center.ToVector2(),
                    Vector2.One, SpriteEffects.None, 0.25f);
            }

            // Tap/Long note
            if (note.ObjectType == 0)
            {
                // Tap note
                if (!note.HoldFlag)
                {
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:BASE"],
                        position, null, Color.White, rRad,
                        GlobalAssetManager.GlobalTextures["n_Tap:BASE"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.55f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:CORE"],
                        position, null, coreColor, 0f,
                        GlobalAssetManager.GlobalTextures["n_Tap:CORE"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.56f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:SPINNER"],
                        position, null, Color.White, rRad - timeToNote * 4f,
                        GlobalAssetManager.GlobalTextures["n_Tap:SPINNER"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.57f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:OVERLAY"],
                        position, null, Color.White, rRad,
                        GlobalAssetManager.GlobalTextures["n_Tap:OVERLAY"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.58f);
                }
                else
                {
                    float timeToNoteEnd = (float)(note.ObjectTime + note.Duration) / 1000f - _stateManager.Position;
                    
                    Vector2 endPosOffset = Utils.Utility.CreateFromAngle(-rAng)
                                        * 512f // TODO: Replace with adjustable scroll speed
                                        * timeToNoteEnd;
            
                    Vector2 endPosition = GetPlayfieldAnchorPosition(note.Position1) + endPosOffset;
                    
                    Rectangle coreTop = GlobalAssetManager.GlobalTextures["n_Tap:CORE"].Bounds;
                    Rectangle coreBottom = GlobalAssetManager.GlobalTextures["n_Tap:CORE"].Bounds;
                    Rectangle overlayTop = GlobalAssetManager.GlobalTextures["n_Tap:OVERLAY"].Bounds;
                    Rectangle overlayBottom = GlobalAssetManager.GlobalTextures["n_Tap:OVERLAY"].Bounds;

                    coreTop.Height /= 2;
                    coreBottom.Y = coreBottom.Height / 2;
                    coreBottom.Height /= 2;
                    
                    overlayTop.Height /= 2;
                    overlayBottom.Y = overlayBottom.Height / 2;
                    overlayBottom.Height /= 2;

                    Rectangle bodyCore = GlobalAssetManager.GlobalTextures["n_Hold:CORE"].Bounds;
                    Rectangle bodyOverlay = GlobalAssetManager.GlobalTextures["n_Hold:OVERLAY"].Bounds;
                    Vector2 bodyCoreLength = Vector2.One * noteScale * new Vector2(1f, 1f / ((float)bodyCore.Height * noteScale));
                    Vector2 bodyOverlayLength = Vector2.One * noteScale * new Vector2(1f, 1f / ((float)bodyCore.Height * noteScale));
                    bodyCoreLength.Y *= (float)note.Duration / 1000f * 512f;
                    bodyOverlayLength.Y *= (float)note.Duration / 1000f * 512f;
                    
                    // Head
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:BASE"],
                        position, null, Color.White, rRad,
                        GlobalAssetManager.GlobalTextures["n_Tap:BASE"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.55f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:CORE"],
                        position, coreBottom, coreColor, rRad,
                        Vector2.UnitX * coreBottom.Width / 2, Vector2.One * noteScale,
                        SpriteEffects.None, 0.58f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:OVERLAY"],
                        position, overlayBottom, Color.White, rRad,
                        Vector2.UnitX * overlayBottom.Width / 2, Vector2.One * noteScale,
                        SpriteEffects.None, 0.59f);
                    
                    // Body
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Hold:CORE"],
                        position, null, coreColor, rRad + Single.Pi,
                        Vector2.UnitX * bodyCore.Width / 2, bodyCoreLength,
                        SpriteEffects.None, 0.56f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Hold:OVERLAY"],
                        position, null, Color.White, rRad + Single.Pi,
                        Vector2.UnitX * bodyOverlay.Width / 2, bodyOverlayLength,
                        SpriteEffects.None, 0.57f);
                    
                    // Tail
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:BASE"],
                        endPosition, null, Color.White, rRad,
                        GlobalAssetManager.GlobalTextures["n_Tap:BASE"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.55f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:CORE"],
                        endPosition, coreTop, coreColor, rRad,
                        GlobalAssetManager.GlobalTextures["n_Tap:CORE"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.58f);
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Tap:OVERLAY"],
                        endPosition, overlayTop, Color.White, rRad,
                        GlobalAssetManager.GlobalTextures["n_Tap:OVERLAY"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                        SpriteEffects.None, 0.59f);
                }
            }

            // Slash note
            if (note.ObjectType == 1)
            {
                rAng += 45f;
                rRad = MathHelper.ToRadians(rAng);
                
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Slash:BASE"],
                    position, null, Color.White, rRad - timeToNote * 4f,
                    GlobalAssetManager.GlobalTextures["n_Slash:BASE"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                    SpriteEffects.None, 0.5f);
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Slash:CORE"],
                    position, null, coreColor, rRad - timeToNote * 8f,
                    GlobalAssetManager.GlobalTextures["n_Slash:CORE"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                    SpriteEffects.None, 0.51f);
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Slash:OVERLAY"],
                    position, null, Color.White, rRad - timeToNote * 8f,
                    GlobalAssetManager.GlobalTextures["n_Slash:OVERLAY"].Bounds.Center.ToVector2(), Vector2.One * noteScale,
                    SpriteEffects.None, 0.52f);
            }
        }

        private void DrawRipple(ChartData.ChartObject note, Color coreColor)
        {
            float timeToNote = (float)note.ObjectTime / 1000f - _stateManager.Position;
            float noteProgress = ((_stateManager.BeatDuration * 3f - timeToNote) / _stateManager.BeatDuration) / 3f;
            float innerApproachFadeIn = Math.Clamp(noteProgress / 0.4f, 0f, 1f);
            float noteFadeIn = Math.Clamp((noteProgress - 0.2f) / 0.4f, 0f, 1f);
            float approachFadeIn = Math.Clamp((noteProgress - 0.3f) / 0.3f, 0f, 1f);
            Vector2 innerApproachScale = new Vector2(0.5f + 0.5f * innerApproachFadeIn, innerApproachFadeIn * (0.5f + 0.5f * innerApproachFadeIn)) *
                                         (noteProgress < 0.2f
                                             ? 1.5f
                                             : (1.5f - (noteProgress - 0.2f) * 0.65f));
            
            Vector2 position = GetPlayfieldAnchorPosition(note.Position1);

            float noteScale = 0.75f;
            
            // Skip rendering of outside of timing threshold
            if(timeToNote > _stateManager.BeatDuration * 3f) return;
            
            // DEBUG: Hide notes
            if (timeToNote <= -0f) return;

            // Inner approach circle
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Ripple:APPROACH"],
                position, null, Color.White * innerApproachFadeIn * 0.7f, 0f,
                GlobalAssetManager.GlobalTextures["n_Ripple:APPROACH"].Bounds.Center.ToVector2(),
                innerApproachScale * noteScale,
                SpriteEffects.None, 0.54f);
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Ripple:APPROACH"],
                position, null, Color.White * innerApproachFadeIn * 0.7f, Single.Pi / 2f,
                GlobalAssetManager.GlobalTextures["n_Ripple:APPROACH"].Bounds.Center.ToVector2(),
                innerApproachScale * noteScale,
                SpriteEffects.None, 0.54f);

            // Note
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Ripple:BASE"],
                position, null, Color.White * noteFadeIn, 0f,
                GlobalAssetManager.GlobalTextures["n_Ripple:BASE"].Bounds.Center.ToVector2(),
                Vector2.One * (0.9f + 0.1f * noteProgress) * noteScale * noteFadeIn,
                SpriteEffects.None, 0.55f);
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Ripple:CORE"],
                position, null, coreColor * noteFadeIn, 0f,
                GlobalAssetManager.GlobalTextures["n_Ripple:CORE"].Bounds.Center.ToVector2(),
                Vector2.One * noteProgress * noteScale * noteFadeIn,
                SpriteEffects.None, 0.56f);
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Ripple:SPINNER"],
                position, null, Color.White * noteFadeIn, -timeToNote * 4f,
                GlobalAssetManager.GlobalTextures["n_Ripple:SPINNER"].Bounds.Center.ToVector2(),
                Vector2.One * (0.9f + 0.1f * noteProgress) * noteScale * noteFadeIn,
                SpriteEffects.None, 0.57f);

            // Approach circle
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Ripple:APPROACH"],
                position, null, coreColor * innerApproachFadeIn * approachFadeIn, 0f,
                GlobalAssetManager.GlobalTextures["n_Ripple:APPROACH"].Bounds.Center.ToVector2(),
                Vector2.One * (5f - noteProgress * 4f) * noteScale,
                SpriteEffects.None, 0.58f);

            // _spriteBatch.DrawString(GlobalAssetManager.GeneralFont, noteProgress.ToString(), position, Color.DarkRed,
            //     0f, Vector2.Zero, Vector2.One * 2f, SpriteEffects.None, 1f);
        }

        private void DrawStream(PlayStateManager.StreamObject stream)
        {
            if (stream.StartTime - _stateManager.Position > _stateManager.BeatDuration * 3f) return;
            // DEBUG: Hide notes
            if (stream.EndTime - _stateManager.Position <= -0f) return;
            if (stream.Points.Length == 0) return;

            // TODO: Move these into a skin config
            float streamThickness = 0.33f;
            bool slidingStreams = true;

            float timeToNote = stream.StartTime - _stateManager.Position;
            float noteProgress = ((_stateManager.BeatDuration * 3f - timeToNote) / _stateManager.BeatDuration) / 3f;
            float noteFadeIn = Math.Clamp(noteProgress / 0.2f, 0f, 1f);
            float coreFadeIn = Math.Clamp((noteProgress - 0.5f) / 0.5f, 0f, 1f);

            // Handle variables
            float handleRot;
            Vector2 handlePos1 = Vector2.Zero;
            Vector2 handlePos2 = Vector2.Zero;
            float handlePosLerp = stream.PointsPassed % 1f;
            Vector2 handleOriginMultiplier = new Vector2(0.33f, 0.5f);

            if (stream.PointsPassed < stream.Points.Length)
            {
                handlePos1 = stream.Points[(int)stream.PointsPassed].Position;
                handlePos2 = stream.Points[(int)stream.PointsPassed + 1].Position;

                handleRot = float.Atan2(handlePos2.Y - handlePos1.Y, handlePos2.X - handlePos1.X);
            }
            else
            {
                handlePos1 = stream.Points[^2].Position;
                handlePos2 = stream.Points[^1].Position;
                handleRot = float.Atan2(
                    stream.Points[^1].Position.Y - stream.Points[^2].Position.Y,
                    stream.Points[^1].Position.X - stream.Points[^2].Position.X);
            }

            // Handle rendering
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:BASE"],
                Vector2.Lerp(handlePos1, handlePos2, handlePosLerp),
                null, Color.White * noteFadeIn, handleRot,
                GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:BASE"].Bounds.Size.ToVector2() *
                handleOriginMultiplier,
                Vector2.One * (1.2f - noteFadeIn * 0.2f), SpriteEffects.None, 0.84f);
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:CORE"],
                Vector2.Lerp(handlePos1, handlePos2, handlePosLerp),
                null, Color.White * noteFadeIn, handleRot,
                GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:CORE"].Bounds.Size.ToVector2() *
                handleOriginMultiplier * new Vector2(coreFadeIn, 1f),
                Vector2.One * MathHelper.Clamp(noteProgress * coreFadeIn, 0f, 1f), SpriteEffects.None, 0.85f);
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:OVERLAY"],
                Vector2.Lerp(handlePos1, handlePos2, handlePosLerp),
                null, Color.White * noteFadeIn, handleRot,
                GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:OVERLAY"].Bounds.Size.ToVector2() *
                handleOriginMultiplier,
                Vector2.One * (1.2f - noteFadeIn * 0.2f), SpriteEffects.None, 0.86f);
            
            // Approach triangle
            if (noteProgress <= 1f)
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:APPROACH"],
                    Vector2.Lerp(handlePos1, handlePos2, handlePosLerp),
                    null, Color.White * noteProgress * 2f, handleRot,
                    GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:APPROACH"].Bounds.Size.ToVector2() *
                    handleOriginMultiplier,
                    Vector2.One * (4f - noteProgress * 3f), SpriteEffects.None, 0.86f);
            
            // Draw points
            for (var i = (int)stream.PointsPassed; i < stream.Points.Length - 1; i++)
            {
                var p = stream.Points[i];

                timeToNote = p.Time - _stateManager.Position;
                noteProgress = ((_stateManager.BeatDuration * 3f - timeToNote) / _stateManager.BeatDuration) / 3f;
                noteFadeIn = Math.Clamp(noteProgress / 0.2f, 0f, 1f);

                float totalProgress = (float)i / (float)stream.Points.Length;
                Color segmentCol = Color.Lerp(StreamStartColor, StreamEndColor, totalProgress);
                float rotation = float.Atan2(stream.Points[i + 1].Position.Y - stream.Points[i].Position.Y,
                    stream.Points[i + 1].Position.X - stream.Points[i].Position.X);
                float length = Vector2.Distance(stream.Points[i].Position, stream.Points[i + 1].Position) * (slidingStreams ? noteFadeIn : 1f);
                Vector2 coreScale =
                    new Vector2(1f / GlobalAssetManager.GlobalTextures["n_Stream:PATH:CORE"].Width * length,
                        streamThickness);
                Vector2 overlayScale =
                    new Vector2(1f / GlobalAssetManager.GlobalTextures["n_Stream:PATH:OVERLAY"].Width * length,
                        streamThickness);

                // Skip rendering of outside of timing threshold
                if (timeToNote > _stateManager.BeatDuration * 3f) continue;

                // Point base
                if (i > stream.PointsPassed)
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"],
                        Vector2.Lerp(stream.Points[i - 1].Position, stream.Points[i].Position,
                            slidingStreams ? noteFadeIn : 1f),
                        null, Color.White * noteFadeIn, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"].Bounds.Center.ToVector2(),
                        Vector2.One * (1f - 0.15f * totalProgress), SpriteEffects.None, 0.8f);

                // Endpoint base
                if (i == stream.Points.Length - 2)
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"],
                        Vector2.Lerp(stream.Points[i].Position, stream.Points[i + 1].Position, noteFadeIn),
                        null, Color.White * noteFadeIn, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"].Bounds.Center.ToVector2(),
                        Vector2.One * 0.85f, SpriteEffects.None, 0.8f);

                // Line
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:PATH:CORE"], stream.Points[i].Position,
                    null, segmentCol * noteFadeIn, rotation,
                    GlobalAssetManager.GlobalTextures["n_Stream:PATH:CORE"].Bounds.Center.ToVector2() * Vector2.UnitY,
                    coreScale, SpriteEffects.None, 0.81f);
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:PATH:OVERLAY"], stream.Points[i].Position,
                    null, Color.White * noteFadeIn, rotation,
                    GlobalAssetManager.GlobalTextures["n_Stream:PATH:OVERLAY"].Bounds.Center.ToVector2() *
                    Vector2.UnitY,
                    overlayScale, SpriteEffects.None, 0.82f);

                // Point core
                if (i > stream.PointsPassed)
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"],
                        Vector2.Lerp(stream.Points[i - 1].Position, stream.Points[i].Position,
                            slidingStreams ? noteFadeIn : 1f),
                        null, segmentCol * noteFadeIn, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"].Bounds.Center.ToVector2(),
                        Vector2.One * (1f - 0.15f * totalProgress), SpriteEffects.None, 0.83f);

                // Endpoint core
                if (i == stream.Points.Length - 2)
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"],
                        Vector2.Lerp(stream.Points[i].Position, stream.Points[i + 1].Position, (slidingStreams ? noteFadeIn : 1f)),
                        null, StreamEndColor * noteFadeIn, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"].Bounds.Center.ToVector2(),
                        Vector2.One * 0.85f, SpriteEffects.None, 0.83f);
            }
        }
    }
}