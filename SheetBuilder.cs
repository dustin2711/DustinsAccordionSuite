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


/// <summary>
///   ToDo:
///     - Triolen einführen 8)
///     - Notenhälse oben umkehren (+ mehr Abstand zw. Zeilen?
///     - Ganz zum Schluss: Basstakt hat 6 Schläge
///     - Ganz zum Schluss: Bass-Beschriftung ist zu eng
/// </summary>
namespace CreateSheetsFromVideo
{
    public class SheetBuilder
    {
        public double MinimumNoteLength => beatDuration / 32;
        public double MinimumNoteLengthBass => beatDuration / 8;

        // Technical settings
        private const int OctaveOffset = -3;
        private const double MaxDeltaToMergeAbs = 0.05;
        private const double MaxDeltaToMergeRel = 0.2;
        private const HandType HandToCreateSheetsFor = HandType.Right;

        private ScorePartwise scorePartwise;

        /// <summary>
        ///   The counter of the current beat
        /// </summary>
        private int beatCounter = -1;
        private readonly double beatDuration;
        private Action<string> Log { get; }
        public SheetBuilder(Action<string> Log, SheetSave sheetSave, string title)
        {
            this.Log = Log;

            // Order tones by starttime
            List<Tone> tones = sheetSave.Tones.OrderBy(t => t.StartTime).ToList();

            // Get beat values
            beatDuration = sheetSave.CalcBeatDuration(out double firstBeatTime, out double lastBeatTime);
            double firstToneStartTime = tones.First().StartTime;
            double latestBeatTime = firstBeatTime - beatDuration; // Gets incremented each added beat

            // Split left | right
            GetLeftAndRightHandTones(tones, out List<Tone> leftTonesUnmerged, out List<Tone> rightTonesUnmerged);
            // Merge to chords
            List<Tone> leftTones = MergeChords(leftTonesUnmerged, firstBeatTime, beatDuration);
            List<Tone> rightTones = MergeChords(rightTonesUnmerged, firstBeatTime, beatDuration);

            // Create score
            scorePartwise = CreateScorePartwise(title, beatDuration, 
                out ScorePartwisePart leftPart, 
                out ScorePartwisePart rightPart);

            if (true)
            {
                // DEBUG
                rightTones = new List<Tone>()
                {
                    new Tone(new ToneHeight(Pitch.G, 7), startTime: 1, duration: 4),
                    new Tone(new ToneHeight(Pitch.D, 8), startTime: 1, duration: 3),
                    new Tone(new ToneHeight(Pitch.C, 8), startTime: 3, duration: 3.5),
                };
                beatDuration = 4;
                firstToneStartTime = 1;
                latestBeatTime = 5;

                //AddBeat();
                beatCounter = 0;
                AddNoteRight(rightTones[0]);
                AddBackupRight(1);
                AddNoteRight(rightTones[1]);
                AddNoteRight(rightTones[2]);
                return;
            }

            void AddBeat()
            {
                leftPart.Measure.Add(new ScorePartwisePartMeasure()
                {
                    Number = beatCounter.ToString(),
                    Width = 192
                });
                rightPart.Measure.Add(new ScorePartwisePartMeasure()
                {
                    Number = beatCounter.ToString(),
                    Width = 192
                });
                beatCounter++;
                latestBeatTime += beatDuration;
            }

            void AddNoteLeft(Tone tone)
            {
                AddTone(tone, leftPart);
                foreach (Tone chordTone in tone.ChordTones)
                {
                    AddTone(chordTone, leftPart);
                }
            }

            void AddNoteRight(Tone tone)
            {
                AddTone(tone, rightPart);
            }

            void AddTone(Tone tone, ScorePartwisePart part)
            {
                if (tone.Duration > 1.0 / 32)
                {
                    Note note = NoteFromTone(tone);
                    part.Measure[beatCounter].Note.Add(note);
                }
            }

            void AddBackupLeft(decimal duration)
            {
                leftPart.Measure[beatCounter].Backup.Add(new Backup() { Duration = duration });
            }

            void AddBackupRight(decimal duration)
            {
                rightPart.Measure[beatCounter].Backup.Add(new Backup() { Duration = duration });
            }

            // Adds pause to the right hand
            void AddRestRight(double duration)
            {
                rightPart.Measure[beatCounter].Note.Add(CreateRest(duration));
            }

            if (HandToCreateSheetsFor == HandType.Left)
            {
                rightTones.Clear();
            }
            else if (HandToCreateSheetsFor == HandType.Right)
            {
                leftTones.Clear();
            }

            // Add first beat
            AddBeat();
            double beatStartTime = firstBeatTime - beatDuration;
            double beatEndTime = beatStartTime + beatDuration;
            double firstPause = firstToneStartTime - beatStartTime;
            AddRestRight(firstPause);

            // Iterate tones
            int indexRight = 0;
            int indexLeft = 0;
            List<Tone> rightTonesCopy = new List<Tone>(rightTones);
            List<Tone> leftTonesCopy = new List<Tone>(leftTones);
            while (indexRight < rightTonesCopy.Count || indexLeft < leftTonesCopy.Count)
            {
                // Right hand
                while (indexRight < rightTonesCopy.Count && rightTonesCopy[indexRight].StartTime < beatEndTime)
                {
                    Tone tone = rightTonesCopy[indexRight++];
                    if (tone.EndTime < beatEndTime)
                    {
                        // Tone ends in beat
                        AddNoteRight(tone);
                    }
                    else
                    {
                        // Tone ends in next beat
                        Tone[] splits = tone.SplitTone(beatEndTime - tone.StartTime);
                        bool firstSplitIsTooShort = splits[0].Duration < MinimumNoteLength;
                        bool secondSplitIsTooShort = splits[1].Duration < MinimumNoteLength;
                        if (firstSplitIsTooShort && secondSplitIsTooShort)
                        {
                            continue;
                        }
                        else if (secondSplitIsTooShort && !firstSplitIsTooShort)
                        {

                            // Note will finish the current beat
                            splits[0].StartStop = null;
                            AddNoteRight(splits[0]);
                        }
                        else if (firstSplitIsTooShort && !secondSplitIsTooShort)
                        {
                            // Note will start the next beat
                            splits[1].StartStop = null;
                            rightTonesCopy.Insert(indexRight, splits[1]);
                        }
                        else
                        {
                            // Note is tied from current into next beat
                            AddNoteRight(splits[0]);
                            rightTonesCopy.Insert(indexRight, splits[1]);
                        }
                    }
                }

                // Left hand
                while (indexLeft < leftTonesCopy.Count && leftTonesCopy[indexLeft].StartTime < beatEndTime)
                {
                    Tone tone = leftTonesCopy[indexLeft++];

                    if (tone.EndTime < beatEndTime)
                    {
                        // Tone ends in beat
                        AddNoteLeft(tone);
                    }
                    else
                    {
                        // Tone ends in next beat
                        Tone[] splits = tone.SplitTone(beatEndTime - tone.StartTime);
                        bool firstSplitIsTooShort = splits[0].Duration < MinimumNoteLengthBass;
                        bool secondSplitIsTooShort = splits[1].Duration < MinimumNoteLengthBass;
                        if (firstSplitIsTooShort && secondSplitIsTooShort)
                        {
                            continue;
                        }
                        else if (secondSplitIsTooShort && !firstSplitIsTooShort)
                        {

                            // Note will finish the current beat
                            splits[0].StartStop = null;
                            AddNoteLeft(splits[0]);
                        }
                        else if (firstSplitIsTooShort && !secondSplitIsTooShort)
                        {
                            // Note will start the next beat
                            splits[1].StartStop = null;
                            leftTonesCopy.Insert(indexLeft, splits[1]);
                        }
                        else
                        {
                            // Note is tied from current into next beat
                            AddNoteLeft(splits[0]);
                            leftTonesCopy.Insert(indexLeft, splits[1]);
                        }
                    }
                }
                
                AddBeat();
                beatStartTime += beatDuration;
                beatEndTime += beatDuration;
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

            bool tones1AreLeftHand = GetMeanPitch(tones1) < GetMeanPitch(tones2);
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

        //public void AddNoteC(ScorePartwisePartMeasure measure)
        //{
        //    measure.Note.Add(new Note()
        //    {
        //        Pitch = new MusicXmlSchema.Pitch()
        //        {
        //            Step = Step.C,
        //            Alter = 0,
        //            Octave = "4" // 4 = Default
        //        },
        //        //Duration = 4, // Only needful for midi
        //        Type = new NoteType() { Value = NoteTypeValue.Whole }
        //    });
        //}

        //private Note CreateNote(string octave, decimal duration, NoteTypeValue value, Step step, decimal alter)
        //{
        //    return new Note()
        //    {
        //        Pitch = new MusicXmlSchema.Pitch()
        //        {
        //            Step = step,
        //            Alter = alter,
        //            AlterSpecified = true,
        //            Octave = octave.ToString() // 4 is default
        //        },
        //        Duration = duration, // Only needful for midi
        //        Type = new NoteType()
        //        {
        //            Value = value
        //        },
        //        Stem = new Stem() { Value = StemValue.Up },
        //    };
        //}

        private Note CreateRest(double duration)
        {
            return new Note()
            {
                Rest = new Rest(),
                Duration = (decimal)duration
            };
        }

        private Note NoteFromTone(Tone tone) 
        {
            int alter = 0;
            int octave = OctaveOffset + tone.Octave;

            string pitchString = tone.Pitch.ToString();

            // Find out step
            if (!Enum.TryParse(pitchString.Substring(0, 1), out Step step))
            {
                throw new Exception("Could not get Step-enum from pitch");
            }

            // Find out alter
            if (pitchString.Contains("is"))
            {
                alter = 1;
            }
            else if (pitchString.Contains("es"))
            {
                alter = -1;
            }

            // Find out duration ("NoteTypeValue")
            NoteTypeValue? noteTypeValue = null;
            Dotting dotting = Dotting.None;
            double noteDuration = tone.Duration / beatDuration;

            Dictionary<Dotting, double> FactorForDotting = new Dictionary<Dotting, double>()
            {
                [Dotting.None] = 1,
                [Dotting.Single] = 1.5,
                [Dotting.Double] = 1.75
            };
            List<NoteValueResult> results = new List<NoteValueResult>();
            /// If roundTolerance == 0.1 (10%), a note with 0.55 duration is only just converted to a note with 0.5 length (one half)
            foreach (var valueForDuration in NoteTypeValueForDurationDict)
            {
                void AddResult(Dotting dotting1)
                {
                    results.Add(new NoteValueResult(
                        valueForDuration.Value,
                        dotting1,
                        Helper.GetPercentageDistance(noteDuration, FactorForDotting[dotting1] * valueForDuration.Key)));
                }

                AddResult(Dotting.None);
                AddResult(Dotting.Single);
                AddResult(Dotting.Double);
            }
            results = results.OrderBy(res => res.deviation).ToList();
            noteTypeValue = results.First().noteTypeValue;

            if (noteTypeValue == null)
            {
                double wrongDuration = noteDuration;
                Debugger.Break();
                noteTypeValue = NoteTypeValue.Item128Th;
            }

            // Create note
            Note note = new Note()
            {
                Chord = (tone.IsPartOfAnotherChord) ? new Empty() : null,
                Pitch = new MusicXmlSchema.Pitch()
                {
                    Step = step,
                    Alter = alter,
                    AlterSpecified = alter != 0,
                    Octave = octave.ToString() // 4 is default
                },
                Duration = (decimal)noteDuration, // Only needful for midi
                Type = new NoteType()
                {
                    Value = noteTypeValue.Value
                },
                // ToDo: Stem > C3 not C
                Stem = new Stem() { Value = tone.Pitch > Pitch.C ? StemValue.Down : StemValue.Up },
            };

            if (tone.ChordTones.Count > 0)
            {
                string bassText = "_";
                if (tone.ChordTones.Count == 0)
                {
                    bassText = note.GetPitchString();
                }
                else if (tone.ChordTones.Count == 2)
                {
                    Pitch firstPitch = tone.Pitch;
                    Pitch secondPitch = tone.ChordTones[0].Pitch;
                    Pitch thirdPitch = tone.ChordTones[1].Pitch;

                    List<PitchIntegerPair> pairs = new List<PitchIntegerPair>()
                    {
                        new PitchIntegerPair(firstPitch),
                        new PitchIntegerPair(secondPitch),
                        new PitchIntegerPair(thirdPitch),
                    }.OrderBy(pair => pair.Integer).ToList();

                    int delta1 = pairs[1].Integer - pairs[0].Integer;
                    int delta2 = pairs[2].Integer - pairs[1].Integer;

                    Pitch pitch = Pitch.A;
                    AcchordType acchord = AcchordType.SeptMin;

                    // Dur
                    if (delta1 == 4 && delta2 == 3)
                    {
                        pitch = pairs[0].Pitch; // correct
                        acchord = AcchordType.Dur;
                    }
                    else if (delta1 == 3 && delta2 == 5)
                    {
                        pitch = pairs[2].Pitch;
                        acchord = AcchordType.Dur;
                    }
                    else if (delta1 == 5 && delta2 == 4)
                    {
                        pitch = pairs[1].Pitch;
                        acchord = AcchordType.Dur;
                    }
                    // Mol
                    else if (delta1 == 3 && delta2 == 4)
                    {
                        pitch = pairs[0].Pitch;
                        acchord = AcchordType.Mol;
                    }
                    else if (delta1 == 4 && delta2 == 5)
                    {
                        pitch = pairs[2].Pitch;
                        acchord = AcchordType.Mol;
                    }
                    else if (delta1 == 5 && delta2 == 3)
                    {
                        pitch = pairs[1].Pitch; // Correct
                        acchord = AcchordType.Mol;
                    }

                    bassText = pitch.ToString().RemoveDigits() + acchord.ToString();
                }

                // Add bassText as "Lyric"
                note.Lyric.Add(new Lyric()
                {
                    Text = new TextElementData()
                    {
                        Value = bassText,
                        // Value = firstPitch.ToString().RemoveDigits() + "/"
                        //    + secondPitch.ToString().RemoveDigits() + "/"
                        //    + thirdPitch.ToString().RemoveDigits(),
                        FontSize = "10"
                    }
                });
            }

            if (dotting == Dotting.Single)
            {
                note.Dot.Add(new EmptyPlacement());
            }
            else if (dotting == Dotting.Double)
            {
                note.Dot.Add(new EmptyPlacement());
                note.Dot.Add(new EmptyPlacement());
            }

            if (tone.StartStop.HasValue)
            {
                // Indicate tiing ("note.Tie.Add()" does not work)
                note.Notations.Add(new Notations());// { Tied = new System.Collections.ObjectModel.Collection<Tied>() { new Tied() { Type = TiedType.Stop } } });
                note.Notations.First().Tied.Add(new Tied() { Type = tone.StartStop.Value == StartStop.Start ? TiedType.Start : TiedType.Stop });
            }

            //string isOrEs = (alter == 1) ? "is" : ((alter == -1) ? "es" : "");
            //Console.WriteLine(tone.Pitch + " => " + step + isOrEs + octave);

            // For debugging
            if (beatCounter == 92)
            {
            }

            return note;
        }


        private static Dictionary<double, NoteTypeValue> NoteTypeValueForDurationDict = new Dictionary<double, NoteTypeValue>()
        {
            {4.0, NoteTypeValue.Long },
            {2.0, NoteTypeValue.Breve },
            {1.0, NoteTypeValue.Whole },
            {0.5, NoteTypeValue.Half },
            {0.25, NoteTypeValue.Quarter },
            {0.125, NoteTypeValue.Eighth },
            {0.0625, NoteTypeValue.Item16Th },
            {0.03125, NoteTypeValue.Item32Nd },
            {0.015625, NoteTypeValue.Item64Th },
            {0.0078125, NoteTypeValue.Item128Th },
        };

        private static List<Tone> MergeChords(List<Tone> tones, double firstBeatTime, double beatDuration)
        {
            List<Tone> mergesTones = new List<Tone>();

            for (int index = 0; index < tones.Count; index++)
            {
                Tone mainTone = tones[index++];

                int beatCounter = (int)((mainTone.StartTime - firstBeatTime) / beatDuration);
                if (beatCounter == 91)
                {
                }

                // Add part tones
                while (index < tones.Count)
                {
                    Tone partTone = tones[index];
                    // Same starttime, endtime & duration?
                    if (partTone.StartTime.IsAboutAbs(mainTone.StartTime, MaxDeltaToMergeAbs)
                        && partTone.EndTime.IsAboutAbs(mainTone.EndTime, MaxDeltaToMergeAbs)
                        && partTone.Duration.IsAboutRel(mainTone.Duration, MaxDeltaToMergeRel))
                    {
                        mainTone.ChordTones.Add(partTone);
                        partTone.IsPartOfAnotherChord = true;
                        index++;
                        partTone.StartTime = mainTone.StartTime;
                        partTone.Duration = mainTone.Duration;
                    }
                    else
                    {
                        index--;
                        break;
                    }
                }

                // ToBeHuman has no double achords, so handle that
                if (mainTone.ChordTones.Count == 1)
                {
                    // Collect lastTones (inclusive mainTone) for easy debugging
                    List<Tone> lastTones = new List<Tone>();
                    int mainToneIndex = tones.IndexOf(mainTone);
                    for (int i = mainToneIndex; i < mainToneIndex + 4 && i < tones.Count; i++)
                    {
                        lastTones.Add(tones[i]);
                    }

                    mainTone.Duration /= 2;
                    mainTone.ChordTones.Add(lastTones[2]);
                    lastTones[2].IsPartOfAnotherChord = true;
                    index++;

                    //Debugger.Break();
                }

                mergesTones.Add(mainTone);
            }

            return mergesTones;
        }

        private static Attributes CreateAttributes(HandType hand, int beatType)
        {
            if (hand == HandType.Both)
            {
                throw new Exception("Hand must be left or right.");
            }

            Attributes attributes = new Attributes()
            {
                Divisions = 1,
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

            //Right hand
            rightHandsPart = new ScorePartwisePart() { Id = RightHand };
            scorePartwise.Part.Add(rightHandsPart);
            ScorePartwisePartMeasure rightPartMeasure = new ScorePartwisePartMeasure()
            {
                Number = "1",
                Width = 192
            };
            rightHandsPart.Measure.Add(rightPartMeasure);

            // Left hand
            leftHandsPart = new ScorePartwisePart() { Id = LeftHand };
            scorePartwise.Part.Add(leftHandsPart);
            ScorePartwisePartMeasure leftPartMeasure = new ScorePartwisePartMeasure()
            {
                Number = "1",
                Width = 192
            };
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
            serializer.Serialize(new FileStream(path, FileMode.OpenOrCreate), scorePartwise);
            Console.WriteLine("Saved Music Xml here as " + path);
        }

        private enum Dotting { None, Single, Double }

        private class NoteValueResult
        {
            public NoteTypeValue noteTypeValue;
            public Dotting dotting;
            public double deviation;

            public NoteValueResult(NoteTypeValue noteTypeValue, Dotting dotting, double deviation)
            {
                this.noteTypeValue = noteTypeValue;
                this.dotting = dotting;
                this.deviation = deviation;
            }

            public override string ToString()
            {
                string dottingString = "";
                if (dotting == Dotting.Single)
                {
                    dottingString = ".";
                }
                else if (dotting == Dotting.Double)
                {
                    dottingString = "..";
                }
                return $"{noteTypeValue}{dottingString} : {deviation}";
            }
        }
    }
}
