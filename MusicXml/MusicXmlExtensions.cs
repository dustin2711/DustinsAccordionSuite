using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicXml
{
    public static class MusicXmlExtensions
    {
        public static bool IsChord(this Note note)
        {
            return note.Chord != null;
        }
    }
}
