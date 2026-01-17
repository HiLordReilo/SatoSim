using System;
using System.Collections.Generic;
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
using MonoGame.Extended.Screens.Transitions;
using MonoGame.Extended.Tweening;
using SatoSim.Core.Data;
using SatoSim.Core.Managers;
using SatoSim.Core.Utils;

namespace SatoSim.Core.Screens
{
    public class SongSelectScreen : GameScreen
    {
        private SpriteBatch _spriteBatch;

        private RenderTarget2D[] _renderedListEntries;
        private int[] _visibleDbEntryIdOffsets;
        private int _visibleDbEntryBaseOffset = 0;
        private float _listScroll;
        private float _listScrollInertia;
        private Rectangle[] _listEntryRects;
        private float _listEntryScale = 0.75f;

        private SongSelectManager _manager;
        private Interpolables _interpolables = new Interpolables();
        private Tweener _interpolator = new Tweener();

        private bool _canStart = false;
        private bool _blockStart = false;
        
        private class Interpolables()
        {
            public float ListEntryContentHeight { get; set; } = 1f;
            public float ListEntryHeight { get; set; } = 1f;
        }
        
        private Texture2D _tex_StartButton;
        private Texture2D _tex_ListEntry;
        private Texture2D _tex_FolderEntry;
        private Texture2D _tex_FolderIcon;
        private Dictionary<ChartMetadata.DifficultyTier, Texture2D> _tex_SongEntryTiers;
        private Texture2D _tex_ListEntrySelection;

        private Vector2 _pos_entrySelectionText = new Vector2(585f, 18f);
        private Vector2 _pos_entryDirName = new Vector2(100f, 26f);
        private Vector2 _pos_entryDirIcon = new Vector2(14f, 10f);
        private Rectangle _pos_entrySongJacket = new Rectangle(97, 10, 64, 64);
        private Vector2 _pos_entrySongRating = new Vector2(6f, 6f);
        private Vector2 _pos_entrySongTitle = new Vector2(182f, 18f);
        private Vector2 _pos_entrySongArtist = new Vector2(182f, 58f);
        private Vector2 _pos_entrySongScore = new Vector2(423f, 10f);
        private Vector2 _pos_entrySongGrade = new Vector2(421f, 45f);
        private Vector2 _pos_entrySongMedal = new Vector2(496f, 45f);
        private Vector2 _pos_listEntryDistance = new Vector2(32f, 86f);
        private Vector2 _pos_listAnchor = new Vector2(200f, 8f);
        private Rectangle _pos_startButton;
        private Rectangle _pos_listParenDirButton;

        private DynamicSpriteFont _entrySelectionFont;
        private DynamicSpriteFont _dirEntryFont;
        private DynamicSpriteFont _songEntryInfoFont;
        private Utility.MonospaceFont _songEntryScoreFont;
        private Texture2DAtlas _songEntryGrades;
        private Texture2DAtlas _songEntryMedals;
        
        
        
        public SongSelectScreen(Game game) : base(game)
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _manager = new SongSelectManager();
            
            _renderedListEntries = new RenderTarget2D[11];
            _visibleDbEntryIdOffsets = new int[11];
            _listEntryRects = new Rectangle[11];
        }

        public override void LoadContent()
        {
            _tex_StartButton = Content.Load<Texture2D>("Graphics/SongSelect/startButton");
            _tex_ListEntry = Content.Load<Texture2D>("Graphics/SongSelect/listEntry");
            _tex_FolderEntry = Content.Load<Texture2D>("Graphics/SongSelect/folderEntry");
            _tex_FolderIcon = Content.Load<Texture2D>("Graphics/Icons/ico_folderDefault");
            _tex_SongEntryTiers = new ()
            {
                { ChartMetadata.DifficultyTier.Invalid, Content.Load<Texture2D>("Graphics/SongSelect/songEntry_invalid") },
                { ChartMetadata.DifficultyTier.Light, Content.Load<Texture2D>("Graphics/SongSelect/songEntry_light") },
                { ChartMetadata.DifficultyTier.Medium, Content.Load<Texture2D>("Graphics/SongSelect/songEntry_med") },
                { ChartMetadata.DifficultyTier.Beast, Content.Load<Texture2D>("Graphics/SongSelect/songEntry_beast") },
                { ChartMetadata.DifficultyTier.Nightmare, Content.Load<Texture2D>("Graphics/SongSelect/songEntry_night") },
                { ChartMetadata.DifficultyTier.SilentNight, Content.Load<Texture2D>("Graphics/SongSelect/songEntry_silnight") },
                { ChartMetadata.DifficultyTier.Other, Content.Load<Texture2D>("Graphics/SongSelect/songEntry_other") },
            };
            _tex_ListEntrySelection = Content.Load<Texture2D>("Graphics/SongSelect/selectedListEntry");
            
            for (int i = 0; i < _renderedListEntries.Length; i++)
            {
                _renderedListEntries[i] = new RenderTarget2D(GraphicsDevice, _tex_SongEntryTiers[0].Width,
                    _tex_SongEntryTiers[0].Height);
            }

            _manager.GoToRoot();
            UpdateEntryIdOffsets(_visibleDbEntryBaseOffset);
            Anim_OnDirectoryChange();

            _entrySelectionFont = GlobalAssetManager.VersatileFontSystem.GetFont(20f);
            _dirEntryFont = GlobalAssetManager.VersatileFontSystem.GetFont(26f);
            _songEntryInfoFont = GlobalAssetManager.VersatileFontSystem.GetFont(16f);
            _songEntryScoreFont =
                new Utility.MonospaceFont(Content.Load<Texture2D>("Graphics/SongSelect/songEntry_scoreFont"), 80, 80,
                    "0123456789");

            _songEntryGrades = Texture2DAtlas.Create("grades",
                Content.Load<Texture2D>("Graphics/SongSelect/songEntry_grades"), 70, 36);
            _songEntryMedals = Texture2DAtlas.Create("medals",
                Content.Load<Texture2D>("Graphics/SongSelect/songEntry_medals"), 40, 36);

            _pos_startButton = _tex_StartButton.Bounds;
            _pos_startButton.Offset(1000, 200);
            
            _pos_listParenDirButton = _tex_FolderIcon.Bounds;
            _pos_listParenDirButton.Offset(86, 300);
            
            
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.FreeDrag | GestureType.Flick;
            TouchPanel.EnableMouseGestures = true;
        }

        public override void UnloadContent()
        {
            TouchPanel.EnabledGestures = GestureType.None;
            TouchPanel.EnableMouseGestures = false;
        }

        #region Gestures handling
        private void Gesture_OnTap(GestureSample gesture)
        {
            Point touchPos = Game1.ScreenToCanvasSpace(gesture.Position.ToPoint());
            
            // List controls
            if (touchPos.X >= _pos_listAnchor.X)
            {
                for (int i = 0; i < _listEntryRects.Length; i++)
                {
                    // Touched something else - continue
                    if(!_listEntryRects[i].Contains(touchPos)) continue;

                    // If entry is already selected - enter selected directory or change selected chart
                    if (_manager.SelectedEntry == _visibleDbEntryIdOffsets[i])
                    {
                        SongDatabase.DbEntry dbEntry =
                            _manager.ActiveDirectory.SubEntries[
                                _visibleDbEntryIdOffsets[i] % _manager.ActiveDirectory.SubEntries.Length];

                        if (dbEntry.GetType() == typeof(SongSelectManager.DirectoryEntry))
                        {
                            _canStart = false;
                            _manager.GoToDeeperLevel();
                            _visibleDbEntryBaseOffset = 0;
                            UpdateEntryIdOffsets();
                            Anim_OnDirectoryChange();
                        }
                        else if (dbEntry.GetType() == typeof(SongSelectManager.SongEntry))
                        {
                            _canStart = !_blockStart;
                            _manager.SelectedChart++;
                            if (_manager.SelectedChart >= ((SongSelectManager.SongEntry)dbEntry).ChartEntries.Length)
                                _manager.SelectedChart = 0;
                            Anim_OnChartChange();
                        }
                    }
                    // Otherwise, change selection
                    else
                    {
                        _manager.SelectedEntry = _visibleDbEntryIdOffsets[i];
                        _canStart = !_blockStart;
                    }
                    
                    return;
                }
            }
            
            // Start button
            if (_pos_startButton.Contains(touchPos) && _canStart)
            {
                _canStart = false;
                _blockStart = true;
                _manager.StartSong(GraphicsDevice, Game);
                return;
            }
            
            // Parent directory button
            if (_pos_listParenDirButton.Contains(touchPos))
            {
                _canStart = false;
                _manager.GoToUpperLevel();
                UpdateEntryIdOffsets();
                Anim_OnDirectoryChange();
                return;
            }
        }
        
        private void Gesture_OnDrag(GestureSample gesture)
        {
            Point touchPos = Game1.ScreenToCanvasSpace(gesture.Position.ToPoint());
            Point touchPos2 = Game1.ScreenToCanvasSpace(gesture.Position2.ToPoint());
            Point touchPosDelta = Game1.ScreenToCanvasSpace(gesture.Delta.ToPoint());
            
            // List controls
            if (touchPos.X >= _pos_listAnchor.X &&
                touchPos.X <= _pos_listAnchor.X + _tex_ListEntry.Width + _pos_listEntryDistance.X * 8f)
            {
                _listScroll += gesture.Delta.Y / (_pos_listEntryDistance.Y * Game1.CanvasScale);
            }
        }

        private void Gesture_OnFlick(GestureSample gesture)
        {
            Point touchPos = Game1.ScreenToCanvasSpace(gesture.Position.ToPoint());
            Point touchPos2 = Game1.ScreenToCanvasSpace(gesture.Position2.ToPoint());
            Point touchPosDelta = Game1.ScreenToCanvasSpace(gesture.Delta.ToPoint());
            
            // List controls
            if (touchPos.X >= _pos_listAnchor.X)
            {
                _listScrollInertia = touchPosDelta.Y;
            }
        }
        #endregion
        


        public override void Update(GameTime gameTime)
        {
            _interpolator.Update(gameTime.GetElapsedSeconds());
            
            // DEBUG: Reload the scene
            if (Keyboard.GetState().IsKeyDown(Keys.F5)) Game1.ScreenManager.ReplaceScreen(new SongSelectScreen(Game), new FadeTransition(GraphicsDevice, Color.Black, 0.5f));

            _listScroll += _listScrollInertia;

            if (float.Abs(_listScrollInertia) > 0f)
            {
                _listScrollInertia -= (_listScrollInertia > 0f ? 0.001f : -0.001f) * gameTime.GetElapsedSeconds();
            }
            else
                _listScrollInertia = 0f;

            if (_listScroll > 1f)
            {
                _listScroll -= 1f;
                _visibleDbEntryBaseOffset--;
                if (_visibleDbEntryBaseOffset < 0)
                    _visibleDbEntryBaseOffset = _manager.ActiveDirectory.SubEntries.Length - 1;
                UpdateEntryIdOffsets(_visibleDbEntryBaseOffset);
            }
            if (_listScroll < -1f)
            {
                _listScroll += 1f;
                _visibleDbEntryBaseOffset++;
                UpdateEntryIdOffsets(_visibleDbEntryBaseOffset);
            }
            
            // Update entry bounding rectangles
            for (int i = 0; i < _renderedListEntries.Length; i++)
            {
                _listEntryRects[i] = new Rectangle((_pos_listAnchor + _pos_listEntryDistance * (i - 1f + _listScroll)).ToPoint(),
                    (_tex_ListEntry.Bounds.Size.ToVector2() * _listEntryScale).ToPoint());
            }
            
            // Check touch gestures
            while(TouchPanel.IsGestureAvailable)
            {
                GestureSample g = TouchPanel.ReadGesture();
                
                if ((g.GestureType & GestureType.Tap) != 0) Gesture_OnTap(g);
                if ((g.GestureType & GestureType.FreeDrag) != 0) Gesture_OnDrag(g);
                if ((g.GestureType & GestureType.Flick) != 0) Gesture_OnFlick(g);
            }
        }
        
        public override void Draw(GameTime gameTime)
        {
            PrepareSonglistEntries();
            
            GraphicsDevice.Clear(Color.WhiteSmoke);
            
            _spriteBatch.Begin(SpriteSortMode.FrontToBack);
            
            for (int i = 0; i < _renderedListEntries.Length; i++)
            {
                Vector2 pos = _pos_listAnchor + _pos_listEntryDistance * (i - 1f + _listScroll);
                SongDatabase.DbEntry entry =
                    _manager.ActiveDirectory.SubEntries[
                        _visibleDbEntryIdOffsets[i] % _manager.ActiveDirectory.SubEntries.Length];
                bool isSong = entry.GetType() == typeof(SongSelectManager.SongEntry);
                
                
                // Base plate
                Vector2 platePosOrigin = _tex_ListEntry.Bounds.Size.ToVector2() / 2f;
                
                _spriteBatch.Draw(_tex_ListEntry, pos + Vector2.One * 3f + platePosOrigin * _listEntryScale, null,
                    Color.White, 0, platePosOrigin,
                    Vector2.One * _listEntryScale * new Vector2(1f, _interpolables.ListEntryHeight), SpriteEffects.None,
                    0.5f);
                
                // Selection border
                if (_manager.SelectedEntry == _visibleDbEntryIdOffsets[i] % _manager.ActiveDirectory.SubEntries.Length)
                {
                    // Selection border
                    _spriteBatch.Draw(_tex_ListEntrySelection, pos, null,
                        Color.White, 0, Vector2.Zero,
                        Vector2.One * _listEntryScale, SpriteEffects.None,
                        0.6f);
                        
                    // Label
                    string selLabelStr = isSong
                        ? $"{int.Clamp(_manager.SelectedChart + 1, 0, ((SongSelectManager.SongEntry)entry).ChartEntries.Length)}/{((SongSelectManager.SongEntry)entry).ChartEntries.Length}"
                        : "Open";
                    Vector2 selLabelPosOrigin = _entrySelectionFont.MeasureString(selLabelStr) / 2f;

                    _spriteBatch.DrawString(_entrySelectionFont, selLabelStr,
                        pos + _pos_entrySelectionText * _listEntryScale, new Color(0xFF1B1B1B),
                        origin: selLabelPosOrigin, scale: Vector2.One * _listEntryScale, layerDepth: 1f);
                }
                
                // Entry contents
                Vector2 contentOrigin = _tex_FolderEntry.Bounds.Size.ToVector2() / 2f;

                _spriteBatch.Draw(_renderedListEntries[i], pos + (contentOrigin + Vector2.One * 10f) * _listEntryScale,
                    null, Color.White, 0, contentOrigin,
                    Vector2.One * _listEntryScale * new Vector2(1f, isSong ? _interpolables.ListEntryContentHeight : _interpolables.ListEntryHeight ),
                    SpriteEffects.None, 0.51f);
            }
            
            // Start button
            _spriteBatch.Draw(_tex_StartButton, _pos_startButton, null, Color.White * (_canStart ? 1f : 0.5f), 0f,
                Vector2.Zero, SpriteEffects.None, 0.5f);
            
            // Parent directory button
            _spriteBatch.Draw(_tex_FolderIcon, _pos_listParenDirButton, null, Color.Orange, 0f,
                Vector2.Zero, SpriteEffects.None, 0.5f);
            
            _spriteBatch.End();
        }



        #region Animations

        private void Anim_OnDirectoryChange()
        {
            _interpolables.ListEntryHeight = 0f;
            _interpolator.TweenTo(_interpolables, x => x.ListEntryHeight, 1f, 0.25f).Easing(EasingFunctions.CubicOut);
            Anim_OnChartChange();
        }
        
        private void Anim_OnChartChange()
        {
            _interpolables.ListEntryContentHeight = 0f;
            _interpolator.TweenTo(_interpolables, x => x.ListEntryContentHeight, 1f, 0.25f).Easing(EasingFunctions.CubicOut);
        }

        #endregion
        
        private void PrepareSonglistEntries()
        {
            for (int i = 0; i < _renderedListEntries.Length; i++)
            {
                RenderEntry(ref _renderedListEntries[i],
                    _manager.ActiveDirectory.SubEntries[_visibleDbEntryIdOffsets[i] % _manager.ActiveDirectory.SubEntries.Length],
                    _manager.SelectedEntry == _visibleDbEntryIdOffsets[i] % _manager.ActiveDirectory.SubEntries.Length);
            }
        }
        
        private void RenderEntry(ref RenderTarget2D entry, SongDatabase.DbEntry dbEntry, bool isSelected)
        {
            GraphicsDevice.SetRenderTarget(entry);
            
            GraphicsDevice.Clear(Color.Transparent);
            
            _spriteBatch.Begin();
            {
                // Folder
                if (dbEntry.GetType() == typeof(SongSelectManager.DirectoryEntry))
                {
                    SongSelectManager.DirectoryEntry dir = (SongSelectManager.DirectoryEntry)dbEntry;
                    
                    // Icon
                    _spriteBatch.Draw(_tex_FolderIcon, _pos_entryDirIcon, Color.White);
                    
                    // Directory name
                    _spriteBatch.DrawString(_dirEntryFont, dir.DirectoryName, _pos_entryDirName, Color.LightYellow,
                        effect: FontSystemEffect.Stroked, effectAmount: 2, layerDepth: 1f);
                    
                    // Directory plate
                    _spriteBatch.Draw(_tex_FolderEntry, Vector2.Zero, Color.White);
                }
                // Song
                else if (dbEntry.GetType() == typeof(SongSelectManager.SongEntry))
                {
                    SongSelectManager.SongEntry song = (SongSelectManager.SongEntry)dbEntry;
                    SongSelectManager.SongEntry.ChartEntry chart =
                        song.ChartEntries[int.Clamp(_manager.SelectedChart, 0, song.ChartEntries.Length - 1)];
                    
                    // Song jacket
                    if (chart.JacketSpecified)
                    {
                        if (chart.Jacket.State == AsyncTexture2D.JacketState.READY)
                            _spriteBatch.Draw(chart.Jacket.Texture, _pos_entrySongJacket, null, Color.White,
                                0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                        else
                            _spriteBatch.DrawRectangle(_pos_entrySongJacket, Color.Aqua, 3f, 1f); // TODO: Add stub jacket.
                    }
                    
                    // Song info
                    _spriteBatch.DrawString(_songEntryInfoFont, chart.Metadata.Title, _pos_entrySongTitle, Color.White,
                        layerDepth: 1f); // Title
                    _spriteBatch.DrawString(_songEntryInfoFont, chart.Metadata.Artist, _pos_entrySongArtist, Color.White,
                        layerDepth: 1f); // Artist
                    
                    // Score data
                    _spriteBatch.DrawString(_songEntryScoreFont, chart.BestScore.Score.ToString("0000000"),
                        _pos_entrySongScore, Color.White, Vector2.One * 0.35f, Vector2.One, Vector2.One, -40f, 0.5f,
                        null, Utility.TextAlignment.Left); // Score
                    _spriteBatch.Draw(_songEntryGrades[chart.BestScore.GetGradeId()], _pos_entrySongGrade,
                        Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.5f); // Grade
                    _spriteBatch.Draw(_songEntryMedals[_songEntryMedals.RegionCount - 1 - (int)chart.BestScore.Medal],
                        _pos_entrySongMedal, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.5f); // Medal
                    
                    // Difficulty plate
                    _spriteBatch.Draw(_tex_SongEntryTiers[chart.Metadata.Difficulty], Vector2.Zero,
                        Color.White);
                    
                    // Difficulty rating
                    _spriteBatch.DrawString(GlobalAssetManager.DifficultyRatingFont, chart.Metadata.RatingString,
                        _pos_entrySongRating, Color.White, Vector2.One * 0.6f, Vector2.One, Vector2.One, -52f, 1f, null,
                        Utility.TextAlignment.Left);
                }
            }
            _spriteBatch.End();
            
            Game1.ResetRenderTarget();
        }

        private void UpdateEntryIdOffsets(int baseOffset = 0)
        {
            for (int i = 0; i < _renderedListEntries.Length; i++)
            {
                _visibleDbEntryIdOffsets[i] = (i + baseOffset) % _manager.ActiveDirectory.SubEntries.Length;
            }
        }
    }
}