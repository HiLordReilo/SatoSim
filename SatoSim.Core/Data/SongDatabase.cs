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
        public static DbDirectory RootDirectory;

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
            public DbEntry[] SubEntries;
        }

        public static void BuildDatabase()
        {
            string rootPath = Path.Combine(GameManager.GameDirectory, "songs");
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
            
            RootDirectory = CreateDbDirectory(rootPath);
        }

        public static DbEntry GetEntry(ref Queue<int> hierarchyPosition)
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
                Path = path
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
                    // We're looking for metadata files.
                    if(!file.EndsWith(".meta")) continue;

                    try
                    {
                        byte[] rawData = File.ReadAllBytes(file);
                        string plainText = Encoding.Default.GetString(rawData);

                        ChartMetadata meta = ChartMetadata.ParseFile(plainText);
                        
                        charts.Add(meta);
                        hashes.Add(Utility.CalculateMD5(rawData));
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
                    song.Charts = charts.ToArray();
                    song.ChartMD5Hashes = hashes.ToArray();
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