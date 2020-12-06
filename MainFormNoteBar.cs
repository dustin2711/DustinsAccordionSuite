using System.Collections.Generic;
using System.Drawing;

namespace CreateSheetsFromVideo
{
    public partial class MainForm
    {
        private int NoteBarWidth;
        private int NoteBarHeight;
        private static Brush BrushGray= new SolidBrush(Color.DarkGray);
        private static Brush BrushBlack = new SolidBrush(Color.Black);
        private static Pen PenBlack = new Pen(BrushBlack);
        private List<int> lineHeights = new List<int>();
        private int lineGap = 0;
        private int firstLineHeight;
        private const float WidthPerSecond = 80f;
        private const double SpacePercentage = 0.4; // above and below lines is each space of 0.3 * imageHeight

        private int CurrentDrawPositionX => (int)((CurrentTime - StartTime) * WidthPerSecond);

        private void DrawBeatDash()
        {
            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                graphics.DrawLine(PenBlack,
                    new Point(CurrentDrawPositionX, 0),
                    new Point(CurrentDrawPositionX, NoteBarHeight));
            }
        }

        /// <summary>
        ///   Draws the lines and prepares the image showing notes visually
        /// </summary>
        private void InitNoteImage()
        {
            NoteBarWidth = pictureBoxNotes.Width;
            NoteBarHeight = pictureBoxNotes.Height;

            // Collect line heights
            firstLineHeight = NoteBarHeight - (int)(SpacePercentage * NoteBarHeight);
            lineGap = (int)(SpacePercentage * NoteBarHeight / 5.0);
            for (int index = 0; index < 5; index++)
            {
                lineHeights.Add(firstLineHeight - lineGap * index);
            }

            pictureBoxNotes.Image = new Bitmap(NoteBarWidth, NoteBarHeight);
            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                // Draw 5 tone-lines
                foreach (int height in lineHeights)
                {
                    graphics.DrawLine(PenBlack, new PointF(0, height), new PointF(NoteBarWidth, height));
                }
            }
        }

        /// <summary>
        ///   Is calibrated on octave = 7
        /// </summary>
        private void DrawTone(Tone tone)
        {
            int yFromToneHeight(ToneHeight toneHeight)
            {
                return firstLineHeight + (int)((toneHeight.GetLineHeight() - (7 * 7 + 2)) * 0.5 * lineGap);
            }

            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                PointF position = new PointF( // 40|160
                        CurrentDrawPositionX,
                        yFromToneHeight(tone.ToneHeight));
                SizeF size = new SizeF( // 80|14
                        (float)(tone.Duration * WidthPerSecond),
                        lineGap);
                graphics.DrawEllipse(PenBlack, new RectangleF(position, size));
                graphics.FillEllipse(BrushGray, new RectangleF(position, size));
                Log("Pos = " + position + ", Size = " + size);
            }
        }
    }
}
