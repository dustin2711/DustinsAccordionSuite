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
                        new Tone(ToneHeight.E6, 0.5, 1.5),
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
    }
}
