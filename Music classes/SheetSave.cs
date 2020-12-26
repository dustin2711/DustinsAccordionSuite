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
        public List<BeatTime> BeatTimes = new List<BeatTime>();

        public double BeatDuration;
        /// <summary>
        ///   Happens after start of first played note
        /// </summary>
        public double FirstBeatTime;
        /// <summary>
        ///   Happens after end of last played bote
        /// </summary>
        public double LastBeatTime;

        protected SheetSave() { }

        public SheetSave(string name, double originStartTime, List<Tone> tones, List<BeatTime> beatTimes)
        {
            this.Name = name;
            this.OriginStartTime = originStartTime;
            this.Tones = tones;
            this.BeatTimes = beatTimes;
            this.BeatDuration = CalcBeatDuration(out FirstBeatTime, out LastBeatTime);
        }

        public double CalcBeatDuration(out double firstBeatTime, out double lastBeatTime)
        {
            if (BeatTimes.Count == 0)
            {
                firstBeatTime = 0;
                lastBeatTime = 0;
                return 1;
            }
            else
            {
                // Get only MainBeats + sorted
                List<BeatTime> times = BeatTimes.Where(t => t.mainBeat).OrderBy(t => t.time).ToList();

                // Calc distances between each two
                List<double> beatDurations = new List<double>();
                for (int i = 0; i < times.Count - 1; i++)
                {
                    beatDurations.Add(times[i + 1].time - times[i].time);
                }

                double meanBeatDuration = beatDurations.Mean();

                if (beatDurations.StandardDeviation() > 0.05 * meanBeatDuration)
                {
                    throw new Exception("Standard deviation is too high");
                }

                double firstBeatTimeAnchor = BeatTimes[0].time;

                double CalcStandardDeviation(double offset)
                {
                    double firstBeatTimeWithOffset = firstBeatTimeAnchor + offset;

                    // Generates test times
                    List<double> timesToTest = new List<double>();
                    for (int i = 0; i < times.Count; i++)
                    {
                        timesToTest.Add(firstBeatTimeWithOffset + i * meanBeatDuration);
                    }

                    // Calc standard deviation for each value
                    List<double> deviations = new List<double>();
                    for (int i = 0; i < times.Count; i++)
                    {
                        double deviation = timesToTest[i] - times[i].time;
                        deviation = Math.Pow(deviation, 2);
                        deviations.Add(deviation);
                    }

                    return deviations.Mean();
                }

                // Improve firstBeatTime by trying small offset shifts 
                double bestOffset = double.MaxValue;
                double bestDeviation = double.MaxValue;
                for (double offset = -0.5; offset <= 0.5; offset += 0.001)
                {
                    double deviation = CalcStandardDeviation(offset);
                    if (deviation < bestDeviation)
                    {
                        bestOffset = offset;
                        bestDeviation = deviation;
                    }
                }

                // Apply best offset
                firstBeatTime = firstBeatTimeAnchor + bestOffset;
                while (firstBeatTime - meanBeatDuration > OriginStartTime)
                {
                    firstBeatTime -= meanBeatDuration;
                }

                // Calc last BeatTime
                lastBeatTime = 0;
                double latestNoteEndTime = Tones.OrderBy(t => t.EndTime).Last().EndTime;
                while (lastBeatTime < latestNoteEndTime)
                {
                    lastBeatTime += meanBeatDuration;
                }

                return meanBeatDuration;
            }
        }

        public override string ToString()
        {
            return $"{Name}: Beat = {BeatDuration}s with Start at {OriginStartTime}s";
        }
    }

    [Serializable]
    public struct BeatTime
    {
        public bool mainBeat;
        public double time;

        public BeatTime(bool mainBeat, double time)
        {
            this.mainBeat = mainBeat;
            this.time = time;
        }

        public override string ToString()
        {
            return $"{time.ToShortString()} {(mainBeat ? "(main)" : "")}";
        }
    }
}
