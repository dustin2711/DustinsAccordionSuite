using MusicXmlSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateSheetsFromVideo
{

    /// <summary>
    ///   <see cref="CreateSheetsFromVideo.Tone"/>
    /// </summary>
    public class BeatNote
    {
        public BeatPart BeatPart;
        public Tone Tone;

        public double Start;
        public double End;

        public double Length
        {
            get => End - Start;
            set => End = Start + value;
        }

        public NoteLength NoteLength;

        [XmlIgnore]
        public NoteTypeValue NoteTypeValue => NoteLength.NoteTypeValue;
        [XmlIgnore]
        public Dotting Dotting => NoteLength.Dotting;

        protected BeatNote() { }

        public BeatNote(Tone tone, double beatStartTime, double beatDuration)
        {
            // Connect tone and note
            this.Tone = tone;
            tone.BeatNote = this;

            Start = (tone.StartTime - beatStartTime) / beatDuration;
            End = (tone.EndTime - beatStartTime) / beatDuration;
            NoteLength = NoteLength.CreateFromDuration(Length);
        }

        public override string ToString()
        {
            return $"{Tone.Pitch}{Tone.Octave}: {NoteLength} from {Start.ToString(5)} to {End.ToString(5)}";
        }
    }

    public class BeatRest : BeatNote
    {
        protected BeatRest() { }

        public BeatRest(
            BeatPart beatPart,
            double start, double length)
        {
            BeatPart = beatPart;
            Start = start;
            End = length;
            NoteLength = NoteLength.CreateFromDuration(Length);
        }
    }

    public class TiedBeatNote : BeatNote
    {
        public TiedTone TiedTone
        {
            get => Tone as TiedTone;
            set
            {
                TiedTone tiedTone = value as TiedTone;
                this.Tone = tiedTone;
                tiedTone.TiedNote = this;
            }
        }

        public TiedBeatNote NoteBefore;
        public TiedBeatNote NoteAfter;

        public void SetNoteBefore(TiedBeatNote tiedBeatNote)
        {
            this.NoteBefore = tiedBeatNote ?? throw new Exception();
            tiedBeatNote.NoteAfter = this;

            this.TiedTone.ToneBefore.TiedNote = NoteBefore;
            this.NoteBefore.TiedTone = this.TiedTone.ToneBefore;
        }

        public void SetNoteAfter(TiedBeatNote tiedBeatNote)
        {
            this.NoteAfter = tiedBeatNote ?? throw new Exception();
            tiedBeatNote.NoteBefore = this;

            this.TiedTone.ToneAfter.TiedNote = NoteAfter;
            this.NoteAfter.TiedTone = this.TiedTone.ToneAfter;
        }

        [XmlIgnore]
        public TiedType TiedType
        {
            get
            {
                if (NoteBefore != null && NoteAfter != null)
                {
                    return TiedType.Continue;
                }
                else if (NoteBefore != null)
                {
                    return TiedType.Stop;
                }
                else if (NoteAfter != null)
                {
                    return TiedType.Start;
                }
                else throw new Exception("No tied tone avilable");
            }
        }

        protected TiedBeatNote() { }

        public TiedBeatNote(Tone tone, double startTime, double beatDuration) : base(tone, startTime, beatDuration)
        {
        }
    }
}
