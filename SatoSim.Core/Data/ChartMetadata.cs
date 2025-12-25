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
        }
        
        private static readonly Dictionary<ParserTokens, string[]> MetaTokens = new()
        {
            { ParserTokens.Title, ["TITLE"] },
            { ParserTokens.SubTitle, ["SUBTITLE", "SUB_TITLE", "TITLESUB", "TITLE_SUB"] },
            { ParserTokens.Artist, ["ARTIST"] },
            { ParserTokens.SubArtist, ["SUBARTIST", "SUB_ARTIST", "ARTISTSUB", "ARTIST_SUB"] },
            { ParserTokens.Genre, ["GENRE"] },
            { ParserTokens.DifficultyTier, ["TIER", "DIFFICULTYTIER", "DIFFICULTY_TIER", "DIFFTIER", "DIFF_TIER"] },
            { ParserTokens.DifficultyRating, ["RATE", "DIFFICULTYRATE", "DIFFICULTY_RATE", "DIFFRATE", "DIFF_RATE", "RATING", "DIFFICULTYRATING", "DIFFICULTY_RATING", "DIFFRATING", "DIFF_RATING"] },
            { ParserTokens.Bpm, ["BPM"] },
            { ParserTokens.LowestBpm, ["LOWBPM", "LOW_BPM", "BPMLOW", "BPM_LOW", "LOWESTBPM", "LOWEST_BPM", "BPMLOWEST", "BPM_LOWEST", "LOBPM", "LO_BPM", "BPMLO", "BPM_LO"] },
            { ParserTokens.HighestBpm, ["HIGHBPM", "HIGH_BPM", "BPMHIGH", "BPM_HIGH", "HIGHESTBPM", "HIGHEST_BPM", "BPMHIGHEST", "BPM_HIGHEST", "HIBPM", "HI_BPM", "BPMHI", "BPM_HI"] },
            { ParserTokens.JacketFilename, ["JACKET", "JK", "JACKETFILE", "JACKET_FILE", "JKFILE", "JK_FILE", "JACKETFILENAME", "JACKET_FILENAME", "JKFILENAME", "JK_FILENAME"] },
            { ParserTokens.JacketCredit, ["ILLUSTRATOR", "JACKETCR", "JACKET_CR", "JACKETCREDIT", "JACKET_CREDIT", "JACKETARTIST", "JACKET_ARTIST", "JKCR", "JK_CR", "JKCREDIT", "JK_CREDIT", "JKARTIST", "JK_ARTIST"] },
            { ParserTokens.MovieFilename, ["MOVIE", "MV", "MOVIEFILE", "MOVIE_FILE", "MVFILE", "MV_FILE", "MOVIEFILENAME", "MOVIE_FILENAME", "MVFILENAME", "MV_FILENAME"] },
            { ParserTokens.MovieCredit, ["MOVIECR", "MOVIE_CR", "MOVIECREDIT", "MOVIE_CREDIT", "MOVIEARTIST", "MOVIE_ARTIST", "MVCR", "MV_CR", "MVCREDIT", "MV_CREDIT", "MVARTIST", "MV_ARTIST"] },
            { ParserTokens.ChartFilename, ["CHART", "CHARTFILE", "CHART_FILE", "CHARTFILENAME", "CHART_FILENAME"] },
            { ParserTokens.ChartCredit, ["CHARTER", "CHARTER", "CHART_CR", "CHARTCREDIT", "CHART_CREDIT", "CHARTAUTHOR", "CHART_AUTHOR", "CHARTCREATOR", "CHART_CREATOR", "CHARTMAKER", "CHART_MAKER",] },
            { ParserTokens.SongFilename, ["AUDIO", "AUDIOFILE", "AUDIO_FILE", "AUDIOFILENAME", "AUDIO_FILENAME", "SONG", "SONGFILE", "SONG_FILE", "SONGFILENAME", "SONG_FILENAME"] },
        };
        
        private static readonly Dictionary<DifficultyTier, string[]> MetaTierTokens = new()
        {
            { DifficultyTier.Light, ["LIGHT", "L", "LT", "LITE", "EASY", "E", "EZ", "BASIC", "1"] },
            { DifficultyTier.Medium, ["MEDIUM", "M", "MD", "MED", "NORMAL", "N", "NM", "2"] },
            { DifficultyTier.Beast, ["BEAST", "B", "BS", "BT", "BST", "HARD", "H", "HD", "3"] },
            { DifficultyTier.Nightmare, ["NIGHTMARE", "N", "NM", "NT", "NIGHT", "SPECIAL", "S", "SP", "4"] },
            { DifficultyTier.SilentNight, ["SILENTNIGHT", "SILENT_NIGHT", "SN", "SNIGHT", "S_NIGHT", "SILNIGHT", "SIL_NIGHT", "INSANE", "I", "IN"] },
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
                    default:
                        continue;
                }
            }

            return result;
        }

        private static ParserTokens GetTokenType(string tokenValue) => (from token in MetaTokens
            from variation in token.Value
            where tokenValue == variation
            select token.Key).FirstOrDefault();
        
        private static DifficultyTier GetDifficultyTokenType(string tokenValue) => (from token in MetaTierTokens
            from variation in token.Value
            where tokenValue == variation
            select token.Key).FirstOrDefault();
    }
}