namespace SatoSim.Core.Data
{
    public class PlayRecord
    {
        public enum PlayMedal
        {
            None = 0,
            Failed = 1,
            Clear = 2,
            CrisisClear = 3,
            HardClear = 4,
            FullCombo = 5,
            Perfect = 6,
        }
        
        public float Score = 0;
        public float BestGauge = 0f;
        public int BestCombo = 0;
        public int[] Judgments = new int[7];
        public PlayMedal Medal = PlayMedal.None;
        
        
        
        public static int GetGradeId(float score)
        {
            return score switch
            {
                >= 970000f => 0,
                >= 950000f => 1,
                >= 850000f => 2,
                >= 700000f => 3,
                >= 600000f => 4,
                >= 500000f => 5,
                > 0f => 6,
                _ => 7
            };
        }
        
        public int GetGradeId()
        {
            return Score switch
            {
                >= 970000f => 0,
                >= 950000f => 1,
                >= 850000f => 2,
                >= 700000f => 3,
                >= 600000f => 4,
                >= 500000f => 5,
                > 0f => 6,
                _ => 7
            };
        }

        public static PlayRecord MergeRecords(params PlayRecord[] records)
        {
            PlayRecord result = new PlayRecord();

            foreach (PlayRecord rec in records)
            {
                result.Score = float.Max(result.Score, rec.Score);
                result.BestGauge = float.Max(result.BestGauge, rec.BestGauge);
                result.BestCombo = int.Max(result.BestCombo, rec.BestCombo);

                result.Medal = (PlayMedal)int.Max((int)result.Medal, (int)rec.Medal);
                
                result.Judgments = rec.Judgments;
            }
            
            return result;
        }
    }
}