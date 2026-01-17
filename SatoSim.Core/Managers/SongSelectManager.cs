using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FmodForFoxes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Screens.Transitions;
using SatoSim.Core.Data;
using SatoSim.Core.Screens;
using SatoSim.Core.Utils;

namespace SatoSim.Core.Managers
{
    public class SongSelectManager
    {
        public SongDatabase.DbDirectory ActiveDirectory { get; private set; }
        public int SelectedEntry = -1;
        public int SelectedChart;

        public event EventHandler OnSongLoadingFinish;
        public event EventHandler OnSongLoadingFail;

        private Stack<int> _directoryHierarchyPosition = new Stack<int>();
        
        
        public class DirectoryEntry : SongDatabase.DbDirectory
        {
            public readonly AsyncTexture2D Icon = null;

            public DirectoryEntry() : base()
            {
                
            }

            public DirectoryEntry(SongDatabase.DbDirectory dbEntry) : base()
            {
                Path = dbEntry.Path;
                
                DirectoryName = dbEntry.DirectoryName;
                SubEntries = (SongDatabase.DbEntry[])dbEntry.SubEntries;

                if (UseCustomIcon) Icon = new AsyncTexture2D(System.IO.Path.Combine(Path, ".dirIcon"));

                for (int i = 0; i < SubEntries.Length; i++)
                {
                    if (SubEntries[i].GetType() == typeof(SongDatabase.DbDirectory) ||
                        SubEntries[i].GetType() == typeof(DirectoryEntry))
                        SubEntries[i] = new DirectoryEntry((SongDatabase.DbDirectory)SubEntries[i]);
                    if (SubEntries[i].GetType() == typeof(SongDatabase.SongEntry) ||
                        SubEntries[i].GetType() == typeof(SongEntry))
                        SubEntries[i] = new SongEntry((SongDatabase.SongEntry)SubEntries[i]);
                }
            }
        }
        
        public class SongEntry : SongDatabase.SongEntry
        {
            public readonly ChartEntry[] ChartEntries;
            
            public class ChartEntry
            {
                public readonly ChartMetadata Metadata;
                public readonly PlayRecord BestScore;
                public readonly AsyncTexture2D Jacket;
                public readonly bool WasPlayed;
                public bool JacketSpecified => Metadata.JacketSpecified;

                public ChartEntry(ChartMetadata meta, string path, string md5)
                {
                    Metadata = meta;
                    WasPlayed = GameManager.ActivePlayer.Records.TryGetValue(md5, out BestScore);

                    if (!WasPlayed) BestScore = new PlayRecord();
                    
                    if (JacketSpecified) Jacket = new AsyncTexture2D(System.IO.Path.Combine(path, meta.JacketFilename));
                }
            }

            public SongEntry() : base()
            {
                
            }
            
            public SongEntry(SongDatabase.SongEntry dbEntry) : base()
            {
                Path = dbEntry.Path;

                Charts = dbEntry.Charts;
                ChartMD5Hashes = dbEntry.ChartMD5Hashes;

                ChartEntries = new ChartEntry[Charts.Length];

                for (int i = 0; i < ChartEntries.Length; i++)
                    ChartEntries[i] = new ChartEntry(Charts[i], Path, ChartMD5Hashes[i]);
            }
        }


        public void GoToRoot()
        {
            ActiveDirectory = new DirectoryEntry(SongDatabase.RootDirectory);
            _directoryHierarchyPosition.Clear();
            SelectedEntry = -1;
        }

        public void GoFromRoot(ref Stack<int> path)
        {
            ActiveDirectory = new DirectoryEntry((SongDatabase.DbDirectory)SongDatabase.GetEntry(ref path));
            _directoryHierarchyPosition = path;
            SelectedEntry = -1;
        }

        public void GoToUpperLevel()
        {
            _directoryHierarchyPosition.TryPop(out int wentFrom);
            GoFromRoot(ref _directoryHierarchyPosition);
            SelectedEntry = wentFrom;
        }

        public void GoToDeeperLevel()
        {
            _directoryHierarchyPosition.Push(SelectedEntry);
            SelectedEntry = 0;
            GoFromRoot(ref _directoryHierarchyPosition);
        }

        public void StartSong(GraphicsDevice device, Game game)
        {
            Task.Run(() =>
            {
                SongEntry song = (SongEntry)ActiveDirectory.SubEntries[SelectedEntry];
                int chart = int.Clamp(SelectedChart, 0, song.ChartEntries.Length - 1);
                
                GameManager.LoadedMetadata = song.ChartEntries[chart].Metadata;
                
                try
                {
                    // Jacket
                    if (GameManager.LoadedMetadata.JacketSpecified)
                    {
                        string jkPath = Path.Combine(song.Path, GameManager.LoadedMetadata.JacketFilename);
                        
                        if(File.Exists(jkPath))
                        {
                            using var stream = new FileStream
                            (
                                jkPath,
                                FileMode.Open, FileAccess.Read, FileShare.Read,
                                (int)new FileInfo(jkPath).Length,
                                true
                            );
                        

                            GameManager.LoadedJacket = Texture2D.FromStream(device, stream);
                        }
                    }

                    // Song
                    if (GameManager.LoadedMetadata.SongSpecified)
                    {
                        string songPath = Path.Combine(song.Path, GameManager.LoadedMetadata.SongFilename);

                        if (File.Exists(songPath))
                        {
                            byte[] songData = File.ReadAllBytes(songPath);

                            GameManager.LoadedSong = CoreSystem.LoadSound(songData);
                        }
                    }
                    
                    // Chart
                    if (!GameManager.LoadedMetadata.ChartSpecified) throw new Exception("Chart file is not specified");

                    string chartPath = Path.Combine(song.Path, GameManager.LoadedMetadata.ChartFilename);

                    if (!File.Exists(chartPath)) throw new Exception("Chart file does not exist.");

                    byte[] chartData = File.ReadAllBytes(chartPath);
                    GameManager.LoadedChart = ChartData.ParsePLY(chartData);
                    GameManager.LoadedMD5 = song.ChartMD5Hashes[chart];
                    
                    Game1.ScreenManager.ReplaceScreen(new GameplayScreen(game),
                        new FadeTransition(device, Color.White, 2f));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to start the song: {0}", e);
                }
            });
        }
    }
}