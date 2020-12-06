using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    [Serializable]
    public class SheetSave
    {
        public List<Tone> tones = new List<Tone>();
        public List<double> beatTimes = new List<double>();

        protected SheetSave() { }

        public SheetSave(List<Tone> tones, List<double> beatTimes)
        {
            this.tones = tones;
            this.beatTimes = beatTimes;
        }
    }
}
