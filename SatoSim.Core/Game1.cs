using System;
using System.Collections.ObjectModel;
using System.IO;
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
        
        private readonly ScreenManager _screenManager;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private RenderTarget2D _gameCanvas;

        private float _elapsedUpdateTime = 0f;

        public static readonly KeyboardListener KeyboardListener = new KeyboardListener();
        public static  readonly MouseListener MouseListener = new MouseListener();
        public static  readonly GamePadListener GamePadListener = new GamePadListener();
        public static  readonly TouchListener TouchListener = new TouchListener();
        
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            Window.Title = "SatoSim";
            
            _screenManager = new ScreenManager();
            
            Components.Add(new InputListenerComponent(this, KeyboardListener, TouchListener,
                MouseListener, GamePadListener));
            
            RandomGenerator = new Random((int)DateTime.Now.Ticks);
        }

        protected override void Initialize()
        {
            ApplySettings();
            
            _gameCanvas = new RenderTarget2D(GraphicsDevice, 1280, 720);
            
            Console.WriteLine($"Touch support: {TouchPanel.GetCapabilities().IsConnected} / {TouchPanel.GetCapabilities().MaximumTouchCount} points");
            if (!TouchPanel.GetCapabilities().IsConnected)
            {
                Console.WriteLine("[WARNING] Touchscreen is not connected or is unsupported. Enabling touch simulation.");
                TouchPanel.EnableMouseTouchPoint = true;
                TouchPanel.EnableMouseGestures = true;
            }

            TouchPanel.EnableHighFrequencyTouch = true;
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.Hold | GestureType.Flick | GestureType.FreeDrag;
            
            base.Initialize();
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
            
            // Load general font
            GlobalAssetManager.GeneralFont = Content.Load<SpriteFont>("Fonts/GeneralFont");
            
            // Load note textures
            LoadNoteTextures();
            
            // Load stream prefabs
            LoadStreamPrefabs();
            
            // DEBUG: Load test chart
            GameManager.LoadedChart = ChartData.ParsePLY(File.ReadAllBytes(Path.Combine(GameManager.GameDirectory, "testChart.ply")));
            
            // Load base screen
            _screenManager.ShowScreen(new GameplayScreen(this));
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardExtended.Update();
            MouseExtended.Update();
            _elapsedUpdateTime = gameTime.GetElapsedSeconds();
            
            _screenManager.Update(gameTime);
            
            // DEBUG: Reload scene
            if (KeyboardExtended.GetState().WasKeyPressed(Keys.F5))
                _screenManager.ReplaceScreen(new GameplayScreen(this));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            
            GraphicsDevice.SetRenderTarget(_gameCanvas);
            _screenManager.Draw(gameTime);
            GraphicsDevice.SetRenderTarget(null);
			
            float canvasScale = (float) GraphicsDevice.Viewport.Height / (float)_gameCanvas.Height;
            if (_gameCanvas.Width * canvasScale > GraphicsDevice.Viewport.Width)
                canvasScale *= (float) GraphicsDevice.Viewport.Width / ((float)_gameCanvas.Width * canvasScale);
			
            _spriteBatch.Begin();
            _spriteBatch.Draw(_gameCanvas, GraphicsDevice.Viewport.Bounds.Center.ToVector2(), null, Color.White, 0, _gameCanvas.Bounds.Center.ToVector2(), canvasScale, SpriteEffects.None, 1f);
            _spriteBatch.End();
            
            _spriteBatch.Begin();
            {
                if (_screenManager.ActiveScreen == null)
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont, "No active screen.", Vector2.One * 32f,
                        Color.White);
                
                // Draw current framerate (if enabled in settings)
                if (SettingsManager.ShowFPS)
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont, $"FPS: { 1f / gameTime.GetElapsedSeconds() } \nTPS: { 1f / _elapsedUpdateTime }", Vector2.One * 8f, Color.Red);

                var state = TouchPanel.GetState();
                for (var t = 0; t < state.Count; t++)
                {
                    var location = state[t];
                    _spriteBatch.DrawString(GlobalAssetManager.GeneralFont,
                        $"[TouchID: {location.Id}] Loc: {location.Position.X} : {location.Position.Y} (State: {location.State})",
                        Vector2.One * 8f + Vector2.UnitY * 64f + Vector2.UnitY * 8f * t, Color.Red);
                }
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
            string prefabsPath = Path.Combine(AppContext.BaseDirectory, Content.RootDirectory, "StreamPrefabs");

            for (int i = 0; i < Directory.GetFiles(prefabsPath).Length; i++)
            {
                PlayfieldUtils.StreamPrefabs.Add(new PlayfieldUtils.PrefabStreamPath(File.ReadAllText(Path.Combine(prefabsPath, $"path{i}"))));
            }
        }
    }
}
