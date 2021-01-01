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

        /// <summary>
        ///   The counter of the current beat (2 because first beat is automatically created)
        /// </summary>
        private int beatNumber = 2;
        private ScorePartwisePartMeasure CurrentMeasureRight => rightPart.Measure[beatNumber - 2];
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

            // Create score
            scorePartwise = CreateScorePartwise(title, save.BeatValues.Duration, out leftPart, out rightPart);

            Score score = new Score(save);
            foreach (Beat beat in score.Beats)
            {
                if (beat != score.Beats.First())
                {
                    AddBeat();
                }

                foreach (Voice voice in beat.Voices)
                {
                    if (voice.Id == 0)
                    {
                        foreach (Note note in voice.Notes)
                        {
                            XmlNote xmlNote = XmlNoteFromNote(note, HandType.Right);
                            CurrentMeasureRight.Note.Add(xmlNote);
                        }
                    }
                    else if (voice.Id == 1)
                    {
                        foreach (Note note in voice.Notes)
                        {
                            XmlNote xmlNote = XmlNoteFromNote(note, HandType.Left);
                            CurrentMeasureLeft.Note.Add(xmlNote);
                        }
                    }
                    //else Debugger.Break();

                    // Insert backup
                    if (voice.Notes.Count > 0)
                    {
                        CurrentMeasureRight.Note.Add(BackupPlaceholder);
                    }
                    //break;
                }

                /// Check if measure is filled 100%
                // Group by voice
                var voices = CurrentMeasureRight.Note.Where(note => note.Voice != null).GroupBy(note => note.Voice).Select(it => it.ToList()).ToList();
                foreach (List<XmlNote> notes in voices)
                {
                    if (notes.Any(note => note.Portion == 0))
                    {
                        Debugger.Break(); // NoteTypeValue is propably null
                    }
                    double totalPortion = notes.Sum(note => note.Portion);
                    if (totalPortion < 1 - Helper.µ || totalPortion > 1 + Helper.µ)
                    {
                        Debugger.Break(); // TotalPortion is invalid
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

        private static void AddBassText()
        {
            // Add chord text for left hand (e.g. Cmol)
            //if (leftTones.Contains(tone) && tone.ChordTones.Count > 0)
            //{
            //    string bassText = "_";
            //    if (tone.ChordTones.Count == 0)
            //    {
            //        bassText = note.GetPitchString();
            //    }
            //    else if (tone.ChordTones.Count == 2)
            //    {
            //        Pitch firstPitch = tone.Pitch;
            //        Pitch secondPitch = tone.ChordTones[0].Pitch;
            //        Pitch thirdPitch = tone.ChordTones[1].Pitch;

            //        List<PitchIntegerPair> pairs = new List<PitchIntegerPair>()
            //        {
            //            new PitchIntegerPair(firstPitch),
            //            new PitchIntegerPair(secondPitch),
            //            new PitchIntegerPair(thirdPitch),
            //        }.OrderBy(pair => pair.Integer).ToList();

            //        int delta1 = pairs[1].Integer - pairs[0].Integer;
            //        int delta2 = pairs[2].Integer - pairs[1].Integer;

            //        Pitch pitch = Pitch.A;
            //        AcchordType acchordType = AcchordType.SeptMin;

            //        // Dur
            //        if (delta1 == 4 && delta2 == 3)
            //        {
            //            pitch = pairs[0].Pitch; // correct
            //            acchordType = AcchordType.Dur;
            //        }
            //        else if (delta1 == 3 && delta2 == 5)
            //        {
            //            pitch = pairs[2].Pitch;
            //            acchordType = AcchordType.Dur;
            //        }
            //        else if (delta1 == 5 && delta2 == 4)
            //        {
            //            pitch = pairs[1].Pitch;
            //            acchordType = AcchordType.Dur;
            //        }
            //        // Mol
            //        else if (delta1 == 3 && delta2 == 4)
            //        {
            //            pitch = pairs[0].Pitch;
            //            acchordType = AcchordType.Mol;
            //        }
            //        else if (delta1 == 4 && delta2 == 5)
            //        {
            //            pitch = pairs[2].Pitch;
            //            acchordType = AcchordType.Mol;
            //        }
            //        else if (delta1 == 5 && delta2 == 3)
            //        {
            //            pitch = pairs[1].Pitch; // Correct
            //            acchordType = AcchordType.Mol;
            //        }

            //        bassText = $"{pitch}{acchordType}";
            //    }

            //    // Add bassText as "Lyric"
            //    note.Lyric.Add(new Lyric()
            //    {
            //        Text = new TextElementData()
            //        {
            //            Value = bassText,
            //            FontSize = "10"
            //        }
            //    });
            //}
        }


        private static Attributes CreateAttributes(HandType hand, int beatType)
        {
            if (hand == HandType.Both)
            {
                throw new Exception("Hand must be left or right.");
            }

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

            return attributes;
        }

        private static ScorePartwise CreateScorePartwise(string title, double beatDuration, out ScorePartwisePart leftHandsPart, out ScorePartwisePart rightHandsPart)
        {
            const string LeftHand = "LeftHand";
            const string RightHand = "RightHand";

            // Create ScorePartwise
            ScorePartwise scorePartwise = new ScorePartwise()
            {
                MovementTitle = title,
                PartList = new PartList()
                {
                    ScorePart = new System.Collections.ObjectModel.Collection<ScorePart>()
                    {
                        new ScorePart() { Id = RightHand, PartName = new PartName() {Value = RightHand } },
                        new ScorePart() { Id = LeftHand, PartName = new PartName() {Value = LeftHand } }
                    }
                }
            };

            // Add Parts
            rightHandsPart = new ScorePartwisePart() { Id = RightHand };
            leftHandsPart = new ScorePartwisePart() { Id = LeftHand };
            scorePartwise.Part.Add(rightHandsPart);
            scorePartwise.Part.Add(leftHandsPart);

            // Add first beat
            ScorePartwisePartMeasure rightPartMeasure = new ScorePartwisePartMeasure()
            {
                Number = "1",
                Width = 192
            };
            ScorePartwisePartMeasure leftPartMeasure = new ScorePartwisePartMeasure()
            {
                Number = "1",
                Width = 192
            };

            rightHandsPart.Measure.Add(rightPartMeasure);
            leftHandsPart.Measure.Add(leftPartMeasure);

            // Add attributes
            int beatType = 4;
            leftHandsPart.Measure[0].Attributes.Add(CreateAttributes(HandType.Left, beatType));
            rightHandsPart.Measure[0].Attributes.Add(CreateAttributes(HandType.Right, beatType));

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

            leftHandsPart.Measure[0].Direction.Add(direction);
            rightHandsPart.Measure[0].Direction.Add(direction);

            return scorePartwise;
        }

        /// <summary>
        ///   Saves the sheets as musicXml. Replaces and beautifies the xml.
        /// </summary>
        public void SaveAsFile(string path)
        {
            File.WriteAllText(path, "");
            XmlSerializer serializer = new XmlSerializer(typeof(ScorePartwise));

            // Using string and WriteAllText
            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, scorePartwise);
                string scoreAsString = textWriter.ToString();

                // Insert Backups (replace placeholders)
                StringBuilder builder = new StringBuilder(scoreAsString);
                IEnumerable<int> backupIndexes = scoreAsString.AllIndexesOf($"<footnote>{BackupFootnote}</footnote>").Reverse(); // Reverse so that removing text has no impact
                foreach (int index in backupIndexes)
                {
                    // Get duration
                    int durationEndIndex = scoreAsString.FindIndexBefore(index, "</duration>");
                    int durationStartIndex = scoreAsString.FindIndexBefore(index, "<duration>", true);
                    string durationString = builder.ToString(durationStartIndex, durationEndIndex - durationStartIndex);
                    int duration = Convert.ToInt32(durationString); // Make sure we have a double here
                    // Get start and end index to replace
                    int startIndex = scoreAsString.FindIndexBefore(index, "<note>");
                    int endIndex = scoreAsString.FindIndexAfter(index, "</note>", true);
                    int length = endIndex - startIndex;
                    string toRemove = builder.ToString(startIndex, length);
                    // Remove Backup-placeholder and insert actual Backup
                    builder.Remove(startIndex, length);
                    builder.Insert(startIndex, 
                          "<backup>\n"
                        + "         <duration>" + duration + "</duration>\n" // MUST NOT contain dot & decimals
                        + "      </backup>");
                }

                // Set attributes to beginning
                scoreAsString = builder.ToString();
                List<int> attributeIndexes = scoreAsString.AllIndexesOf("<direction>").Reverse().ToList(); // Attributes is always followed by Direction
                foreach (int startIndex in attributeIndexes)
                {
                    int endIndex = scoreAsString.FindIndexAfter(startIndex, "</attributes>", true);
                    string attributeText = builder.Cut(startIndex, endIndex - startIndex);
                    int measureStartIndex = scoreAsString.FindIndexBefore(startIndex, "measure");
                    int measureEndIndex = scoreAsString.FindIndexAfter(measureStartIndex, ">", true);
                    builder.Insert(measureEndIndex, "\n      " + attributeText);
                }

                // Insert LineBreaks before Notes and Backups
                builder.Replace("    <measure>", "\n\n    <measure>");
                builder.Replace("      <note>", "\n      <note>");
                builder.Replace("      <backup>", "\n      <backup>");
                // Remove encoding for MuseScore
                builder.Replace(" encoding=\"utf-16\"", "");

                // Write to file
                scoreAsString = builder.ToString();
                File.WriteAllText(path, scoreAsString);
            }

            // Using FileStream
            //serializer.Serialize(new FileStream(path, FileMode.OpenOrCreate), scorePartwise);

            Console.WriteLine("Saved Music Xml here as " + path);
        }
    }
}
