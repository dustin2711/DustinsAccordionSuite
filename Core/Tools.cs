using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using XmlSchemaClassGenerator;
using System.Diagnostics;
using System.Xml;

// Use downlaoded schema classes (works for Kass' Theme better)
using MusicXmlSchema;
using XmlNote = MusicXmlSchema.Note;

// Use self-generated schema classes
//using MusicXml;
//using XmlNote = MusicXml.Note;

namespace CreateSheetsFromVideo
{
    public static class Tools
    {
        /// <summary>
        ///   Searches fot the next "duration" element and returns its value
        /// </summary>
        public static int FilterDuration(string text, int startIndex, string rootElementName)
        {
            startIndex = text.FindIndexBefore(startIndex, "<" + rootElementName);
            int durationStartIndex = text.FindIndexAfter(startIndex, "<duration>", true);
            int durationEndIndex = text.FindIndexAfter(startIndex, "</duration>");
            string durationString = text.Substring(durationStartIndex, durationEndIndex - durationStartIndex);
            int duration = Convert.ToInt32(durationString);
            return duration;
        }

        public static void AddBassLyrics(string musicXmlPath, bool removeStaccato = true)
        {
            StringBuilder builder = new StringBuilder(File.ReadAllText(musicXmlPath));
            string scoreString = builder.ToString();
            
            /// Replace backups by notes (to keep order)
            IEnumerable<int> backupStartIndexes = scoreString.AllIndexesOf("<backup>");
            foreach (int startIndex in backupStartIndexes.Reverse())
            {
                // Filter duration
                int duration = FilterDuration(scoreString, startIndex, "backup");

                // Remove backup
                int backupEndIndex = scoreString.FindIndexAfter(startIndex, "</backup>", true);
                string textThatWillBeRemoved = builder.ToString(startIndex, backupEndIndex - startIndex);
                builder.Remove(startIndex, backupEndIndex - startIndex);

                // Insert backup-note
                builder.Insert(startIndex,
                   "<note>\n"
                 + "         <duration>" + duration + "</duration>\n" // MUST NOT contain dot & decimals
                 +$"         <footnote>{SheetsBuilder.BackupFootnote}</footnote>"
                 + "      </note>");
            }

            using (StringReader reader = new StringReader(builder.ToString()))
            {
                // Create score from string
                XmlSerializer serializer = new XmlSerializer(typeof(ScorePartwise));
                ScorePartwise scorePartwise = serializer.Deserialize(reader) as ScorePartwise;

                if (string.IsNullOrWhiteSpace(scorePartwise.MovementTitle))
                {
                    scorePartwise.MovementTitle = System.IO.Path.GetFileNameWithoutExtension(musicXmlPath);
                    scorePartwise.MovementTitle = "MS Anne";
                }

                int circleOfFifthsPosition = Convert.ToInt32(scorePartwise.Part[0].Measure[0].Attributes[0].Key[0].Fifths);

                // Go through measures
                int measureNumberXml = 0;
                for (int partNumber = 0; partNumber < Math.Min(2, scorePartwise.Part.Count); partNumber++)
                {
                    foreach (ScorePartwisePartMeasure measure in scorePartwise.Part[partNumber].Measure)
                    {
                        //// Setup measure/Takt
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

                        foreach (XmlNote note in measure.Note)
                        {
                            if (removeStaccato)
                            {
                                Articulations articulations = note.Notations.FirstOrDefault()?.Articulations.FirstOrDefault();
                                if (articulations != null)
                                {
                                    articulations.Staccato.Clear();
                                }
                            }
                        }

                        /// Insert lyrics for staff "2"
                        // Get notes of current beat
                        List<XmlNote> notes = measure.Note.Where(note => note.Staff == "2"
                                                                      && note.Rest == null).ToList();
                        if (partNumber == 1 && notes.Count == 0)
                        {
                            // Do not consider rests :(
                            notes = measure.Note.Where(note => note.Rest == null).ToList();
                            //notes = measure.Note.ToList();
                        }

                        if (notes.Count > 0)
                        {
                            // Clear on new measure
                            List<List<Pitch>> previousPitchesList = new List<List<Pitch>>();

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

                                List<Pitch> pitches = new List<Pitch>();
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

                using (StringWriter textWriter = new StringWriter())
                {
                    string savePath = System.IO.Path.ChangeExtension(musicXmlPath, null) + "(withBass)" + System.IO.Path.GetExtension(musicXmlPath);

                    SheetsBuilder.SaveScoreAsMusicXml(savePath, scorePartwise);
                    Helper.OpenWithDefaultProgram(savePath);

                    Debugger.Break();
                }
            }
        }

        public static void CreateSchemaFiles()
        {
            string xsdPath = @"C:\Users\Dustin\Desktop\CreateXsd v2\musicxml-3.1\schema\musicxml.xsd";
            Generator generator = new Generator
            {
                OutputFolder = @"C:\Users\Dustin\Desktop\CreateXsd v2\Output",
                Log = s => Console.Out.WriteLine(s),
                // Important
                GenerateNullables = true,
                // PascalCase makes << practice, >> to << [System.Xml.Serialization.XmlEnumAttribute("practice")] Practice, >>
                //NamingScheme = NamingScheme.Direct,
                // True adds << public partial class miscellaneous >> (not important I think)
                //GenerateComplexTypesForCollections = true,
                // Generic types will be ignored
                //CollectionImplementationType = null, //typeof(IList),
                // Public allowes setting new collections (nice!)
                CollectionSettersMode = CollectionSettersMode.Public,
                // Removes Namespace-Prefix and uses Usings instead (nice but kills strings)
                //CompactTypeNames = true,
                // Value => Text (nice but this is realized by a simple string.Replace("Value", "Text") which also renames other Properties named "Value"
                //TextValuePropertyName = "Text",
                // Removes some Range and RegexAttributes (not good!)
                //DataAnnotationMode = DataAnnotationMode.None,
                // Add the "Order" property counting up to XmlElementAttribute (maybe its helpful, so keep it),
                EmitOrder = true,
                // Unneccessary since the same text is already added as /// comment
                //GenerateDescriptionAttribute = false,
                // Creates interface i will never use, e.g. Itext_decoration
                //GenerateInterfaces = true,
                // Adds the default MatchboxConfigUC-Style Properties with OnPropertyChanged
                //EnableDataBinding = false,
                // No difference
                //UseXElementForAny = true,
                // No difference
                //SeparateSubstitutes = true,
                // Allows custom variable Naming (snake_case => PascalCase)
                NamingProvider = new NamingProviderCustom(NamingScheme.PascalCase),
                // True will add some attributes and even id properties I wont ever use
                //EntityFramework = false,
                
                NamespaceProvider = new Dictionary<NamespaceKey, string>
                {
                    [new NamespaceKey("https://www.musicxml.com/de/for-developers/musicxml-xsd/")] = "MusicXml" 
                }
                .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "MusicXml" }.NamespaceProvider.GenerateNamespace)
            };

            generator.Generate(new List<string>()
            {
                xsdPath,

            });
        }
    }
    public class NamingProviderCustom : NamingProvider
    {
        private NamingScheme namingScheme;

        /// <summary>
        ///   Converts e.g. "test_variable_12_" to "testVariable12_"
        /// </summary>
        public static string MakePascal(string name)
        {
            string newName = "";
            for (int i = 0; i < name.Length; i++)
            {
                char currentChar = name[i];

                if (currentChar != '_' || i == name.Length - 1)
                {
                    newName += currentChar;
                }
                else
                {
                    // currentChar is '_'
                    char nextChar = name[i + 1];
                    if (char.IsLetter(nextChar))
                    {
                        newName += nextChar.ToString().ToUpper();
                        i++;
                    }
                }
            }

            return newName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamingProviderCustom"/> class.
        /// </summary>
        /// <param name="namingScheme">The naming scheme.</param>
        public NamingProviderCustom(NamingScheme namingScheme) : base(namingScheme)
        {
            this.namingScheme = namingScheme;
        }

        /// <summary>
        /// Creates a name for a property from an attribute name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>Name of the property</returns>
        public override string PropertyNameFromAttribute(string typeModelName, string attributeName)
        {
            return MakePascal(base.PropertyNameFromAttribute(typeModelName, attributeName));
        }

        /// <summary>
        /// Creates a name for a property from an element name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="elementName">Element name</param>
        /// <returns>Name of the property</returns>
        public override string PropertyNameFromElement(string typeModelName, string elementName)
        {
            return MakePascal(base.PropertyNameFromElement(typeModelName, elementName));
        }

        /// <summary>
        /// Creates a name for an enum member based on a value
        /// </summary>
        /// <param name="enumName">Name of the enum</param>
        /// <param name="value">Value name</param>
        /// <returns>Name of the enum member</returns>
        public override string EnumMemberNameFromValue(string enumName, string value)
        {
            return MakePascal(base.EnumMemberNameFromValue(enumName, value));
        }

        /// <summary>
        /// Used internally to make the QualifiedName have the desired naming schema.
        /// </summary>
        /// <param name="qualifiedName">Not null element.</param>
        /// <returns>A string formatted as desired.</returns>
        protected override string QualifiedNameToTitleCase(XmlQualifiedName qualifiedName)
        {
            return MakePascal(qualifiedName.Name.ToTitleCase(namingScheme));
        }
    }
}
