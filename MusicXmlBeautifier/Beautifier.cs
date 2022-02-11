// Use downlaoded schema classes (works for Kass' Theme better)
using MusicXmlSchema;
using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;
using XmlNote = MusicXmlSchema.Note;
using CreateSheetsFromVideo;
using Pitch = CreateSheetsFromVideo.Pitch;

namespace MusicXmlBeautifier
{
    internal class Beautifier
    {
        internal static void AddBassLyrics(string musicXmlPath, bool removeStaccato = true)
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
            ScorePartwise scorePartwise = serializer.Deserialize(reader) as ScorePartwise;

            if (string.IsNullOrWhiteSpace(scorePartwise.MovementTitle))
            {
                scorePartwise.MovementTitle = Path.GetFileNameWithoutExtension(musicXmlPath);
            }

            int circleOfFifthsPosition = Convert.ToInt32(scorePartwise.Part[0].Measure[0].Attributes[0].Key[0].Fifths);

            // Go through parts
            int measureNumberXml = 0;
            for (int partNumber = 0; partNumber < Math.Min(2, scorePartwise.Part.Count); partNumber++)
            {
                // Go through measures (Takte)
                foreach (ScorePartwisePartMeasure measure in scorePartwise.Part[partNumber].Measure)
                {
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

                    measureNumberXml++;

                    // Remove staccatos of current measure
                    if (removeStaccato)
                    {
                        foreach (XmlNote note in measure.Note)
                        {
                            Articulations articulations = note.Notations.FirstOrDefault()?.Articulations.FirstOrDefault();
                            if (articulations != null)
                            {
                                articulations.Staccato.Clear();
                            }
                        }
                    }

                    /// Insert bass lyrics for staff "2"
                    // Get notes of current beat
                    List<XmlNote> notes = measure.Note.Where(note => note.Rest == null).ToList();
                    if (partNumber != 1)
                    {
                        notes = notes.Where(note => note.Staff == "2").ToList();
                    }

                    if (notes.Count > 0)
                    {
                        // Clear on new measure
                        List<List<Pitch>> previousPitchesList = new();

                        // Iterate notes
                        for (int i = 0; i < notes.Count; i++)
                        {
                            XmlNote mainNote = notes[i];

                            //if (mainNote.Rest != null)
                            //{
                            //    previousPitches = null;
                            //    continue;
                            //}

                            if (mainNote.IsChord() || mainNote.IsBackup)
                                continue;

                            List<Pitch> pitches = new();
                            pitches.Add(mainNote.Pitch.ThisAppsPitch);

                            for (int j = i + 1; j <= notes.Count; j++)
                            {
                                if (j < notes.Count && notes[j].IsChord())
                                {
                                    if (mainNote.Duration != notes[j].Duration
                                        || mainNote.Type.Value != notes[j].Type.Value)
                                    {
                                        // Chord notes must have same duration!
                                        Debugger.Break();
                                    }

                                    // Collect pitch
                                    pitches.Add(notes[j].Pitch.ThisAppsPitch);
                                }
                                else
                                {
                                    if (measureNumberXml == 4)
                                    { }


                                    // Remove redundant tones
                                    pitches = pitches.Distinct().ToList();

                                    // Set MainNote's lyrics
                                    string lyrics = SheetsBuilder.CreateBassLyrics(pitches, previousPitchesList, circleOfFifthsPosition, out bool _);

                                    // Pitches must not be equal to print lyrics
                                    if (previousPitchesList.Count == 0
                                        || !Helper.ListsEqual(previousPitchesList.LastOrDefault(), pitches))
                                    {
                                        previousPitchesList.Add(pitches);
                                        mainNote.Lyric.Add(new Lyric()
                                        {
                                            Text = new TextElementData()
                                            {
                                                Value = lyrics,
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
            }

            using StringWriter textWriter = new();
            string savePath = Path.ChangeExtension(musicXmlPath, null) + "(withBass)" + Path.GetExtension(musicXmlPath);

            SheetsBuilder.SaveScoreAsMusicXml(savePath, scorePartwise);
            Helper.OpenWithDefaultProgram(savePath);

            //Debugger.Break();
        }
    }
}
