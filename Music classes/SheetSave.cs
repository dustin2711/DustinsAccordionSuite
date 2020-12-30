using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    [Serializable]
    public class SheetSave
    {
        public string Name;
        public double OriginStartTime = 0;
        public List<Tone> Tones = new List<Tone>();
        public List<BeatHit> BeatHits = new List<BeatHit>(); // Just for calculating
        public BeatValues BeatValues;

        protected SheetSave() { }

        public SheetSave(string name, double originStartTime, List<Tone> tones, List<BeatHit> beatHits)
        {
            Name = name;
            OriginStartTime = originStartTime;
            Tones = tones;
            BeatHits = beatHits;
            BeatValues = new BeatValues(BeatHits, tones, originStartTime);
        }

        public override string ToString()
        {
            return $"{Name}: Beat = {BeatValues.Duration}s with Start at {OriginStartTime}s and {Tones.Count} Tones";
        }
    }

    /// <summary>
    ///   Struct of StartTime + bool if MainBeat
    /// </summary>
    [Serializable]
    public struct BeatHit
    {
        public bool isMainBeat;
        public double startTime;

        public BeatHit(bool mainBeat, double startTime)
        {
            this.isMainBeat = mainBeat;
            this.startTime = startTime;
        }

        public override string ToString()
        {
            return $"{Extensions.ToString(startTime)} {(isMainBeat ? "(main)" : "")}";
        }
    }
}
