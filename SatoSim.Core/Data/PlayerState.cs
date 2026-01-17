using System.Collections.Generic;
using static SatoSim.Core.Utils.TimingUtils;

namespace SatoSim.Core.Data
{
    public class PlayerState(int totalNotes, int totalStreams, PlayerState.GaugeType gauge = PlayerState.GaugeType.Normal)
    {
        public enum GaugeType
        {
            Normal,
            Crisis,
            Hard
        }
        
        public PlayRecord RecordData { get; } = new PlayRecord();
        
        public float Score { get => RecordData.Score; private set => RecordData.Score = value; }
        public int Combo {
            get => _currentCombo;
            private set
            {
                _currentCombo = value;
                if (_currentCombo > RecordData.BestCombo) RecordData.BestCombo = _currentCombo;
            }
        }
        private int _currentCombo = 0;
        
        public readonly GaugeType ActiveGauge = gauge;
        public float Gauge {
            get => _currentGauge;
            private set
            {
                _currentGauge = value;
                if (_currentGauge > RecordData.BestGauge) RecordData.BestGauge = _currentGauge;
            }
        }
        private float _currentGauge = gauge == GaugeType.Normal ? 30f : 100f;
        private readonly float _gaugeScaleFactor = gauge == GaugeType.Normal && totalNotes + totalStreams <= 100
            ? (totalNotes + totalStreams) / 3f
            : 100 + (totalNotes + totalStreams - 100) / 3f;

        public int[] Judgments => RecordData.Judgments;
        
        public List<float> TimingDeviations = new List<float>();
        public int LongBreaks = 0;
        public int StreamBreaks = 0;
        public int TappedSlashes = 0;
        public int HeldSlashes = 0;
        public float StreamsHit = 0;
        public int CompletedStreams = 0;

        public int TotalFantastics => Judgments[JUDGE_ID_FANTASTIC];
        public int TotalGreats => Judgments[JUDGE_ID_E_GREAT] + Judgments[JUDGE_ID_L_GREAT];
        public int TotalFines => Judgments[JUDGE_ID_E_FINE] + Judgments[JUDGE_ID_L_FINE];
        public int TotalMisses => Judgments[JUDGE_ID_E_MISS] + Judgments[JUDGE_ID_L_MISS];
        public int TotalObjectsHit => TotalFantastics + TotalGreats + TotalFines + TotalMisses;
        

        public float AverageTiming
        {
            get
            {
                float result = 0f;
                
                foreach (float dev in TimingDeviations) result += dev;

                result /= TimingDeviations.Count;
                
                return result;
            }
        }
        
        

        public void UpdateScore()
        {
            float points = TotalFantastics * 10
                           + (StreamsHit - CompletedStreams) * 10
                           + TotalGreats * 8
                           + TotalFines * 5;

            int maxPoints = (totalNotes + totalStreams) * 10;
            
            Score = points / maxPoints * 1000000f;
        }

        public void UpdateMedal()
        {
            // Check if all objects were processed
            if (TotalObjectsHit == totalNotes + totalStreams) // If so, we calculate the medal
            {
                if (TotalMisses == 0)
                    RecordData.Medal = TotalGreats + TotalFines > 0
                        ? PlayRecord.PlayMedal.FullCombo
                        : PlayRecord.PlayMedal.Perfect;
                else // TODO: Implement other gauges and corresponding clear medals
                    RecordData.Medal = Gauge >= 70f 
                        ? PlayRecord.PlayMedal.Clear
                        : PlayRecord.PlayMedal.Failed;
            }
            else // Otherwise - no point in calculations, the player did not finish playing the song, give 'em Failed
                RecordData.Medal = PlayRecord.PlayMedal.Failed; 
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

        public void AddTimingDeviation(float deviation) => TimingDeviations.Add(deviation);

        public void ProcessStreamPoint(float pointWeight)
        {
            Combo++;

            StreamsHit += pointWeight;
            
            AddGauge(pointWeight);
            UpdateScore();
            UpdateMedal();
        }

        public void AddLongBreak()
        {
            Combo = 0;
            LongBreaks++;
            Judgments[JUDGE_ID_E_MISS]++;
            DrainGauge();
            UpdateMedal();
        }

        public void AddStreamBreak()
        {
            Combo = 0;
            StreamBreaks++;
            Judgments[JUDGE_ID_L_MISS]++;
            DrainGauge();
            UpdateMedal();
        }
        
        public void AddCompletedStream()
        {
            CompletedStreams++;
            Judgments[JUDGE_ID_FANTASTIC]++;
            UpdateScore();
            UpdateMedal();
        }
        
        public void AddTappedSlash()
        {
            TappedSlashes++;
            Combo++;
            Judgments[JUDGE_ID_E_FINE]++;
            UpdateScore();
            UpdateMedal();
        }
        
        public void AddHeldSlash()
        {
            HeldSlashes++;
            Combo = 0;
            Judgments[JUDGE_ID_L_MISS]++;
            DrainGauge();
            UpdateMedal();
        }

        public void AddMissedStream()
        {
            Combo = 0;
            Judgments[JUDGE_ID_L_MISS]++;
            DrainGauge();
            UpdateMedal();
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