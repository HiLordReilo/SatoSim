namespace SatoSim.Core.Utils
{
    public static class TimingUtils
    {
        public const float JUDGE_FANTASTIC = 1f / 60f * 2.5f;
        public const float JUDGE_GREAT = 1f / 60f * 5.5f;
        public const float JUDGE_FINE = 1f / 60f * 10.5f;
        public const float JUDGE_MISS = 1f / 60f * 16f;

        public const int JUDGE_ID_E_MISS = 0;
        public const int JUDGE_ID_E_FINE = 1;
        public const int JUDGE_ID_E_GREAT = 2;
        public const int JUDGE_ID_FANTASTIC = 3;
        public const int JUDGE_ID_L_GREAT = 4;
        public const int JUDGE_ID_L_FINE = 5;
        public const int JUDGE_ID_L_MISS = 6;

        public static int GetJudgmentId(float timeToNote)
        {
            return timeToNote switch
            {
                <= JUDGE_MISS and > JUDGE_FINE => JUDGE_ID_E_MISS, // Early miss
                <= JUDGE_FINE and > JUDGE_GREAT => JUDGE_ID_E_FINE, // Early Fine
                <= JUDGE_GREAT and > JUDGE_FANTASTIC => JUDGE_ID_E_GREAT, // Early Great
                <= JUDGE_FANTASTIC and > -JUDGE_FANTASTIC => JUDGE_ID_FANTASTIC, // Fantastic
                <= -JUDGE_FANTASTIC and > -JUDGE_GREAT => JUDGE_ID_L_GREAT, // Late Great
                <= -JUDGE_GREAT and > -JUDGE_FINE => JUDGE_ID_L_FINE, // Late Fine
                < -JUDGE_FINE => JUDGE_ID_L_MISS, // miss
                _ => -1 // Invalid judgment
            };
        }

        public static string GetJudgmentName(int judge)
        {
            return judge switch
            {
                JUDGE_ID_E_MISS => "E. miss",
                JUDGE_ID_E_FINE => "E. Fine",
                JUDGE_ID_E_GREAT => "E. Great",
                JUDGE_ID_FANTASTIC => "Fantastic",
                JUDGE_ID_L_GREAT => "E. Great",
                JUDGE_ID_L_FINE => "E. Fine",
                JUDGE_ID_L_MISS => "miss",
                _ => "INVALID_JUDGEMENT"
            };
        }
    }
}