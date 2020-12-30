using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CreateSheetsFromVideo
{

    /// <summary>
    ///   A music beat containing voices that contain the tones.
    /// </summary>
    [Serializable]
    public class Beat
    {
        public int Number;
        public List<BeatPart> BeatParts = new List<BeatPart>();

        public List<BeatNote> AllNotes => BeatParts.SelectMany(beatPart => beatPart.Notes).OrderBy(it => it.BeatPart.VoiceId).ThenBy(it => it.Start).ToList();

        public BeatValues Values;

        public double StartTime => Values.FirstBeatStartTime + Number * Duration;
        public double EndTime => StartTime + Duration;
        public double Duration => Values.Duration;

        protected Beat() { }

        public Beat(int number, List<BeatNote> beatTones, BeatValues values)
        {
            Number = number;
            Values = values;

            // Iterate tones and group into voices
            foreach (BeatNote note in beatTones.OrderBy(it => it.Start))
            {
                if (note is TiedBeatNote tiedNote && tiedNote.NoteBefore != null)
                {
                    /// Create voice with same number as last beat
                    BeatPart beatPart = new BeatPart(tiedNote.NoteBefore.BeatPart.VoiceId);
                    BeatParts.Add(beatPart);
                    beatPart.Notes.Add(tiedNote);
                    note.BeatPart = beatPart;
                }
                else
                {
                    // Voice with room for tone?
                    if (BeatParts.First(voice => note.Start >= voice.Notes.Last().End, out BeatPart fittingBeatPart))
                    {
                        // Yes: Add to existing voice
                        fittingBeatPart.Notes.Add(note);
                        note.BeatPart = fittingBeatPart;
                    }
                    else
                    {
                        // No: Create new voice
                        for (int id = 1; true; id++)
                        {
                            if (BeatParts.None(voice => voice.VoiceId == id))
                            {
                                BeatPart voice = new BeatPart(id);
                                voice.Notes.Add(note);
                                note.BeatPart = voice;
                                BeatParts.Add(voice);
                                break;
                            }
                        }
                    }
                }

                if (note.BeatPart == null)
                {
                    Debugger.Break();
                }
            } // foreach tone

            const double minimum = 1.0 / 32;

            double GetRoundedTime(double proportion)
            {
                double roundedProportion = minimum * Math.Round(proportion / minimum);
                double roundedTime = (roundedProportion * Duration) + StartTime;
                return roundedTime;
            }

            // Make voice fit to 1/64 and fill with pauses
            foreach (BeatPart beatPart in BeatParts)
            {
                for (int i = 0; i < beatPart.Notes.Count - 1; i++)
                {
                    BeatNote note = beatPart.Notes[i];
                    BeatNote nextNote = beatPart.Notes[i + 1];
                    double start = note.Start / Duration;
                    double end = note.End / Duration;
                    double startNext = nextNote.Start / Duration;
                    double endNext = nextNote.End / Duration;

                    if (i == 0)
                    {
                        // First tone
                        if (start <= minimum)
                        {
                            // May stretch first toneStart to start of beat
                            note.Start = 0;
                        }
                        else
                        {
                            /// Insert rest before first tone
                            double newStartProportion = minimum * Math.Round(start / minimum);
                            double newStart = (newStartProportion * Duration) + StartTime;
                            // Set new startTime
                            note.Start = newStart;
                            // Add rest
                            beatPart.Notes.Insert(0, new BeatRest(beatPart, 0, newStart));
                        }
                    }
                    else if (i == beatPart.Notes.Count - 1)
                    {
                        // Last tone
                        if (Duration - endNext <= minimum)
                        {
                            // May stretch last toneEnd to end of beat
                            nextNote.End = 1;
                        }
                    }
                    else
                    {
                        // Tone inbetween
                        note.Start = GetRoundedTime(start);
                        note.End = GetRoundedTime(end);
                    }
                }

                // Check voice
                var x = beatPart;
            }
        }

        public override string ToString()
        {
            return $"{StartTime.ToString(3)}-{EndTime.ToString(3)}: {BeatParts.Count} Voices with total {AllNotes.Count} Tones";
        }
    }


    [Serializable]
    public class BeatValues
    {
        public double FirstBeatStartTime;
        public double LastBeatStartTime;
        public double Duration;

        public double LastBeatEndTime => LastBeatStartTime + Duration;

        protected BeatValues() { }

        public BeatValues(List<BeatHit> beatHits, List<Tone> tones, double OriginStartTime)
        {
            Debug.Assert(beatHits.Count > 0);

            // Get only MainBeats + sorted
            List<BeatHit> times = beatHits.Where(t => t.isMainBeat).OrderBy(t => t.startTime).ToList();

            // Calc distances between each two
            List<double> beatDurations = new List<double>();
            for (int i = 0; i < times.Count - 1; i++)
            {
                beatDurations.Add(times[i + 1].startTime - times[i].startTime);
            }

            double meanBeatDuration = beatDurations.Mean();

            if (beatDurations.StandardDeviation() > 0.05 * meanBeatDuration)
            {
                throw new Exception("Standard deviation is too high");
            }

            double firstBeatTimeAnchor = beatHits[0].startTime;

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
                    double deviation = timesToTest[i] - times[i].startTime;
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
            FirstBeatStartTime = firstBeatTimeAnchor + bestOffset;
            while (FirstBeatStartTime - meanBeatDuration > OriginStartTime)
            {
                FirstBeatStartTime -= meanBeatDuration;
            }

            // Calc last BeatTime
            LastBeatStartTime = 0;
            double latestNoteEndTime = tones.OrderBy(t => t.EndTime).Last().EndTime;
            while (LastBeatStartTime < latestNoteEndTime)
            {
                LastBeatStartTime += meanBeatDuration;
            }

            foreach (Tone tone in tones)
            {
                tone.BeatDuration = meanBeatDuration;
            }

            Duration = meanBeatDuration;
        }

        public void ApplyOffset(double beatOffsetProportion)
        {
            FirstBeatStartTime += beatOffsetProportion * Duration;
            LastBeatStartTime += beatOffsetProportion * Duration;
        }

        public List<double> GetBeatStartTimes(double beatOffsetProportion)
        {
            List<double> times = new List<double>();
            for (double time = FirstBeatStartTime; time <= LastBeatStartTime; time += Duration)
            {
                times.Add(time + beatOffsetProportion * Duration);
            }
            return times;
        }

        public override string ToString()
        {
            return $"Beats with {Duration}s, first {FirstBeatStartTime} to last {LastBeatStartTime}";
        }
    }

    /// <summary>
    ///   A voice in a single beat
    /// </summary>
    [Serializable]
    public class BeatPart
    {
        public List<BeatNote> Notes = new List<BeatNote>();
        public int VoiceId;

        protected BeatPart() { }

        public BeatPart(int id)
        {
            Notes = new List<BeatNote>();
            VoiceId = id;
        }

        public override string ToString()
        {
            return $"Voice {VoiceId}: {Notes.Count} Tones: {string.Join(", ", Notes.Select(it => it.Start.ToString(3) + "-" + it.End.ToString(3)))}";
        }
    }
}
