using System.Collections.Generic;
using System.Drawing;

namespace CreateSheetsFromVideo
{
    public partial class MainForm
    {
        private static Brush BrushGray= new SolidBrush(Color.DarkGray);
        private static Brush BrushBlack = new SolidBrush(Color.Black);
        private static Brush BrushOrange = new SolidBrush(Color.Orange);
        private static Brush BrushTransparent = new SolidBrush(Color.Transparent);
        private static Pen PenBlack = new Pen(BrushBlack, 2);
        private static Pen PenGray = new Pen(BrushGray, 2);
        private List<int> lineHeights = new List<int>();

        private const float WidthPerSecond = 80f;
        private const int LineGap = 12;
        private const int FirstLineDistanceFromBorder = 7 * LineGap;

        private float GetDrawPositionX(double time)
        {
            return (float)((time - StartTime) * WidthPerSecond);
        }

        private void DrawBeatDash(float drawPosition, bool isMainBeat)
        {
            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                graphics.DrawLine(isMainBeat ? PenBlack : PenGray,
                    new PointF(drawPosition, 0),
                    new PointF(drawPosition, pictureBoxNotes.Height));
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

        private void DrawSheetSave(SheetSave save)
        {
            foreach (Tone tone in save.Tones)
            {
                DrawTone(tone);
            }
            foreach (double beatTime in save.BeatValues.GetBeatStartTimes(SheetsBuilder.BeatOffsetProportion))
            {
                double beatOffset = SheetsBuilder.BeatOffsetProportion * save.BeatValues.Duration;
                DrawBeatDash((int)((beatTime - save.OriginStartTime + beatOffset) * WidthPerSecond), true);
            }
            textBoxBeatDuration.Text = "Beat Dur = " + save.BeatValues.Duration.ToString(3);
        }

        private void DrawTone(Tone tone)
        {
            const int KeyOffset = 51;
            float width = (float)(tone.Duration * WidthPerSecond); // Note width in pixels
            float drawingPosX = GetDrawPositionX(tone.EndTime) - width;
            int keyNumber = tone.ToneHeight.GetKeyNumber();
            string debugString = tone.ToneHeight + " => " + keyNumber;
            float drawingPosY = FirstLineDistanceFromBorder + (KeyOffset - tone.ToneHeight.GetKeyNumber()) * 0.5f * LineGap;

            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                RectangleF rect = new RectangleF(
                    x: drawingPosX,
                    y: drawingPosY,
                    width: width,
                    height: LineGap);// * 0.84f);
                graphics.DrawEllipse(PenBlack, rect);
                graphics.FillEllipse(new SolidBrush(tone.Color), rect);

                // Draw dash for *es and * is
                PointF midPosition = new PointF(drawingPosX, drawingPosY + 0.5f * LineGap);
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
            }
        }

        private void ResetProgress()
        {
            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                graphics.FillRectangle(BrushTransparent,
                    x: 0.5f * pictureBoxNotes.Width,
                    y: pictureBoxNotes.Height - 2,
                    width: pictureBoxNotes.Width,
                    height: 2);
            }
        }

        private void DrawProgress()
        {
            using (Graphics graphics = Graphics.FromImage(pictureBoxNotes.Image))
            {
                graphics.FillRectangle(BrushOrange,
                    x: GetDrawPositionX(CurrentTime),// + save.BeatDuration),
                    y: pictureBoxNotes.Height - 2,
                    width: 2, 
                    height: 2);
            }
        }
    }
}
