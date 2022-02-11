using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using XmlNote = MusicXmlSchema.Note;

// NoteDuration = Time in seconds e.g. 0.122
// NoteLength = Time in Beats e.g. 1/16

/// <summary>
///   ToDo:
///     - Triolen einführen 8)
///     - Notenhälse oben umkehren (+ mehr Abstand zw. Zeilen?
///     - Ganz zum Schluss: Basstakt hat 6 Schläge
///     - Ganz zum Schluss: Bass-Beschriftung ist zu eng
/// </summary>
namespace CreateSheetsFromVideo
{
    public class SheetsBuilder
    {
        // Technical settings
        private const int OctaveOffset = -2;
        private const int FourDivisions = 1; // Divisions per Quarter (MuseScore does not care about this value)
        private const string Up = "\""; // Indicates ThirdBass


        public const string BackupFootnote = "Backup";

        private ScorePartwise scorePartwise;
        private ScorePartwisePart leftPart;
        private ScorePartwisePart rightPart;
        private ScorePartwisePart rightPart2;

        /// <summary>
        ///   The counter of the current beat (2 because first beat is automatically created)
        /// </summary>
        private int beatNumber = 2;

        public SheetsBuilder(SheetSave save, string title)
        {
            // Check for tones
            Debug.Assert(save.Tones.Count > 0);

            // Order tones by starttime
            List<Tone> tones = save.Tones.OrderBy(t => t.StartTime).ToList();

            // Create XmlScore

            Score score = new Score(save);

            scorePartwise = CreateScorePartwise(title, save.BeatValues.Duration, score.LeftVoice != null, score.RightVoices.Count > 1, 
                out leftPart, out rightPart, out rightPart2);

            foreach (int beatIndex in Enumerable.Range(0, score.RightVoices[0].Beats.Count))
            {
                if (beatIndex != 0)
                {
                    AddBeat();
                }

                // Right parts
                foreach (Voice voice in score.RightVoices)
                {
                    Beat beat = voice.Beats[beatIndex];
                    foreach (Note note in beat.Notes)
                    {
                        XmlNote xmlNote = XmlNoteFromNote(note, HandType.Right);
                        if (voice.Id == 0)
                        {
                            CurrentMeasureRight.Note.Add(xmlNote);
                        }
                        else
                        {
                            CurrentMeasureRight2.Note.Add(xmlNote);
                        }
                    }

                    // Insert backup
                    //if (voice.Notes.Count > 0)
                    //{
                    //    CurrentMeasureRight.Note.Add(BackupPlaceholder);
                    //}
                }

                // Left part
                if (score.LeftVoice != null)
                {
                    Beat beat = score.LeftVoice.Beats[beatIndex];
                    foreach (Note note in beat.Notes)
                    {
                        XmlNote xmlNote = XmlNoteFromNote(note, HandType.Left);
                        CurrentMeasureLeft.Note.Add(xmlNote);
                    }
                }

                /// Check if measure is filled 100%
                // Group by voice
                var voices = CurrentMeasureRight.Note.Where(note => note.Voice != null).GroupBy(note => note.Voice).Select(it => it.ToList()).ToList();
                foreach (List<XmlNote> notes in voices)
                {
                    if (notes.Any(note => note.Portion == 0))
                    {
                        //Debugger.Break(); // NoteTypeValue is propably null
                    }
                    double totalPortion = notes.Sum(note => note.Portion);
                    if (totalPortion < 1 - Helper.µ || totalPortion > 1 + Helper.µ)
                    {
                        //Debugger.Break(); // TotalPortion is invalid
                    }
                }
            }
        }

        private ScorePartwisePartMeasure CurrentMeasureRight => rightPart.Measure[beatNumber - 2];
        private ScorePartwisePartMeasure CurrentMeasureRight2 => rightPart2.Measure[beatNumber - 2];
        private ScorePartwisePartMeasure CurrentMeasureLeft => leftPart.Measure[beatNumber - 2];

        /// <summary>
        ///   Note that is later replaced by backup (must be a note to have correct placement in notes list)
        /// </summary>
        private XmlNote BackupPlaceholder => new XmlNote()
        {
            Duration = FourDivisions,
            Footnote = new FormattedText() { Value = BackupFootnote },
        };

        void AddBeat()
        {
            leftPart.Measure.Add(new ScorePartwisePartMeasure()
            {
                Number = beatNumber.ToString(),
                Width = 192
            });
            rightPart.Measure.Add(new ScorePartwisePartMeasure()
            {
                Number = beatNumber.ToString(),
                Width = 192
            });
            beatNumber++;
        }

        void AddBackupLeft()
        {
            AddBackup(leftPart);
        }

        void AddBackupRight()
        {
            AddBackup(rightPart);
        }

        // This is always a whole beat
        void AddBackup(ScorePartwisePart part)
        {
            rightPart.Measure[beatNumber - 2].Note.Add(new XmlNote()
            {
                Duration = FourDivisions,
                Footnote = new FormattedText() { Value = BackupFootnote }
            });
        }

        private XmlNote XmlNoteFromNote(Note note, HandType hand)
        {
            // Create note
            XmlNote xmlNote = new XmlNote()
            {
                Type = new NoteType()
                {
                    Value = note.NoteTypeValue
                },
                Duration = FourDivisions * (decimal)note.Portion, // Can be 0 or 1000 and MuseScore still works
                Voice = (note.Voice.Id + 1).ToString()
            };

            if (note.IsRest)
            {
                xmlNote.Rest = new Rest();
            }
            else
            {
                int alter = note.Pitch.GetAlter();
                xmlNote.Pitch = new MusicXmlSchema.Pitch()
                {
                    Step = note.Pitch.GetStep(),
                    Alter = alter,
                    AlterSpecified = alter != 0,
                    Octave = (OctaveOffset + note.Octave).ToString() // 4 is default
                };

                int stemDownThreshold = hand == HandType.Right ? 84 : -1;
                xmlNote.Stem = new Stem()
                {
                    Value = note.ToneHeight.TotalValue > stemDownThreshold ? StemValue.Down : StemValue.Up
                };

                xmlNote.Chord = (note.ChordToneHeights.Count > 0) ? new Empty() : null;
            }

             if (xmlNote.Duration == 0 || xmlNote.Duration > FourDivisions)
                Debugger.Break();

            // Handle tiing
            if (note.Tiing != null)
            {
                // btw "note.Tie.Add()" does not work
                Notations notations = new Notations()
                {
                    Tied =
                    {
                        new Tied()
                        {
                            Type = note.Tiing.TiedType
                        }
                    }
                };
                xmlNote.Notations.Add(notations);// { Tied = new System.Collections.ObjectModel.Collection<Tied>() { new Tied() { Type = TiedType.Stop } } });
            }

            if (note.Dotting == Dotting.Dotted)
            {
                xmlNote.Dot.Add(new EmptyPlacement());
            }
            else if (note.Dotting == Dotting.DoubleDotted)
            {
                xmlNote.Dot.Add(new EmptyPlacement());
                xmlNote.Dot.Add(new EmptyPlacement());
            }

            if (xmlNote.Type == null
                || xmlNote.Voice == null)
            {
                Debugger.Break();
            }

            return xmlNote;
        }

        /// <summary>
        ///   Creates the accord lyrics for the given pitches.
        /// </summary>
        /// <param name="chord"></param>
        /// <param name="precedingChords">Contains all previous accords from current beat.</param>
        /// <param name="circleOfFifthsPosition"></param>
        /// <param name="isOrdinaryChord"></param>
        /// <returns></returns>
        public static string CreateBassLyrics(List<Pitch> chord, List<List<Pitch>> precedingChords, int circleOfFifthsPosition, out bool isOrdinaryChord)
        {
            const bool ReplaceBbyH = true;
            const bool Print4thChordNote = false;
            const bool ExtendTwoNotesToMolAndDur = false;
            const bool SkipPeriodicRepititions = false;

            List<Pitch> precedingPitches = precedingChords.LastOrDefault();

            /// Make Gis to As and so on...
            string ApplyFifths(string text)
            {
                if (circleOfFifthsPosition == -6)
                {
                    switch (text)
                    {
                        case "Gis":
                            return "As";
                        case "A":
                            return "F" + Up;
                        case "Cis":
                            return "Des";
                        case "Fis":
                            return "Ges";
                        case "Ē":
                            return "As";
                        case "H̅":
                            return "Es";
                        case "Fis" + Up:
                            return "Bes";
                    }
                }

                return text;
            }

            isOrdinaryChord = false;
            string lyrics = "";

            // Remove redundant tones (This is done in Tools.AddBassLyrics)
            //pitches = pitches.Distinct().ToList();

            if (SkipPeriodicRepititions)
            {
                List<List<Pitch>> allChords = new List<List<Pitch>>(precedingChords) { chord };

                // Check all pitches of current measure if we have the pattern A-Adur-A-Adur-... so we can write only A-Adur
                if (allChords.Count >= 3)
                {
                    for (int i = 0; i < allChords.Count - 2; i++)
                    {
                        if (Helper.ListsEqual(allChords[i], allChords[i + 2]))
                        {
                            return "";
                        }
                    }
                }

                // Skip lyrics if they are the same to current pitches
                if (allChords.Count == 3)
                {
                    if (Helper.ListsEqual(allChords[0], allChords[2]))
                    {
                        return "";
                    }
                }

                if (allChords.Count == 4)
                {
                    // First and third are same + second and fourth are same: SKIP
                    if (Helper.ListsEqual(allChords[0], allChords[2])
                        && Helper.ListsEqual(allChords[1], allChords[3]))
                    {
                        return "";
                    }
                }
            }


            switch (chord.Count)
            {
                case 1:
                    lyrics = chord[0].ToString();
                    lyrics = ApplyFifths(lyrics);
                    break;
                case 2:
                {
                    if (precedingPitches?.Count == 1)
                    {
                        // Check if the preceding pitch complements the current pitches to form a chord
                        List<Pitch> mergedPitches = new List<Pitch>(chord) { precedingPitches[0] }.Distinct().ToList();
                        string text = CreateBassLyrics(mergedPitches, new List<List<Pitch>>(), circleOfFifthsPosition, out isOrdinaryChord);
                        if (isOrdinaryChord)
                        {
                            return text;
                        }
                    }
                    if (ExtendTwoNotesToMolAndDur && chord.Count == 2)
                    {
                        List<PitchIntegerPair> pairs = new List<PitchIntegerPair>()
                        {
                            new PitchIntegerPair(chord[0]),
                            new PitchIntegerPair(chord[1]),
                        }.OrderBy(pair => pair.Integer).ToList();

                        int delta = pairs[1].Integer - pairs[0].Integer;

                        Pitch pitch = Pitch.A;
                        AcchordType acchordType = AcchordType.Undefined;

                        bool succes = true;

                        // Dur: 4 and 3
                        if (delta == 4)
                        {
                            pitch = pairs[0].Pitch;
                            acchordType = AcchordType.Dur;

                        }
                        else if (delta == 9)
                        {
                            pitch = pairs[1].Pitch;
                            acchordType = AcchordType.Mol;
                        }
                        else if (delta == 5)
                        {
                            pitch = chord[1];
                            foreach (int i in Enumerable.Range(0, 5))
                            {
                                pitch = pitch.Next();
                            }
                            acchordType = AcchordType.Dur;
                        }
                        // Mol 3 and 4
                        else if (delta == 3)
                        {
                            pitch = pairs[0].Pitch;
                            acchordType = AcchordType.Mol;
                        }
                        else
                        {
                            succes = false;
                        }

                        lyrics = $"{ApplyFifths(pitch.ToString())}{StringForAcchordType[acchordType]}";

                        if (succes)
                        {
                            goto end;
                        }
                    }
                    lyrics = ApplyFifths(chord[0].ToString()) + ApplyFifths(chord[1].ToString());
                    break;
                }
                case 3:
                    {
                        isOrdinaryChord = true;

                        Pitch firstPitch = chord[0];
                        Pitch secondPitch = chord[1];
                        Pitch thirdPitch = chord[2];

                        List<PitchIntegerPair> pairs = new List<PitchIntegerPair>()
                    {
                        new PitchIntegerPair(firstPitch),
                        new PitchIntegerPair(secondPitch),
                        new PitchIntegerPair(thirdPitch),
                    }.OrderBy(pair => pair.Integer).ToList();

                        int delta1 = pairs[1].Integer - pairs[0].Integer;
                        int delta2 = pairs[2].Integer - pairs[1].Integer;

                        Pitch pitch = Pitch.A;
                        AcchordType acchordType = AcchordType.Undefined;

                        // Dur
                        if (delta1 == 4 && delta2 == 3)
                        {
                            pitch = pairs[0].Pitch; // correct
                            acchordType = AcchordType.Dur;
                        }
                        else if (delta1 == 3 && delta2 == 5)
                        {
                            pitch = pairs[2].Pitch;
                            acchordType = AcchordType.Dur;
                        }
                        else if (delta1 == 5 && delta2 == 4)
                        {
                            pitch = pairs[1].Pitch;
                            acchordType = AcchordType.Dur;
                        }
                        // Mol
                        else if (delta1 == 3 && delta2 == 4)
                        {
                            pitch = pairs[0].Pitch;
                            acchordType = AcchordType.Mol;
                        }
                        else if (delta1 == 4 && delta2 == 5)
                        {
                            pitch = pairs[2].Pitch;
                            acchordType = AcchordType.Mol;
                        }
                        else if (delta1 == 5 && delta2 == 3)
                        {
                            pitch = pairs[1].Pitch;
                            acchordType = AcchordType.Mol;
                        }
                        // Sept
                        else if (delta1 == 4 && delta2 == 6)
                        {
                            pitch = pairs[0].Pitch;
                            acchordType = AcchordType.Sept;
                        }
                        else if (delta1 == 2 && delta2 == 4)
                        {
                            pitch = pairs[1].Pitch;
                            acchordType = AcchordType.Sept;
                        }
                        else if (delta1 == 6 && delta2 == 2)
                        {
                            pitch = pairs[2].Pitch;
                            acchordType = AcchordType.Sept;
                        }
                        // Min
                        else if (delta1 == 3 && delta2 == 3)
                        {
                            pitch = pairs[0].Pitch;
                            acchordType = AcchordType.Min;
                        }
                        else if (delta1 == 6 && delta2 == 3)
                        {
                            pitch = pairs[1].Pitch;
                            acchordType = AcchordType.Min;
                        }
                        else if (delta1 == 3 && delta2 == 6)
                        {
                            pitch = pairs[2].Pitch;
                            acchordType = AcchordType.Min;
                        }
                        else
                        {
                            // Non-default
                            isOrdinaryChord = false;

                            string CreateCustomBassLyrics(Pitch pitch0, Pitch pitch1, Pitch pitch2, params int[] indexesToGetThirdBass)
                            {
                                string text = "";
                                string first = indexesToGetThirdBass.Contains(0) ? BassStringForThirdBass[pitch0] : pitch0.ToString();
                                text += ApplyFifths(first);
                                string second = indexesToGetThirdBass.Contains(1) ? BassStringForThirdBass[pitch1] : pitch1.ToString();
                                text += ApplyFifths(second);
                                string third = indexesToGetThirdBass.Contains(2) ? BassStringForThirdBass[pitch2] : pitch2.ToString();
                                text += ApplyFifths(third);
                                return text;
                            }

                            if (delta1 == 2 && delta2 == 4)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[0].Pitch, pairs[1].Pitch, pairs[2].Pitch, 2);
                            }
                            else if (delta1 == 3 && delta2 == 3)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[0].Pitch, pairs[1].Pitch, 1);
                            }
                            else if (delta1 == 3 && delta2 == 6)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[1].Pitch, pairs[2].Pitch, pairs[0].Pitch, 1);
                            }
                            else if (delta1 == 6 && delta2 == 3)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[0].Pitch, pairs[1].Pitch, pairs[2].Pitch, 1);
                            }
                            else if (delta1 == 4 && delta2 == 2)
                            {
                                // e.g. EE'Fis'
                                lyrics = CreateCustomBassLyrics(pairs[0].Pitch, pairs[1].Pitch, pairs[2].Pitch, 1, 2);
                            }
                            else if (delta1 == 5 && delta2 == 2)
                            {
                                // e.g. E'H'Fis'
                                lyrics = CreateCustomBassLyrics(pairs[0].Pitch, pairs[1].Pitch, pairs[2].Pitch, 0, 1, 2);
                            }
                            else if (delta1 == 2 && delta2 == 5)
                            {
                                // e.g. EsBesF
                                lyrics = CreateCustomBassLyrics(pairs[0].Pitch, pairs[2].Pitch, pairs[1].Pitch);
                            }
                            else if (delta1 == 3 && delta2 == 2)
                            {
                                // e.g. GisBesF (Must be covnerted to as!)
                                lyrics = CreateCustomBassLyrics(pairs[1].Pitch, pairs[2].Pitch, pairs[0].Pitch);
                            }
                            else if (delta1 == 1 && delta2 == 3)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[1].Pitch, pairs[0].Pitch, 2);
                            }
                            else if (delta1 == 1 && delta2 == 6)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[1].Pitch, pairs[0].Pitch, pairs[2].Pitch, 1, 2);
                            }
                            else if (delta1 == 6 && delta2 == 2)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[1].Pitch, pairs[2].Pitch, pairs[0].Pitch, 2);
                            }
                            else if (delta1 == 4 && delta2 == 4)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[0].Pitch, pairs[1].Pitch, pairs[2].Pitch, 2);
                            }
                            else if (delta1 == 2 && delta2 == 7)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[1].Pitch, pairs[0].Pitch, 1);
                            }
                            else if (delta1 == 5 && delta2 == 5)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[1].Pitch, pairs[0].Pitch);
                            }
                            else if (delta1 == 1 && delta2 == 7)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[0].Pitch, pairs[2].Pitch, pairs[1].Pitch, 1);
                            }
                            else if (delta1 == 2 && delta2 == 3)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[0].Pitch, pairs[1].Pitch);
                            }
                            else if (delta1 == 7 && delta2 == 3)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[0].Pitch, pairs[1].Pitch);
                            }
                            else if (delta1 == 2 && delta2 == 8)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[0].Pitch, pairs[1].Pitch);
                            }
                            else if (delta1 == 8 && delta2 == 2)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[1].Pitch, pairs[2].Pitch, pairs[0].Pitch);
                            }
                            else if (delta1 == 3 && delta2 == 7)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[1].Pitch, pairs[2].Pitch, pairs[0].Pitch);
                            }
                            else if (delta1 == 7 && delta2 == 4)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[0].Pitch, pairs[1].Pitch);
                            }
                            else if (delta1 == 4 && delta2 == 1)
                            {
                                lyrics = CreateCustomBassLyrics(pairs[2].Pitch, pairs[0].Pitch, pairs[1].Pitch, 1);
                            }
                            else
                            {
                                Debugger.Break();
                            }

                            break;
                        }

                        if (acchordType == AcchordType.Undefined)
                        {
                            Debugger.Break();
                        }

                        lyrics = $"{ApplyFifths(pitch.ToString())}{StringForAcchordType[acchordType]}";

                        break;
                    }
                case 4:
                    {
                        Pitch firstPitch = chord[0];
                        Pitch secondPitch = chord[1];
                        Pitch thirdPitch = chord[2];
                        Pitch fourthPitch = chord[3];

                        List<PitchIntegerPair> pairs = new List<PitchIntegerPair>()
                    {
                        new PitchIntegerPair(firstPitch),
                        new PitchIntegerPair(secondPitch),
                        new PitchIntegerPair(thirdPitch),
                        new PitchIntegerPair(fourthPitch),
                    }.OrderBy(pair => pair.Integer).ToList();

                        int delta1 = pairs[1].Integer - pairs[0].Integer;
                        int delta2 = pairs[2].Integer - pairs[1].Integer;
                        int delta3 = pairs[3].Integer - pairs[2].Integer;

                        bool success = false;
                        // Take first 3 to create a dur/mol/sept/min
                        lyrics = CreateBassLyrics(chord.Take(3).ToList(), precedingChords?.Take(3).ToList(), circleOfFifthsPosition, out success)
                            + (Print4thChordNote ? chord[3].ToString() : "");
                        if (!success)
                        {
                            // Take last 3 to create accord
                            lyrics = CreateBassLyrics(chord.Skip(1).ToList(), precedingChords?.Skip(1).ToList(), circleOfFifthsPosition, out success)
                                + (Print4thChordNote ? chord[1].ToString() : "");
                        }

                        //pitch = Pitch.A;
                        //acchordType = AcchordType.Undefined;
                        //if (delta1 == 2 && delta2 == 3 && delta3 == 4)
                        //{
                        //    pitch = pairs[1].Pitch;
                        //    acchordType = AcchordType.Min;
                        //}
                        //else if (delta1 == 2 && delta2 == 4 && delta3 == 3)
                        //{
                        //    pitch = pairs[3].Pitch;
                        //    acchordType = AcchordType.Min;
                        //}
                        //else
                        //{
                        //    Debugger.Break();
                        //}

                        //if (acchordType == AcchordType.Undefined)
                        //{
                        //    Debugger.Break();
                        //}
                        //lyrics = $"{pitch}{StringForAcchordType[acchordType]}";
                        break;
                    }
                default:
                    lyrics = string.Join("", chord);
                    break;
            }

        end:
            if (ReplaceBbyH)
            {
                lyrics = lyrics.Replace("B", "H");
                lyrics = lyrics.Replace("B̅", "H̅");
                lyrics = lyrics.Replace("Hes", "B");
                lyrics = lyrics.Replace("H̅es", "B̅");
            }

            if (true)
            {
                lyrics = lyrics.Replace("is", "#");
                lyrics = lyrics.Replace("es", "♭");
                lyrics = lyrics.Replace("s", "♭");
            }

            // When e.g. Dmol follows D, we can write only "mol"
            if (isOrdinaryChord
                && precedingChords?.Count == 1
                && CreateBassLyrics(precedingPitches, new List<List<Pitch>>(), circleOfFifthsPosition, out bool _).Substring(0, 1) == lyrics.Substring(0, 1))
            {
                lyrics = lyrics.Substring(1);
            }

            return lyrics;
        }

        private static string GetBassStringForThirdBass(Pitch pitch, int fifths)
        {
            //if (fifths == -6 && pitch == Pitch.)
            if (BassStringForThirdBass.TryGetValue(pitch, out string bassString))
            {
                return bassString;
            }

            return pitch.ToString();
        }

        /// <summary>
        ///   Returnt e.g. C̄ for Terzbass E (because easier to read)
        /// </summary>
        private static Dictionary<Pitch, string> BassStringForThirdBass = new Dictionary<Pitch, string>()
        {
            [Pitch.C] = "Ḡis",
            [Pitch.D] = "B̅es",
            [Pitch.E] = "C̄",
            [Pitch.F] = "C̄is",
            [Pitch.G] = "Ēs",
            [Pitch.A] = "F" + Up,
            [Pitch.B] = "Ḡ",
            [Pitch.Cis] = "Ā",
            [Pitch.Es] = "B̅es",
            [Pitch.Fis] = "D" + Up,
            [Pitch.Gis] = "Ē",
            [Pitch.Bes] = "Fis" + Up,
        };

        private static Dictionary<AcchordType, string> StringForAcchordType = new Dictionary<AcchordType, string>()
        {
            [AcchordType.Undefined] = "?",
            [AcchordType.Dur] = "dur",
            [AcchordType.Mol] = "mol",
            [AcchordType.Sept] = "7",
            [AcchordType.Min] = "min",
        };

        private static ScorePartwise CreateScorePartwise(string title, double beatDuration, bool addLeftPart, bool addRightHandsPart2, out ScorePartwisePart leftHandsPart, out ScorePartwisePart rightHandsPart, out ScorePartwisePart rightHandsPart2)
        {
            const int beatType = 4;
            const string _LeftHand_ = "LeftHand";
            const string _RightHand_ = "RightHand";
            const string _RightHand2_ = "RightHand2";

            // Create ScorePartwise
            ScorePartwise scorePartwise = new ScorePartwise()
            {
                MovementTitle = title,
                PartList = new PartList()
                {
                    ScorePart = new System.Collections.ObjectModel.Collection<ScorePart>()
                    {
                        new ScorePart() { Id = _RightHand_, PartName = new PartName() {Value = _RightHand_ } },
                        new ScorePart() { Id = _LeftHand_, PartName = new PartName() {Value = _LeftHand_ } }
                    }
                }
            };


            ScorePartwisePartMeasure CreateFirstMeasure(HandType hand)
            {

                ScorePartwisePartMeasure measure = new ScorePartwisePartMeasure()
                {
                    Number = "1",
                    Width = 192,
                };

                // Create & add attributes
                Attributes attributes = new Attributes()
                {
                    Divisions = FourDivisions,
                    DivisionsSpecified = true
                };
                // Clef (= Notenschlüssel)
                attributes.Clef.Add(new Clef()
                {
                    Sign = hand == HandType.Left ? ClefSign.F : ClefSign.G,
                    Line = hand == HandType.Left ? "4" : "2"
                });
                // 4/4 Takt
                Time time = new Time();
                time.Beats.Add(beatType.ToString());
                time.BeatType.Add(beatType.ToString());
                attributes.Time.Add(time);
                // 5th?
                //attributes.Key.Add(new Key() { Fifths = "0" });
                measure.Attributes.Add(attributes);

                /// Create and add direction
                // Set tempo
                Direction direction = new Direction()
                {
                    Sound = new Sound()
                    {
                        Tempo = (decimal)Math.Round(60 * beatType / beatDuration, 1),
                        TempoSpecified = true
                    },
                    //Directive = YesNo.Yes,
                    //Placement = AboveBelow.Above,
                };
                direction.DirectionType.Add(new DirectionType());
                direction.DirectionType.First().Words.Add(new FormattedTextId()
                {
                    Lang = "null"
                });
                measure.Direction.Add(direction);

                return measure;
            }


            // Add Parts
            rightHandsPart = new ScorePartwisePart() { Id = _RightHand_ };
            rightHandsPart2 = new ScorePartwisePart() { Id = _RightHand2_ };
            leftHandsPart = new ScorePartwisePart() { Id = _LeftHand_ };

            // Add first measure (beat)
            rightHandsPart.Measure.Add(CreateFirstMeasure(HandType.Right));
            rightHandsPart2.Measure.Add(CreateFirstMeasure(HandType.Right));
            leftHandsPart.Measure.Add(CreateFirstMeasure(HandType.Left));

            // Add parts
            scorePartwise.Part.Add(rightHandsPart);
            if (addRightHandsPart2)
            {
                scorePartwise.Part.Add(rightHandsPart2);
            }
            if (addLeftPart)
            {
                scorePartwise.Part.Add(leftHandsPart);
            }

            return scorePartwise;
        }


        public static void SaveScoreAsMusicXml(string path, ScorePartwise scorePartwise)
        {
            File.WriteAllText(path, "");
            XmlSerializer serializer = new XmlSerializer(typeof(ScorePartwise));

            // Using string and WriteAllText
            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, scorePartwise);
                string scoreString = textWriter.ToString();
                StringBuilder builder = new StringBuilder(scoreString);

                // Insert Backups (replace placeholders)
                IEnumerable<int> footnoteIndexes = scoreString.AllIndexesOf($"<footnote>{BackupFootnote}</footnote>"); // Reverse so that removing text has no impact
                foreach (int index in footnoteIndexes.Reverse())
                {
                    int duration = Tools.FilterDuration(scoreString, index, "note");

                    // Get start and end index to replace
                    int startIndex = scoreString.FindIndexBefore(index, "<note>");
                    int endIndex = scoreString.FindIndexAfter(index, "</note>", true);
                    int length = endIndex - startIndex;
                    string toRemove = builder.ToString(startIndex, length);
                    // Remove Backup-placeholder and insert actual Backup
                    builder.Remove(startIndex, length);
                    builder.Insert(startIndex,
                          "<backup>\n"
                        + "         <duration>" + duration + "</duration>\n" // MUST NOT contain dot & decimals
                        + "      </backup>");
                }

                // Move elements to measure start
                IEnumerable<int> measureIndexes = builder.ToString().AllIndexesOf("<measure").ToList(); // Attributes is always followed by Direction
                foreach (int startIndex in measureIndexes.Reverse())
                {
                    int endOfmeasureOpener = builder.ToString().FindIndexAfter(startIndex, ">", true);
                    int endIndex = builder.ToString().FindIndexAfter(startIndex, "</measure>", true);
                    if (endIndex == -1)
                    {
                        Debugger.Break();
                    }

                    int attributeStartIndex = builder.ToString().FindIndexAfter(startIndex, "<attributes", false, endIndex);
                    if (attributeStartIndex != -1)
                    {
                        int attributeEndIndex = builder.ToString().FindIndexAfter(attributeStartIndex, "</attributes>", true, endIndex);
                        // Move to beginning
                        string textToMove = builder.CutOut(attributeStartIndex, attributeEndIndex - attributeStartIndex);
                        builder.Insert(endOfmeasureOpener, "\n      " + textToMove);
                    }

                    // Directions: Collect textsToMove and insert after (to prevent disturbing)
                    List<string> textsToMove = new List<string>();
                    IEnumerable<int> indexes = builder.ToString().AllIndexesOf("<direction>", "<direction ").Where(i => i >= startIndex && i < endIndex).ToList();
                    foreach (int directionStartIndex in indexes.Reverse())
                    {
                        // Collect
                        int directionEndIndex = builder.ToString().FindIndexAfter(directionStartIndex, "</direction>", true, endIndex);
                        textsToMove.Add(builder.CutOut(directionStartIndex, directionEndIndex - directionStartIndex));
                    }
                    foreach (string text in textsToMove)
                    {
                        // Insert
                        builder.Insert(endOfmeasureOpener, "\n      " + text);
                    }
                }


                // Make tie follow duration (else Musescore error)
                scoreString = builder.ToString();
                IEnumerable<int> noteIndexes = scoreString.AllIndexesOf("<note").ToList();
                foreach (int startIndex in noteIndexes.Reverse())
                {
                    int endIndex = scoreString.FindIndexAfter(startIndex, "</note");

                    int tieStartIndex = scoreString.FindIndexAfter(startIndex, "<tie", false, endIndex);
                    int durationStartIndex = scoreString.FindIndexAfter(startIndex, "<duration", false, endIndex);
                    if (tieStartIndex == -1 || durationStartIndex == -1)
                    { 
                        continue;
                    }
                    int tieEndIndex = scoreString.FindIndexAfter(tieStartIndex, "/>", true);

                    int durationEndIndex = scoreString.FindIndexAfter(durationStartIndex, "</duration>", true);

                    if (tieEndIndex >= endIndex || durationStartIndex == -1)
                        Debugger.Break();

                    if (durationStartIndex > tieStartIndex)
                    {
                        // We need to swap: Cut out "duration" and insert it before "tie"
                        string durationText = builder.CutOut(durationStartIndex, durationEndIndex - durationStartIndex);
                        builder.Insert(tieStartIndex, durationText + "\n");
                    }
                }

                // Reinsert beam numbers
                builder.Replace("<beam>", "<beam number=\"1\">");

                // Beautify with LineBreaks
                builder.Replace("    <measure>", "\n\n    <measure>");
                builder.Replace("      <note>", "\n      <note>");
                builder.Replace("      <backup>", "\n      <backup>");

                // Remove encoding for MuseScore
                builder.Replace(" encoding=\"utf-16\"", "");

                // Write to file
                scoreString = builder.ToString();
                File.WriteAllText(path, scoreString);
            }

            // Using FileStream
            //serializer.Serialize(new FileStream(path, FileMode.OpenOrCreate), scorePartwise);

            Console.WriteLine("Saved Music Xml here as " + path);
        }

        /// <summary>
        ///   Saves the sheets as musicXml. Replaces and beautifies the xml.
        /// </summary>
        public void SaveAsFile(string path)
        {
            SaveScoreAsMusicXml(path, scorePartwise);
        }
    }
}
