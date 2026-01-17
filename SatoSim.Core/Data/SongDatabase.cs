using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SatoSim.Core.Managers;
using SatoSim.Core.Utils;

namespace SatoSim.Core.Data
{
    public static class SongDatabase
    {
        public static DbDirectory RootDirectory { get; private set; }

        public class DbEntry
        {
            public string Path;
        }
        
        public class SongEntry : DbEntry
        {
            public ChartMetadata[] Charts;
            public string[] ChartMD5Hashes;
        }

        public class DbDirectory : DbEntry
        {
            public string DirectoryName;
            public DbEntry[] SubEntries;
            public bool UseCustomIcon;
        }

        public static void BuildDatabase()
        {
            string rootPath = Path.Combine(GameManager.GameDirectory, "songs");
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
            
            RootDirectory = CreateDbDirectory(rootPath);
        }

        public static DbEntry GetEntry(ref Stack<int> hierarchyPosition)
        {
            DbEntry result = RootDirectory;
            
            foreach (int i in hierarchyPosition)
            {
                result = ((DbDirectory)result).SubEntries[i];
            }

            return result;
        }
        
        private static DbDirectory CreateDbDirectory(string path)
        {
            DbDirectory result = new DbDirectory()
            {
                Path = path,
                DirectoryName = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1),
                UseCustomIcon = false
            };
            
            string[] subDirs = Directory.GetDirectories(path);
            result.SubEntries = new DbEntry[subDirs.Length];

            List<DbEntry> songEntries = new List<DbEntry>();
            List<DbEntry> dirEntries = new List<DbEntry>();
            
            // Loop through folders in the directory
            foreach (string dir in subDirs)
            {
                SongEntry song = new SongEntry
                {
                    Path = dir
                };
                
                List<ChartMetadata> charts = new List<ChartMetadata>();
                List<string> hashes = new List<string>();
                
                // Loop through files in that directory
                foreach (string file in Directory.GetFiles(dir))
                {
                    // Check if we're not using custom icon for that directory and if we should use one.
                    if (!result.UseCustomIcon && file == ".dirIcon") result.UseCustomIcon = true;
                    
                    // We're looking for metadata files.
                    if(!file.EndsWith(".meta")) continue;

                    try
                    {
                        byte[] rawData = File.ReadAllBytes(file);
                        string plainText = Encoding.Default.GetString(rawData);

                        ChartMetadata meta = ChartMetadata.ParseFile(plainText);

                        byte[] chartData = [];
                        
                        if (meta.ChartSpecified)
                        {
                            chartData = File.ReadAllBytes(Path.Combine(dir, meta.ChartFilename));
                        }
                        
                        hashes.Add(Utility.CalculateMD5(chartData));
                        charts.Add(meta);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                }

                // Charts were found. Add a song entry into entries list and continue onto the next directory.
                if (charts.Count > 0)
                {
                    Tuple<ChartMetadata, string>[] chartsHashes = new Tuple<ChartMetadata, string>[charts.Count];
                    for (int i = 0; i < charts.Count; i++)
                    {
                        chartsHashes[i] = new Tuple<ChartMetadata, string>(charts[i], hashes[i]);
                    }

                    chartsHashes = chartsHashes.OrderBy(x => x.Item1.Difficulty).ToArray();

                    song.Charts = new ChartMetadata[charts.Count];
                    song.ChartMD5Hashes = new string[hashes.Count];
                    for (int i = 0; i < charts.Count; i++)
                    {
                        song.Charts[i] = chartsHashes[i].Item1;
                        song.ChartMD5Hashes[i] = chartsHashes[i].Item2;
                    }
                    
                    songEntries.Add(song);
                    continue;
                }
                
                // Otherwise, build a new subdirectory and add it to the list
                dirEntries.Add(CreateDbDirectory(dir));
            }

            List<DbEntry> allEntries = new List<DbEntry>();
            allEntries.AddRange(dirEntries);
            allEntries.AddRange(songEntries);

            result.SubEntries = allEntries.ToArray();
            
            return result;
        }
    }
}