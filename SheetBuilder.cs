using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public double MinimumNoteLength => BeatDuration / 64;
        
        // Technical settings
        private const int SkipBeats = 0; // For printing musicxml (Default 0)
        public const double BeatOffsetPortion = -0.155; // Positive = shifts Beats right
        private const int OctaveOffset = -2;
        private const int Divisions = 1; // Divisions per Quarter (MuseScore does not care about this value)

        private const HandType HandToCreateSheetsFor = HandType.Right;

        private ScorePartwise scorePartwise;
        private List<Tone> leftTones;
        private List<Tone> rightTones;

        /// <summary>
        ///   The counter of the current beat
        /// </summary>
        private int beatNumber = 2; // "measure"

        private SheetSave Save;
        private double BeatDuration => Save.BeatValues.Duration;

        public SheetsBuilder(SheetSave save, string title)
        {
            Save = save;
            // Check for tones
            Debug.Assert(save.Tones.Count > 0);

            // Order tones by starttime
            List<Tone> tones = save.Tones.OrderBy(t => t.StartTime).ToList();

            List<double> beatTimes = save.BeatValues.GetBeatStartTimes();

            // Split left | right

            // Merge to chords
            //leftTones = MergeChords(leftTonesUnmerged, save.BeatValues.FirstBeatStartTime, save.BeatValues.Duration);
            //rightTones = MergeChords(rightTonesUnmerged, save.BeatValues.FirstBeatStartTime, save.BeatValues.Duration);

            Score score = new Score(save);

            // Create score
            scorePartwise = CreateScorePartwise(title, BeatDuration,
                out ScorePartwisePart leftPart,
                out ScorePartwisePart rightPart);

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

            void AddNoteRight(Note Note)
            {
                foreach (XmlNote note in XmlNoteFromNote(Note))
                {
                    rightPart.Measure[beatNumber - 2].Note.Add(note);
                }
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
                    Duration = Divisions * 4,
                    Footnote = new FormattedText() { Value = "Backup"}
                });
            }

            // Go through Beats
            //beats = beats.Skip(SkipBeats).ToList();
            //foreach (Beat beat in beats)
            //{
            //    if (beat != beats.First())
            //    {
            //        AddBeat();
            //    }

            //    foreach (Part Part in beat.Parts)
            //    {
            //        foreach (Note tone in Part.Notes)
            //        {
            //            AddNoteRight(tone);
            //        }

            //        // Insert backup
            //        if (Part.Notes.Count > 0)
            //        {
            //            AddBackupRight();
            //        }
            //    }
            //}
        }


        private XmlNote[] XmlNoteFromNote(Note note)
        {
            // Create note
            XmlNote xmlNote;
            if (note is Rest)
            {
                xmlNote = new XmlNote()
                {
                    Rest = new MusicXmlSchema.Rest(),
                    Duration = (decimal)note.Portion,
                    Type = new NoteType()
                    {
                        Value = note.NoteTypeValue
                    },
                    //Voice = Note.Part.VoiceId.ToString()
                };
            }
            else
            {
                int alter = note.Pitch.GetAlter();
                xmlNote = new XmlNote()
                {
                    //Chord = (Note.Tone.IsPartOfAnotherChord) ? new Empty() : null,
                    Pitch = new MusicXmlSchema.Pitch()
                    {
                        Step = note.Pitch.GetStep(),
                        Alter = alter,
                        AlterSpecified = alter != 0,
                        Octave = (OctaveOffset + note.Octave).ToString() // 4 is default
                    },
                    Duration = (decimal)note.Portion,
                    Type = new NoteType()
                    {
                        Value = note.NoteTypeValue
                    },
                    Stem = new Stem() { Value = note.Pitch > Pitch.C ? StemValue.Down : StemValue.Up },
                    //Voice = Note.Part.VoiceId.ToString()
                };
            }

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

            if (note.Dotting.HasFlag(Dotting.None))
            {
                // Do nothing
            }
            if (note.Dotting.HasFlag(Dotting.Dotted))
            {
                xmlNote.Dot.Add(new EmptyPlacement());
            }
            else if (note.Dotting.HasFlag(Dotting.DoubleDotted))
            {
                xmlNote.Dot.Add(new EmptyPlacement());
                xmlNote.Dot.Add(new EmptyPlacement());
            }
            else // Handle PlusQuarter, PlusEight and so on
            {
                decimal bigPortion = 1m / (decimal)NoteLength.FactorForDotting[note.Dotting]; // e.g. f(1¼) = 1 / 1.25 = 0.8 
                // Adapt main note duration
                xmlNote.Duration = bigPortion * (decimal)note.Duration;
                // Set extra note duration (1/4x or 1/8x or 1/16x)
                XmlNote extraXmlNote = CopyXmlNote(xmlNote);
                extraXmlNote.Duration = (1m - bigPortion) * (decimal)note.Duration;

                if (note.Dotting.HasFlag(Dotting.PlusQuarter))
                {
                    Debug.Assert((int)note.NoteTypeValue>= (int)NoteTypeValue.Item256Th); // Else cannot take 1/4
                    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous();
                }
                else if (note.Dotting.HasFlag(Dotting.PlusEight))
                {
                    Debug.Assert((int)note.NoteTypeValue >= (int)NoteTypeValue.Item128Th); // Else cannot take 1/8
                    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous().Previous();
                }
                else if (note.Dotting.HasFlag(Dotting.Plus16th))
                {
                    Debug.Assert((int)note.NoteTypeValue >= (int)NoteTypeValue.Item64Th); // Else cannot take 1/16
                    extraXmlNote.Type.Value = note.NoteTypeValue.Previous().Previous().Previous().Previous();
                }
                
                if (note.Tiing != null)
                {
                    if (note.Tiing.TiedType == TiedType.Start)
                    {
                        extraXmlNote.Notations.First().Tied.First().Type = TiedType.Continue;
                    }
                    else if (note.Tiing.TiedType == TiedType.Stop)
                    {
                    xmlNote.Notations.First().Tied.First().Type = TiedType.Continue;
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }

                return new XmlNote[] { xmlNote, extraXmlNote };
            }

            return new XmlNote[] { xmlNote };
        }

        private XmlNote[] NotesFromTone(Tone tone)
        {
            int alter = tone.Pitch.GetAlter();
            int octave = OctaveOffset + tone.Octave;

            double noteLength = tone.Duration / Save.BeatValues.Duration;
            NoteLength lengthHolder = NoteLength.CreateFromPortion(noteLength);

            // Create note
            XmlNote note = new XmlNote()
            {
                //Chord = (tone.IsPartOfAnotherChord) ? new Empty() : null,
                Pitch = new MusicXmlSchema.Pitch()
                {
                    Step = tone.Pitch.GetStep(),
                    Alter = alter,
                    AlterSpecified = alter != 0,
                    Octave = octave.ToString() // 4 is default
                },
                //Duration = lengthHolder.GetMusicXmlDuration(Divisions),
                Type = new NoteType()
                {
                    Value = lengthHolder.NoteTypeValue
                },
                Stem = new Stem() { Value = tone.Pitch > Pitch.C ? StemValue.Down : StemValue.Up },
                //Voice = tone.Part.VoiceId.ToString()
            };

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

            // Handle tiing
            //if (tone is TiedTone tiedTone)
            //{
            //    // btw "note.Tie.Add()" does not work
            //    Notations notations = new Notations()
            //    {
            //        Tied =
            //        {
            //            new Tied()
            //            {
            //                Type = tiedTone.TiedType
            //            }
            //        }
            //    };
            //    note.Notations.Add(notations);// { Tied = new System.Collections.ObjectModel.Collection<Tied>() { new Tied() { Type = TiedType.Stop } } });
            //}

            switch (lengthHolder.Dotting)
            {
                case Dotting.Dotted:
                    note.Dot.Add(new EmptyPlacement());
                    break;
                case Dotting.DoubleDotted:
                    note.Dot.Add(new EmptyPlacement());
                    note.Dot.Add(new EmptyPlacement());
                    break;
                case Dotting.Plus16th:
                case Dotting.PlusEight:
                case Dotting.PlusQuarter:
                    decimal bigPortion = 1m / (decimal)NoteLength.FactorForDotting[lengthHolder.Dotting];
                    /// Split tone into whole and quarter 
                    XmlNote wholeNote = CopyXmlNote(note);
                    wholeNote.Duration = bigPortion * note.Duration;


                    XmlNote extraXmlNote = CopyXmlNote(note);
                    extraXmlNote.Duration = (1 - bigPortion) * note.Duration;
                    if (lengthHolder.Dotting == Dotting.PlusQuarter)
                    {
                        Debug.Assert((int)note.Type.Value >= (int)NoteTypeValue.Item256Th); // Else cannot take 1/4
                        extraXmlNote.Type.Value = note.Type.Value.Previous().Previous();
                    }
                    else if (lengthHolder.Dotting == Dotting.PlusEight)
                    {
                        Debug.Assert((int)note.Type.Value >= (int)NoteTypeValue.Item128Th); // Else cannot take 1/8
                        extraXmlNote.Type.Value = note.Type.Value.Previous().Previous().Previous();
                    }
                    else if (lengthHolder.Dotting == Dotting.Plus16th)
                    {
                        Debug.Assert((int)note.Type.Value >= (int)NoteTypeValue.Item64Th); // Else cannot take 1/16
                        extraXmlNote.Type.Value = note.Type.Value.Previous().Previous().Previous().Previous();
                    }


                    //if (note is TiedTone split)
                    //{
                    //    if (split.TiedType == TiedType.Start)
                    //    {
                    //        quarterNote.Notations.First().Tied.First().Type = TiedType.Continue;
                    //    }
                    //    else if (split.TiedType == TiedType.Stop)
                    //    {
                    //        wholeNote.Notations.First().Tied.First().Type = TiedType.Continue;
                    //    }
                    //    else
                    //    {
                    //        Debugger.Break();
                    //    }
                    //}

                    return new XmlNote[] { wholeNote, extraXmlNote };
            }

            return new XmlNote[] { note };
        }

        /// <summary>
        ///   Ignores dotting
        /// </summary>
        private static XmlNote CopyXmlNote(XmlNote other)
        {
            XmlNote note = new XmlNote()
            {
                Chord = other.Chord,
                Pitch = other.Pitch,
                Duration = other.Duration,
                Type = other.Type,
                Stem = other.Stem,
                Rest = other.Rest
            }; 

            // Tiing
            if (other.Notations.Count > 0)
            {
                note.Notations.Add(other.Notations.First());
            }
            return note;
        }





        private static Attributes CreateAttributes(HandType hand, int beatType)
        {
            if (hand == HandType.Both)
            {
                throw new Exception("Hand must be left or right.");
            }

            Attributes attributes = new Attributes()
            {
                Divisions = Divisions,
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
                Lang = "bla"
            });

            leftHandsPart.Measure[0].Direction.Add(direction);
            rightHandsPart.Measure[0].Direction.Add(direction);

            return scorePartwise;
        }

        public void SaveAsFile(string path)
        {
            File.WriteAllText(path, "");
            XmlSerializer serializer = new XmlSerializer(typeof(ScorePartwise));

            // Using string and WriteAllText
            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, scorePartwise);
                string scoreAsString = textWriter.ToString();

                // Replace synonyms by actual Backups
                StringBuilder builder = new StringBuilder(scoreAsString);
                //builder.Replace(" encoding", "");
                IEnumerable<int> backupIndexes = scoreAsString.AllIndexesOf("<footnote>Backup</footnote>").Reverse();
                foreach (int index in backupIndexes)
                {
                    /// Get duration
                    int durationEndIndex = scoreAsString.FindIndexBefore(index, "</duration>");
                    int durationStartIndex = scoreAsString.FindIndexBefore(index, "<duration>", true);
                    string durationString = builder.ToString(durationStartIndex, durationEndIndex - durationStartIndex);
                    int duration = Convert.ToInt32(durationString); // Make sure we have a double here
                    /// Get start and end index to replace
                    int startIndex = scoreAsString.FindIndexBefore(index, "<note>");
                    int endIndex = scoreAsString.FindIndexAfter(index, "</note>", true);
                    int length = endIndex - startIndex;
                    string toRemove = builder.ToString(startIndex, length);
                    /// Remove Backup-placeholder and insert actual Backup
                    builder.Remove(startIndex, length);
                    builder.Insert(startIndex, 
                          "<backup>\n"
                        + "         <duration>" + duration + "</duration>\n" // MUST NOT contain dot & decimals
                        + "      </backup>");
                }

                builder.Replace(" encoding=\"utf-16\"", "");
                scoreAsString = builder.ToString();
                File.WriteAllText(path, scoreAsString);
            }

            // Using FileStream
            //serializer.Serialize(new FileStream(path, FileMode.OpenOrCreate), scorePartwise);

            Console.WriteLine("Saved Music Xml here as " + path);
        }
    }


    /// <summary>
    ///   Presents a NoteLength by NoteTypeValue + Dotting.
    ///   This is a approximation of a time in seconds, thats why deviation exists giving the deviation from origin time
    /// </summary>
    public class NoteLength
    {
        public NoteTypeValue NoteTypeValue { get; }

        public Dotting Dotting { get; }

        public double ActualPortion { get; }

        public double ProposedPortion { get; }

        public bool HasDeviation => Deviation > 0.001;

        public double Deviation => MakeItHarderDict[Dotting] * Helper.GetPercentageDistance(ActualPortion, ProposedPortion);

        private static Dictionary<Dotting, double> MakeItHarderDict = new Dictionary<Dotting, double>()
        {
            [Dotting.None] = 1,
            [Dotting.Dotted] = 1,
            [Dotting.DoubleDotted] = 3,

            [Dotting.Plus16th] = 3,
            [Dotting.PlusEight] = 3,
            [Dotting.PlusQuarter] = 2,

            [Dotting.DottedPlus16th] = 3,
            [Dotting.DottedPlusEight] = 3,

            [Dotting.DoubleDottedPlus16th] = 4,
            [Dotting.DoubleDottedPlusEight] = 4,
        };

        public NoteLength(KeyValuePair<double, NoteTypeValue> valueForDuration, Dotting dotting, double actualPortion)
        {
            NoteTypeValue = valueForDuration.Value;
            Dotting = dotting;
            ActualPortion = actualPortion;
            ProposedPortion = FactorForDotting[dotting] * valueForDuration.Key;
        }

        public override string ToString()
        {
            string dottingString = "";
            if (Dotting.HasFlag(Dotting.Dotted))
            {
                dottingString += "•";
            }
            else if (Dotting.HasFlag(Dotting.DoubleDotted))
            {
                dottingString += "••";
            }

            if (Dotting.HasFlag(Dotting.PlusQuarter))
            {
                dottingString += "¼";
            }
            else if (Dotting.HasFlag(Dotting.PlusEight))
            {
                dottingString += "+1/8";
            }
            else if (Dotting.HasFlag(Dotting.Plus16th))
            {
                dottingString += "+1/16";
            }

            string deviationString = HasDeviation ? $"🗲{ProposedPortion.ToString(3)} statt {ActualPortion.ToString(3)})" : "✔";

            return $"{NoteTypeValue}{dottingString}[{deviationString}]";
        }

        // Static section
        //////////////////
        
        public static NoteLength CreateFromPortion(double portion)
        {
            List<NoteLength> noteLengths = new List<NoteLength>();
            /// If roundTolerance == 0.1 (10%), a note with 0.55 duration is only just converted to a note with 0.5 length (one half)
            foreach (KeyValuePair<double, NoteTypeValue> valueForDuration in NoteTypeValueForDuration)
            {
                void AddNoteLength(Dotting dotting)
                {
                    noteLengths.Add(new NoteLength(valueForDuration, dotting, portion));
                }

                AddNoteLength(Dotting.None);
                AddNoteLength(Dotting.Dotted);
                AddNoteLength(Dotting.DoubleDotted);

                AddNoteLength(Dotting.Plus16th);
                AddNoteLength(Dotting.PlusEight);
                AddNoteLength(Dotting.PlusQuarter);

                AddNoteLength(Dotting.DottedPlus16th);
                AddNoteLength(Dotting.DottedPlusEight);
                AddNoteLength(Dotting.DoubleDottedPlus16th);
                AddNoteLength(Dotting.DoubleDottedPlusEight);
            }

            // Get result with lowest deviation
            noteLengths = noteLengths.OrderBy(res => res.Deviation).ToList();
            return noteLengths.First();
        }

        public static Dictionary<double, NoteTypeValue> NoteTypeValueForDuration { get; } = new Dictionary<double, NoteTypeValue>()
            {
                {4, NoteTypeValue.Long },
                {2, NoteTypeValue.Breve },
                {1, NoteTypeValue.Whole },
                {1.0 / 2, NoteTypeValue.Half },
                {1.0 / 4, NoteTypeValue.Quarter },
                {1.0 / 8, NoteTypeValue.Eighth },
                {1.0 / 16, NoteTypeValue.Item16Th },
                {1.0 / 32, NoteTypeValue.Item32Nd },
                {1.0 / 64, NoteTypeValue.Item64Th },
                {1.0 / 128, NoteTypeValue.Item128Th },
                {1.0 / 256, NoteTypeValue.Item256Th },
                {1.0 / 512, NoteTypeValue.Item512Th },
                {1.0 / 1024, NoteTypeValue.Item1024Th },
            };

        public static Dictionary<NoteTypeValue, decimal> DurationForNoteTypeValue { get; } = new Dictionary<NoteTypeValue, decimal>()
        {
            [NoteTypeValue.Long] = 4,
            [NoteTypeValue.Breve] = 2,
            [NoteTypeValue.Whole] = 1,
            [NoteTypeValue.Half] = 1m / 2,
            [NoteTypeValue.Quarter] = 1m / 4,
            [NoteTypeValue.Eighth] = 1m / 8,
            [NoteTypeValue.Item16Th] = 1m / 16,
            [NoteTypeValue.Item32Nd] = 1m / 32,
            [NoteTypeValue.Item64Th] = 1m / 64,
            [NoteTypeValue.Item128Th] = 1m / 128,
            [NoteTypeValue.Item256Th] = 1m / 256,
            [NoteTypeValue.Item512Th] = 1m / 512,
            [NoteTypeValue.Item1024Th] = 1m / 1024,
        };

        public static Dictionary<Dotting, double> FactorForDotting { get; } = new Dictionary<Dotting, double>()
        {
            [Dotting.None] = 1,
            [Dotting.Dotted] = 1.5,
            [Dotting.DoubleDotted] = 1.75,

            [Dotting.Plus16th] = 1.0625,
            [Dotting.PlusEight] = 1.125,
            [Dotting.PlusQuarter] = 1.25,

            [Dotting.DottedPlus16th] = 1.5625,
            [Dotting.DottedPlusEight] = 1.625,

            [Dotting.DoubleDottedPlus16th] = 1.8125,
            [Dotting.DoubleDottedPlusEight] = 1.875,
        };
    }

    [Flags]
    public enum Dotting
    {
        None = 0,
        Dotted = 32,
        DoubleDotted = 64,

        Plus16th = 1,
        PlusEight = 2,
        PlusQuarter = 4,

        DottedPlus16th = Dotted | Plus16th,
        DottedPlusEight = Dotted | PlusEight,
        DoubleDottedPlus16th = DoubleDotted | Plus16th,
        DoubleDottedPlusEight = DoubleDotted | PlusEight,
    }
}
