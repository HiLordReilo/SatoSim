using System;
using System.Runtime.InteropServices;
using FmodForFoxes;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
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

        private Channel _songChannel;
        public bool IsSongPlaying => _songChannel.IsPlaying && !_songChannel.Paused;
        
        private PlayStateManager _stateManager;
        private HitEffectManager _effectManager;
        
        private float _noteSpeed;
        private bool _noteSpeedLocked = true;
        private float _noteSpeedLockTimer = 1f;
        private float _songOffset;
        private float _bgDim;
        private bool _useJacketBg = false;
        private float _jacketBgScale;
        private Vector2 _jacketOrigin;
        
        private Texture2D _tex_octagonReceptor;
        private Texture2D _tex_rippleReceptor;
        private Texture2D _tex_guideLamp;
        private Texture2D _tex_guideArrow;
        private Texture2D _tex_guideArrowTip;
        private Texture2D _tex_gradient;
        private Texture2D _tex_interlacer;

        private Texture2D _tex_HUD_ComboLabel;
        private Texture2D _tex_HUD_ScoreLabel;
        private Texture2D _tex_HUD_ProgressHead;
        private Texture2D _tex_HUD_NoteSpeedButton;
        private Texture2D _tex_HUD_NoteSpeedButtonUnderlayLocked;
        private Texture2D _tex_HUD_NoteSpeedButtonUnderlayUnlocked;
        
        private Texture2DAtlas _atl_DifficultyLabels;

        private Utility.MonospaceFont _scoreFont;
        private Utility.MonospaceFont _gaugeFont;
        private DynamicSpriteFont _noteSpeedFont;
        private DynamicSpriteFont _songInfoFont;

        private Vector2 _screenCenter = new Vector2(Game1.ResolutionX, Game1.ResolutionY) / 2;
        
        private Vector2 _pos_HUD_ScoreValue = new Vector2(1250, 60);
        private Vector2 _pos_HUD_ScoreLabel = new Vector2(945, 4);
        private Vector2 _pos_HUD_ComboValue = new Vector2(640, 170);
        private Vector2 _pos_HUD_ComboLabel = new Vector2(608, 120);
        private Rectangle _pos_HUD_ProgressBarBackground = new Rectangle(0, 680, 1280, 200);
        private Rectangle _pos_HUD_NoteSpeedButton = new Rectangle(1280 - 64, 200, 128 / 2, 172 / 2);
        private Vector2 _pos_HUD_NoteSpeedValue = new Vector2(1250, 228);
        private Vector2 _pos_HUD_DifficultyTier = new Vector2(80, 670);
        private Vector2 _pos_HUD_DifficultyRating = new Vector2(10, 670);
        private Vector2 _pos_HUD_SongTitle = new Vector2(20, 630);
        private Vector2 _pos_HUD_SongArtist = new Vector2(20, 655);
        
        
        
        public GameplayScreen(Game game) : base(game)
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Initialize()
        {
            Console.WriteLine("GameplayScreen in");

            Game1.TouchListener.TouchStarted += Input_OnTouchStarted;
            Game1.TouchListener.TouchMoved += Input_OnTouchMoved;
            Game1.TouchListener.TouchEnded += Input_OnTouchEnded;
            
            _songChannel = GameManager.LoadedSong.Play(new ChannelGroup("BGM"), true);

            _noteSpeed = (float)GameManager.ActivePlayer.Options["PLAY_NoteSpeed"];
            _songOffset = (float)GameManager.ActivePlayer.Options["SOUND_SongOffset"];
            _bgDim = (float)GameManager.ActivePlayer.Options["PLAY_BackgroundDim"];
            
            _stateManager = new PlayStateManager(this, _songOffset);
        }

        public override void Dispose()
        {
            Console.WriteLine("GameplayScreen out");
            
            _songChannel.Stop();
            
            _spriteBatch.Dispose();
            
            Game1.TouchListener.TouchStarted -= Input_OnTouchStarted;
            Game1.TouchListener.TouchMoved -= Input_OnTouchMoved;
            Game1.TouchListener.TouchEnded -= Input_OnTouchEnded;
        }

        private void Input_OnTouchStarted(object sender, TouchEventArgs e) =>
            _stateManager.ProcessTap(e.RawTouchLocation);

        private void Input_OnTouchMoved(object sender, TouchEventExArgs e) =>
            _stateManager.ProcessSwipe(Game1.ScreenToCanvasSpace(e.Position),
                Game1.ScreenToCanvasSpace(e.DistanceMoved.ToPoint()).ToVector2());

        private void Input_OnTouchEnded(object sender, TouchEventArgs e) => _stateManager.ProcessRelease(e.RawTouchLocation);
                

        public override void LoadContent()
        {
            _tex_octagonReceptor = Content.Load<Texture2D>("Graphics/Gameplay/Objects/r_Center");
            _tex_rippleReceptor = Content.Load<Texture2D>("Graphics/Gameplay/Objects/r_Ripple");
            _tex_guideLamp = Content.Load<Texture2D>("Graphics/Gameplay/Objects/g_guideLamp");
            _tex_guideArrow = Content.Load<Texture2D>("Graphics/Gameplay/Objects/g_guideArrow");
            _tex_guideArrowTip = Content.Load<Texture2D>("Graphics/Gameplay/Objects/g_guideArrowTip");
            _tex_gradient = Content.Load<Texture2D>("Graphics/General/gradient");
            _tex_interlacer = Content.Load<Texture2D>("Graphics/Gameplay/interlacer");
            
            _tex_HUD_ComboLabel = Content.Load<Texture2D>("Graphics/Gameplay/hud_ComboLabel");
            _tex_HUD_ScoreLabel = Content.Load<Texture2D>("Graphics/Gameplay/hud_ScoreLabel");
            _tex_HUD_ProgressHead = Content.Load<Texture2D>("Graphics/Gameplay/progressHead");
            _tex_HUD_NoteSpeedButton = Content.Load<Texture2D>("Graphics/Gameplay/hud_NoteSpeed");
            _tex_HUD_NoteSpeedButtonUnderlayLocked = Content.Load<Texture2D>("Graphics/Gameplay/hud_NoteSpeedUnderlayLocked");
            _tex_HUD_NoteSpeedButtonUnderlayUnlocked = Content.Load<Texture2D>("Graphics/Gameplay/hud_NoteSpeedUnderlayUnlocked");

            _atl_DifficultyLabels = Texture2DAtlas.Create("TierLabels",
                Content.Load<Texture2D>("Graphics/Gameplay/DifficultyTierLabels"), 500, 100);
            
            _effectManager = new HitEffectManager(_spriteBatch, (bool)GameManager.ActivePlayer.Options["VISUAL_ExtraEffects"],
                Content.Load<Texture2D>("Graphics/Gameplay/HitEffects/HanteiText"),
                Content.Load<Texture2D>("Graphics/Gameplay/HitEffects/Base"),
                Content.Load<Texture2D>("Graphics/Gameplay/HitEffects/Slash"),
                Content.Load<Texture2D>("Graphics/Gameplay/HitEffects/Extra_RippleFlash"));
            
            _scoreFont = new Utility.MonospaceFont(Content.Load<Texture2D>("Graphics/General/ScoreFont"), 128, 128,
                "0123456789");
            _gaugeFont = new Utility.MonospaceFont(Content.Load<Texture2D>("Graphics/General/GaugeFont"), 32, 32,
                "0123456789%");
            _noteSpeedFont = GlobalAssetManager.MainFontSystem.GetFont(28f);
            _songInfoFont = GlobalAssetManager.VersatileFontSystem.GetFont(22f);

            if (GameManager.LoadedMetadata.JacketSpecified)
            {
                _useJacketBg = true;

                _jacketBgScale = (float)Game1.ResolutionX / GameManager.LoadedJacket.Width;

                _jacketOrigin = GameManager.LoadedJacket.Bounds.Center.ToVector2();
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_stateManager.IsPlaying && _stateManager.Position >= -_songOffset && _songChannel.Paused)
            {
                _songChannel.TrackPosition = (uint)((_stateManager.Position) * 1000f);
                _songChannel.Resume();
            }
                
            _stateManager.Update(gameTime);
            
            // DEBUG
            if (KeyboardExtended.GetState().WasKeyPressed(Keys.Space))
            {
                _stateManager.IsPlaying = !_stateManager.IsPlaying;
                _songChannel.Paused = !_stateManager.IsPlaying || _stateManager.Position < _songOffset;
            }
            // if(MouseExtended.GetState().DeltaScrollWheelValue < 0)
            //     _stateManager.Position -= gameTime.GetElapsedSeconds() * 10f;
            // if(MouseExtended.GetState().DeltaScrollWheelValue > 0)
            //     _stateManager.Position += gameTime.GetElapsedSeconds() * 10f;

            // Note speed lock behavior
            {
                bool heldDown = false;

                if (GameManager.UseTouch)
                {
                    foreach (TouchLocation location in TouchPanel.GetState())
                        if (_pos_HUD_NoteSpeedButton.Contains(Game1.ScreenToCanvasSpace(location.Position.ToPoint())))
                        {
                            heldDown = true;
                            break;
                        }
                }
                else
                    heldDown =
                        _pos_HUD_NoteSpeedButton.Contains(Game1.ScreenToCanvasSpace(Mouse.GetState().Position)) &&
                        MouseExtended.GetState().IsButtonDown(MouseButton.Left);
                
                if (heldDown)
                    _noteSpeedLockTimer -= gameTime.GetElapsedSeconds() * 2f;
                
                if (!heldDown && _noteSpeedLocked)
                    _noteSpeedLockTimer = 2f;
                else
                    _noteSpeedLockTimer -= gameTime.GetElapsedSeconds();
            
                if (_noteSpeedLockTimer <= 0f)
                {
                    _noteSpeedLocked = !_noteSpeedLocked;
                    _noteSpeedLockTimer = _noteSpeedLocked ? 1f : 5f;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_useJacketBg ? Color.Black : Color.White * 0.5f);
            
            // Back layer
            _spriteBatch.Begin(SpriteSortMode.FrontToBack);
            {
                // Draw background
                if (_useJacketBg)
                {
                    _spriteBatch.Draw(GameManager.LoadedJacket, _screenCenter, null, Color.White * (1f - _bgDim), 0f,
                        _jacketOrigin, _jacketBgScale, SpriteEffects.None, 0f);
                }
                
                // Draw background gradient and interlacer
                _spriteBatch.Draw(_tex_gradient, GraphicsDevice.ScissorRectangle, null, Color.Black * 0.5f,
                    0f, Vector2.Zero, SpriteEffects.None, 0.1f);

                if (_stateManager.PlayerState.Gauge < 70f)
                    _spriteBatch.Draw(_tex_interlacer, GraphicsDevice.ScissorRectangle, null, new Color(0xAA101010),
                        0f, Vector2.Zero, SpriteEffects.None, 0.11f);
                
                // Combo
                if (_stateManager.PlayerState.Combo > 1)
                {
                    _spriteBatch.DrawString(_scoreFont, _stateManager.PlayerState.Combo.ToString(),
                        _pos_HUD_ComboValue,
                        Color.White * 0.33f, Vector2.One * 0.5f, Vector2.One, Vector2.One * 64f,
                        -52f, 1f, null);
                    _spriteBatch.Draw(_tex_HUD_ComboLabel, _pos_HUD_ComboLabel, null, Color.White * 0.33f, 0f, Vector2.Zero,
                        Vector2.One * 0.5f, SpriteEffects.None, 1f);
                }
                
                // Gauge
                string gaugeString = ((int)_stateManager.PlayerState.Gauge).ToString() + '%';
                Color gaugeCol = _stateManager.PlayerState.Gauge >= 70f ? Color.Magenta : Color.Cyan;
                
                _spriteBatch.DrawString(_gaugeFont, gaugeString, _screenCenter - Vector2.UnitY * 76f, gaugeCol,
                    Vector2.One, Vector2.One, Vector2.One * 16f, -12f, 1f, null);
            }
            _spriteBatch.End();
            
            // Middle layer
            _spriteBatch.Begin(SpriteSortMode.FrontToBack);
            {
                // Draw receptors
                // Octagon
                for (int i = 0; i < 8; i++)
                {
                    _spriteBatch.Draw(_tex_octagonReceptor, GetPlayfieldAnchorPosition(i), null, Color.White,
                        MathHelper.ToRadians(360f / 8f * i), _tex_octagonReceptor.Bounds.Center.ToVector2(),
                        Vector2.One, SpriteEffects.None, 0.3f);
                    
                    //_spriteBatch.DrawCircle(GetPlayfieldAnchorPosition(i), ReceptorRadius, 32, Color.Green, 3f, 0.2f);
                }

                // Ripples
                for (int i = 8; i < 15; i++)
                {
                    _spriteBatch.Draw(_tex_rippleReceptor, GetPlayfieldAnchorPosition(i), null, Color.White,
                        0, _tex_rippleReceptor.Bounds.Center.ToVector2(),
                        Vector2.One, SpriteEffects.None, 0.3f);
                    
                    //_spriteBatch.DrawCircle(GetPlayfieldAnchorPosition(i), ReceptorRadius, 32, Color.Green, 3f, 0.2f);
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

                // Draw suspended notes
                if (_stateManager.SuspendedNotes.Length > 0)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        if (_stateManager.SuspendedJudgments[b] == -1) continue;

                        if (_stateManager.SuspendedNotes[b].Notes[0].ObjectType == 0)
                            DrawSuspendedHold(_stateManager.SuspendedNotes[b].Notes[0], _stateManager.SuspendedNotes[b].Color, _stateManager.SuspensionTimes[b]);
                        if (_stateManager.SuspendedNotes[b].Notes[0].ObjectType == 1)
                            DrawNote(_stateManager.SuspendedNotes[b].Notes[0], _stateManager.SuspendedNotes[b].Color);
                        if (_stateManager.SuspendedNotes[b].Notes[0].ObjectType == 2)
                            DrawRipple(_stateManager.SuspendedNotes[b].Notes[0], _stateManager.SuspendedNotes[b].Color);
                    }
                }

                // Draw streams
                for (int s = _stateManager.StreamsPassed; s < _stateManager.Streams.Length; s++)
                {
                    DrawStream(_stateManager.Streams[s]);
                }
                
                // Draw effects
                _effectManager.DrawEffects(gameTime);
            }
            _spriteBatch.End();
            
            // Top layer
            _spriteBatch.Begin(SpriteSortMode.FrontToBack);
            {
                // Progress bar
                _spriteBatch.Draw(_tex_gradient, _pos_HUD_ProgressBarBackground, null, Color.White * 0.5f, 0f,
                    Vector2.Zero, SpriteEffects.None, 0f);
                _spriteBatch.DrawLine(Vector2.UnitY * 720f,
                    1280f * _stateManager.Progress, 0f,
                    Color.Green, 6f, 0.9f);
                _spriteBatch.Draw(_tex_HUD_ProgressHead,
                    Vector2.UnitY * 720f + Vector2.UnitX * (1280f * _stateManager.Progress), null, Color.White, 0f,
                    Vector2.One * 16f, Vector2.One, SpriteEffects.None, 1f);
                
                // Score
                _spriteBatch.Draw(_tex_HUD_ScoreLabel, _pos_HUD_ScoreLabel, null, Color.White * 0.66f, 0f, Vector2.Zero,
                    Vector2.One * 0.5f, SpriteEffects.None, 1f);
                _spriteBatch.DrawString(_scoreFont, _stateManager.PlayerState.Score.ToString("0000000"),
                    _pos_HUD_ScoreValue,
                    Color.White * 0.33f, Vector2.One * 0.6f, Vector2.One, Vector2.One * 64f,
                    -52f, 1f, null,
                    Utility.TextAlignment.Right);
                if (_stateManager.PlayerState.Score > 0)
                    _spriteBatch.DrawString(_scoreFont, _stateManager.PlayerState.Score.ToString("N0"),
                        _pos_HUD_ScoreValue,
                        Color.White * 0.5f, Vector2.One * 0.6f, Vector2.One, Vector2.One * 64f,
                        -52f, 1f, null,
                        Utility.TextAlignment.Right);

                // Song info
                _spriteBatch.DrawString(_songInfoFont, GameManager.LoadedMetadata.Title, _pos_HUD_SongTitle,
                    Color.White * 0.5f, layerDepth: 1f);
                _spriteBatch.DrawString(_songInfoFont, GameManager.LoadedMetadata.Artist, _pos_HUD_SongArtist,
                    Color.White * 0.5f, scale: Vector2.One * 0.8f, layerDepth: 1f);
                
                // Difficulty
                Color diffRateCol = GameManager.LoadedMetadata.Difficulty != ChartMetadata.DifficultyTier.SilentNight
                    ? GameManager.LoadedMetadata.DifficultyRating > 14 ? Color.LightGoldenrodYellow : Color.White
                    : Color.White;
                
                _spriteBatch.Draw(_atl_DifficultyLabels[(int)GameManager.LoadedMetadata.Difficulty - 1],
                    _pos_HUD_DifficultyTier, Color.White * 0.5f, 0f, Vector2.Zero, Vector2.One * 0.5f,
                    SpriteEffects.None, 0.5f);
                _spriteBatch.DrawString(GlobalAssetManager.DifficultyRatingFont,
                    GameManager.LoadedMetadata.RatingString,
                    _pos_HUD_DifficultyRating,
                    diffRateCol * 0.5f, Vector2.One * 0.5f, Vector2.One, Vector2.Zero,
                    -52f, 1f, null,
                    Utility.TextAlignment.Left);
                
                // Note speed button
                _spriteBatch.Draw(
                    _noteSpeedLocked
                        ? _tex_HUD_NoteSpeedButtonUnderlayLocked
                        : _tex_HUD_NoteSpeedButtonUnderlayUnlocked, _pos_HUD_NoteSpeedButton, null, Color.White, 0f,
                    Vector2.Zero, SpriteEffects.None, 0.95f);
                _spriteBatch.Draw(_tex_HUD_NoteSpeedButton, _pos_HUD_NoteSpeedButton, null, Color.White, 0f,
                    Vector2.Zero, SpriteEffects.None, 0.96f);
                _spriteBatch.DrawString(_noteSpeedFont, _noteSpeed.ToString("F1"), _pos_HUD_NoteSpeedValue,
                    new Color(0xFF1B1B1B), layerDepth: 0.97f);

                //_stateManager.DebugDraw(_spriteBatch);
                if (SettingsManager.Debug_ShowDeviation)
                {
                    string devStr = (int)float.Round(_stateManager.PlayerState.AverageTiming * 1000f) + "ms";
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont, devStr,
                        _screenCenter + Vector2.UnitY * 300f, Color.White * 0.5f, 0f,
                        GlobalAssetManager.GeneralFont.MeasureString(devStr) / 2f, Vector2.One, SpriteEffects.None, 1f);
                }

            }
            _spriteBatch.End();
        }

        
        
        private void DrawNote(ChartData.ChartObject note, Color coreColor)
        {
            float rAng = 360f / 8f * note.Position1 + 180f;
            float rRad = MathHelper.ToRadians(rAng) + Single.Pi;

            float speedMult = 300f * _noteSpeed;
            
            
            float timeToNote = note.ObjectTime / 1000f - _stateManager.Position;
            
            Vector2 posOffset = Utility.CreateFromAngle(-rAng)
                                * speedMult
                                * timeToNote;

            if (timeToNote <= 0) posOffset *= 0.5f;
            
            Vector2 position = GetPlayfieldAnchorPosition(note.Position1) + posOffset;
            
            float noteScale = 0.75f;
            
            // Skip rendering of outside of timing threshold
            if(timeToNote > 3f) return;
            
            // DEBUG: Hide notes
            //if(timeToNote <= -0f) return;

            // Guide indicators
            if (timeToNote <= _stateManager.BeatDuration * 4f)
            {

                // Guide arrow
                if (note.Position1 is >= 3 and <= 5)
                {
                    float arrowProgress = (_stateManager.BeatDuration * 4f - timeToNote) / (_stateManager.BeatDuration * 4f);
                    Vector2 arrowOffset = Utility.CreateFromAngle(-rAng) * 32f;
                    Vector2 arrowTipOffset = Utility.CreateFromAngle(-rAng) * (80f * arrowProgress + 32f);
                    Rectangle arrowSrc = new Rectangle(0, 0, _tex_guideArrow.Width,
                        (int)(_tex_guideArrow.Height * arrowProgress) + 1);
                    
                    float arrowFadeIn = float.Clamp(arrowProgress * 8f, 0f, 1f);
                    float arrowFadeOut = 1f - float.Clamp((arrowProgress - 0.95f) * 8f, 0f, 1f);
                    
                    
                    _spriteBatch.Draw(_tex_guideArrow, _screenCenter + arrowOffset, arrowSrc, Color.White * arrowFadeIn * arrowFadeOut,
                        rRad + Single.Pi, _tex_guideArrow.Bounds.Center.ToVector2() * Vector2.UnitX,
                        Vector2.One, SpriteEffects.None, 0.24f);
                    _spriteBatch.Draw(_tex_guideArrowTip, _screenCenter + arrowTipOffset, null, Color.White * arrowFadeIn * arrowFadeOut,
                        rRad + Single.Pi, _tex_guideArrowTip.Bounds.Center.ToVector2() * Vector2.UnitX + Vector2.UnitY,
                        Vector2.One, SpriteEffects.None, 0.25f);
                }
                
                // Guide lamp
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
                    float timeToNoteEnd = (note.ObjectTime + note.Duration) / 1000f - _stateManager.Position;

                    Vector2 endPosOffset = Utility.CreateFromAngle(-rAng)
                                           * speedMult
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
                    Vector2 bodyCoreLength = Vector2.One * noteScale * new Vector2(1f, 1f / (bodyCore.Height * noteScale));
                    Vector2 bodyOverlayLength = Vector2.One * noteScale * new Vector2(1f, 1f / (bodyCore.Height * noteScale));
                    bodyCoreLength.Y *= (note.Duration / 1000f + (timeToNote <= 0 ? timeToNote * 0.5f : 0)) * speedMult;
                    bodyOverlayLength.Y *= (note.Duration / 1000f + (timeToNote <= 0 ? timeToNote * 0.5f : 0)) * speedMult;
                    
                    
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

        private void DrawSuspendedHold(ChartData.ChartObject note, Color coreColor, float suspensionTime)
        {
            float rAng = 360f / 8f * note.Position1 + 180f;
            float rRad = MathHelper.ToRadians(rAng) + Single.Pi;

            float speedMult = 300f * _noteSpeed;
            
            float timeToNote = note.ObjectTime / 1000f - suspensionTime;
            
            Vector2 posOffset = Utility.CreateFromAngle(-rAng)
                                * speedMult
                                * timeToNote;
            
            float timeToNoteEnd = (note.ObjectTime + note.Duration) / 1000f - _stateManager.Position;
            
            Vector2 endPosOffset = Utility.CreateFromAngle(-rAng)
                                   * speedMult
                                   * timeToNoteEnd;

            if (timeToNote <= 0) posOffset *= 0.5f;
            
            Vector2 position = GetPlayfieldAnchorPosition(note.Position1) + posOffset;
            
            Vector2 endPosition = GetPlayfieldAnchorPosition(note.Position1) + endPosOffset;
            
            float noteScale = 0.75f;

            // Guide lamp
            if (timeToNote <= _stateManager.BeatDuration * 4f)
            {
                _spriteBatch.Draw(_tex_guideLamp, GetPlayfieldAnchorPosition(note.Position1), null, Color.White,
                    rRad, _tex_guideLamp.Bounds.Center.ToVector2(),
                    Vector2.One, SpriteEffects.None, 0.25f);
            }
                    
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
            Vector2 bodyCoreLength = Vector2.One * noteScale * new Vector2(1f, 1f / (bodyCore.Height * noteScale));
            Vector2 bodyOverlayLength = Vector2.One * noteScale * new Vector2(1f, 1f / (bodyCore.Height * noteScale));
            bodyCoreLength.Y *= (timeToNoteEnd - (timeToNote <= 0 ? timeToNote * 0.5f : timeToNote)) * speedMult;
            bodyOverlayLength.Y *= (timeToNoteEnd - (timeToNote <= 0 ? timeToNote * 0.5f : timeToNote)) * speedMult;
                    
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

        private void DrawRipple(ChartData.ChartObject note, Color coreColor)
        {
            float timeToNote = note.ObjectTime / 1000f - _stateManager.Position;
            float noteProgress = ((_stateManager.BeatDuration * 3f - timeToNote) / _stateManager.BeatDuration) / 3f;
            float innerApproachFadeIn = Math.Clamp(noteProgress / 0.4f, 0f, 1f);
            float noteFadeIn = Math.Clamp((noteProgress - 0.2f) / 0.4f, 0f, 1f);
            float approachFadeIn = Math.Clamp((noteProgress - 0.3f) / 0.3f, 0f, 1f);

            
            // Skip rendering of outside of timing threshold
            if(timeToNote > _stateManager.BeatDuration * 3f) return;
            
            // TODO: Implement ripple style changing
            DrawStandardRippleStyle(note.Position1, coreColor, timeToNote, noteProgress, innerApproachFadeIn,
                noteFadeIn, approachFadeIn);
        }

        private void DrawStandardRippleStyle(int location, Color coreColor, float timeToNote, float noteProgress,
            float innerApproachFadeIn, float noteFadeIn, float approachFadeIn)
        {
            Vector2 innerApproachScale = new Vector2(0.5f + 0.5f * innerApproachFadeIn, innerApproachFadeIn * (0.5f + 0.5f * innerApproachFadeIn)) *
                                         (noteProgress < 0.2f
                                             ? 1.5f
                                             : (1.5f - (noteProgress - 0.2f) * 0.65f));
            
            Vector2 position = GetPlayfieldAnchorPosition(location);

            float noteScale = 0.75f;
            
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
        }

        private void DrawFallingRippleStyle(int location, Color coreColor, float timeToNote, float noteProgress,
            float innerApproachFadeIn, float noteFadeIn, float approachFadeIn)
        {
            // TODO: Implement falling ripple style
        }

        private void DrawSplitRippleStyle(int location, Color coreColor, float timeToNote, float noteProgress,
            float innerApproachFadeIn, float noteFadeIn, float approachFadeIn)
        {
            // TODO: Implement split ripple style
        }

        private void DrawStream(PlayStateManager.StreamObject stream)
        {
            if (stream.StartTime - _stateManager.Position > _stateManager.BeatDuration * 2f) return;
            // DEBUG: Hide notes
            if (stream.EndTime - _stateManager.Position <= -TimingUtils.JUDGE_FINE) return;
            if (stream.Points.Length == 0) return;
            if (stream.PointsPassed >= stream.Points.Length - 1) return;

            // TODO: Move these into a skin config
            float streamThickness = 0.33f;

            float timeToNote = stream.StartTime - _stateManager.Position;
            float timeToNoteEnd = stream.EndTime - _stateManager.Position;
            float noteProgress = ((_stateManager.BeatDuration * 2f - timeToNote) / _stateManager.BeatDuration) / 2f;
            float noteFadeIn = Math.Clamp(noteProgress / 0.2f, 0f, 1f);
            float coreFadeIn = Math.Clamp((noteProgress - 0.5f) / 0.5f, 0f, 1f);

            float timeOut = 1f - Math.Clamp((TimingUtils.JUDGE_FINE - timeToNoteEnd) / TimingUtils.JUDGE_FINE, 0f, 1f);
            
            // Handle variables
            float handleRot;
            Vector2 handlePos1;
            Vector2 handlePos2;
            float handlePosLerp = stream.PointsPassed % 1f;
            Vector2 handleOriginMultiplier = new Vector2(0.33f, 0.5f);

            if (stream.PointsPassed < stream.Points.Length - 1)
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

            Vector2 handlePos = Vector2.Lerp(handlePos1, handlePos2, handlePosLerp);
            
            // Handle rendering
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:BASE"], handlePos, null,
                Color.White * noteFadeIn * timeOut, handleRot,
                GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:BASE"].Bounds.Size.ToVector2() *
                handleOriginMultiplier, Vector2.One * (1.2f - noteFadeIn * 0.2f), SpriteEffects.None, 0.84f);
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:CORE"], handlePos, null,
                Color.White * noteFadeIn * timeOut, handleRot,
                GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:CORE"].Bounds.Size.ToVector2() *
                handleOriginMultiplier * new Vector2(coreFadeIn, 1f),
                Vector2.One * MathHelper.Clamp(noteProgress * coreFadeIn, 0f, 1f), SpriteEffects.None, 0.85f);
            _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:OVERLAY"], handlePos, null,
                Color.White * noteFadeIn * timeOut, handleRot,
                GlobalAssetManager.GlobalTextures["n_Stream:HANDLE:OVERLAY"].Bounds.Size.ToVector2() *
                handleOriginMultiplier, Vector2.One * (1.2f - noteFadeIn * 0.2f), SpriteEffects.None, 0.86f);
            
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
                noteProgress = ((_stateManager.BeatDuration * 2f - timeToNote) / _stateManager.BeatDuration) / 2f;
                noteFadeIn = Math.Clamp(noteProgress / 0.2f, 0f, 1f);

                float totalProgress = (float)i / stream.Points.Length;
                float segProgress = (int)stream.PointsPassed == i ? stream.PointsPassed % 1f : 0f;
                Color segmentCol = Color.Lerp(StreamStartColor, StreamEndColor, totalProgress);
                float rotation = float.Atan2(stream.Points[i + 1].Position.Y - stream.Points[i].Position.Y,
                    stream.Points[i + 1].Position.X - stream.Points[i].Position.X);
                float length = Vector2.Distance(stream.Points[i].Position, stream.Points[i + 1].Position) * (1f - segProgress);
                Vector2 segPos = Vector2.Lerp(stream.Points[i].Position, stream.Points[i + 1].Position, segProgress);
                Vector2 coreScale =
                    new Vector2(1f / GlobalAssetManager.GlobalTextures["n_Stream:PATH:CORE"].Width * length,
                        streamThickness);
                Vector2 overlayScale =
                    new Vector2(1f / GlobalAssetManager.GlobalTextures["n_Stream:PATH:OVERLAY"].Width * length,
                        streamThickness);

                // Skip rendering of outside of timing threshold
                if (timeToNote > _stateManager.BeatDuration * 3f) continue;

                // Line
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:PATH:CORE"], segPos,
                    null, segmentCol * noteFadeIn * timeOut, rotation,
                    GlobalAssetManager.GlobalTextures["n_Stream:PATH:CORE"].Bounds.Center.ToVector2() * Vector2.UnitY,
                    coreScale, SpriteEffects.None, 0.81f);
                _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:PATH:OVERLAY"], segPos,
                    null, Color.White * noteFadeIn * timeOut, rotation,
                    GlobalAssetManager.GlobalTextures["n_Stream:PATH:OVERLAY"].Bounds.Center.ToVector2() *
                    Vector2.UnitY,
                    overlayScale, SpriteEffects.None, 0.82f);
                
                // Point
                if (i > stream.PointsPassed && i % 2 == 0)
                {
                    // Base
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"],
                        stream.Points[i].Position, null, Color.White * noteFadeIn * timeOut, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"].Bounds.Center.ToVector2(),
                        Vector2.One * (1f - 0.25f * totalProgress), SpriteEffects.None, 0.8f);
                    
                    // Core
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"],
                        stream.Points[i].Position, null, segmentCol * noteFadeIn * timeOut, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"].Bounds.Center.ToVector2(),
                        Vector2.One * (1f - 0.25f * totalProgress), SpriteEffects.None, 0.83f);
                }


                // Endpoint
                if (i == stream.Points.Length - 2)
                {
                    // Base
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"],
                        stream.Points[i + 1].Position, null, Color.White * noteFadeIn * timeOut, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:BASE"].Bounds.Center.ToVector2(),
                        Vector2.One * 0.75f, SpriteEffects.None, 0.8f);
                    
                    // Core
                    _spriteBatch.Draw(GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"],
                        stream.Points[i + 1].Position, null, StreamEndColor * noteFadeIn * timeOut, rotation,
                        GlobalAssetManager.GlobalTextures["n_Stream:POINT:CORE"].Bounds.Center.ToVector2(),
                        Vector2.One * 0.75f, SpriteEffects.None, 0.83f);
                }

            }
        }

        public void TriggerHitEffect(int locationId, int judgeId, int noteType, float rotation)
        {
            _effectManager.TriggerEffect(locationId, judgeId, noteType, rotation);
        }

        public void TriggerHitsound()
        {
            // TODO: Implement hitsounds
        }

        public float GetSongTime() => _songChannel.TrackPosition / 1000f;
    }
}