// Use downlaoded schema classes (works for Kass' Theme better)

namespace MusicXmlBeautifier
{
    class LyricsReplacement
    {
        public string NewLyric;
        public string[] LyricsChainToReplace;
        public bool CanReplaceMultipleTimesPerMeasure;

        public LyricsReplacement(string newLyric, string[] lyricsChainToReplace, bool canReplaceMultipleTimesPerMeasure)
        {
            NewLyric = newLyric;
            LyricsChainToReplace = lyricsChainToReplace;
            CanReplaceMultipleTimesPerMeasure = canReplaceMultipleTimesPerMeasure;
        }
    }
}
