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
        public const string BackupFootnote = "Backup";

        // Technical settings
        private const int OctaveOffset = -2;
        private const int FourDivisions = 1; // Divisions per Quarter (MuseScore does not care about this value)

        private ScorePartwise scorePartwise;
        private ScorePartwisePart leftPart;
        private ScorePartwisePart rightPart;
        private ScorePartwisePart rightPart2;

        /// <summary>
        ///   The counter of the current beat (2 because first beat is automatically created)
        /// </summary>
        private int beatNumber = 2;
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

        public static string CreateBassText(params Pitch[] pitches)
        {
            switch (pitches.Length)
            {
                case 1:
                    return pitches[0].ToString();

                case 2:
                    return $"{pitches[0]}+{pitches[1]}";

                case 3:
                    Pitch firstPitch = pitches[0];
                    Pitch secondPitch = pitches[1];
                    Pitch thirdPitch = pitches[2];

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
                        pitch = pairs[1].Pitch; // Correct
                        acchordType = AcchordType.Mol;
                    }
                    else if (delta1 == 2 && delta2 == 4)
                    {
                        pitches = pitches;

                        string text = "" + pairs[0].Pitch + pairs[1].Pitch + BassStringForThirdBass[pairs[2].Pitch];
                        return text;
                    }
                    else if (delta1 == 3 && delta2 == 3)
                    {
                        string text = "" + pairs[2].Pitch + BassStringForThirdBass[pairs[0].Pitch] + pairs[1].Pitch;
                        return text;
                    }
                    else if (delta1 == 3 && delta2 == 6)
                    {
                        string text = "" + pairs[1].Pitch + BassStringForThirdBass[pairs[2].Pitch] + pairs[0].Pitch;
                        return text;
                    }
                    else if (delta1 == 6 && delta2 == 3)
                    {
                        string text = "" + pairs[0].Pitch + BassStringForThirdBass[pairs[1].Pitch] + pairs[2].Pitch;
                        return text;
                    }
                    else
                    {
                        Debugger.Break();
                    }
                    string bassText = $"{pitch}{StringForAcchordType[acchordType]}";
                    return bassText;

                case 4:
                    firstPitch = pitches[0];
                    secondPitch = pitches[1];
                    thirdPitch = pitches[2];
                    Pitch fourthPitch = pitches[3];

                    pairs = new List<PitchIntegerPair>()
                    {
                        new PitchIntegerPair(firstPitch),
                        new PitchIntegerPair(secondPitch),
                        new PitchIntegerPair(thirdPitch),
                        new PitchIntegerPair(fourthPitch),
                    }.OrderBy(pair => pair.Integer).ToList();

                    delta1 = pairs[1].Integer - pairs[0].Integer;
                    delta2 = pairs[2].Integer - pairs[1].Integer;
                    int delta3 = pairs[3].Integer - pairs[2].Integer;

                    pitch = Pitch.A;
                    acchordType = AcchordType.Undefined;
                    if (delta1 == 2 && delta2 == 3 && delta3 == 4)
                    {
                        pitch = pairs[1].Pitch;
                        acchordType = AcchordType.SeptMin;
                    }
                    else if (delta1 == 2 && delta2 == 4 && delta3 == 3)
                    {
                        pitch = pairs[3].Pitch;
                        acchordType = AcchordType.SeptMin;
                    }
                    else
                    {
                        Debugger.Break();
                    }
                    bassText = $"{pitch}{StringForAcchordType[acchordType]}";
                    return bassText;
                default:
                    return string.Join("", pitches);
            }
            
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
            [Pitch.A] = "F‾",
            [Pitch.B] = "Ḡ",
            [Pitch.Cis] = "Ā",
            [Pitch.Es] = "B̅",
            [Pitch.Fis] = "D‾",
            [Pitch.Gis] = "Ē",
            [Pitch.Bes] = "F‾is",
            //[Pitch.C] = "C̄",
            //[Pitch.D] = "D‾",
            //[Pitch.E] = "Ē",
            //[Pitch.F] = "F‾",
            //[Pitch.G] = "Ḡ",
            //[Pitch.A] = "Ā",
            //[Pitch.B] = "B̅",
            //[Pitch.Cis] = "C̄is",
            //[Pitch.Es] = "Ēs",
            //[Pitch.Fis] = "F‾is",
            //[Pitch.Gis] = "Ḡis",
            //[Pitch.Bes] = "B̅es",
        };

        private static Dictionary<AcchordType, string> StringForAcchordType = new Dictionary<AcchordType, string>()
        {
            [AcchordType.Undefined] = "?",
            [AcchordType.Dur] = "dur",
            [AcchordType.Mol] = "mol",
            [AcchordType.Sept] = "7",
            [AcchordType.SeptMin] = "7min",
        };

        public static void AddBassText(Tone tone)
        {
            ////Add chord text for left hand (e.g.Cmol)
            //if (leftTones.Contains(tone) && tone.ChordTones.Count > 0)
            //    {
            //        string bassText = "_";
            //        if (tone.ChordTones.Count == 0)
            //        {
            //            bassText = note.GetPitchString();
            //        }
            //        else if (tone.ChordTones.Count == 2)
            //        {
            //            Pitch firstPitch = tone.Pitch;
            //            Pitch secondPitch = tone.ChordTones[0].Pitch;
            //            Pitch thirdPitch = tone.ChordTones[1].Pitch;

            //            List<PitchIntegerPair> pairs = new List<PitchIntegerPair>()
            //            {
            //                new PitchIntegerPair(firstPitch),
            //                new PitchIntegerPair(secondPitch),
            //                new PitchIntegerPair(thirdPitch),
            //            }.OrderBy(pair => pair.Integer).ToList();

            //            int delta1 = pairs[1].Integer - pairs[0].Integer;
            //            int delta2 = pairs[2].Integer - pairs[1].Integer;

            //            Pitch pitch = Pitch.A;
            //            AcchordType acchordType = AcchordType.SeptMin;

            //            // Dur
            //            if (delta1 == 4 && delta2 == 3)
            //            {
            //                pitch = pairs[0].Pitch; // correct
            //                acchordType = AcchordType.Dur;
            //            }
            //            else if (delta1 == 3 && delta2 == 5)
            //            {
            //                pitch = pairs[2].Pitch;
            //                acchordType = AcchordType.Dur;
            //            }
            //            else if (delta1 == 5 && delta2 == 4)
            //            {
            //                pitch = pairs[1].Pitch;
            //                acchordType = AcchordType.Dur;
            //            }
            //            // Mol
            //            else if (delta1 == 3 && delta2 == 4)
            //            {
            //                pitch = pairs[0].Pitch;
            //                acchordType = AcchordType.Mol;
            //            }
            //            else if (delta1 == 4 && delta2 == 5)
            //            {
            //                pitch = pairs[2].Pitch;
            //                acchordType = AcchordType.Mol;
            //            }
            //            else if (delta1 == 5 && delta2 == 3)
            //            {
            //                pitch = pairs[1].Pitch; // Correct
            //                acchordType = AcchordType.Mol;
            //            }

            //            bassText = $"{pitch}{acchordType}";
            //        }

            //        // Add bassText as "Lyric"
            //        note.Lyric.Add(new Lyric()
            //        {
            //            Text = new TextElementData()
            //            {
            //                Value = bassText,
            //                FontSize = "10"
            //            }
            //        });
            //    }
        }


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
