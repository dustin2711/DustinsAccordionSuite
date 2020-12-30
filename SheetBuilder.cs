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
        public const double BeatOffsetProportion = -0.155; // Positive = shifts Beats right
        private const int OctaveOffset = -2;
        private const int Divisions = 1; // Divisions per Quarter (MuseScore does not care about this value)
        private const double MaxDeltaToMergeAbs = 0.05;
        private const double MaxDeltaToMergeRel = 0.2;
        private const HandType HandToCreateSheetsFor = HandType.Right;

        private ScorePartwise scorePartwise;
        private List<Tone> leftTones;
        private List<Tone> rightTones;

        /// <summary>
        ///   The counter of the current beat
        /// </summary>
        private int beatNumber = 2; // "measure"

        private List<Beat> GenerateBeats(List<Tone> tonesIn, BeatValues beatValues)
        {
            // Copy tones
            List<Tone> tonesForNextBeat = new List<Tone>();

            List<ToneBeats> toneBeats = new List<ToneBeats>();

            // Iterate beat start times
            foreach (double beatStartTime in beatValues.GetBeatStartTimes(BeatOffsetProportion))
            {
                double beatEndTime = beatStartTime + beatValues.Duration;

                List<Tone> currentBeatTones = new List<Tone>();
                List<Tone> tones = tonesIn.Concat(tonesForNextBeat).ToList();
                tonesForNextBeat.Clear();

                foreach (Tone tone in tones)
                {
                    // Tone starts in beat?
                    if (beatStartTime <= tone.StartTime && tone.StartTime < beatEndTime)
                    {
                        // Yes: Tone ends in beat?
                        if (beatStartTime <= tone.EndTime && tone.EndTime < beatEndTime)
                        {
                            // Yes: add
                            currentBeatTones.Add(tone);
                        }
                        else
                        {
                            //No : split
                            Tone[] splits = tone.SplitTone(beatEndTime - tone.StartTime, MinimumNoteLength);
                            if (splits[0] != null)
                            {
                                currentBeatTones.Add(splits[0]);
                            }
                            if (splits[1] != null)
                            {
                                tonesForNextBeat.Add(splits[1]);
                            }
                        }
                    }
                }

                // Convert tones to beatNotes
                List<BeatNote> beatNotes = new List<BeatNote>();
                foreach (Tone tone in currentBeatTones)
                {
                    BeatNote note;
                    if (tone is TiedTone tiedTone)
                    {
                        note = new TiedBeatNote(tone, beatStartTime, beatValues.Duration);
                    }
                    else
                    {
                        note = new BeatNote(tone, beatStartTime, beatValues.Duration);
                    }
                    beatNotes.Add(note);

                    if (note.Tone == null)
                    {
                    }
                    if (tone.BeatNote == null)
                    {
                    }
                }

                foreach (BeatNote beatNote in beatNotes)
                {
                    // Make tiing
                    if (beatNote is TiedBeatNote tiedNote)
                    {
                        bool needsNoteAfter = tiedNote.TiedTone.ToneAfter != null;
                        bool needsNoteBefore = tiedNote.TiedTone.ToneBefore != null;
                        if (needsNoteAfter)
                        {
                            tiedNote.SetNoteAfter(tiedNote.TiedTone.ToneAfter.TiedNote);
                        }
                        if (needsNoteBefore)
                        {
                            tiedNote.SetNoteBefore(tiedNote.TiedTone.ToneBefore.TiedNote);
                        }

                        if (needsNoteAfter && tiedNote.NoteAfter == null)
                        {
                        }
                        if (needsNoteBefore && tiedNote.NoteBefore == null)
                        {
                        }
                        if (tiedNote.NoteAfter == null || tiedNote.NoteBefore == null)
                        {
                        }
                    }
                }

                //beatNotes.Reverse();
                //foreach (BeatNote beatNote in beatNotes)
                //{
                //    // Make tiing
                //    if (beatNote is TiedBeatNote tiedNote)
                //    {
                //        tiedNote.SetNoteAfter(tiedNote.TiedTone.ToneAfter?.TiedNote);
                //        tiedNote.SetNoteBefore(tiedNote.TiedTone.ToneBefore?.TiedNote);

                //        if (tiedNote.NoteAfter == null || tiedNote.NoteBefore == null)
                //        {
                //        }
                //    }
                //}

                // Add beat
                toneBeats.Add(new ToneBeats()
                {
                });

            }

            List<Beat> beats = new List<Beat>();
            toneBeats.Add(new Beat(beatNumber, beats, beatValues));
            return beats;
        }

        class ToneBeats
        {
            public int number;
            public List<Tone> tones;
        }

        private SheetSave Save;
        private double BeatDuration => Save.BeatValues.Duration;

        public SheetsBuilder(SheetSave save, string title)
        {
            Save = save;
            /// There must be tones
            Debug.Assert(save.Tones.Count > 0);

            // Order tones by starttime
            List<Tone> tones = save.Tones.OrderBy(t => t.StartTime).ToList();
            // Calc beatTimes
            double latestBeatTime = save.BeatValues.FirstBeatStartTime - save.BeatValues.Duration; // Gets incremented each added beat
            List<double> beatTimes = save.BeatValues.GetBeatStartTimes(BeatOffsetProportion);
            // Split left | right
            GetLeftAndRightHandTones(tones, out List<Tone> leftTonesUnmerged, out List<Tone> rightTonesUnmerged);
            // Merge to chords
            leftTones = MergeChords(leftTonesUnmerged, save.BeatValues.FirstBeatStartTime, save.BeatValues.Duration);
            rightTones = MergeChords(rightTonesUnmerged, save.BeatValues.FirstBeatStartTime, save.BeatValues.Duration);

            List<Beat> beats = GenerateBeats(rightTones, save.BeatValues);

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
                latestBeatTime += BeatDuration;
            }

            void AddNoteLeft(Tone tone)
            {
                AddNote(tone, leftPart);
                foreach (Tone chordTone in tone.ChordTones)
                {
                    AddNote(chordTone, leftPart);
                }
            }

            void AddNoteRight(Tone tone)
            {
                AddNote(tone, rightPart);
            }

            void AddBeatNoteRight(BeatNote beatNote)
            {
                foreach (Note note in NotesFromBeatNote(beatNote))
                {
                    rightPart.Measure[beatNumber - 2].Note.Add(note);
                }
            }

            void AddNote(Tone tone, ScorePartwisePart part)
            {
                foreach (Note note in NotesFromTone(tone))
                {
                    part.Measure[beatNumber - 2].Note.Add(note);
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
                rightPart.Measure[beatNumber - 2].Note.Add(new Note()
                {
                    Duration = Divisions * 4,
                    Footnote = new FormattedText() { Value = "Backup"}
                });
            }

            // Go through Beats
            beats = beats.Skip(SkipBeats).ToList();
            foreach (Beat beat in beats)
            {
                if (beat != beats.First())
                {
                    AddBeat();
                }

                foreach (BeatPart beatPart in beat.BeatParts)
                {
                    foreach (BeatNote tone in beatPart.Notes)
                    {
                        AddBeatNoteRight(tone);
                    }

                    // Insert backup
                    if (beatPart.Notes.Count > 0)
                    {
                        AddBackupRight();
                    }
                }
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

        private Note[] NotesFromBeatNote(BeatNote beatNote)
        {
            // Create note
            Note note;
            if (beatNote is BeatRest)
            {
                note = new Note()
                {
                    Rest = new Rest(),
                    Duration = (decimal)beatNote.Length,
                    Type = new NoteType()
                    {
                        Value = beatNote.NoteTypeValue
                    },
                    Voice = beatNote.BeatPart.VoiceId.ToString()
                };
            }
            else
            {
                int alter = beatNote.Tone.Pitch.GetAlter();
                note = new Note()
                {
                    Chord = (beatNote.Tone.IsPartOfAnotherChord) ? new Empty() : null,
                    Pitch = new MusicXmlSchema.Pitch()
                    {
                        Step = beatNote.Tone.Pitch.GetStep(),
                        Alter = alter,
                        AlterSpecified = alter != 0,
                        Octave = (OctaveOffset + beatNote.Tone.Octave).ToString() // 4 is default
                    },
                    Duration = (decimal)beatNote.Length,
                    Type = new NoteType()
                    {
                        Value = beatNote.NoteTypeValue
                    },
                    Stem = new Stem() { Value = beatNote.Tone.Pitch > Pitch.C ? StemValue.Down : StemValue.Up },
                    Voice = beatNote.BeatPart.VoiceId.ToString()
                };
            }

            // Handle tiing
            if (beatNote is TiedBeatNote tiedNote)
            {
                // btw "note.Tie.Add()" does not work
                Notations notations = new Notations()
                {
                    Tied =
                    {
                        new Tied()
                        {
                            Type = tiedNote.TiedType
                        }
                    }
                };
                note.Notations.Add(notations);// { Tied = new System.Collections.ObjectModel.Collection<Tied>() { new Tied() { Type = TiedType.Stop } } });
            }

            switch (beatNote.Dotting)
            {
                case Dotting.Single:
                    note.Dot.Add(new EmptyPlacement());
                    break;
                case Dotting.Double:
                    note.Dot.Add(new EmptyPlacement());
                    note.Dot.Add(new EmptyPlacement());
                    break;
                case Dotting.PlusQuarter:
                    /// Split tone into whole and quarter 
                    Note wholeNote = CopyNote(note);
                    wholeNote.Duration = 0.8m * note.Duration;

                    Debug.Assert((int)note.Type.Value >= (int)NoteTypeValue.Item256Th); // Else cannot take quarter

                    Note quarterNote = CopyNote(note);
                    quarterNote.Duration = 0.2m * note.Duration;
                    quarterNote.Type.Value = note.Type.Value.Previous().Previous();

                    if (beatNote is TiedBeatNote tiedNote2)
                    {
                        if (tiedNote2.TiedType == TiedType.Start)
                        {
                            quarterNote.Notations.First().Tied.First().Type = TiedType.Continue;
                        }
                        else if (tiedNote2.TiedType == TiedType.Stop)
                        {
                            wholeNote.Notations.First().Tied.First().Type = TiedType.Continue;
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }

                    return new Note[] { wholeNote, quarterNote };
            }

            return new Note[] { note };
        }


        private Note[] NotesFromTone(Tone tone)
        {
            int alter = tone.Pitch.GetAlter();
            int octave = OctaveOffset + tone.Octave;

            double noteLength = tone.Duration / Save.BeatValues.Duration;
            NoteLength lengthHolder = NoteLength.CreateFromDuration(noteLength);

            // Create note
            Note note = new Note()
            {
                Chord = (tone.IsPartOfAnotherChord) ? new Empty() : null,
                Pitch = new MusicXmlSchema.Pitch()
                {
                    Step = tone.Pitch.GetStep(),
                    Alter = alter,
                    AlterSpecified = alter != 0,
                    Octave = octave.ToString() // 4 is default
                },
                Duration = lengthHolder.GetMusicXmlDuration(Divisions),
                Type = new NoteType()
                {
                    Value = lengthHolder.NoteTypeValue
                },
                Stem = new Stem() { Value = tone.Pitch > Pitch.C ? StemValue.Down : StemValue.Up },
                Voice = tone.BeatPart.VoiceId.ToString()
            };

            if (tone is RestTone restTone)
            {
                note.Rest = new Rest();
                note.Chord = null;
                note.Pitch = null;
                note.Stem = null;
            }

            // Add chord text for left hand (e.g. Cmol)
            if (leftTones.Contains(tone) && tone.ChordTones.Count > 0)
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
                    AcchordType acchordType = AcchordType.SeptMin;

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

                    bassText = $"{pitch}{acchordType}";
                }

                // Add bassText as "Lyric"
                note.Lyric.Add(new Lyric()
                {
                    Text = new TextElementData()
                    {
                        Value = bassText,
                        FontSize = "10"
                    }
                });
            }

            // Handle tiing
            if (tone is TiedTone tiedTone)
            {
                // btw "note.Tie.Add()" does not work
                Notations notations = new Notations()
                {
                    Tied =
                    {
                        new Tied()
                        {
                            Type = tiedTone.TiedType
                        }
                    }
                };
                note.Notations.Add(notations);// { Tied = new System.Collections.ObjectModel.Collection<Tied>() { new Tied() { Type = TiedType.Stop } } });
            }

            switch (lengthHolder.Dotting)
            {
                case Dotting.Single:
                    note.Dot.Add(new EmptyPlacement());
                    break;
                case Dotting.Double:
                    note.Dot.Add(new EmptyPlacement());
                    note.Dot.Add(new EmptyPlacement());
                    break;
                case Dotting.PlusQuarter:
                    /// Split tone into whole and quarter 
                    Note wholeNote = CopyNote(note);
                    wholeNote.Duration = 0.8m * note.Duration;

                    Debug.Assert((int)note.Type.Value >= (int)NoteTypeValue.Item256Th); // Else cannot take quarter

                    Note quarterNote = CopyNote(note);
                    quarterNote.Duration = 0.2m * note.Duration;
                    quarterNote.Type.Value = note.Type.Value.Previous().Previous();

                    if (tone is TiedTone split)
                    {
                        if (split.TiedType == TiedType.Start)
                        {
                            quarterNote.Notations.First().Tied.First().Type = TiedType.Continue;
                        }
                        else if (split.TiedType == TiedType.Stop)
                        {
                            wholeNote.Notations.First().Tied.First().Type = TiedType.Continue;
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }

                    return new Note[] { wholeNote, quarterNote };
            }

            return new Note[] { note };
        }

        private static Note CopyNote(Note other)
        {
            Note note = new Note()
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
        public NoteTypeValue NoteTypeValue;
        public Dotting Dotting;

        protected NoteLength() { }

        public NoteLength(NoteTypeValue noteTypeValue, Dotting dotting)
        {
            NoteTypeValue = noteTypeValue;
            Dotting = dotting;
        }

        public override string ToString()
        {
            string dottingString = "";
            if (Dotting == Dotting.Single)
            {
                dottingString = ".";
            }
            else if (Dotting == Dotting.Double)
            {
                dottingString = "..";
            }
            return $"{NoteTypeValue}{dottingString}";
        }

        public static NoteLength CreateFromDuration(double noteDuration)
        {
            List<NoteLengthWithDeviation> noteLengths = new List<NoteLengthWithDeviation>();
            /// If roundTolerance == 0.1 (10%), a note with 0.55 duration is only just converted to a note with 0.5 length (one half)
            foreach (KeyValuePair<double, NoteTypeValue> valueForDuration in NoteTypeValueForDurationDict)
            {
                void AppendCurrentNoteDuration(Dotting dotting)
                {
                    double distance = (dotting == Dotting.Double ? 4 : 1) // Make it harder to get double dotting (cause they suck)
                        * Helper.GetPercentageDistance(noteDuration, FactorForDotting[dotting] * valueForDuration.Key);
                    noteLengths.Add(new NoteLengthWithDeviation(valueForDuration.Value, dotting, distance));
                }

                AppendCurrentNoteDuration(Dotting.None);
                AppendCurrentNoteDuration(Dotting.PlusQuarter);
                AppendCurrentNoteDuration(Dotting.Single);
                AppendCurrentNoteDuration(Dotting.Double);
            }

            // Get result with lowest deviation
            noteLengths = noteLengths.OrderBy(res => res.Deviation).ToList();
            NoteLengthWithDeviation winner = noteLengths.First();
            return new NoteLength(winner.NoteTypeValue, winner.Dotting);
        }

        public decimal GetMusicXmlDuration(decimal divisions)
        {
            return 4 * divisions * DurationForNoteTypeValue[NoteTypeValue] * (decimal)FactorForDotting[Dotting];
        }

        public static Dictionary<double, NoteTypeValue> NoteTypeValueForDurationDict = new Dictionary<double, NoteTypeValue>()
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

        public static readonly Dictionary<NoteTypeValue, decimal> DurationForNoteTypeValue = new Dictionary<NoteTypeValue, decimal>()
        {
            [NoteTypeValue.Long] = 4m,
            [NoteTypeValue.Breve] = 2m,
            [NoteTypeValue.Whole] = 1m,
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


        public static readonly Dictionary<Dotting, double> FactorForDotting = new Dictionary<Dotting, double>()
        {
            [Dotting.None] = 1,
            [Dotting.PlusQuarter] = 1.25,
            [Dotting.Single] = 1.5,
            [Dotting.Double] = 1.75
        };
    }

    public class NoteLengthWithDeviation : NoteLength
    {
        public double Deviation { get; }

        public NoteLengthWithDeviation(NoteTypeValue noteTypeValue, Dotting dotting, double deviation) : base(noteTypeValue, dotting)
        {
            Deviation = deviation;
        }

        public override string ToString()
        {
            return base.ToString() + " : " + Deviation;
        }
    }
}
