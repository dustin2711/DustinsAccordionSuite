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
        ///   Searches fot the next "duration" element after the root element and returns its value
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
