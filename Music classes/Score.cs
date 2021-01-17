using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using XmlNote = MusicXmlSchema.Note;

namespace CreateSheetsFromVideo
{
    public class Score : SingleInstance<Score>
    {
        public static float LeftHandHue = Color.Violet.GetHue();
        public static float RightHandHue = Color.Yellow.GetHue();

        public static bool IsRightHand(Tone tone)
        {
            float toneHue = tone.Color.GetHue();
            return Math.Abs(toneHue - RightHandHue) < Math.Abs(toneHue - LeftHandHue);
        }

        // Settings
        public const double BeatOffsetPortion = 0;// -4.5 / 32; // Positive = shifts Beats right
        public const double SmallestNotePortion = 1.0 / 16; // 1/16 => keine kleineren Noten als 16tel
        public const double FrameLength = 1.0 / 30;

        // Merging
        public const double MaxMergeTimeDistance = 2 * FrameLength;
        public double SmallestNoteDuration;

        public string Name;
        public List<Voice> RightVoices = new List<Voice>();
        public Voice LeftVoice;
        public BeatValues BeatValues;

        public List<Note> AllNotes => RightVoices.SelectMany(voice => voice.AllNotes).OrderBy(note => note.StartTime).ToList();

        public Score(SheetSave save) : base()
        {
            Name = save.Name;
            BeatValues = new BeatValues(save.BeatHits, save.Tones);

            SmallestNoteDuration = SmallestNotePortion * BeatValues.Duration;

            SplitTonesToHands(save.Tones, out List<Tone> leftTones, out List<Tone> rightTones);

            List<Note> notes = ConvertToNotes(rightTones);
            CreateChords(notes);

            RightVoices = CreateVoices(notes);
            foreach (Voice voice in RightVoices)
            {
                voice.AnchorNotes();
                voice.CreateBeats();
                foreach (Beat beat in voice.Beats)
                {
                    beat.OptimizeNotePortions();
                }
            }

            return;

            //foreach (Beat beat in Beats)
            //{
            //    beat.CreateVoices();
            //    beat.OptimizeNotePortions();
            //    beat.MergeAgain();
            //}

            //foreach (Beat beat in Beats)
            //{
            //    // Must be done in a second run (because tiing works betweens beats)
            //    beat.RemoveEmptysAndFinalize();
            //}

            //Debugger.Break();
        }

        private static List<Note> ConvertToNotes(List<Tone> tones)
        {
            List<Note> notes = new List<Note>();
            foreach (Tone tone in tones)
            {
                Note note = new Note(tone);
                notes.Add(note);
            }
            return notes;
        }

        /// <summary>
        ///   Notes with ~ same start and end time will be appended to other notes and removed afterwards.
        /// </summary>
        private static void CreateChords(List<Note> notes)
        {
            List<Note> partOfOtherNotes = new List<Note>();

            for (int index = 0; index < notes.Count; index++)
            {
                Note currentNoteMote = notes[index++];

                // Add part tones
                while (index < notes.Count)
                {
                    Note potentialPartNote = notes[index];

                    // Same starttime, endtime & duration?
                    if (potentialPartNote.StartTime.IsAboutAbsolute(currentNoteMote.StartTime, MaxMergeTimeDistance)
                        && potentialPartNote.EndTime.IsAboutAbsolute(currentNoteMote.EndTime, MaxMergeTimeDistance)
                        && potentialPartNote.Duration.IsAboutRelative(currentNoteMote.Duration, MaxMergeTimeDistance))
                    {
                        currentNoteMote.ChordToneHeights.Add(potentialPartNote.ToneHeight);
                        partOfOtherNotes.Add(potentialPartNote);
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
            foreach (Note note in notes)
            {
                foreach (Note partOfOther in partOfOtherNotes.Where(chordNote
                    => notes.Contains(chordNote)))
                {
                    notes.Remove(partOfOther);
                }
            }
        }

        private List<Voice> CreateVoices(List<Note> notes) // Here happens a bug with wrong assignment
        {
            List<Voice> voices = new List<Voice>();

            // Iterate tones and group into voices
            notes = notes.OrderBy(it => it.StartTime).ToList(); // Order
            foreach (Note note in notes)
            {
                // Voice with room for tone?
                if (voices.First(it => note.StartTime >= it.AllNotes.Last().EndTime - SmallestNoteDuration, out Voice voice))
                {

                    // Yes: Add to existing voice
                    voice.AllNotes.Add(note);
                    note.Voice = voice;
                }
                else
                {
                    // No: Create new voice
                    for (int id = 0; true; id++)
                    {
                        if (voices.None(it => it.Id == id))
                        {
                            voice = voices.FirstOrDefault(it => it.Id == id);
                            if (voice == null)
                            {
                                voice = new Voice(id, this);
                                voices.Add(voice);
                            }

                            voice.AllNotes.Add(note);
                            note.Voice = voice;

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
            List<int> ids = voices.Select(voice => voice.Id).ToList();
            if (ids.Count != ids.Distinct().Count())
                Debugger.Break();

            // Check if notes have correct beat
            foreach (Voice voice in voices)
            {
                foreach (Note note in voice.AllNotes)
                {
                    if (note.Voice == null)
                        Debugger.Break();
                }
            }

            voices = voices.OrderBy(voice => voice.Id).ToList();
            return voices;
        }

        public static List<Note> CopyNotes(List<Note> notes)
        {
            return notes.Select(note => new Note(note, true, true)).ToList();
        }



        private static void SplitTonesToHands(List<Tone> tones, out List<Tone> leftHandTones, out List<Tone> rightHandTones)
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
                tones1AreLeftHand = IsRightHand(tones2.First());
            }
            else if (tones2.Count == 0)
            {
                tones1AreLeftHand = !IsRightHand(tones1.First());
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


        public override string ToString()
        {
            return $"{Name}: {RightVoices.Count} Voices with Notes {string.Join(", ", RightVoices.Select(v => v.AllNotes.Count))} (T={BeatValues.Duration.ToString(3)})";
        }
    }

    public class Beat
    {
        public Voice Voice;
        public BeatValues Values;

        public int Number;
        public List<Note> Notes = new List<Note>();

        public double Duration => Values.Duration;
        public double StartTime => Values.FirstBeatStartTime + Number * Duration;
        public double EndTime => Values.FirstBeatStartTime + (Number + 1) * Duration;

        public Beat(Voice voice, BeatValues values, int number)
        {
            Voice = voice;
            Values = values;
            Number = number;
        }

        /// <summary>
        ///   Optimizes the note portions for each part(e.g. 0.46 becomes 0.50)
        /// </summary>
        public void OptimizeNotePortions()
        {
            // Local
            double GetRoundedPortion(double portion)
            {
                return Score.SmallestNotePortion * Math.Round(portion / Score.SmallestNotePortion);
            }

            if (Notes.Count == 0)
            {
                return;
            }

            // Debug
            List<Note> notesBeforeOptimizing = new List<Note>();
            foreach (Note note in Notes)
            {
                notesBeforeOptimizing.Add(new Note(note, true)); // Temp copy
            }

            // FIRST NOTE
            Note firstNote = Notes.First();
            // Gap to beatStart < minimum?
            if (firstNote.StartPortion <= Score.SmallestNotePortion)
            {
                // Yes: Stretch first toneStart to start of beat
                firstNote.StartPortion = 0;
            }
            else
            {
                // No : Insert rest and adapt noteStart
                firstNote.StartPortion = GetRoundedPortion(firstNote.StartPortion);
                Note rest = Note.CreateRest(this, this.Voice, 0, firstNote.StartPortion);
                Notes.Insert(0, rest);
            }

            // NOTES INBETWEEN
            for (int i = 0; i < Notes.Count - 1; i++)
            {
                Note note = Notes[i];
                Note nextNote = Notes[i + 1];

                if (note.IsRest || nextNote.IsRest)
                {
                    continue;
                }

                // Close enough?
                double gap = nextNote.StartPortion - note.EndPortion;
                if (gap < Score.SmallestNotePortion)
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
                    Note rest = Note.CreateRest(this, Voice, restStartPotion, restEndPotion);
                    Notes.Insert(i++, rest);
                    note.EndPortion = restStartPotion;
                    nextNote.StartPortion = restEndPotion;
                }
            }

            // LAST NOTE
            Note lastNote = Notes.Last();
            // Gap to beatEnd < minimum? (Care: nextNote, not note)
            if (1 - lastNote.EndPortion <= Score.SmallestNotePortion)
            {
                // Yes: Stretch last toneEnd to end of beat
                lastNote.EndPortion = 1;
            }
            else
            {
                // No: Add rest and adapt noteEnd
                lastNote.EndPortion = GetRoundedPortion(lastNote.EndPortion);
                Note rest = Note.CreateRest(this, Voice, lastNote.EndPortion, 1);
                Notes.Add(rest);
            }

            // Remove notes with portion = 0
            foreach (Note note in Notes.Where(note => note.Portion == 0).ToArray())
            {
                MainForm.LogLine("Removing " + note);
                Notes.Remove(note);
            }

            // Sort all
            List<Note> OrderByStart(ref List<Note> notes) => notes = notes.OrderBy(note => note.StartPortion).ToList();
            OrderByStart(ref notesBeforeOptimizing);
            OrderByStart(ref Notes);

            // Debug
            notesBeforeOptimizing.Clear();
            foreach (Note note in Notes)
            {
                notesBeforeOptimizing.Add(new Note(note, true)); // Temp copy
            }

            // Split notes so make them xml-conform
            for (int i = 0; i < Notes.Count; i++)
            {
                Note note = Notes[i];
                if (note.NoteLength.HasDeviation)
                {
                    double totalPortion = note.NoteLength.ActualPortion;

                    double firstPortion = note.NoteLength.ProposedPortion;
                    double secondPortion = totalPortion - firstPortion;

                    note.Portion = firstPortion;
                    Note extraNote = new Note(note)
                    {
                        StartPortion = note.EndPortion,
                        Portion = secondPortion,
                    };

                    Notes.Insert(i + 1, extraNote);

                    // Adapt tiing
                    if (!note.IsRest)
                    {
                        if (note.Tiing != null)
                        {
                            extraNote.Tiing = new Tiing();

                            //if (note.Tiing.NoteAfter != null && note.Tiing.NoteBefore != null)
                            //{
                            //    throw new Exception("This can only happen if the beat is very strange (e.g. 7/16 Beat)");
                            //} else
                            if (note.Tiing.NoteAfter != null)
                            {
                                extraNote.Tiing.NoteAfter = note.Tiing.NoteAfter;
                                note.Tiing.NoteAfter.Tiing.NoteBefore = extraNote;
                            }
                            if (note.Tiing.NoteBefore != null)
                            {
                                note.Tiing.NoteBefore.Tiing.NoteAfter = note;
                            }

                            // Tie note and extraNote
                            note.Tiing.NoteAfter = extraNote;
                            extraNote.Tiing.NoteBefore = note;
                        }
                        else
                        {
                            // Create new tiing
                            note.Tiing = new Tiing(noteAfter: extraNote);
                            extraNote.Tiing = new Tiing(noteBefore: note);
                        }
                    }
                }
            }

            // Check for deviation
            foreach (Note note in Notes.Where(note => note.NoteLength.HasDeviation))
            {
                Debugger.Break();
            }

            // Remove emptys
            foreach (Note note in Notes.Where(note => note.Portion < Helper.µ).ToList())
            {
                Notes.Remove(note);
                //Debugger.Break();
            }

            double totalPortionSum = Notes.Sum(note => note.Portion);
            if (Helper.GetPercentageDistance(totalPortionSum, 1) > Helper.µ)
            {
                //Debugger.Break();
            }
        }

        /// <summary>
        ///   Look for notes with same start and end portion and merges them
        /// </summary>
        //public void MergeAgain()
        //{
        //    for (int i = 0; i < Voices.Count; i++)
        //    {
        //        Voice voice = Voices[i];
        //        for (int j = i + 1; j < Voices.Count; j++)
        //        {
        //            Voice otherVoice = Voices[j];

        //            // Compare each voice with each other voice
        //            foreach (Note note in voice.Notes.Where(it => !it.IsRest))
        //            {
        //                // Collect notes to remove
        //                List<Note> notesToReplace = new List<Note>();

        //                foreach (Note otherNote in otherVoice.Notes.Where(it => !it.IsRest))
        //                {
        //                    if (note.StartPortion.IsAboutAbsolute(otherNote.StartPortion, 0.001)
        //                        && note.EndPortion.IsAboutAbsolute(otherNote.EndPortion, 0.001))
        //                    {
        //                        // Add and collect
        //                        note.ChordToneHeights.Add(otherNote.ToneHeight);
        //                        notesToReplace.Add(otherNote);
        //                    }
        //                }

        //                // Replace by rest
        //                foreach (Note toReplace in notesToReplace.ToArray())
        //                {
        //                    int indexToReplace = otherVoice.Notes.IndexOf(toReplace);
        //                    otherVoice.Notes[indexToReplace] = Note.CreateRest(toReplace.Beat, toReplace.Voice, toReplace.StartPortion, toReplace.EndPortion);
        //                }
        //            }
        //        }
        //    }

        //}

        //public void RemoveEmptysAndFinalize()
        //{
        //    // Remove voices with only rests..
        //    foreach (Voice voice in Voices.Where(voice => voice.Notes.All(note => note.IsRest)).ToArray())
        //    {
        //        if (Voices.Count > 1) //.. as long as minimum 1 voice remains
        //        {
        //            Voices.Remove(voice);
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    // Look for wrong-tied notes (and more)
        //    foreach (Voice voice in Voices)
        //    {
        //        foreach (Note note in voice.Notes.Where(note => note.Tiing != null))
        //        {
        //            // Has voice?
        //            if (note.Voice == null)
        //            {
        //                Debugger.Break();
        //            }

        //            //if (note.Tiing?.TiedType == TiedType.Start && note.Pitch == Pitch.Bes)
        //            //Debugger.Break();

        //            Note noteAfter = note.Tiing.NoteAfter;
        //            if (noteAfter != null)
        //            {
        //                // Are notes tied correctly?
        //                if (noteAfter.Tiing.NoteBefore != note)
        //                {
        //                    // Fix (Dirty)
        //                    noteAfter.Tiing.NoteBefore = note;
        //                }
        //                if (noteAfter.ToneHeight != note.ToneHeight)
        //                {
        //                    Debugger.Break();
        //                }

        //                // Delete tiing when tied note is empty
        //                if (noteAfter.Portion == 0)
        //                {
        //                    note.Tiing.NoteAfter = null;
        //                }
        //            }

        //            Note noteBefore = note.Tiing.NoteBefore;
        //            if (noteBefore != null)
        //            {
        //                // Are notes tied correctly?
        //                if (noteBefore.Tiing.NoteAfter != note)
        //                {
        //                    // Fix (Dirty)
        //                    noteBefore.Tiing.NoteAfter = note;
        //                }
        //                if (noteBefore.ToneHeight != note.ToneHeight)
        //                {
        //                    Debugger.Break();
        //                }

        //                // Delete tiing when tied note is empty
        //                if (noteBefore.Portion == 0)
        //                {
        //                    note.Tiing.NoteBefore = null;
        //                }
        //            }

        //            if (note.Tiing.NoteBefore == null && note.Tiing.NoteAfter == null)
        //            {
        //                note.Tiing = null;
        //            }

        //        }
        //    }

        //    // Sort voices with longer notes first (May allow MusicXml the correct presentation)
        //    Voices = Voices.OrderByDescending(voice => voice.Notes.Select(note => note.Portion).Mean()).ToList();

        //    // Set AllBeatNotes
        //    Notes = Voices.SelectMany(voice => voice.Notes).OrderBy(note => note.StartTime).ToList();
        //}

        public override string ToString()
        {
            string timeString = $"{StartTime.ToString(3)}-{EndTime.ToString(3)}";
            return $"{Number} ({timeString}): {string.Join(", ", Notes)}";
        }
    }

    /// <summary>
    ///   A part of a beat with a unique VoiceId.
    /// </summary>
    [Serializable]
    public class Voice
    {
        public Score Score;
        public List<Beat> Beats = new List<Beat>();
        public List<Note> AllNotes = new List<Note>();
        public int Id;

        BeatValues BeatValues => Score.BeatValues;

        public Voice(int id, Score score)
        {
            Score = score;
            AllNotes = new List<Note>();
            Id = id;
        }

        public void AnchorNotes()
        {
            List<Note> notes = Score.CopyNotes(AllNotes);

            double allowedPortionOfDuration = 0.01;

            List<Note> startUnanchoredNotes = new List<Note>(notes);
            List<Note> endUnanchoredNotes = new List<Note>(notes);

            while (startUnanchoredNotes.Count + endUnanchoredNotes.Count > 0)
            {
                // Increment tolerance
                allowedPortionOfDuration *= 1.2;

                List<double> beatTimes = new List<double>(BeatValues.BeatTimes);
                double beatTimeDelta = beatTimes[1] - beatTimes[0];

                while (beatTimeDelta >= Score.SmallestNoteDuration - Helper.µ)
                {
                    foreach (Note note in startUnanchoredNotes.ToArray())
                    {
                        List<double> fittingStartTimes = beatTimes.Where(time => Helper.Distance(time, note.StartTime) / note.Duration < allowedPortionOfDuration)
                            .OrderBy(time => Helper.Distance(time, note.StartTime)).ToList();
                        if (fittingStartTimes.Count > 0)
                        {
                            if (Helper.Distance(note.StartTime, fittingStartTimes[0]) > 0.2)
                            {
                                Debugger.Break();
                            }
                            //if (note.ToneHeight.ToString() == "F5")
                            //Debugger.Break();
                            note.StartTime = fittingStartTimes.First();
                            startUnanchoredNotes.Remove(note);
                        }
                    }

                    foreach (Note note in endUnanchoredNotes.ToArray())
                    {
                        List<double> fittingEndTimes = beatTimes.Where(time => Helper.Distance(time, note.EndTime) / note.Duration < allowedPortionOfDuration)
                            .OrderBy(time => Helper.Distance(time, note.EndTime)).ToList();
                        if (fittingEndTimes.Count > 0)
                        {
                            if (Helper.Distance(note.EndTime, fittingEndTimes[0]) > 0.2)
                            {
                                Debugger.Break();
                            }
                            note.EndTime = fittingEndTimes.First();
                            endUnanchoredNotes.Remove(note);
                        }
                    }

                    // Insert halfs into beatTimes (e.g [1,3,5] becomes [1,2,3,4,5])
                    double newCount = 2 * beatTimes.Count - 1;
                    for (int i = 0; i < newCount - 1; i++)
                    {
                        double newBeatTime = 0.5 * (beatTimes[i + 1] + beatTimes[i]);
                        beatTimes.Insert(++i, newBeatTime);
                    }
                    // This changes from 2 -> 1 -> 0.5...
                    beatTimeDelta = beatTimes[1] - beatTimes[0];
                }

            }

            foreach (Note note in notes.Where(note => note.Duration.IsAboutAbsolute(0, 0.001)))
            {
                var x = AllNotes;
                Debugger.Break();
            }

            Dictionary<double, int> hitsPerTime = new Dictionary<double, int>();
            // Calc hits per NoteDuration
            for (double time = 0; time < BeatValues.LastBeatEndTime; time += Score.SmallestNoteDuration)
            {
                int numberNotes = notes.Where(note => note.StartTime.IsAboutAbsolute(time, Helper.µ)).Count();
                hitsPerTime.Add(time, numberNotes);
            }

            // Set new notes
            AllNotes = notes;
        }

        public void CreateBeats()
        {
            AllNotes = AllNotes.OrderBy(tone => tone.StartTime).ToList();

            double firstNoteStartTime = AllNotes.First().StartTime;
            double lastNoteEndTime = AllNotes.OrderBy(tone => tone.EndTime).Last().EndTime;

            List<Note> notesNotInBeat = new List<Note>(AllNotes);
            List<Note> notesInBeat = new List<Note>();

            int beatNum = 0;
            while (notesNotInBeat.Count > 0)
            {
                Beat beat = new Beat(this, BeatValues, beatNum++);

                // Fill notes that start in beat
                foreach (Note note in notesNotInBeat.ToArray())
                {
                    if (beat.StartTime - Helper.µ <= note.StartTime && note.StartTime < beat.EndTime)
                    {
                        notesNotInBeat.Remove(note);
                        notesInBeat.Add(note);

                        beat.Notes.Add(note);
                        note.Beat = beat;
                    }
                }

                Beats.Add(beat);
            }


            // Handle overhanging notes
            for (int i = 0; i < Beats.Count - 1; i++)
            {
                Beat currentBeat = Beats[i];
                Beat nextBeat = Beats[i + 1];

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

            // Check if tone in last beat is overhanging


            // Check for correct beats and correct startTimes
            foreach (Beat beat in Beats)
            {
                foreach (Note note in beat.Notes.Where(note =>
                    note.Beat != beat
                    || note.StartTime + Helper.µ < note.Beat.StartTime))
                {
                    Debugger.Break();
                }
            }
        }

        public override string ToString()
        {
            string notesAsString = string.Join(", ", AllNotes.Select(note 
                => $"{note.ToneHeightString}({note.StartPortion.ToString(2)}-{note.EndPortion.ToString(2)}"));
            return $"{Id}: {AllNotes.Count} Notes: {notesAsString}";
        }
    }

    [Serializable]
    public class BeatValues
    {
        public double FirstBeatStartTime => BeatTimes.First();
        public double LastBeatStartTime => LastBeatEndTime - Duration;
        public double LastBeatEndTime => BeatTimes.Last();

        public double Duration => BeatTimes[1] - BeatTimes[0];

        public double FirstToneStartTime => Tones.OrderBy(t => t.StartTime).First().StartTime;
        public double LastToneEndTime => Tones.OrderBy(t => t.EndTime).Last().EndTime;

        private List<Tone> Tones;

        /// <summary>
        ///   All StartTimes + last EndTime
        /// </summary>
        public List<double> BeatTimes { get; } = new List<double>();

        public double BeatOffsetPortion;

        protected BeatValues() { }

        public BeatValues(List<BeatHit> hits, List<Tone> tones, bool doubleIt = false)
        {
            Debug.Assert(hits.Count > 0);

            Tones = tones.OrderBy(t => t.StartTime).ToList();

            // Get only MainBeats + sorted
            List<BeatHit> mainBeatHits = hits.Where(hit => hit.IsMainBeat).OrderBy(hit => hit.Time).ToList();

            // Calc distances between each two
            List<double> mainBeatDistances = new List<double>();
            for (int i = 0; i < mainBeatHits.Count - 1; i++)
            {
                mainBeatDistances.Add(mainBeatHits[i + 1].Time - mainBeatHits[i].Time);
            }

            double duration = mainBeatDistances.Mean();

            if (mainBeatDistances.StandardDeviation() > 0.05 * duration)
            {
                throw new Exception("Standard deviation is too high");
            }

            double firstHitTime = hits[0].Time;

            // Calc deviation from timesToTest (with offset applied) to mainBeatHits
            double CalcStandardDeviation(double offset)
            {
                // Generates test times
                List<double> timesToTest = new List<double>();
                for (int i = 0; i < mainBeatHits.Count; i++)
                {
                    timesToTest.Add(firstHitTime + offset + i * duration);
                }

                // Calc standard deviation for each value
                List<double> deviations = new List<double>();
                for (int i = 0; i < mainBeatHits.Count; i++)
                {
                    double deviation = timesToTest[i] - mainBeatHits[i].Time;
                    deviation = Math.Pow(deviation, 2);
                    deviations.Add(deviation);
                }

                return deviations.Mean();
            }

            // Improve firstBeatTime by trying small offset shifts and saving those with lowest deviation
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
            // Use this offset to correct firstHitTime
            firstHitTime += bestOffset;

            // Lower firstHitTime until its lower than first tone
            while (firstHitTime > FirstToneStartTime)
            {
                firstHitTime -= duration;
            }

            for (double time = firstHitTime; time <= LastToneEndTime + duration; time += duration)
            {
                BeatTimes.Add(time);
            }

            if (doubleIt)
            {
                Helper.ExtendListByHalfs(BeatTimes);
            }
        }

        public void ApplyOffset(double beatOffsetPortion)
        {
            for (int i = 0; i < BeatTimes.Count; i++)
            {
                BeatTimes[i] += beatOffsetPortion * Duration;
            }
        }

        public override string ToString()
        {
            return $"Duration= {Duration.ToString(3)}s, from {FirstBeatStartTime.ToString(3)}s to {LastBeatEndTime}s";
        }
    }
}
