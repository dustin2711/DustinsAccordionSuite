// Use downlaoded schema classes (works for Kass' Theme better)
using MusicXmlSchema;
using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;
using XmlNote = MusicXmlSchema.Note;
using CreateSheetsFromVideo;
using PitchEnum = CreateSheetsFromVideo.PitchEnum;

namespace MusicXmlBeautifier
{
    /// <summary>
    ///   Omits bass lyrics if they previously appeared.
    /// </summary>
    [Flags]
    enum LyricsSimplification
    {
        /// <summary>
        ///   Always write bass lyrics.
        /// </summary>
        None = 0,
        /// <summary>
        ///   Omit bass lyrics if a tone is repeated in a measure.
        /// </summary>
        SameTone = 1,
        /// <summary>
        ///   Omit bass lyrics if a tone is repeated über measures hinweg.
        /// </summary>
        SameMeasure = 2,
        /// <summary>
        ///   Omit bass lyrics if it is repeated in a measure or über measures hinweg.
        /// </summary>
        All = SameTone | SameMeasure
    }

    internal class Beautifier
    {
        /// <summary>
        ///   Create a music xml for accordion at the same location as the given music xml with the given file name addition.
        /// </summary>
        /// <param name="musicXmlPath"></param>
        /// <param name="fileNameAddition">This string is appended to the name of the newly created .musicxml file.</param>
        /// <param name="removeNotesOutOfRange">Removes the notes that cannot be played by my red accordion.</param>
        /// <param name="removeStaccato"></param>
        /// <exception cref="Exception"></exception>
        internal static void CreateMusicXmlForAccordion(
            string musicXmlPath,
            string fileNameAddition = "(Accordion)",
            bool clampBassNotesToAccordionRange = true,
            bool removeNotesOutOfRange = true,
            bool removeStaccato = true,
            List<int>? voicesToExclude = default,
            bool reduceMainHandChordsToOneTone = false,
            bool removeLineBreaks = true,
            LyricsSimplification lyricSimplification = LyricsSimplification.All,
            params LyricsReplacement[] lyricsReplacements)
        {
            StringBuilder builder = new(File.ReadAllText(musicXmlPath));
            string scoreString = builder.ToString();

            /// Replace backups by notes (to keep order)
            IEnumerable<int> backupStartIndexes = scoreString.AllIndexesOf("<backup>");
            foreach (int startIndex in backupStartIndexes.Reverse())
            {
                // Filter duration
                int duration = Tools.FilterDuration(scoreString, startIndex, "backup");

                // Remove backup
                int backupEndIndex = scoreString.FindIndexAfter(startIndex, "</backup>", true);
                string textThatWillBeRemoved = builder.ToString(startIndex, backupEndIndex - startIndex);
                builder.Remove(startIndex, backupEndIndex - startIndex);

                // Insert backup-note
                builder.Insert(startIndex,
                   "<note>\n"
                 + "         <duration>" + duration + "</duration>\n" // MUST NOT contain dot & decimals
                 + $"         <footnote>{SheetsBuilder.BackupFootnote}</footnote>"
                 + "      </note>");
            }

            using StringReader reader = new(builder.ToString());
            // Create score from string
            XmlSerializer serializer = new(typeof(ScorePartwise));
            if (serializer.Deserialize(reader) is ScorePartwise scorePartwise)
            {
                // Set accordion as instrument
                foreach (ScorePart part in scorePartwise.PartList.ScorePart)
                {
                    part.PartName.Value = "Accordion";

                    ScoreInstrument scoreInstrument = part.ScoreInstrument.First();
                    scoreInstrument.InstrumentName = "Accordion";
                    //scoreInstrument.Id = "P2-I1"; // Invalid

                    MidiInstrument midiInstrument = part.MidiInstrument.First();
                    midiInstrument.MidiProgram = "22"; // MidiProgram 22 = Accordion
                    //midi.Volume = 80;
                    //midiInstrument.Id = "P2-I1";
                    //midiInstrument.MidiChannel = "2";
                    //midiInstrument.Pan = 0;
                }

                if (string.IsNullOrWhiteSpace(scorePartwise.MovementTitle))
                {
                    scorePartwise.MovementTitle = Path.GetFileNameWithoutExtension(musicXmlPath);
                }

                int circleOfFifthsPosition = Convert.ToInt32(scorePartwise.Part[0].Measure[0].Attributes[0].Key[0].Fifths);

                // Go through parts

                // Number of the measure that is also visible in Musescore (1 = first measure)
                int measureIndex = 0;

                for (int partNumber = 0; partNumber < Math.Min(2, scorePartwise.Part.Count); partNumber++)
                {
                    List<ScorePartwisePartMeasure> measures = scorePartwise.Part[partNumber].Measure.ToList();

                    // Go through measures (Takte)
                    foreach (ScorePartwisePartMeasure measure in measures)
                    {
                        // Remove line breaks
                        if (removeLineBreaks)
                        {
                            foreach (Print print in measure.Print)
                            {
                                print.NewPage = YesNo.No;
                                print.NewSystem = YesNo.No;
                            }
                        }

                        // Setup measure
                        // NewPages are invalid due to added lyrics: Remove them
                        if (measure.Print.Count > 0)
                        {
                            measure.Print[0].NewPage = YesNo.No;
                        }
                        if (measure.Attributes.Count > 0 && measure.Attributes[0].Key.Count > 0)
                        {
                            // Write # and b over each note instead of using key
                            measure.Attributes[0].Key[0].Fifths = "0";
                        }

                        measureIndex++;

                        // Remove staccatos of current measure
                        if (removeStaccato)
                        {
                            foreach (XmlNote note in measure.Note)
                            {
                                if (note.Notations.FirstOrDefault()?.Articulations.FirstOrDefault() is Articulations articulations)
                                {
                                    articulations.Staccato.Clear();
                                }
                            }
                        }

                        // Get actual notes (no rests)
                        List<XmlNote> notes = measure.Note.Where(note => note.Rest == null).ToList(); // Ignore rests

                        if (voicesToExclude != null)
                        {
                            notes = notes.Where(note => !voicesToExclude.Contains(Convert.ToInt32(note.Voice))).ToList();
                        }

                        if (removeNotesOutOfRange)
                        {
                            List<XmlNote> rightNotes = partNumber == 2 ? notes : notes.Where(note => note.Staff == "1").ToList();

                            foreach (XmlNote note in rightNotes)
                            {
                                int octave = int.Parse(note.Pitch.Octave);
                                PitchEnum pitch = note.Pitch.PitchEnum;
                                if (octave == 6 && pitch == PitchEnum.Fis)
                                { }
                                if (octave <= 2
                                    || (octave == 3 && pitch < PitchEnum.F)
                                    || octave >= 7
                                    || (octave == 6 && pitch > PitchEnum.F))
                                {
                                    measure.Note.Remove(note);
                                }
                                //octave = octave.Clamp(3, 3); // Octave 4 ist die Akkordeon-Standard-Tonleiter (C bis C')
                                //note.Pitch.Octave = octave.ToString();
                            }
                        }

                        // Insert bass lyrics for staff "2" (bass)

                        // Get notes of current beat
                        List<XmlNote> leftHandNotes = partNumber == 1 ? notes : notes.Where(note => note.Staff == "2").ToList();
                        List<XmlNote> rightHandNotes = notes.Except(leftHandNotes).Where(it => !it.IsBackup).ToList();

                        // Reduce main hand chords to one tone?
                        if (reduceMainHandChordsToOneTone)
                        {
                            // Split right hand chords into main and sub notes
                            List<XmlNote> baseNotes = rightHandNotes.Where(it => !it.IsChordSideNote()).ToList();
                            List<XmlNote> sideNotes = rightHandNotes.Except(baseNotes).ToList();

                            if (measureIndex == 24)
                            { }

                            // If base and subnotes have the same durations, the higher one can be picked
                            if (baseNotes.Select(it => it.Duration).SequenceEqual(sideNotes.Select(it => it.Duration)))
                            {
                                List<XmlNote> higherNotes = new();
                                for (int i = 0; i < baseNotes.Count; i++)
                                {
                                    XmlNote baseNote = baseNotes[i];
                                    XmlNote subNote = sideNotes[i];
                                    higherNotes.Add(baseNote.TotalHeight > subNote.TotalHeight ? baseNote : subNote);
                                    string bla = baseNote.TotalHeight + " vs " + subNote.TotalHeight;

                                }
                                List<XmlNote> lowerNotes = rightHandNotes.Except(higherNotes).ToList();

                                // Remove one list of notes..
                                measure.Note.RemoveRange(sideNotes);

                                // ..and assign the higher notes to the other list
                                for (int i = 0; i < baseNotes.Count; i++)
                                {
                                    XmlNote baseNote = baseNotes[i];
                                    XmlNote higherNote = higherNotes[i];
                                    baseNote.Pitch = higherNote.Pitch;
                                }
                            }

                        }

                        if (clampBassNotesToAccordionRange)
                        {
                            ClampBassNotesToAccordionRange(leftHandNotes);
                        }

                        if (leftHandNotes.Count > 0)
                        {
                            // Clear on new measure
                            List<List<PitchEnum>> previousPitchesList = new();

                            // Iterate notes
                            for (int noteIndex = 0; noteIndex < leftHandNotes.Count; noteIndex++)
                            {
                                XmlNote note = leftHandNotes[noteIndex];

                                if (!note.IsChordSideNote() && !note.IsBackup)
                                {
                                    List<PitchEnum> pitches = new();
                                    pitches.Add(note.Pitch.PitchEnum);

                                    // Iterate the other notes and maybe extent pitches
                                    for (int otherNoteIndex = noteIndex + 1; otherNoteIndex <= leftHandNotes.Count; otherNoteIndex++)
                                    {
                                        if (leftHandNotes.ElementAtOrDefault(otherNoteIndex) is XmlNote otherNote && otherNote.IsChordSideNote())
                                        {
                                            if (note.Duration != otherNote.Duration
                                                || note.Type.Value != otherNote.Type.Value)
                                            {
                                                // Chord notes must have same duration!
                                                Debugger.Break();
                                            }

                                            // Add pitch of otherNote
                                            pitches.Add(otherNote.Pitch.PitchEnum);
                                        }
                                        else
                                        {
                                            // >> Breakpoint "parking-spot" for certain measures <<

                                            // Remove redundant tones
                                            pitches = pitches.Distinct().ToList();

                                            // Set MainNote's lyrics
                                            string bassLyrics = SheetsBuilder.CreateBassLyrics(pitches, previousPitchesList, circleOfFifthsPosition, out bool _);

                                            // Pitches must not be equal to print lyrics
                                            if (previousPitchesList.Count == 0
                                                || previousPitchesList.LastOrDefault()?.SequenceEqual(pitches) == false)
                                            {
                                                previousPitchesList.Add(pitches);

                                                // Add bass lyrics
                                                note.Lyric.Add(new Lyric()
                                                {
                                                    Text = new TextElementData()
                                                    {
                                                        Value = bassLyrics,
                                                        FontSize = "9",
                                                        //Overline = overline ? "1" : "0", // Does not work (also not with FontStyleSpecified true)
                                                    }
                                                });
                                            }

                                            // Set next MainNote...
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    } // foreach measure

                    SimplificateLyrics(measures, lyricSimplification, lyricsReplacements);
                }

                using StringWriter textWriter = new();
                string savePath = Path.ChangeExtension(musicXmlPath, null) + fileNameAddition + Path.GetExtension(musicXmlPath);

                SheetsBuilder.SaveScoreAsMusicXml(savePath, scorePartwise);

                // Open created file with musescore
                Helper.OpenWithDefaultProgram(savePath);

                //Debugger.Break();
            }
            else
            {
                throw new Exception("ScorePartWise could not be deserialized: " + musicXmlPath);
            }
        }

        /// <summary>
        ///   Omit lyrics if they e.g. already appear in the measure before.
        /// </summary>
        private static void SimplificateLyrics(List<ScorePartwisePartMeasure> measures, LyricsSimplification lyricsSimplification, LyricsReplacement[] lyricsReplacements)
        {
            // Local methods
            static List<Lyric> GetLyrics(ScorePartwisePartMeasure measure)
                => measure.Note.SelectMany(note => note.Lyric).ToList();
            static List<string> ToStrings(List<Lyric> lyrics) 
                => lyrics.Select(lyr => lyr.Value).ToList();


            // Handle LyricsReplacements

            // Go through measures
            foreach (ScorePartwisePartMeasure measure in measures)
            {
                List<Lyric> lyrics = GetLyrics(measure);
                if (lyrics.Count > 0)
                {
                    List<string> texts = ToStrings(lyrics);
                    foreach (LyricsReplacement replacement in lyricsReplacements)
                    {
                        if (replacement.CanReplaceMultipleTimesPerMeasure)
                        {
                            for (int index = 0; index < texts.Count; index++)
                            {
                                List<Lyric> currentlyrics = new();
                                for (int innerIndex = index; innerIndex < index + replacement.LyricsChainToReplace.Length; innerIndex++)
                                {
                                    if (innerIndex >= texts.Count)
                                    {
                                        break;
                                    }
                                    currentlyrics.Add(lyrics[innerIndex]);
                                }
                                if (ToStrings(currentlyrics).SequenceEqual(replacement.LyricsChainToReplace))
                                {
                                    // Set all lyrics empty..
                                    foreach (Lyric lyric in currentlyrics)
                                    {
                                        lyric.Value = "";
                                    }
                                    // .. except the first one that is replaced
                                    lyrics[index].Value = replacement.NewLyric;
                                }
                            }
                        }
                        else
                        {
                            if (texts.SequenceEqual(replacement.LyricsChainToReplace))
                            {
                                lyrics.ForEach(lyric => lyric.Value = "");
                                lyrics[0].Value = replacement.NewLyric;
                            }
                        }
                    }
                }
            }


            // Handle LyricsSimplication (boil down)

            List<string> lastlyPrintedTexts = ToStrings(GetLyrics(measures[0]));

            // Go through all measures starting with the second
            foreach (ScorePartwisePartMeasure measure in measures.Skip(1))
            {
                List<Lyric> lyrics = GetLyrics(measure);
                List<string> texts = ToStrings(lyrics);

                if (lyrics.Count > 0)
                {
                    // If previous measure has only one same lyric, the same lyrics of the current measure can be deleted until there is another lyric
                    if (lastlyPrintedTexts.AllAreSame() && texts.First() == lastlyPrintedTexts.First())
                    {
                        if (lyricsSimplification.HasFlag(LyricsSimplification.SameTone))
                        {
                            string lastlyPrintedText = lastlyPrintedTexts.First();
                            foreach (Lyric lyric in lyrics)
                            {
                                if (lyric.Value == lastlyPrintedText)
                                {
                                    lyric.Value = "";
                                }
                                else break;
                            }
                        }
                    }
                    // If lyrics sequence of current to previous measure is same, delete the lyrics
                    else if (texts.SequenceEqual(lastlyPrintedTexts))
                    {
                        if (lyricsSimplification.HasFlag(LyricsSimplification.SameMeasure))
                        {
                            lyrics.ForEach(lyric => lyric.Value = "");
                        }
                    }

                    lastlyPrintedTexts = texts;
                }
            }


            // Remove pedals

            foreach (ScorePartwisePartMeasure measure in measures)
            {
                foreach (Direction direction in measure.Direction)
                {
                    foreach (var type in direction.DirectionType)
                    {
                        type.Pedal = null;
                    }
                }
            }
        }

        /// <summary>
        ///   Moves all pitch heights of the given notes to the one that can be played on accordion.
        /// </summary>
        private static void ClampBassNotesToAccordionRange(List<XmlNote> notes)
        {
            // Clamp notes into accordion bass
            foreach (XmlNote note in notes)
            {
                if (!note.IsBackup)
                {
                    note.Pitch.Octave = "3";
                    if ((note.Pitch.Step is Step.C or Step.D && note.Pitch.Alter is 0 or 1)
                        || (note.Pitch.Step is Step.D or Step.E && note.Pitch.Alter is -1 or -2)
                        || (note.Pitch.Step is Step.E or Step.F && note.Pitch.Alter is -2 or -3 or -4)
                        || (note.Pitch.Step is Step.B or Step.C && note.Pitch.Alter is 1 or 2 or 3))
                    {
                        note.Pitch.Octave = "4";
                    }
                }
            }
        }
    }
}
