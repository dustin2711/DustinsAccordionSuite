using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicXmlSchema
{
    public static class MusicXmlExtensions
    {
        public static bool IsChordBaseNote(this Note note)
        {
            return note.Chord == null;
        }

        /// <summary>
        ///   True if this note belongs to another note to form a chord.
        /// </summary>
        public static bool IsChordSideNote(this Note note)
        {
            return note.Chord != null;
        }
    }
}
