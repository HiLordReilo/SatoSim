using static SatoSim.Core.Utils.TimingUtils;

namespace SatoSim.Core.Data
{
    public class PlayerState(int totalNotes, int totalStreams)
    {
        public float Score = 0;
        public float Gauge = 30f;
        public int Combo = 0;
        public int BestCombo = 0;
        public int[] Judgments = new int[7];
        public int LongBreaks = 0;
        public int StreamBreaks = 0;
        public int TappedSlashes = 0;
        public int HeldSlashes = 0;
        public float StreamsHit = 0;

        public float TotalFantastics => Judgments[JUDGE_ID_FANTASTIC] + float.Round(StreamsHit, 5);
        public int TotalGreats => Judgments[JUDGE_ID_E_GREAT] + Judgments[JUDGE_ID_L_GREAT];
        public int TotalFines => Judgments[JUDGE_ID_E_FINE] + Judgments[JUDGE_ID_L_FINE] + TappedSlashes;
        public int TotalMisses => Judgments[JUDGE_ID_E_MISS] + Judgments[JUDGE_ID_L_MISS] + LongBreaks + StreamBreaks + HeldSlashes;

        private readonly float _gaugeScaleFactor = totalNotes + totalStreams <= 100
            ? (totalNotes + totalStreams) / 3f
            : 100 + (totalNotes + totalStreams - 100) / 3f;
        
        

        public void UpdateScore()
        {
            float points = TotalFantastics * 10
                           + TotalGreats * 8
                           + TotalFines * 5;

            int maxPoints = (totalNotes + totalStreams) * 10;
            
            Score = points / maxPoints * 1000000f;
        }

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
                _ => 6
            };
        }

        public void ProcessJudgment(int judge)
        {
            Judgments[judge]++;

            if (judge is JUDGE_ID_L_MISS or JUDGE_ID_E_MISS)
            {
                Combo = 0;
                
                DrainGauge();
            }
            else
            {
                Combo++;
                if (Combo > BestCombo) BestCombo = Combo;
                
                AddGauge(judge switch
                {
                    JUDGE_ID_FANTASTIC => 1f,
                    JUDGE_ID_E_GREAT or JUDGE_ID_L_GREAT => 0.7f,
                    JUDGE_ID_E_FINE or JUDGE_ID_L_FINE => 0.45f,
                    _ => 1f
                });
            }
            
            UpdateScore();
        }

        public void ProcessStreamPoint(float pointWeight)
        {
            Combo++;
            if (Combo > BestCombo) BestCombo = Combo;

            StreamsHit += pointWeight;
            
            AddGauge(pointWeight);
            UpdateScore();
        }

        public void AddLongBreak()
        {
            Combo = 0;
            LongBreaks++;
            DrainGauge();
        }

        public void AddStreamBreak()
        {
            Combo = 0;
            StreamBreaks++;
            DrainGauge();
        }
        
        public void AddTappedSlash()
        {
            TappedSlashes++;
            Combo++;
            UpdateScore();
        }
        
        public void AddHeldSlash()
        {
            HeldSlashes++;
            Combo = 0;
            DrainGauge();
        }

        public void AddMissedStream()
        {
            Combo = 0;
            DrainGauge();
        }
        
        public void AddGauge(float mult)
        {
            Gauge += (mult * 100f / _gaugeScaleFactor);
            if (Gauge > 100f) Gauge = 100f;
        }

        public void DrainGauge()
        {
            Gauge -= 3f;
            if (Gauge < 0f) Gauge = 0f;
        }
    }
}