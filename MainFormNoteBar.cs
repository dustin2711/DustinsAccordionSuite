using System.Collections.Generic;
using System.Drawing;

namespace CreateSheetsFromVideo
{
    public partial class MainForm
    {
        private static Brush BrushGray= new SolidBrush(Color.DarkGray);
        private static Brush BrushBlack = new SolidBrush(Color.Black);
        private static Pen PenBlack = new Pen(BrushBlack, 2);
        private static Pen PenGray = new Pen(BrushGray, 2);
        private List<int> lineHeights = new List<int>();

        private const float WidthPerSecond = 80f;
        private const int LineGap = 12;
        private const int FirstLineDistanceFromBorder = 3 * LineGap;

        private int CurrentDrawPositionX => (int)((CurrentTime - StartTime) * WidthPerSecond);

        private void DrawBeatDash(bool mainBeat)
        {
            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                graphics.DrawLine(mainBeat ? PenBlack : PenGray,
                    new Point(CurrentDrawPositionX, 0),
                    new Point(CurrentDrawPositionX, pictureBoxNotes.Height));
            }
        }

        /// <summary>
        ///   Draws the lines and prepares the image showing notes visually
        /// </summary>
        private void InitializeNoteBar()
        {
            // Calc heights for right hand..
            for (int index = 0; index < 5; index++)
            {
                lineHeights.Add(FirstLineDistanceFromBorder + LineGap * index);
            }
            // ..and left hand
            for (int index = 0; index < 5; index++)
            {
                lineHeights.Add(FirstLineDistanceFromBorder + LineGap * (index + 7));
            }

            pictureBoxNotes.Image = new Bitmap(pictureBoxNotes.Width, pictureBoxNotes.Height);
            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                // Draw 5 tone lines
                foreach (int height in lineHeights)
                {
                    graphics.DrawLine(PenBlack, 
                        new PointF(0, height), 
                        new PointF(pictureBoxNotes.Width, height));
                }
            }
        }

        private void DrawTones(List<Tone> tones)
        {
            foreach (Tone tone in tones)
            {
                DrawTone(tone);
            }
        }

        /// <summary>
        ///   Is calibrated on octave = 7
        /// </summary>
        private void DrawTone(Tone tone)
        {
            int GetDrawingHeight(ToneHeight toneHeight)
            {
                // The higher the key number
                int keyNumber = toneHeight.GetKeyNumber();
                int offset = (int)((51 - keyNumber) * 0.5 * LineGap); // 51 * 0.5 * 12
                //Log(toneHeight + ": " + keyNumber + " (" + (IsRightHand(tone) ? "R" : "L") + ", " + offset + ")");
                return FirstLineDistanceFromBorder + offset;
            }

            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                float width = (float)(tone.Duration * WidthPerSecond); // Note width in pixels
                PointF position = new PointF( // e.g. 40|160
                        (int)((tone.EndTime - StartTime) * WidthPerSecond),
                        GetDrawingHeight(tone.ToneHeight));
                SizeF size = new SizeF(width, LineGap); // e.g. 80|14
                graphics.DrawEllipse(PenBlack, new RectangleF(position, size));
                graphics.FillEllipse(new SolidBrush(tone.Color), new RectangleF(position, size));

                PointF midPosition = new PointF(position.X, position.Y + 0.5f * LineGap);
                switch (tone.Pitch.Alter())
                {
                    case 1:
                        midPosition.Y -= 0.25f * LineGap;
                        graphics.DrawLine(PenBlack, midPosition, new PointF(midPosition.X + width, midPosition.Y));
                        break;
                    case -1:
                        midPosition.Y += 0.25f * LineGap;
                        graphics.DrawLine(PenBlack, midPosition, new PointF(midPosition.X + width, midPosition.Y));
                        break;
                }
                //Log("Pos = " + position + ", Size = " + size);
            }
        }
    }
}
