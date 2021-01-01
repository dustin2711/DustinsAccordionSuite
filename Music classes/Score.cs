using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CreateSheetsFromVideo
{
    public class SingleInstance<T> where T : SingleInstance<T>
    {
        public T Instance;

        protected SingleInstance()
        {
            Instance = this as T;
        }
    }

    public class Score : SingleInstance<Score>
    {
        public const double MinimumPortion = 1.0 / 32;
        public const double FrameLength = 1.0 / 30;
        public const double MaxFrameDeltaToAddToSameVoice = 1.5;
        /// <summary>
        ///   Tolerance for merging tones and adding tones to same voice
        /// </summary>
        public static double PortionTolerance;

        public string Name;
        public List<Beat> Beats = new List<Beat>();
        public BeatValues BeatValues;

        public List<Note> AllNotes => Beats.SelectMany(beat => beat.AllBeatNotes).OrderBy(note => note.StartTime).ToList();

        public Score(SheetSave save) : base()
        {
            Name = save.Name;
            BeatValues = new BeatValues(save.BeatHits, save.Tones, save.OriginStartTime);
            PortionTolerance = MaxFrameDeltaToAddToSameVoice * FrameLength / BeatValues.Duration;

            GetLeftAndRightHandTones(save.Tones, out List<Tone> leftTonesUnmerged, out List<Tone> rightTonesUnmerged);

            Beats = CreateBeats(this, rightTonesUnmerged, BeatValues);

            foreach (Beat beat in Beats)
            {
                beat.CreateVoices();
                beat.OptimizeNotePortions();
                beat.MergeAgain();
                //beat.SplitNotesToFitXml();
                beat.RemoveEmptysAndFinalize();
            }

            //Debugger.Break();
            var lookHere = Beats;
        }

        private static List<Beat> CreateBeats(Score score, List<Tone> tones, BeatValues beatValues)
        {
            List<Beat> beats = new List<Beat>();

            List<double> beatTimes = beatValues.GetBeatStartTimes();

            tones = tones.OrderBy(tone => tone.EndTime).ToList();
            double firstStartTime = tones.First().StartTime;
            double lastEndTime = tones.Last().EndTime;

            // Create beats
            int beatNum = 0;
            for (double beatTime = beatValues.FirstBeatStartTime; beatTime < lastEndTime; beatTime += beatValues.Duration)
            {
                beats.Add(new Beat(score, beatNum++));
            }

            /// Convert Tones to Notes
            foreach (Tone tone in tones)
            {
                // Get fitting beatNumber..
                int beatNumber = beatTimes.FindIndex(beatTime => beatTime - Helper.µ <= tone.StartTime && tone.StartTime < beatTime + beatValues.Duration); // can be -1 due to beatOffsetProportion!
                // .. and get beat
                if (!beats.FirstOrDefault(it => it.Number == beatNumber, out Beat beat))
                {
                    Debugger.Break(); // Beat missing!
                }

                Note note = new Note(beat, tone);
                beat.AllBeatNotes.Add(note);
            }

            // Merge chords
            MergeChords(beats);

            /// Handle overhanging notes
            for (int i = 0; i < beats.Count - 1; i++)
            {
                Beat currentBeat = beats[i];
                Beat nextBeat = beats[i + 1];

                foreach (Note note in currentBeat.AllBeatNotes.Where(note 
                    => note.EndPortion > 1))
                {
                    Note overhangNote = new Note(note)
                    {
                        Beat = nextBeat,
                        StartPortion = 0,
                        Portion = note.EndPortion - 1,
                        Tiing = new Tiing(noteBefore: note)
                    };
                    nextBeat.AllBeatNotes.Add(overhangNote);

                    note.EndPortion = 1;
                    note.Tiing = new Tiing(noteAfter: overhangNote);
                }
            }

            // Check if tone in last beat is overhanging


            // Check for correct beats and correct startTimes
            foreach (Beat beat in beats)
            {
                foreach (Note note in beat.AllBeatNotes.Where(note => 
                    note.Beat != beat
                    || note.StartTime + Helper.µ < note.Beat.StartTime))
                {
                    Debugger.Break();
                }
            }

            return beats;
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
            List<Note> notes = beats.SelectMany(beat => beat.AllBeatNotes).OrderBy(note => note.StartTime).ToList();

            const double MaxRelativeDelta = 0.3;

            List<Note> chordNotes = new List<Note>();

            for (int index = 0; index < notes.Count; index++)
            {
                Note mainNote = notes[index++];

                // Add part tones
                while (index < notes.Count)
                {
                    Note chordNote = notes[index];
                    // Same starttime, endtime & duration?
                    if (chordNote.StartPortion.IsAboutAbsolute(mainNote.StartPortion, PortionTolerance)
                        && chordNote.EndPortion.IsAboutAbsolute(mainNote.EndPortion, PortionTolerance)
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
                    => beat.AllBeatNotes.Contains(chordNote)))
                {
                    beat.AllBeatNotes.Remove(chordNote);
                }
            }
        }

        public override string ToString()
        {
            return $"{Name}: {Beats.Count} Beats (T={BeatValues.Duration.ToString(3)}) with {AllNotes.Count} Notes";
        }
    }

    public class Beat
    {
        public Score Score;
        public int Number;
        public List<Note> AllBeatNotes = new List<Note>();
        public List<Voice> Voices = new List<Voice>();

        public double Duration => Score.BeatValues.Duration;
        public double StartTime => Score.BeatValues.FirstBeatStartTime + (Number) * Duration;
        public double EndTime => Score.BeatValues.FirstBeatStartTime + (Number + 1) * Duration;

        public Beat(Score score, int number)
        {
            Score = score;
            Number = number;
        }

        public void CreateVoices() // Here happens a bug with wrong assignment
        {
            Voice AddVoice(int id, Note noteToAdd)
            {
                if (!Voices.FirstOrDefault(it => it.Id == id, out Voice voice))
                {
                    voice = new Voice(this, id);
                    Voices.Add(voice);
                }
                voice.Notes.Add(noteToAdd);
                return voice;
            }

            // Iterate tones and group into voices
            AllBeatNotes = AllBeatNotes.OrderBy(it => it.StartPortion).ToList(); // Order
            foreach (Note note in AllBeatNotes)
            {
                // Musescore doesnt care about same voice for tied notes
                //if (note.Tiing?.NoteBefore != null)
                //{
                //    //if (note.ToString() == "1: E7 5,159 to 5,219")
                //    //    Debugger.Break();
                //    // Create part with same VoiceId as last beat
                //    note.Voice = AddVoice(note.Tiing.NoteBefore.Voice.Id, note);
                //} else

                // Voice with room for tone?
                if (Voices.First(voice => note.StartPortion >= voice.Notes.Last().EndPortion - Score.PortionTolerance, out Voice fittingPart))
                {
                    // Yes: Add to existing voice
                    fittingPart.Notes.Add(note);
                    note.Voice = fittingPart;
                }
                else
                {
                    // No: Create new voice
                    for (int id = 0; true; id++)
                    {
                        if (Voices.None(voice => voice.Id == id))
                        {
                            note.Voice = AddVoice(id, note);
                            break;
                        }
                    }
                }

                if (note.Voice == null)
                {
                    Debugger.Break();
                }

            } // foreach note

            // Check for same ids
            List<int> ids = Voices.Select(voice => voice.Id).ToList();
            if (ids.Count != ids.Distinct().Count())
            {
                Debugger.Break();
            }

            // Check if notes have correct beat
            foreach (Voice voice in Voices)
            {
                foreach (Note note in voice.Notes)
                {
                    if (note.Beat != this)
                    {
                        Debugger.Break();
                    }

                    if (note.Voice == null)
                        Debugger.Break();
                }
            }

            Voices = Voices.OrderBy(voice => voice.Id).ToList();
        }

        /// <summary>
        ///   Optimizes the note portions for each part (e.g. 0.46 becomes 0.50)
        /// </summary>
        public void OptimizeNotePortions()
        {
            // Make voice fit to 1/32 and fill with pauses
            double minimum = Score.MinimumPortion;

            double GetRoundedPortion(double portion)
            {
                double roundedPortion = minimum * Math.Round(portion / minimum);
                return roundedPortion;
            }

            foreach (Voice voice in Voices)
            {
                // Debug
                List<Note> notesBeforeOptimizing = new List<Note>();
                foreach (Note note in voice.Notes)
                {
                    notesBeforeOptimizing.Add(new Note(note)); // Temp copy
                }

                if (Number == 6 && voice.Id == 3)
                {
                    //Debugger.Break();
                }

                // FIRST NOTE
                Note firstNote = voice.Notes.First();
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
                    Rest rest = new Rest(this, voice, 0, firstNote.StartPortion);
                    voice.Notes.Insert(0, rest);
                }

                // NOTES INBETWEEN
                for (int i = 0; i < voice.Notes.Count - 1; i++)
                {
                    Note note = voice.Notes[i];
                    Note nextNote = voice.Notes[i + 1];

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
                        Rest rest = new Rest(this, voice, restStartPotion, restEndPotion);
                        voice.Notes.Insert(i++, rest);
                        note.EndPortion = restStartPotion;
                        nextNote.StartPortion = restEndPotion;
                    }
                }

                // LAST NOTE
                Note lastNote = voice.Notes.Last();
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
                    Rest rest = new Rest(this, voice, lastNote.EndPortion, 1);
                    voice.Notes.Add(rest);
                }

                // Remove notes with portion = 0
                foreach (Note note in voice.Notes.Where(note => note.Portion == 0).ToArray())
                {
                    MainForm.LogLine("Removing " + note);
                    voice.Notes.Remove(note);
                    voice.RemovedNotes.Add(note);
                }

                // Sort all
                List<Note> OrderByStart(ref List<Note> notes) => notes = notes.OrderBy(note => note.StartPortion).ToList();
                OrderByStart(ref notesBeforeOptimizing);
                OrderByStart(ref voice.RemovedNotes);
                OrderByStart(ref voice.Notes);

                // Debug
                //var notesBefore = notesBeforeOptimizing;
                //var notesAfter = voice.Notes;
                //var removed = removedNotes;

                // Split notes that are not xml-conform
                for (int i = 0; i < voice.Notes.Count; i++)
                {
                    Note note = voice.Notes[i];
                    if (note.NoteLength.HasDeviation)
                    {
                        double totalPortion = note.NoteLength.ActualPortion;

                        double firstPortion = note.NoteLength.ProposedPortion;
                        double secondPortion = totalPortion - firstPortion;

                        note.Portion = firstPortion;
                        Note extraNote = new Note(note)
                        {
                            StartPortion = note.EndPortion,
                            Portion = secondPortion
                        };
                    }
                }


                // Check for deviation
                foreach (Note note in voice.Notes.Where(note => note.NoteLength.HasDeviation))
                {
                    Debugger.Break();
                }
            } // foreach voice

            // Check for voices that are not complete
            foreach (Voice voice in Voices)
            {
                double totalPortion = voice.Notes.Sum(note => note.Portion);
                if (Math.Abs(totalPortion - 1) > 0.001)
                {
                    Debugger.Break();
                }
            }
        }

        public void MergeAgain()
        {
            for (int i = 0; i < Voices.Count; i++)
            {
                Voice voice = Voices[i];
                for (int j = i + 1; j < Voices.Count; j++)
                {
                    Voice otherVoice = Voices[j];

                    // Compare each voice with each other voice
                    foreach (Note note in voice.Notes.Where(it => !(it is Rest)))
                    {
                        // Collect notes to remove
                        List<Note> notesToReplace = new List<Note>();

                        foreach (Note otherNote in otherVoice.Notes.Where(it => !(it is Rest)))
                        {
                            if (note.StartPortion.IsAboutAbsolute(otherNote.StartPortion, 0.001)
                                && note.EndPortion.IsAboutAbsolute(otherNote.EndPortion, 0.001))
                            {
                                // Add and collect
                                note.ChordToneHeights.Add(otherNote.ToneHeight);
                                notesToReplace.Add(otherNote);
                            }
                        }

                        // Replace by rest
                        foreach (Note toReplace in notesToReplace.ToArray())
                        {
                            int indexToReplace = otherVoice.Notes.IndexOf(toReplace);
                            otherVoice.Notes[indexToReplace] = new Rest(toReplace.Beat, toReplace.Voice, toReplace.StartPortion, toReplace.EndPortion);
                        }
                    }
                }
            }

        }

        public void SplitNotesToFitXml()
        {
            foreach (Voice voice in Voices)
            {
                for (int i = 0; i < voice.Notes.Count; i++)
                {

                }

                // Check for deviation
                foreach (Note note in voice.Notes.Where(note => note.NoteLength.HasDeviation))
                {
                    Debugger.Break();
                }
            }
        }

        public void RemoveEmptysAndFinalize()
        {
            // Remove voices with only rests..
            foreach (Voice voice in Voices.Where(voice => voice.Notes.All(note => note is Rest)).ToArray())
            {
                if (Voices.Count > 1) //.. as long as minimum 1 voice remains
                {
                    Voices.Remove(voice);
                }
                else
                {
                    break;
                }
            }

            // Sort voices with longer notes first
            Voices = Voices.OrderByDescending(voice => voice.Notes.Select(note => note.Portion).Mean()).ToList();

            // Set AllBeatNotes
            AllBeatNotes = Voices.SelectMany(voice => voice.Notes).OrderBy(note => note.StartTime).ToList();
        }

        public override string ToString()
        {
            string timeString = $"{StartTime.ToString(3)}-{EndTime.ToString(3)}";
            string notesString = Voices.Count > 0
                ? $"{Voices.Count} voices with {string.Join(", ", Voices.Select(it => it.Notes.Count))} tones"
                : $"{AllBeatNotes.Count} Notes: {string.Join(", ", AllBeatNotes)}";
            //return $"{StartTime.ToString(3)}-{EndTime.ToString(3)}: {Parts.Count} Voices with total {Notes.Count} Tones";
            return $"{Number} ({timeString}): {notesString}";
        }
    }

    /// <summary>
    ///   A part of a beat with a unique VoiceId.
    /// </summary>
    [Serializable]
    public class Voice
    {
        public Beat Beat;
        public List<Note> Notes = new List<Note>();
        public int Id;

        public List<Note> RemovedNotes = new List<Note>();

        protected Voice() { }

        public Voice(Beat beat, int id)
        {
            Beat = beat;
            Notes = new List<Note>();
            Id = id;
        }

        public override string ToString()
        {
            string notesAsString = string.Join(", ", Notes.Select(note 
                => $"{(note is Rest ? "Rest" : note.ToneHeightString)}({note.StartPortion.ToString(2)}-{note.EndPortion.ToString(2)}"));
            return $"{Beat.Number}-{Id}: {Notes.Count} Notes: {notesAsString}";
        }
    }

    [Serializable]
    public class BeatValues
    {
        public double FirstBeatStartTime;
        public double LastBeatStartTime;
        public double Duration;

        public double FirstToneStartTime;
        public double LastToneEndTime;

        public double BeatOffsetPortion;

        public double LastBeatEndTime => LastBeatStartTime + Duration;

        protected BeatValues() { }

        public BeatValues(List<BeatHit> hits, List<Tone> tones, double OriginStartTime)
        {
            Debug.Assert(hits.Count > 0);

            tones = tones.OrderBy(t => t.EndTime).ToList();

            // Get only MainBeats + sorted
            List<BeatHit> mainHits = hits.Where(t => t.isMainBeat).OrderBy(t => t.startTime).ToList();

            // Calc distances between each two
            List<double> mainBeatDistances = new List<double>();
            for (int i = 0; i < mainHits.Count - 1; i++)
            {
                mainBeatDistances.Add(mainHits[i + 1].startTime - mainHits[i].startTime);
            }

            Duration = mainBeatDistances.Mean();

            if (mainBeatDistances.StandardDeviation() > 0.05 * Duration)
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
                    timesToTest.Add(firstBeatTimeWithOffset + i * Duration);
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

            double beatAnchor = firstBeatHitTime + bestOffset;

            // Apply best offset
            FirstToneStartTime = tones.First().StartTime;
            FirstBeatStartTime = beatAnchor;
            while (FirstBeatStartTime > FirstToneStartTime)
            {
                FirstBeatStartTime -= Duration;
            }

            // Calc last BeatTime
            LastToneEndTime = tones.Last().EndTime;
            LastBeatStartTime = beatAnchor;
            while (LastBeatStartTime + Duration < LastToneEndTime)
            {
                LastBeatStartTime += Duration;
            }

            if (FirstBeatStartTime > FirstToneStartTime || LastBeatEndTime < LastToneEndTime)
            {
                Debugger.Break();
            }
        }

        public void ApplyOffset(double beatOffsetPortion)
        {
            BeatOffsetPortion = beatOffsetPortion;
            FirstBeatStartTime += beatOffsetPortion * Duration;
            LastBeatStartTime += beatOffsetPortion * Duration;
        }

        public List<double> GetBeatStartTimes()
        {
            List<double> times = new List<double>();
            for (double time = FirstBeatStartTime; time <= LastBeatStartTime + 0.001; time += Duration)
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
}
