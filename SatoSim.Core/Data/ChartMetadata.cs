using System;
using System.Collections.Generic;
using System.Linq;
using static System.String;

namespace SatoSim.Core.Data
{
    public class ChartMetadata
    {
        public enum DifficultyTier
        {
            Invalid,
            Light,
            Medium,
            Beast,
            Nightmare,
            SilentNight,
            Other
        }
        
        public string Title = "Untitled";
        public string SubTitle = Empty;
        public string Artist = "Unknown";
        public string SubArtist = Empty;
        public string Genre = "Unspecified";
        public DifficultyTier Difficulty = DifficultyTier.Other;
        public int DifficultyRating = 1;
        public float LowestBpm = 0f;
        public float HighestBpm = 0f;
        public string JacketFilename = Empty;
        public string JacketCredit = "-";
        public string MovieFilename = Empty;
        public string MovieCredit = "-";
        public string ChartFilename = Empty;
        public string ChartCredit = "-";
        public string SongFilename = Empty;
        public string PreviewFilename = Empty;
        public string BackgroundFilename = Empty;
        public string BackgroundCredit = "-";
        public string ResultsBackgroundFilename = Empty;
        public string ResultsBackgroundCredit = "-";
        
        public string RatingString =>
            Difficulty is DifficultyTier.Other or DifficultyTier.Invalid ? DifficultyRating.ToString()
            : Difficulty == DifficultyTier.SilentNight ? $"\u2605{DifficultyRating}" // Star rating
            : int.Clamp(DifficultyRating, 0, 16) switch
            {
                9 => "09-",
                10 => "09",
                11 => "09+",
                12 => "10-",
                13 => "10",
                14 => "10+",
                15 => "\u795E", // Kami
                16 => "\u9ED2\u795E", // Kurokami
                _ => '0' + DifficultyRating.ToString()
            };

        public string BpmString =>
            LowestBpm != 0f || HighestBpm != 0f
                ? LowestBpm == HighestBpm
                    ? float.Round(float.Max(LowestBpm, HighestBpm), 2).ToString()
                    : $"{float.Round(LowestBpm, 2)} ~ {float.Round(HighestBpm, 2)}"
                : "???";

        public bool JacketSpecified => !IsNullOrEmpty(JacketFilename);
        public bool MovieSpecified => !IsNullOrEmpty(MovieFilename);
        public bool ChartSpecified => !IsNullOrEmpty(ChartFilename);
        public bool SongSpecified => !IsNullOrEmpty(SongFilename);
        public bool SongPreviewSpecified => !IsNullOrEmpty(PreviewFilename);
        public bool BackgroundSpecified => !IsNullOrEmpty(BackgroundFilename);
        public bool ResultsBackgroundSpecified => !IsNullOrEmpty(ResultsBackgroundFilename);
        

        private enum ParserTokens
        {
            Invalid,
            Title,
            SubTitle,
            Artist,
            SubArtist,
            Genre,
            DifficultyTier,
            DifficultyRating,
            Bpm,
            LowestBpm,
            HighestBpm,
            JacketFilename,
            JacketCredit,
            MovieFilename,
            MovieCredit,
            ChartFilename,
            ChartCredit,
            SongFilename,
            SongPreviewFilename,
            BackgroundFilename,
            BackgroundCredit,
            ResultsBackgroundFilename,
            ResultsBackgroundCredit,
            VisualsCredit,
        }
        
        private static readonly Dictionary<ParserTokens, string[]> MetaTokens = new()
        {
            { ParserTokens.Title, ["Title"] },
            { ParserTokens.SubTitle, ["SubTitle", "TitleSub"] },
            { ParserTokens.Artist, ["Artist"] },
            { ParserTokens.SubArtist, ["SubArtist", "ArtistSub"] },
            { ParserTokens.Genre, ["Genre"] },
            { ParserTokens.DifficultyTier, ["Tier", "DifficultyTier", "DiffTier"] },
            { ParserTokens.DifficultyRating, ["Rate", "DifficultyRate", "DiffRate", "Rating", "DifficultyRating", "DiffRating"] },
            { ParserTokens.Bpm, ["BPM", "Tempo"] },
            { ParserTokens.LowestBpm, ["LowBPM", "BPMLow", "LowestBPM", "BPMLowest", "LoBPM", "BPMLo", "MinBPM", "BPMMin"] },
            { ParserTokens.HighestBpm, ["HighBPM", "BPMHigh", "HighestBPM", "BPMHighest", "HiBPM", "BPMHi", "MaxBPM", "BPMMax"] },
            { ParserTokens.JacketFilename, ["Jacket", "JK", "JacketFile", "JKFile", "JacketFilename", "JKFilename"] },
            { ParserTokens.JacketCredit, ["Illustrator", "JacketCR", "JacketCredit", "JacketArtist", "JKCR", "JKCredit", "JKArtist"] },
            { ParserTokens.MovieFilename, ["Movie", "MV", "BGA", "MovieFile", "MVFile", "BGAFile", "MovieFilename", "MVFilename", "BGAFilename"] },
            { ParserTokens.MovieCredit, ["MovieCR", "MovieCredit", "MovieArtist", "MVCR", "MVCredit", "BGAArtist", "BGACR", "BGACredit", "BGAArtist"] },
            { ParserTokens.ChartFilename, ["Chart", "ChartFile", "ChartFilename"] },
            { ParserTokens.ChartCredit, ["Charter", "ChartCR", "ChartCredit", "ChartAuthor", "ChartCreator", "ChartMaker"] },
            { ParserTokens.SongFilename, ["Audio", "AudioFile", "AudioFilename", "Song", "SongFile", "SongFilename"] },
            { ParserTokens.SongPreviewFilename, ["Preview", "PreviewFile", "PreviewFilename"] },
            { ParserTokens.BackgroundFilename, ["Background", "BG", "BackgroundFile", "BGFile", "BackgroundFilename", "BGFilename"] },
            { ParserTokens.BackgroundCredit, ["BackgroundIllustrator", "BGIllustrator", "BackgroundCR", "BackgroundCredit", "BackgroundArtist", "BGCR", "BGCredit", "BGArtist"] },
            { ParserTokens.ResultsBackgroundFilename, ["ResultsBackground", "ResultsBG", "ResultsBackgroundFile", "ResultsBGFile", "ResultsBackgroundFilename", "ResultsBGFilename"] },
            { ParserTokens.ResultsBackgroundCredit, ["ResultsBackgroundIllustrator", "ResultsBGIllustrator", "ResultsBackgroundCR", "ResultsBackgroundCredit", "ResultsBackgroundArtist", "ResultsBGCR", "ResultsBGCredit", "ResultsBGArtist"] },
            { ParserTokens.VisualsCredit, ["Visual", "Visuals", "VisualCR", "VisualsCR", "VisualArtist", "VisualsArtist", "VisualCredit", "VisualsCredit"] },
        };
        
        private static readonly Dictionary<DifficultyTier, string[]> MetaTierTokens = new()
        {
            { DifficultyTier.Light, ["LIGHT", "L", "LT", "LITE", "EASY", "E", "EZ", "BASIC", "1"] },
            { DifficultyTier.Medium, ["MEDIUM", "M", "MD", "MED", "NORMAL", "N", "NM", "2"] },
            { DifficultyTier.Beast, ["BEAST", "B", "BS", "BT", "BST", "HARD", "H", "HD", "3"] },
            { DifficultyTier.Nightmare, ["NIGHTMARE", "N", "NM", "NT", "NIGHT", "SPECIAL", "S", "SP", "4"] },
            { DifficultyTier.SilentNight, ["SILENTNIGHT", "SN", "SNIGHT", "SILNIGHT", "INSANE", "I", "IN"] },
            { DifficultyTier.Other, ["OTHER", "O", "OT", "EXTRA", "EX", "APPEND", "APP", "0", "-1"] },
        };
        


        public static ChartMetadata ParseFile(string data)
        {
            string[] dataLF = data.Split('\n');
            string[] dataCRLF = data.Split("\r\n");
            
            string[] lines = dataLF.Length > dataCRLF.Length ? dataLF : dataCRLF;
            
            ChartMetadata result = new ChartMetadata();

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (IsNullOrEmpty(line)) continue;
                if (line.StartsWith(';') || line.StartsWith("//")) continue;

                if (line.IndexOf('=') == -1)
                    throw new Exception("Error parsing metadata due to missing token terminator (=) on line " + i +
                                        " (zero-based).");
                
                if (line.IndexOf('=') == line.Length - 1) continue;
                
                string token = line[..line.IndexOf('=')].ToUpperInvariant();
                
                string value = line[(line.IndexOf('=') + 1)..];

                switch (GetTokenType(token))
                {
                    case ParserTokens.Invalid:
                        throw new Exception("Error parsing metadata due to malformed token on line " + i +
                                            " (zero-based).");
                    case ParserTokens.Title:
                        result.Title = value;
                        break;
                    case ParserTokens.SubTitle:
                        result.SubTitle = value;
                        break;
                    case ParserTokens.Artist:
                        result.Artist = value;
                        break;
                    case ParserTokens.SubArtist:
                        result.SubArtist = value;
                        break;
                    case ParserTokens.Genre:
                        result.Genre = value;
                        break;
                    case ParserTokens.DifficultyTier:
                        result.Difficulty = GetDifficultyTokenType(value);
                        
                        if (result.Difficulty == DifficultyTier.Invalid)
                            throw new Exception("Error parsing metadata due to invalid difficulty tier specified");
                        break;
                    case ParserTokens.DifficultyRating:
                        if (!int.TryParse(value, out result.DifficultyRating))
                            throw new Exception("Error parsing metadata due to invalid difficulty rating specified");
                        break;
                    case ParserTokens.Bpm:
                        if (!float.TryParse(value, out var bpm))
                            throw new Exception("Error parsing metadata due to invalid BPM value specified");
                        
                        result.LowestBpm = bpm;
                        result.HighestBpm = bpm;
                        break;
                    case ParserTokens.LowestBpm:
                        if (!float.TryParse(value, out var loBpm))
                            throw new Exception("Error parsing metadata due to invalid lowest BPM value specified");
                        
                        result.LowestBpm = loBpm;
                        break;
                    case ParserTokens.HighestBpm:
                        if (!float.TryParse(value, out var hiBpm))
                            throw new Exception("Error parsing metadata due to invalid highest BPM value specified");
                        
                        result.HighestBpm = hiBpm;
                        break;
                    case ParserTokens.JacketFilename:
                        result.JacketFilename = value;
                        break;
                    case ParserTokens.JacketCredit:
                        result.JacketCredit = IsNullOrEmpty(value) ? "-" : value;
                        break;
                    case ParserTokens.MovieFilename:
                        result.MovieFilename = value;
                        break;
                    case ParserTokens.MovieCredit:
                        result.MovieCredit = IsNullOrEmpty(value) ? "-" : value;
                        break;
                    case ParserTokens.ChartFilename:
                        result.ChartFilename = value;
                        break;
                    case ParserTokens.ChartCredit:
                        result.ChartCredit = IsNullOrEmpty(value) ? "-" : value;
                        break;
                    case ParserTokens.SongFilename:
                        result.SongFilename = value;
                        break;
                    case ParserTokens.SongPreviewFilename:
                        result.PreviewFilename = value;
                        break;
                    case ParserTokens.BackgroundFilename:
                        result.BackgroundFilename = value;
                        break;
                    case ParserTokens.BackgroundCredit:
                        result.BackgroundCredit = IsNullOrEmpty(value) ? "-" : value;;
                        break;
                    case ParserTokens.ResultsBackgroundFilename:
                        result.ResultsBackgroundFilename = value;
                        break;
                    case ParserTokens.ResultsBackgroundCredit:
                        result.ResultsBackgroundCredit = IsNullOrEmpty(value) ? "-" : value;;
                        break;
                    case ParserTokens.VisualsCredit:
                        if(result.JacketCredit == "-") result.JacketCredit = IsNullOrEmpty(value) ? "-" : value;
                        if(result.MovieCredit == "-") result.MovieCredit = IsNullOrEmpty(value) ? "-" : value;
                        if(result.BackgroundCredit == "-") result.BackgroundCredit = IsNullOrEmpty(value) ? "-" : value;;
                        if(result.ResultsBackgroundCredit == "-") result.ResultsBackgroundCredit = IsNullOrEmpty(value) ? "-" : value;;
                        break;
                    default:
                        continue;
                }
            }

            return result;
        }

        private static ParserTokens GetTokenType(string tokenValue) => (from token in MetaTokens
            from variation in token.Value
            where tokenValue.ToUpperInvariant().Replace("_", "") == variation.ToUpperInvariant()
            select token.Key).FirstOrDefault();
        
        private static DifficultyTier GetDifficultyTokenType(string tokenValue) => (from token in MetaTierTokens
            from variation in token.Value
            where tokenValue.ToUpperInvariant().Replace("_", "") == variation.ToUpperInvariant()
            select token.Key).FirstOrDefault();
    }
}