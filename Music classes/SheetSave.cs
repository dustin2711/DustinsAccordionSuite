using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateSheetsFromVideo
{
    [Serializable]
    public class SheetSave
    {
        public string Name;
        public double OriginStartTime = 0;
        public List<Tone> Tones = new List<Tone>();
        public List<BeatHit> BeatHits = new List<BeatHit>(); // Just for calculating
        public BeatValues BeatValues;

        protected SheetSave() { }

        public SheetSave(string name, double originStartTime, List<Tone> tones, List<BeatHit> beatHits)
        {
            Name = name;
            OriginStartTime = originStartTime;
            Tones = tones;
            BeatHits = beatHits;
            BeatValues = new BeatValues(BeatHits, tones, originStartTime);
        }

        public static SheetSave Load(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                SheetSave save = new XmlSerializer(typeof(SheetSave)).Deserialize(stream) as SheetSave;
                return save;
            }
        }

        public static void Save(string path, double StartTime, List<Tone> tones, List<BeatHit> hits)
        {
            File.WriteAllText(path, "");
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                SheetSave save = new SheetSave(Path.GetFileNameWithoutExtension(path), StartTime, tones, hits);
                new XmlSerializer(typeof(SheetSave)).Serialize(stream, save);
            }
        }

        public override string ToString()
        {
            return $"{Name}: Beat = {BeatValues.Duration}s with Start at {OriginStartTime}s and {Tones.Count} Tones";
        }
    }

    /// <summary>
    ///   Struct of StartTime + bool if MainBeat
    /// </summary>
    [Serializable]
    public struct BeatHit
    {
        public bool isMainBeat;
        public double startTime;

        public BeatHit(bool mainBeat, double startTime)
        {
            this.isMainBeat = mainBeat;
            this.startTime = startTime;
        }

        public override string ToString()
        {
            return $"{Extensions.ToString(startTime)} {(isMainBeat ? "(main)" : "")}";
        }
    }
}
