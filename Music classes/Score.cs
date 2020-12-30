using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CreateSheetsFromVideo
{
    public class Score
    {
        public List<Note> AllNotes => Beats.SelectMany(beat => beat.Notes).OrderBy(note => note.StartTime).ToList();
        public List<Beat> Beats = new List<Beat>();
        public BeatValues BeatValues;

        public Score(SheetSave save)
        {
            BeatValues = save.BeatValues;

            GetLeftAndRightHandTones(save.Tones, out List<Tone> leftTonesUnmerged, out List<Tone> rightTonesUnmerged);

            Beats = CreateBeats(this, rightTonesUnmerged, BeatValues);
            CreateParts();

            foreach (Beat beat in Beats)
            {
                beat.OptimizeNotePortions();
            }
        }

        private static List<Beat> CreateBeats(Score score, List<Tone> tones, BeatValues beatValues)
        {
            List<Beat> beats = new List<Beat>();

            List<double> beatTimes = beatValues.GetBeatStartTimes();

            /// Convert Tones to Notes
            foreach (Tone tone in tones)
            {
                // Get fitting beatNumber..
                int beatNumber = beatTimes.FindIndex(it => it < tone.StartTime && tone.StartTime < it + beatValues.Duration); // can be -1 due to beatOffsetProportion!
                // ..and try get beat..
                Beat beat = beats.FirstOrDefault(it => it.Number == beatNumber);
                if (beat == null)
                {
                    //.. or create
                    beat = new Beat(score, beatNumber);
                    beats.Add(beat);
                }

                Note note = new Note(beat, tone);
                beat.Notes.Add(note);
            }

            /// Merge chords
            MergeChords(beats);

            /// Handle overhanging notes
            for (int i = 0; i < beats.Count - 1; i++)
            {
                Beat currentBeat = beats[i];
                Beat nextBeat = beats[i + 1];

                foreach (Note note in currentBeat.Notes.Where(note 
                    => note.EndPortion > 1))
                {
                    Note overhangNote = new Note(note)
                    {
                        Beat = nextBeat,
                        StartPortion = 0,
                        Portion = note.EndPortion - 1,
                        Tiing = new Tiing(noteBefore: note)
                    };
                    nextBeat.Notes.Add(overhangNote);

                    note.EndPortion = 1;
                    note.Tiing = new Tiing(noteAfter: overhangNote);
                }
            }

            return beats;
        }

        public void CreateParts()
        {
            foreach (Beat beat in Beats)
            {
                // Iterate tones and group into voices
                foreach (Note note in beat.Notes.OrderBy(it => it.StartPortion))
                {
                    if (note.Tiing?.NoteBefore != null)
                    {
                        /// Create part with same VoiceId as last beat
                        Part Part = new Part(note.Tiing.NoteBefore.Part.VoiceId);
                        beat.Parts.Add(Part);
                        Part.Notes.Add(note.Tiing.NoteBefore);
                        note.Part = Part;
                    }
                    else
                    {
                        // Voice with room for tone?
                        if (beat.Parts.First(voice => note.StartPortion >= voice.Notes.Last().EndPortion, out Part fittingPart))
                        {
                            // Yes: Add to existing voice
                            fittingPart.Notes.Add(note);
                            note.Part = fittingPart;
                        }
                        else
                        {
                            // No: Create new voice
                            for (int id = 1; true; id++)
                            {
                                if (beat.Parts.None(voice => voice.VoiceId == id))
                                {
                                    Part voice = new Part(id);
                                    voice.Notes.Add(note);
                                    note.Part = voice;
                                    beat.Parts.Add(voice);
                                    break;
                                }
                            }
                        }
                    }

                    if (note.Part == null)
                    {
                        Debugger.Break();
                    }
                } // foreach tone
            }
        }

        private static void GetLeftAndRightHandTones(List<Tone> tones, out List<Tone> leftHandTones, out List<Tone> rightHandTones)
        {
            List<Tone> tonesOrderedByHue = tones.OrderBy(t => t.Color.GetHue()).ToList();
            int splitIndex = -1;
            for (int i = 0; i < tonesOrderedByHue.Count - 1; i++)
            {
                float hueDifference = Math.Abs(tonesOrderedByHue[i].Color.GetHue() - tonesOrderedByHue[i + 1].Color.GetHue());
                if (hueDifference > 50)
                {
                    splitIndex = i;
                    break;
                }
            }

            List<Tone> tones1 = new List<Tone>(tonesOrderedByHue.Take(splitIndex).OrderBy(it => it.StartTime));
            List<Tone> tones2 = new List<Tone>(tonesOrderedByHue.Skip(splitIndex).OrderBy(it => it.StartTime));

            // Get mean pitch of tones, because: lower pitch = left hand
            double GetMeanPitch(List<Tone> toneList)
            {
                double meanPitch = 0;
                foreach (Tone tone in toneList)
                {
                    meanPitch += 12 * tone.Octave + (int)tone.Pitch;
                }
                meanPitch /= toneList.Count;
                return meanPitch;
            }

            bool tones1AreLeftHand = GetMeanPitch(tones1) < GetMeanPitch(tones2); // Use lower pitch = left

            // At least one hand must have tones
            Debug.Assert(tones1.Count > 0 || tones2.Count > 0);
            // If one handed, get right or left
            if (tones1.Count == 0)
            {
                tones1AreLeftHand = MainForm.IsRightHand(tones2.First());
            }
            else if (tones2.Count == 0)
            {
                tones1AreLeftHand = !MainForm.IsRightHand(tones1.First());
            }

            leftHandTones = tones1AreLeftHand ? tones1 : tones2;
            rightHandTones = tones1AreLeftHand ? tones2 : tones1;

            // Set hand enum for left..
            foreach (Tone tone in leftHandTones)
            {
                tone.Hand = Hand.Left;
            }
            // .. and right tones
            foreach (Tone tone in rightHandTones)
            {
                tone.Hand = Hand.Right;
            }
        }

        /// <summary>
        ///   Notes with ~ same start and end time will be appended to other notes and removed afterwards.
        /// </summary>
        private static void MergeChords(List<Beat> beats)
        {
            List<Note> notes = beats.SelectMany(beat => beat.Notes).OrderBy(note => note.StartTime).ToList();

            const double MaxAbsoluteTimeDelta = 0.040; // in seconds (0.040 ~ 2.5 Frames)
            const double MaxRelativeDelta = 0.2;

            List<Note> chordNotes = new List<Note>();

            for (int index = 0; index < notes.Count; index++)
            {
                Note mainNote = notes[index++];

                // Add part tones
                while (index < notes.Count)
                {
                    Note chordNote = notes[index];
                    // Same starttime, endtime & duration?
                    if (chordNote.StartTime.IsAboutAbsolute(mainNote.StartTime, MaxAbsoluteTimeDelta)
                        && chordNote.EndTime.IsAboutAbsolute(mainNote.EndTime, MaxAbsoluteTimeDelta)
                        && chordNote.Duration.IsAboutRelative(mainNote.Duration, MaxRelativeDelta))
                    {
                        mainNote.ChordToneHeights.Add(chordNote.ToneHeight);
                        chordNotes.Add(chordNote);
                        index++;
                    }
                    else
                    {
                        index--;
                        break;
                    }
                }

            }

            // Remove chordNotes
            foreach (Beat beat in beats)
            {
                foreach (Note chordNote in chordNotes.Where(chordNote
                    => beat.Notes.Contains(chordNote)))
                {
                    beat.Notes.Remove(chordNote);
                }
            }
        }
    }

    public class Beat
    {
        public Score Score;
        public int Number;
        public List<Note> Notes = new List<Note>();
        public List<Part> Parts = new List<Part>();

        public double Duration => Score.BeatValues.Duration;
        public double StartTime => Score.BeatValues.FirstBeatStartTime + Number * Duration;
        public double EndTime => Score.BeatValues.FirstBeatStartTime + (Number + 1) * Duration;

        public Beat(Score score, int number)
        {
            Score = score;
            Number = number;
        }

        /// <summary>
        ///   Optimizes the note portions for each part (e.g. 0.46 gets 0.50)
        /// </summary>
        public void OptimizeNotePortions()
        {
            // Make voice fit to 1/32 and fill with pauses
            const double minimum = 1.0 / 32;

            double GetRoundedPortion(double portion)
            {
                double roundedPortion = minimum * Math.Round(portion / minimum);
                return roundedPortion;
            }

            List<Note> removedNotes = new List<Note>();

            foreach (Part part in Parts)
            {
                //Debug
                List<Note> notesBeforeOptimizing = new List<Note>();
                foreach (Note note in part.Notes)
                {
                    notesBeforeOptimizing.Add(new Note(note));
                }
                bool needToBreak = part.Notes.Count >= 6;
                if (needToBreak)
                {
                    //Debugger.Break();
                }
                if (notesBeforeOptimizing.GetHashCode() == 1795329)
                {

                }

                // FIRST NOTE
                Note firstNote = part.Notes.First();
                // Gap to beatStart < minimum?
                if (firstNote.StartPortion <= minimum)
                {
                    // Yes: Stretch first toneStart to start of beat
                    firstNote.StartPortion = 0;
                }
                else
                {
                    // No : Insert rest and adapt noteStart
                    firstNote.StartPortion = GetRoundedPortion(firstNote.StartPortion);
                    Rest rest = new Rest(this, 0, firstNote.StartPortion);
                    part.Notes.Insert(0, rest);
                }

                // NOTES INBETWEEN
                for (int i = 0; i < part.Notes.Count - 1; i++)
                {
                    Note note = part.Notes[i];
                    Note nextNote = part.Notes[i + 1];

                    if (note is Rest || nextNote is Rest)
                    {
                        continue;
                    }

                    // Close enough?
                    double gap = nextNote.StartPortion - note.EndPortion;
                    if (gap < minimum)
                    {
                        // Yes: Equalize end and start
                        double portion = (nextNote.StartPortion + note.EndPortion) / 2;
                        double roundedPortion = GetRoundedPortion(portion);
                        note.EndPortion = roundedPortion;
                        nextNote.StartPortion = roundedPortion;
                    }
                    else
                    {
                        // No: Insert rest
                        double restStartPotion = GetRoundedPortion(note.EndPortion);
                        double restEndPotion = GetRoundedPortion(nextNote.StartPortion);
                        part.Notes.Insert(i++, new Rest(this, restStartPotion, restEndPotion));
                    }

                    note.StartPortion = GetRoundedPortion(note.StartPortion);
                    note.EndPortion = GetRoundedPortion(note.EndPortion);
                }

                // LAST NOTE
                Note lastNote = part.Notes.Last();
                // Gap to beatEnd < minimum? (Care: nextNote, not note)
                if (1 - lastNote.EndPortion <= minimum)
                {
                    // Yes: Stretch last toneEnd to end of beat
                    lastNote.EndPortion = 1;
                }
                else
                {
                    // No: Add rest and adapt noteEnd
                    lastNote.EndPortion = GetRoundedPortion(lastNote.EndPortion);
                    Rest rest = new Rest(this, 0, lastNote.EndPortion);
                    part.Notes.Add(rest);
                }

                // Remove notes with portion = 1
                foreach (Note note in part.Notes.Where(note => note.Duration == 0).ToArray())
                {
                    MainForm.LogLine("Removing " + note);
                    part.Notes.Remove(note);
                    removedNotes.Add(note);
                }

                // Sort all
                List<Note> OrderByStart(ref List<Note> notes) => notes = notes.OrderBy(note => note.StartPortion).ToList();
                OrderByStart(ref notesBeforeOptimizing);
                OrderByStart(ref removedNotes);
                OrderByStart(ref part.Notes);

                var notesBefore = notesBeforeOptimizing;
                var notesAfter = part.Notes;
                var c = removedNotes;
                // DEBUG HERE
                foreach (var note in part.Notes)
                {
                    if (note.NoteLength.HasDeviation)
                    {
                        //Debugger.Break();
                    }
                }
                if (needToBreak)
                {
                    Debugger.Break();
                }
  
            }
        }

        public override string ToString()
        {
            return $"{StartTime.ToString(3)}-{EndTime.ToString(3)}: {Parts.Count} Voices with total {Notes.Count} Tones";
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

        public BeatValues(List<BeatHit> hits, List<Tone> tones, double OriginStartTime)
        {
            Debug.Assert(hits.Count > 0);

            // Get only MainBeats + sorted
            List<BeatHit> mainHits = hits.Where(t => t.isMainBeat).OrderBy(t => t.startTime).ToList();

            // Calc distances between each two
            List<double> mainBeatDistances = new List<double>();
            for (int i = 0; i < mainHits.Count - 1; i++)
            {
                mainBeatDistances.Add(mainHits[i + 1].startTime - mainHits[i].startTime);
            }

            double meanBeatDuration = mainBeatDistances.Mean();

            if (mainBeatDistances.StandardDeviation() > 0.05 * meanBeatDuration)
            {
                throw new Exception("Standard deviation is too high");
            }

            double firstBeatHitTime = hits[0].startTime;

            double CalcStandardDeviation(double offset)
            {
                double firstBeatTimeWithOffset = firstBeatHitTime + offset;

                // Generates test times
                List<double> timesToTest = new List<double>();
                for (int i = 0; i < mainHits.Count; i++)
                {
                    timesToTest.Add(firstBeatTimeWithOffset + i * meanBeatDuration);
                }

                // Calc standard deviation for each value
                List<double> deviations = new List<double>();
                for (int i = 0; i < mainHits.Count; i++)
                {
                    double deviation = timesToTest[i] - mainHits[i].startTime;
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
            FirstBeatStartTime = firstBeatHitTime + bestOffset;
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

            Duration = meanBeatDuration;
        }

        public void ApplyOffset(double beatOffsetPortion)
        {
            FirstBeatStartTime += beatOffsetPortion * Duration;
            LastBeatStartTime += beatOffsetPortion * Duration;
        }

        public List<double> GetBeatStartTimes()
        {
            List<double> times = new List<double>();
            for (double time = FirstBeatStartTime; time <= LastBeatStartTime; time += Duration)
            {
                times.Add(time);
            }
            return times;
        }

        public override string ToString()
        {
            return $"Beats with {Duration}s, first {FirstBeatStartTime} to last {LastBeatStartTime}";
        }
    }

    /// <summary>
    ///   A part of a beat having a unique VoiceId per beat.
    /// </summary>
    [Serializable]
    public class Part
    {
        public List<Note> Notes = new List<Note>();
        public int VoiceId;

        protected Part() { }

        public Part(int id)
        {
            Notes = new List<Note>();
            VoiceId = id;
        }

        public override string ToString()
        {
            return $"Voice {VoiceId}: {Notes.Count} Tones: {string.Join(", ", Notes.Select(it => it.StartPortion.ToString(3) + "-" + it.EndPortion.ToString(3)))}";
        }
    }
}
