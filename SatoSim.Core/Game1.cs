using System;
using System.Globalization;
using System.IO;
using System.Text;
using FmodForFoxes;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Extended;
using MonoGame.Extended.Content;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Screens;
using SatoSim.Core.Data;
using SatoSim.Core.Managers;
using SatoSim.Core.Screens;
using SatoSim.Core.Utils;

namespace SatoSim.Core
{
    public class Game1 : Game
    {
        public static Random RandomGenerator;
        public static GraphicsDeviceManager Graphics;
        public static FMOD.System SoundSystem;
        
        private readonly ScreenManager _screenManager;
        private SpriteBatch _spriteBatch;

        private INativeFmodLibrary _fmodLibrary;
        
        private static RenderTarget2D _gameCanvas;
        private Texture2D _froze;

        private float _elapsedUpdateTime;

        public static KeyboardListener KeyboardListener;
        public static MouseListener MouseListener;
        public static GamePadListener GamePadListener;
        public static TouchListenerFix TouchListener;

        private static InputListenerComponent _inputListenerComponent;

        public static int ResolutionX = 1280;
        public static int ResolutionY = 720;
        
        public Game1(INativeFmodLibrary nativeLibrary)
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            Window.Title = "SatoSim";
            
            _screenManager = new ScreenManager();
            
            RandomGenerator = new Random((int)DateTime.Now.Ticks);

            CultureInfo.CurrentCulture =
                new CultureInfo(CultureInfo.InvariantCulture.LCID)
                {
                    NumberFormat = new NumberFormatInfo
                    {
                        NumberDecimalSeparator = ".",
                        NumberGroupSeparator = string.Empty
                    }
                };

            _fmodLibrary = nativeLibrary;
        }

        protected override void Initialize()
        {
            ApplySettings();

            Graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            Graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            Graphics.SynchronizeWithVerticalRetrace = false;
            Graphics.ApplyChanges();
            
            _gameCanvas = new RenderTarget2D(GraphicsDevice, ResolutionX, ResolutionY);
            
            Console.WriteLine($"Touch support: {TouchPanel.GetCapabilities().IsConnected} / {TouchPanel.GetCapabilities().MaximumTouchCount} points");
            if (!TouchPanel.GetCapabilities().IsConnected)
            {
                Console.WriteLine("[WARNING] Touchscreen is not connected or is unsupported. Enabling touch simulation.");
                TouchPanel.EnableMouseTouchPoint = true;
            }

            GameManager.UseTouch = TouchPanel.GetCapabilities().IsConnected;

            TouchPanel.EnableHighFrequencyTouch = true;
            
            KeyboardListener = new KeyboardListener();
            MouseListener = new MouseListener();
            GamePadListener = new GamePadListener();
            TouchListener = new TouchListenerFix();
            
            _inputListenerComponent = new InputListenerComponent(this, KeyboardListener, TouchListener,
                MouseListener, GamePadListener);
            
            FmodManager.Init(_fmodLibrary, FmodInitMode.Core, "Content");
            
            base.Initialize();
        }

        protected override void UnloadContent()
        {
            FmodManager.Unload();
            base.UnloadContent();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Prepare splashes
            using (var stream = Content.OpenStream("splashes.txt"))
            {
                using (var reader = new StreamReader(stream))
                {
                    string splashes = reader.ReadToEnd();
                    GlobalAssetManager.SplashBlurbs = splashes.Split('\n');
                }
            }

            // Set splash blurb in the game window title, Terraria style
            if (GlobalAssetManager.SplashBlurbs.Length > 0)
                Window.Title += " ~ " + GlobalAssetManager.SplashBlurbs[RandomGenerator.Next(GlobalAssetManager.SplashBlurbs.Length)] ;
            
            // Load fonts
            GlobalAssetManager.GeneralFont = Content.Load<SpriteFont>("Fonts/GeneralFont");
            GlobalAssetManager.VersatileFontSystem = new FontSystem();
            using (var stream = Content.OpenStream("Fonts/KosugiMaru.ttf"))
            {
                GlobalAssetManager.VersatileFontSystem.AddFont(stream);
            }
            GlobalAssetManager.MainFontSystem = new FontSystem();
            using (var stream = Content.OpenStream("Fonts/Outfit-Regular.ttf"))
            {
                GlobalAssetManager.MainFontSystem.AddFont(stream);
            }

            GlobalAssetManager.DifficultyRatingFont = new Utility.MonospaceFont(
                Content.Load<Texture2D>("Graphics/General/DifficultyRatingFont"), 100, 100,
                "0123456789-+\u9ED2\u795E\u2605");
            
            // Load note textures
            LoadNoteTextures();
            
            // Load stream prefabs
            LoadStreamPrefabs();
            
            // DEBUG: Load test chart
            GameManager.LoadedMetadata =
                ChartMetadata.ParseFile(File.ReadAllText(Path.Combine(GameManager.GameDirectory, "test",
                    "beast.meta")));
            GameManager.LoadedChart = ChartData.ParsePLY(File.ReadAllBytes(Path.Combine(GameManager.GameDirectory,
                "test", GameManager.LoadedMetadata.ChartFilename)));
            if (GameManager.LoadedMetadata.JacketSpecified)
                GameManager.LoadedJacket = Texture2D.FromFile(GraphicsDevice, Path.Combine(GameManager.GameDirectory,
                    "test", GameManager.LoadedMetadata.JacketFilename));
            if (GameManager.LoadedMetadata.SongSpecified)
            {
                string songPath = Path.Combine(GameManager.GameDirectory, "test",
                    GameManager.LoadedMetadata.SongFilename);
                byte[] songData = File.ReadAllBytes(songPath);
                
                GameManager.LoadedSong = CoreSystem.LoadSound(songData);
            }

            _froze = Content.Load<Texture2D>("Graphics/froze");
            
            
            // Load base screen
            _screenManager.ShowScreen(new GameplayScreen(this));
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardExtended.Update();
            MouseExtended.Update();
            FmodManager.Update();
            _elapsedUpdateTime = gameTime.GetElapsedSeconds();
            
            _inputListenerComponent.Update(gameTime);
            _screenManager.Update(gameTime);
            
            // DEBUG: Reload scene
            if (KeyboardExtended.GetState().WasKeyPressed(Keys.F5))
                _screenManager.ReplaceScreen(new GameplayScreen(this));
            
            // DEBUG: Log out touches
            // foreach (TouchLocation location in TouchPanel.GetState())
            // {
            //     if (location.State == TouchLocationState.Pressed)
            //     {
            //         Console.WriteLine("PRESSED");
            //     }
            //
            //     location.TryGetPreviousLocation(out var prev);
            //     if (prev.State != location.State)
            //         Console.WriteLine(prev.State + " -> " + location.State);
            // }
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Magenta);
            
            GraphicsDevice.SetRenderTarget(_gameCanvas);
            _screenManager.Draw(gameTime);
            GraphicsDevice.SetRenderTarget(null);
			
            float canvasScale = (float)GraphicsDevice.Viewport.Height / _gameCanvas.Height;
            if (_gameCanvas.Width * canvasScale > GraphicsDevice.Viewport.Width)
                canvasScale *= GraphicsDevice.Viewport.Width / (_gameCanvas.Width * canvasScale);
			
            _spriteBatch.Begin();
            _spriteBatch.Draw(_gameCanvas, GraphicsDevice.Viewport.Bounds.Center.ToVector2(), null, Color.White, 0, _gameCanvas.Bounds.Center.ToVector2(), canvasScale, SpriteEffects.None, 1f);
            _spriteBatch.End();
            
            _spriteBatch.Begin();
            {
                if (_screenManager.ActiveScreen == null)
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont, "No active screen.", Vector2.One * 32f,
                        Color.White);

                // _spriteBatch.DrawRectangle(0, 0, GraphicsDevice.Adapter.CurrentDisplayMode.Width,
                //     GraphicsDevice.Adapter.CurrentDisplayMode.Height, Color.Red, 3f, 1f);
                
                // Draw current framerate (if enabled in settings)
                if (SettingsManager.ShowFPS)
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont, $"FPS: { 1f / gameTime.GetElapsedSeconds() } \nTPS: { 1f / _elapsedUpdateTime }", Vector2.One * 8f, Color.Red);
                
                // for (var t = 0; t < TouchListener.CurrentState.Count; t++)
                // {
                //     var location = TouchListener.CurrentState[t];
                //     Point pos = ScreenToCanvasSpace(location.Position.ToPoint());
                //     _spriteBatch.DrawString(GlobalAssetManager.GeneralFont,
                //         $"[TouchID: {location.Id}] Loc: {pos.X} : {pos.Y} (State: {location.State})",
                //         Vector2.One * 8f + Vector2.UnitY * 64f + Vector2.UnitY * 16f * t, Color.Red);
                // }
                
                // RGM BUILD EXCLUSIVE
                // _spriteBatch.Draw(_froze, _gameCanvas.Bounds.Size.ToVector2() * canvasScale, null, Color.White * 0.33f, 0,
                //     _froze.Bounds.Size.ToVector2(), 1f, SpriteEffects.None, 1f);
            }
            _spriteBatch.End();
			
            base.Draw(gameTime);
        }

        public void ApplySettings()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1f / SettingsManager.FramerateTarget);
        }

        public void LoadNoteTextures()
        {
            GlobalAssetManager.GlobalTextures.TryAdd("n_Tap:BASE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_tap_Base"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Tap:CORE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_tap_Core"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Tap:SPINNER", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_tap_Spinner"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Tap:OVERLAY", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_tap_Overlay"));
            
            GlobalAssetManager.GlobalTextures.TryAdd("n_Hold:CORE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_hold_Core"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Hold:OVERLAY", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_hold_Overlay"));
            
            GlobalAssetManager.GlobalTextures.TryAdd("n_Slash:BASE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_slash_Base"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Slash:CORE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_slash_Core"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Slash:OVERLAY", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_slash_Overlay"));
            
            GlobalAssetManager.GlobalTextures.TryAdd("n_Ripple:BASE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_ripple_Base"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Ripple:CORE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_ripple_Core"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Ripple:SPINNER", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_ripple_Spinner"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Ripple:APPROACH", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_ripple_Approach"));
            
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:PATH:CORE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_PathCore"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:PATH:OVERLAY", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_PathOverlay"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:POINT:BASE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_PointBase"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:POINT:CORE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_PointCore"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:HANDLE:BASE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_HandleBase"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:HANDLE:CORE", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_HandleCore"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:HANDLE:OVERLAY", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_HandleOverlay"));
            GlobalAssetManager.GlobalTextures.TryAdd("n_Stream:HANDLE:APPROACH", Content.Load<Texture2D>("Graphics/Gameplay/Objects/n_stream_HandleApproach"));
        }

        public void LoadStreamPrefabs()
        {
            //string prefabsPath = Path.Combine(AppContext.BaseDirectory, Content.RootDirectory, "StreamPrefabs");
            
            for (int i = 0; i < 24; i++)
            {
                using (StreamReader reader = new StreamReader(TitleContainer.OpenStream($"Content/StreamPrefabs/path{i}")))
                {
                    PlayfieldUtils.StreamPrefabs.Add(new PlayfieldUtils.PrefabStreamPath(reader.ReadToEnd()));
                }
            }
        }

        public static Point ScreenToCanvasSpace(Point inputLocation)
        {
            float canvasScale = (float) Graphics.GraphicsDevice.Viewport.Height / ResolutionY;
            if (ResolutionX * canvasScale > Graphics.GraphicsDevice.Viewport.Width)
                canvasScale *= Graphics.GraphicsDevice.Viewport.Width / (ResolutionX * canvasScale);

            Point canvasOffset =
                new Point((Graphics.GraphicsDevice.Viewport.Width - (int)(ResolutionX * canvasScale)) / 2,
                    (Graphics.GraphicsDevice.Viewport.Height - (int)(ResolutionY * canvasScale)) / 2);
            
            Point newLoc = new Point((int)((inputLocation.X - canvasOffset.X) / canvasScale), (int)((inputLocation.Y - canvasOffset.Y) / canvasScale));
            
            return newLoc;
        }

        public static void ResetRenderTarget()
        {
            Graphics.GraphicsDevice.SetRenderTarget(_gameCanvas);
        }
    }
}
