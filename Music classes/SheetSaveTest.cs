using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSheetsFromVideo
{
    public class SheetSaveTest
    {
        public static SheetSave BeatWith2Voices
        {
            get
            {
                SheetSave save = new SheetSave( 
                    name: "2 Voices", 
                    originStartTime: 0,
                    tones: new List<Tone>()
                    {
                        new Tone(ToneHeight.C6, 0, 1),
                        new Tone(ToneHeight.D6, 0, 0.5),
                        new Tone(ToneHeight.E6, 0.5, 2.0),
                    },
                    beatHits: new List<BeatHit>()
                    {
                        new BeatHit(true, 0),
                        new BeatHit(true, 1),
                        new BeatHit(true, 2),
                        new BeatHit(true, 3),
                    });
                foreach (Tone tone in save.Tones)
                {
                    tone.Color = Color.Green;
                }
                return save;
            }
        }

        /// <summary>
        ///   Use 1/8
        /// </summary>
        public static SheetSave AnchorTest
        {
            get
            {
                SheetSave save = new SheetSave(
                    name: "AnchorTest",
                    originStartTime: 0,
                    tones: new List<Tone>()
                    {
                        new Tone(ToneHeight.C6, 0.05, 0.10),
                        new Tone(ToneHeight.D6, 0.125, 0.25),
                        new Tone(ToneHeight.E6, 0.27, 0.44),
                        new Tone(ToneHeight.F6, 0.50, 1.00),
                    },
                    beatHits: new List<BeatHit>()
                    {
                        new BeatHit(true, 0),
                        new BeatHit(true, 1),
                        new BeatHit(true, 2),
                        new BeatHit(true, 3),
                        new BeatHit(true, 4),
                        new BeatHit(true, 5),
                    });
                foreach (Tone tone in save.Tones)
                {
                    tone.Color = Color.Green;
                }
                return save;
            }
        }
    }
}
