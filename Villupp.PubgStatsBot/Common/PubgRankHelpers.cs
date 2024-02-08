namespace Villupp.PubgStatsBot.Common
{
    internal class PubgRankHelpers
    {
        public static string GetSubTierRomanNumeral(string subTier)
        {
            return subTier switch
            {
                "1" => "I",
                "2" => "II",
                "3" => "III",
                "4" => "IV",
                "5" => "V",
                _ => "",
            };
        }
    }
}
