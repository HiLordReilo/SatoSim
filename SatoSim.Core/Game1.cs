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
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;
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
        
        public static readonly ScreenManager ScreenManager = new ScreenManager();
        
        private SpriteBatch _spriteBatch;

        private INativeFmodLibrary _fmodLibrary;
        
        private static RenderTarget2D _gameCanvas;
        public static float CanvasScale { get; private set; }

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
            
            // TEMPORARY: Build song database from scratch
            RebuildSongDatabase();
            
            // TEMPORARY: Load temporary options
            {
                string[] opts = ["1.7", "0", "0.6", "1", "false", "true", "1.5", "test", "beast.meta"];
                
                string optsPath = Path.Combine(GameManager.GameDirectory, "test_opts.txt");
                
                if(!File.Exists(optsPath))
                    File.WriteAllLines(optsPath, opts);
                else 
                    opts = File.ReadAllLines(optsPath);

                GameManager.ActivePlayer.Options["PLAY_NoteSpeed"] = float.Parse(opts[0]);
                GameManager.ActivePlayer.Options["SOUND_SongOffset"] = float.Parse(opts[1]);
                GameManager.ActivePlayer.Options["PLAY_BackgroundDim"] = float.Parse(opts[2]);
                SettingsManager.ChartPositionMode = (SettingsManager.PositionMode)int.Parse(opts[3]);
                SettingsManager.ShowFPS = bool.Parse(opts[4]);
                SettingsManager.Debug_ShowDeviation = bool.Parse(opts[5]);
                SettingsManager.Debug_StreamInertiaMultiplier = float.Parse(opts[6]);
            }
            
            // Load base screen
            ScreenManager.ShowScreen(new SongSelectScreen(this), new FadeTransition(GraphicsDevice, Color.White, 2f));
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardExtended.Update();
            MouseExtended.Update();
            FmodManager.Update();
            _elapsedUpdateTime = gameTime.GetElapsedSeconds();
            
            _inputListenerComponent.Update(gameTime);
            ScreenManager.Update(gameTime);
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Magenta);
            
            GraphicsDevice.SetRenderTarget(_gameCanvas);
            ScreenManager.Draw(gameTime);
            GraphicsDevice.SetRenderTarget(null);
			
            CanvasScale = (float)GraphicsDevice.Viewport.Height / _gameCanvas.Height;
            if (_gameCanvas.Width * CanvasScale > GraphicsDevice.Viewport.Width)
                CanvasScale *= GraphicsDevice.Viewport.Width / (_gameCanvas.Width * CanvasScale);
			
            _spriteBatch.Begin();
            {
                _spriteBatch.Draw(_gameCanvas, GraphicsDevice.Viewport.Bounds.Center.ToVector2(), null, Color.White, 0, _gameCanvas.Bounds.Center.ToVector2(), CanvasScale, SpriteEffects.None, 1f);
                
                if (ScreenManager.ActiveScreen == null)
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont, "No active screen.", Vector2.One * 32f,
                        Color.White);
                
                // Draw current framerate (if enabled in settings)
                if (SettingsManager.ShowFPS)
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont,
                        $"FPS: {1f / gameTime.GetElapsedSeconds()} \nTPS: {1f / _elapsedUpdateTime}", Vector2.One * 8f,
                        Color.Red);
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
            for (int i = 0; i < 24; i++)
            {
                using (StreamReader reader = new StreamReader(TitleContainer.OpenStream($"Content/StreamPrefabs/path{i}")))
                {
                    PlayfieldUtils.StreamPrefabs.Add(new PlayfieldUtils.PrefabStreamPath(reader.ReadToEnd()));
                }
            }
        }

        public static void RebuildSongDatabase()
        {
            SongDatabase.BuildDatabase();
            Console.WriteLine("Song Database rebuilt.");
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
