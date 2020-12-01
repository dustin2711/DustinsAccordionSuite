using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using tessnet2;
using System.Threading;
using MusicXmlSchema;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using MediaToolkit.Model;
using MediaToolkit;
using MediaToolkit.Options;

namespace CreateSheetsFromVideo
{
    enum AppMode
    {
        Save, // Play and SAVE
        Load // LOAD and evaluate
    }

    /// <summary>
    ///   Color of which tones shall be noted?
    /// </summary>
    enum ColorMode
    {
        /// <summary>
        ///   Collect only hue < 180
        /// </summary>
        Green,
        /// <summary>
        ///   Collect only hue > 180
        /// </summary>
        Blue,
        /// <summary>
        ///   Color does not matter for collecting tones
        /// </summary>
        All
    }

    /// <summary>
    ///   ToDo:
    ///   - Lange Note und währenddessen viele kleine
    ///   (- Rechts werden zu viele Noten angezeigt (3 obwohl 1 gespielt) ODER rechts/links vertauscht sich innerhalb des Stücks)
    /// </summary>
    public partial class MainForm : Form
    {
        // Serialize List<Tone> to file
        private const string TonesPath = @"C:\Users\Dustin\Desktop\tones";

        // Video file
        private const string VideoPath = @"C:\Users\Dustin\Desktop\Slider Yellow.mp4";

        // Settings
        private const ColorMode KeyColorMode = ColorMode.All;
        private const AppMode Mode = AppMode.Save;
        private const double StartTime = 4;
        private const double EndTime = double.MaxValue;
        private const bool IsPlayingDefault = false;
        private const bool PlayRealtimeDefault = true;
        private const bool ShowVisualsDefault = true;
        private const bool PrintOccuredTonesAgain = false;

        private bool isPlaying = IsPlayingDefault;
        private long maximumPlayedFrameIndex;
        private long frameIndex;
        private VideoFileReader videoReader = new VideoFileReader();
        private Bitmap frame;

        private List<Tone> tonesActive = new List<Tone>();
        private List<Tone> tonesPast = new List<Tone>();
        private List<PianoKey> pianoKeys;

        private List<double> beatTimes = new List<double>();

        //private void CreateFrames()
        //{
        //    // Accord.Video.FFMPEG
        //    using (var vFReader = new VideoFileReader())
        //    {
        //        vFReader.Open("video.mp4");
        //        for (int i = 0; i < vFReader.FrameCount; i++)
        //        {
        //            Bitmap bmpBaseOriginal = vFReader.ReadVideoFrame();
        //        }
        //        vFReader.Close();
        //    }

        //    // MediaToolkit
        //    using (Engine engine = new Engine())
        //    {
        //        MediaFile videoFile = new MediaFile
        //        {
        //            Filename = VideoPath
        //        };

        //        engine.GetMetadata(videoFile);

        //        var i = 0;
        //        while (i < videoFile.Metadata.Duration.Seconds)
        //        {
        //            var options = new ConversionOptions
        //            {
        //                Seek = TimeSpan.FromSeconds(i)
        //            };

        //            var outputFile = new MediaFile
        //            {
        //                //Filename = string.Format("{0}\\image-{1}.jpeg", outputPath, i)
        //            };

        //            engine.GetThumbnail(videoFile, outputFile, options);
        //            i++;
        //        }
        //    }
        //}

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();

            mediaPlayer.URL = VideoPath;
            mediaPlayer.Ctlcontrols.currentPosition = StartTime;
            mediaPlayer.Ctlcontrols.stop();
            //mediaPlayer.Ctlcontrols.currentPosition;
            mediaPlayer.Ctlcontrols.play();
            mediaPlayer.PositionChange += MediaPlayer_PositionChange;
            //mediaPlayer.Ctlcontrols.stop();

            if (Mode == AppMode.Load)
            {
                //tones = new List<Tone>() { new Tone() { Pitch = Pitch.A, Color = Color.Red } };
                //SaveTones(tonesPath);
                //var loaded = LoadTones(tonesPath);

                tonesPast = LoadTones(TonesPath);

                SheetBuilder builder = new SheetBuilder(tonesPast, Path.GetFileNameWithoutExtension(VideoPath));
                builder.SaveAsFile(Path.ChangeExtension(VideoPath, ".musicxml"));

                Load += (s, e) => Close();
                return;
            }

            Shown += (s, e) =>
            {
                LoadVideo(VideoPath);

                // Video play thread
                new Thread(() =>
                {
                    while (frameIndex < videoReader.FrameCount)
                    {
                        while (mediaPlayer.Ctlcontrols.currentPosition < CurrentTime)
                        {
                            Thread.Sleep(20);
                        }

                        PlayNextFrame();
                    }
                }).Start();

                RefreshUI();
            };
        }

        private void MediaPlayer_PositionChange(object sender, AxWMPLib._WMPOCXEvents_PositionChangeEvent e)
        {
            throw new NotImplementedException();
        }

        private bool ShowVisuals => checkBoxShowVisuals.Checked;
        private int FrameWidth => Frame.Width;
        private int FrameHeight => Frame.Height;
        private double FrameRate => videoReader.FrameRate.Value;
        private double FrameLength => 1.0 / videoReader.FrameRate.Value;
        private long FrameCount => videoReader.FrameCount;
        private double VideoDuration => videoReader.FrameCount / videoReader.FrameRate.Value;
        private double CurrentTime => frameIndex * FrameLength;

        private Bitmap Frame
        {
            get => frame;
            set
            {
                frame = value;
                if (checkBoxShowVisuals.Checked)
                {
                    pictureBox.Image = value;
                }
            }
        }

        private void InitializeUI()
        {
            checkBoxRealtime.Checked = PlayRealtimeDefault;
            checkBoxShowVisuals.Checked = ShowVisualsDefault;
            textBoxResetTime.Text = StartTime.ToString();

            buttonReset.Click += (s, e) => ResetToProgramStart();
            buttonPlay.Click += (s, e) => PlayPause();
            buttonNextFrame.Click += (s, e) => PlayNextFrame();
            buttonPreviousFrame.Click += (s, e) => PlayPreviousFrame();
            buttonClearNotesPast.Click += (s, e) => ClearNotesPast();
            buttonClearNotesActive.Click += (s, e) => textBoxTonesActiveLeft.Clear();
            textBoxJumpTo.KeyDown += TextBoxJumpTo_KeyDown;
            buttonClearBeatTimes.Click += (s, e) => ClearBeatTimes();
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
        }

        private void TextBoxJumpTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (double.TryParse(textBoxJumpTo.Text, out double time))
                {
                    frameIndex = (int)(time / FrameLength);
                    UpdateFrameToFrameIndex();
                }
            }
        }

        private void ResetToProgramStart()
        {
            mediaPlayer.Ctlcontrols.currentPosition = StartTime;

            tonesActive.Clear();
            tonesPast.Clear();

            textBoxTonesPastLeft.Clear();
            textBoxTonesActiveLeft.Clear();
            textBoxTonesRight.Clear();
            textBoxTonesRightActive.Clear();

            SetFrameIndexToStart();
            PlayNextFrame();
        }

        private void ClearNotesPast()
        {
            textBoxTonesPastLeft.Clear();
            textBoxLog.Clear();
        }

        private void ClearBeatTimes()
        {
            beatTimes.Clear();
            textBoxBeatTimes.Clear();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    PlayPause();
                    e.Handled = true;
                    break;
                case Keys.Q:
                    PlayPreviousFrame();
                    e.Handled = true;
                    break;
                case Keys.W:
                    PlayNextFrame();
                    e.Handled = true;
                    break;
                case Keys.B:
                    InsertBeat();
                    e.Handled = true;
                    break;
            }
        }

        private void InsertBeat()
        {
            beatTimes.Add(CurrentTime);
            textBoxBeatTimes.Clear();
            foreach (double beatTime in beatTimes)
            {
                textBoxBeatTimes.AppendText(beatTime.ToShortString() + Environment.NewLine);
            }
        }

        /// <summary>
        ///   When video is over, this is invoked
        /// </summary>
        private void Evaluate()
        {
            SaveTones(TonesPath);
            Close();
        }

        private void LoadVideo(string path)
        {
            videoReader.Open(path);
            SetFrameIndexToStart();
            UpdateFrameToFrameIndex();
            pianoKeys = InitializePianoKeys();
        }

        private void SetFrameIndexToStart()
        {
            double resetTime = double.TryParse(textBoxResetTime.Text, out double time) ? time : StartTime;
            frameIndex = (long)(resetTime / VideoDuration * FrameCount);
            maximumPlayedFrameIndex = frameIndex;
        }

        private void RefreshUI()
        {
            if (Visible && InvokeRequired)
            {
                Invoke(new Action(() => RefreshUI()));
                return;
            }

            // Set UI
            Text = $"{CurrentTime.ToShortString()} / {(videoReader.FrameCount * FrameLength)} ({frameIndex})";
            textBoxJumpTo.Text = CurrentTime.ToShortString();
            buttonPlay.Text = isPlaying ? "Pause" : "Play";
            PlayPause(false);
        }

        private void SaveTones(string path)
        {
            File.WriteAllText(path, "");
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                new XmlSerializer(typeof(List<Tone>)).Serialize(stream, tonesPast);
            }
        }

        private List<Tone> LoadTones(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                return new XmlSerializer(typeof(List<Tone>)).Deserialize(stream) as List<Tone>;
            }
        }

        private ScorePartwise LoadScore(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                return new XmlSerializer(typeof(ScorePartwise)).Deserialize(stream) as ScorePartwise;
            }
        }

        private void SaveScore(ScorePartwise score, string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                new XmlSerializer(typeof(ScorePartwise)).Serialize(stream, score);
            }
        }

        private string TimeStringFromMs(double milliseconds)
        {
            return TimeSpan.FromMilliseconds(milliseconds).ToString(@"mm\:ss\:fff");
        }

        private void PlayPause(bool toggleIsPlaying = true)
        {
            if (toggleIsPlaying)
            {
                isPlaying = !isPlaying;
            }

            if (isPlaying)
            {
                mediaPlayer.Ctlcontrols.play();
            }
            else
            {
                mediaPlayer.Ctlcontrols.pause();
            }
        }

        private void PlayPreviousFrame()
        {
            frameIndex = Math.Max(0, frameIndex - 1);
            this.InvokeIfRequired(() => UpdateFrameToFrameIndex());
        }

        private void PlayNextFrame()
        {
            // Increment
            frameIndex = Math.Min(frameIndex + 1, videoReader.FrameCount);

            // Evaluate at end
            if (frameIndex >= videoReader.FrameCount - 1
                || CurrentTime > EndTime)
            {
                isPlaying = false;
                Evaluate();
                return;
            }

            // Show current frame
            this.InvokeIfRequired(() 
                => UpdateFrameToFrameIndex());

            // COLLECT TONES
            /////////////////

            if (frameIndex > maximumPlayedFrameIndex || PrintOccuredTonesAgain)
            {
                foreach (PianoKey key in pianoKeys)
                {
                    Color color = Frame.GetPixel(key.Point.X, key.Point.Y);
                    float hue = color.GetHue();
                    float sat = color.GetSaturation();


                    if (!color.SimilarColorsAs(key.NotPressedColor, 100))
                    {
                        // Only record tone if:
                        if (sat > 0.2 &&
                              (KeyColorMode == ColorMode.All
                            || KeyColorMode == ColorMode.Blue && hue > 180
                            || KeyColorMode == ColorMode.Green && hue < 180))
                        {
                            if (tonesActive.First(prev => prev.Pitch == key.Pitch, out Tone activeTone))
                            {
                                //Log("Hold " + key.Pitch);
                                // Key hold
                                if (ShowVisuals)
                                {
                                    Frame.SetPixel4(key.Point.X, key.Point.Y, Color.Red);
                                }
                                activeTone.EndTime = CurrentTime;
                            }
                            else
                            {
                                //Log("Pressed new " + key.Pitch);
                                // Key pressed down
                                if (ShowVisuals)
                                {
                                    Frame.SetPixel16(key.Point.X, key.Point.Y, Color.Red);
                                }
                                Tone tone = new Tone(key.Pitch, color, CurrentTime)
                                {
                                    EndTime = CurrentTime
                                };
                                tonesActive.Add(tone);
                            }
                        }
                    }
                    else
                    {
                        // Key released: If it was active, shift to tonesPast
                        if (tonesActive.First(prev => prev.Pitch == key.Pitch, out Tone activeTone))
                        {
                            //Log("Released new " + key.Pitch);
                            activeTone.EndTime = CurrentTime;
                            tonesActive.Remove(activeTone);
                            tonesPast.Add(activeTone);
                        }

                        // Add tone tone to bitmap
                        //pictureBoxNotes.Image
                    }
                }
            }

            // Update tone textboxes
            if (checkBoxShowVisuals.Checked)
            {
                this.InvokeIfRequired(() =>
                {
                    // Active tones
                    textBoxTonesActiveLeft.Clear();
                    textBoxTonesRightActive.Clear();
                    foreach (Tone tone in tonesActive)
                    {
                        TextBox textBox = tone.Color.GetHue() > 190 ? textBoxTonesActiveLeft : textBoxTonesRightActive;
                        textBox.AppendText(tone.StartTime.ToShortString() + " - " + tone.EndTime.ToShortString() + ": " + tone.Pitch + Environment.NewLine);
                    }

                    // Previous tones
                    textBoxTonesPastLeft.Clear();
                    textBoxTonesRight.Clear();
                    foreach (Tone tone in tonesPast.OrderByDescending(it => it.StartTime).Take(15))
                    {
                        TextBox textBox = tone.Color.GetHue() > 190 ? textBoxTonesPastLeft : textBoxTonesRight;
                        textBox.AppendText(tone.StartTime.ToShortString() + " - " + tone.EndTime.ToShortString() + ": " + tone.Pitch + Environment.NewLine);
                    }
                });
            }

            maximumPlayedFrameIndex = frameIndex;

            RefreshUI();
        }

        private void UpdateFrameToFrameIndex()
        {
            Frame?.Dispose();
            Frame = new Bitmap(videoReader.ReadVideoFrame((int)frameIndex), pictureBox.Width, pictureBox.Height);

            // Set taskbar progress
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            TaskbarManager.Instance.SetProgressValue((int)frameIndex, (int)FrameCount);

            RefreshUI();
        }

        private List<PianoKey> InitializePianoKeys()
        {
            DirectBitmap bitmap = new DirectBitmap(Frame);

            // Find y with lowest deviation of bright values

            List<LineWithStatistics> linesWidthStatistics = new List<LineWithStatistics>();

            for (int y = FrameHeight - 300; y < FrameHeight; y += 10)
            {
                //y = FrameHeight - 200;
                bool[] boolArray = new bool[FrameWidth];
                for (int x = 0; x < FrameWidth; x++)
                {
                    boolArray[x] = bitmap.IsBright(x, y);

                    // Colorize pixels green or red (bright or dark)
                    //bitmap.SetPixel(x, y, boolArray[x] ? Color.Green : Color.Red);
                }
                //Log(string.Join(", ", boolArray.Take(10)));

                // Saves number of light and dark pixels in a row
                List<int> valueList = new List<int>(FrameWidth) { 0 };
                bool previousBool = boolArray[0];
                for (int x = 1; x < FrameWidth; x++)
                {
                    bool currentBool = boolArray[x];
                    if (currentBool == previousBool)
                    {
                        valueList[valueList.Count - 1]++;
                    }
                    else
                    {
                        valueList.Add(1);
                    }
                    previousBool = currentBool;
                }

                // Split into light and dark
                List<int> pixelsA = new List<int>();
                List<int> pixelsB = new List<int>();
                bool toggle = false;
                foreach (int value in valueList)
                {
                    toggle = !toggle;
                    if (toggle)
                    {
                        pixelsA.Add(value);
                    }
                    else
                    {
                        pixelsB.Add(value);
                    }
                }
                bool aIsBright = pixelsA.Sum() > pixelsB.Sum();
                List<int> brightPixels = aIsBright ? pixelsA : pixelsB;
                List<int> darkPixels = aIsBright ? pixelsB : pixelsA;

                //Log("Bright) " + string.Join(", ", brightPixels.Take(20)));
                //Log("Dark) " + string.Join(", ", darkPixels.Take(20)));

                double brightMean = (double)brightPixels.Sum() / brightPixels.Count;
                double darkMean = (double)darkPixels.Sum() / darkPixels.Count;
                //Log("Bright mean = " + brightMean);

                linesWidthStatistics.Add(new LineWithStatistics(y, brightPixels, darkPixels));
                //break;
            }


            // WHITE KEYS

            // Find pair with lowest white deviation = white keys
            LineWithStatistics line = null;
            foreach (LineWithStatistics currentLine in linesWidthStatistics.Where(it => !double.IsNaN(it.BrightMean) && !double.IsNaN(it.DarkMean)))
            {
                if (line == null || currentLine.BrightDeviation < line.BrightDeviation)
                {
                    line = currentLine;
                }
            }

            double keyDistance = line.BrightMean + line.DarkMean;


            // Get measure points white keys
            List<Point> whitePoints = new List<Point>();

            for (double x = 0.5 * keyDistance; x < FrameWidth; x += keyDistance)
            {
                whitePoints.Add(new Point((int)x, line.Y));
                //bitmap.SetPixel4((int)x, pairWhiteKeys.Y, Color.Violet);
            }

            if (whitePoints.Count == 0)
            {
                throw new Exception("Could not find white keys.");
            }


            // BLACK KEYS

            // Find pair with low white deviation & high black deviation = black keys
            LineWithStatistics pairBlacksKeys = null;
            foreach (LineWithStatistics pair in linesWidthStatistics.Where(it => !double.IsNaN(it.BrightMean) && !double.IsNaN(it.DarkMean)))
            {
                if (pairBlacksKeys == null || pair.BrightDeviation - pair.DarkDeviation < pairBlacksKeys.BrightDeviation - pairBlacksKeys.DarkDeviation)
                {
                    pairBlacksKeys = pair;
                }
            }

            // Get measure points black keys
            List<Point> blackPoints = new List<Point>();

            for (double x = keyDistance; x < FrameWidth; x += keyDistance)
            {
                if (!bitmap.IsBright((int)x, pairBlacksKeys.Y)
                    && !bitmap.IsBright((int)x + 3, pairBlacksKeys.Y))
                {
                    blackPoints.Add(new Point((int)x, pairBlacksKeys.Y));
                    //bitmap.SetPixel4((int)x, pairBlacksKeys.Y, Color.Orange);
                }
            }

            if (blackPoints.Count == 0)
            {
                throw new Exception("Could not find black keys.");
            }

            // Create white & black keye

            List<PianoKey> blackKeys = blackPoints.Select(point => new PianoKey(point, bitmap)).ToList();
            List<PianoKey> whiteKeys = whitePoints.Select(point => new PianoKey(point, bitmap)).ToList();

            // Find black key - Cis1

            for (int index = 0; index < blackKeys.Count - 1; index++)
            {
                blackKeys[index].DistanceToNext = blackKeys[index + 1].Point.X - blackKeys[index].Point.X;
            }
            var distances = blackKeys.Select(point => point.DistanceToNext).Where(dist => dist != 0).ToList();
            Log("Distances = " + distances.ToLog());
            double average = distances.Average();
            List<double> valuesBelowAverage = distances.Where(dist => dist < average).ToList();
            List<double> valuesAboveAverage = distances.Where(dist => dist > average).ToList();
            double shortDistance = valuesBelowAverage.Average();
            double longDistance = valuesAboveAverage.Average();
            //Log("Above: " + longDistanceToDecimalString() + " +- " + valuesAboveAverage.StandardDeviation().ToDecimalString() + ", " +
            //    "Below: " + shortDistance.ToDecimalString() + " +- " + valuesBelowAverage.StandardDeviation().ToDecimalString());


            // Assign pitch values to black keys

            for (int index = 0; index < blackKeys.Count - 3; index++)
            {
                // These black key distances clearly identify the first Cis
                if (blackKeys[index].DistanceToNext.IsAboutRel(shortDistance)
                    && blackKeys[index + 1].DistanceToNext.IsAboutRel(longDistance)
                    && blackKeys[index + 2].DistanceToNext.IsAboutRel(shortDistance)
                    && blackKeys[index + 3].DistanceToNext.IsAboutRel(shortDistance))
                {
                    // Tone is C#
                    blackKeys[index].Pitch = (Pitch)BlackPitch.Cis1;
                    BlackPitch startTone = BlackPitch.Cis1;
                    int startIndex = index;

                    // Set lower tones
                    index = startIndex;
                    BlackPitch previousTone = startTone;
                    while (index > 0)
                    {
                        previousTone = previousTone.Previous();
                        blackKeys[--index].Pitch = (Pitch)previousTone;
                    }

                    // Set higher tones
                    index = startIndex;
                    BlackPitch nextTone = startTone;
                    while (index < blackKeys.Count - 1)
                    {
                        nextTone = nextTone.Next();
                        blackKeys[++index].Pitch = (Pitch)nextTone;
                    }

                    break;
                }
            }


            // Colorize black keys

            foreach (PianoKey point in blackKeys)
            {
                bitmap.SetPixel4(point.Point, ColorForTone(point.Pitch));
            }


            // Find white key - C1
            ///////////////////////

            PianoKey KeyCis1 = blackKeys.Where(p => p.Pitch == Pitch.Cis1).First();
            int xPositionC1 = (int)(KeyCis1.Point.X - 0.5 * keyDistance);

            // Find white key nearest to C1

            PianoKey keyC1 = null;
            foreach (PianoKey currentKey in whiteKeys)
            {
                if (keyC1 == null
                    || Math.Abs(currentKey.Point.X - xPositionC1) < Math.Abs(keyC1.Point.X - xPositionC1))
                {
                    // C1 = nearest key
                    keyC1 = currentKey;
                }
            }
            keyC1.Pitch = (Pitch)WhitePitch.C1;

            bitmap.SetPixel4(keyC1.Point, ColorForTone(keyC1.Pitch));

            // Set white key pitches

            // Tone is C#
            WhitePitch startTone_ = WhitePitch.C1;
            int startIndex_ = whiteKeys.IndexOf(keyC1);

            // Set lower tones
            int index_ = startIndex_;
            WhitePitch previousTone_ = startTone_;
            while (index_ > 0)
            {
                previousTone_ = previousTone_.Previous();
                whiteKeys[--index_].Pitch = (Pitch)previousTone_;
            }

            // Set higher tones
            index_ = startIndex_;
            WhitePitch nextTone_ = startTone_;
            while (index_ < whiteKeys.Count - 1)
            {
                nextTone_ = nextTone_.Next();
                whiteKeys[++index_].Pitch = (Pitch)nextTone_;
            }

            // Colorize white keys
            foreach (PianoKey point in whiteKeys)
            {
                bitmap.SetPixel4(point.Point, ColorForTone(point.Pitch));
            }

            // Set edited bitmap
            Frame = bitmap.Bitmap.Copy();
            bitmap.Dispose();

            return whiteKeys.Concat(blackKeys).ToList();
        }

        private void Log(object text)
        {
            LogNoLine(text + Environment.NewLine);
        }

        private void LogNoLine(object text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => LogNoLine(text)));
                return;
            }

            textBoxLog.AppendText(DateTime.Now.ToString("hh:mm:ss") + ":  " + text.ToString());
        }

        public Color ColorForTone(Pitch pitch)
        {
            // Normalize
            pitch = (Pitch)((int)pitch % ColorByPitch.Count);

            //pitch = (Pitch)((int)pitch - ColorByPitch.Count);

            return ColorByPitch[pitch];
        }

        private static Dictionary<Pitch, Color> ColorByPitch = new Dictionary<Pitch, Color>()
        {
            { Pitch.C, Color.DodgerBlue },
            { Pitch.D, Color.DeepSkyBlue },
            { Pitch.E, Color.DarkTurquoise },
            { Pitch.F, Color.MediumAquamarine },
            { Pitch.G, Color.MediumSeaGreen },
            { Pitch.A, Color.LimeGreen },
            { Pitch.B, Color.Lime },
            { Pitch.Cis, Color.Yellow },
            { Pitch.Es, Color.Gold },
            { Pitch.Fis, Color.Orange },
            { Pitch.Gis, Color.OrangeRed },
            { Pitch.Bes, Color.Crimson },
        };

        private void ButtonNextFrame_Click(object sender, EventArgs e)
        {

        }

        private void CheckBoxShowVisuals_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void CheckBoxRealtime_CheckedChanged(object sender, EventArgs e)
        {

        }
    }

    public enum Pitch
    {
        C,
        Cis,
        D,
        Es,
        E,
        F,
        Fis,
        G,
        Gis,
        A,
        Bes,
        B,
        C1,
        Cis1,
        D1,
        Es1,
        E1,
        F1,
        Fis1,
        G1,
        Gis1,
        A1,
        Bes1,
        B1,
        C2,
        Cis2,
        D2,
        Es2,
        E2,
        F2,
        Fis2,
        G2,
        Gis2,
        A2,
        Bes2,
        B2,
        C3,
        Cis3,
        D3,
        Es3,
        E3,
        F3,
        Fis3,
        G3,
        Gis3,
        A3,
        Bes3,
        B3,
        C4,
        Cis4,
        D4,
        Es4,
        E4,
        F4,
        Fis4,
        G4,
        Gis4,
        A4,
        Bes4,
        B4,
        C5,
        Cis5,
        D5,
        Es5,
        E5,
        F5,
        Fis5,
        G5,
        Gis5,
        A5,
        Bes5,
        B5,
        C6,
        Cis6,
        D6,
        Es6,
        E6,
        F6,
        Fis6,
        G6,
        Gis6,
        A6,
        Bes6,
        B6,
    }

    public enum WhitePitch
    {
        C = Pitch.C,
        D = Pitch.D,
        E = Pitch.E,
        F = Pitch.F,
        G = Pitch.G,
        A = Pitch.A,
        B = Pitch.B,
        C1 = Pitch.C1,
        D1 = Pitch.D1,
        E1 = Pitch.E1,
        F1 = Pitch.F1,
        G1 = Pitch.G1,
        A1 = Pitch.A1,
        B1 = Pitch.B1,
        C2 = Pitch.C2,
        D2 = Pitch.D2,
        E2 = Pitch.E2,
        F2 = Pitch.F2,
        G2 = Pitch.G2,
        A2 = Pitch.A2,
        B2 = Pitch.B2,
        C3 = Pitch.C3,
        D3 = Pitch.D3,
        E3 = Pitch.E3,
        F3 = Pitch.F3,
        G3 = Pitch.G3,
        A3 = Pitch.A3,
        B3 = Pitch.B3,
        C4 = Pitch.C4,
        D4 = Pitch.D4,
        E4 = Pitch.E4,
        F4 = Pitch.F4,
        G4 = Pitch.G4,
        A4 = Pitch.A4,
        B4 = Pitch.B4,
        C5 = Pitch.C5,
        D5 = Pitch.D5,
        E5 = Pitch.E5,
        F5 = Pitch.F5,
        G5 = Pitch.G5,
        A5 = Pitch.A5,
        B5 = Pitch.B5,
        C6 = Pitch.C6,
        D6 = Pitch.D6,
        E6 = Pitch.E6,
        F6 = Pitch.F6,
        G6 = Pitch.G6,
        A6 = Pitch.A6,
        B6 = Pitch.B6,
    }

    public enum BlackPitch
    {
        Cis = Pitch.Cis,
        Es = Pitch.Es,
        Fis = Pitch.Fis,
        Gis = Pitch.Gis,
        Bes = Pitch.Bes,
        Cis1 = Pitch.Cis1,
        Es1 = Pitch.Es1,
        Fis1 = Pitch.Fis1,
        Gis1 = Pitch.Gis1,
        Bes1 = Pitch.Bes1,
        Cis2 = Pitch.Cis2,
        Es2 = Pitch.Es2,
        Fis2 = Pitch.Fis2,
        Gis2 = Pitch.Gis2,
        Bes2 = Pitch.Bes2,
        Cis3 = Pitch.Cis3,
        Es3 = Pitch.Es3,
        Fis3 = Pitch.Fis3,
        Gis3 = Pitch.Gis3,
        Bes3 = Pitch.Bes3,
        Cis4 = Pitch.Cis4,
        Es4 = Pitch.Es4,
        Fis4 = Pitch.Fis4,
        Gis4 = Pitch.Gis4,
        Bes4 = Pitch.Bes4,
        Cis5 = Pitch.Cis5,
        Es5 = Pitch.Es5,
        Fis5 = Pitch.Fis5,
        Gis5 = Pitch.Gis5,
        Bes5 = Pitch.Bes5,
        Cis6 = Pitch.Cis6,
        Es6 = Pitch.Es6,
        Fis6 = Pitch.Fis6,
        Gis6 = Pitch.Gis6,
        Bes6 = Pitch.Bes6,
    }

    /// <summary>
    ///   Represents a horizontal line with all light and dark keyboard keys and mathematical stuff
    /// </summary>
    public class LineWithStatistics
    {
        public int Y { private set; get; }
        public List<int> BrightPixels { private set; get; }
        public List<int> DarkPixels { private set; get; }

        public double BrightDeviation { private set; get; }
        public double DarkDeviation { private set; get; }
        public double BrightMean { private set; get; }
        public double DarkMean { private set; get; }

        public LineWithStatistics(int y, List<int> brightPixels, List<int> darkPixels)
        {
            Y = y;
            BrightPixels = brightPixels;
            DarkPixels = darkPixels;

            BrightDeviation = BrightPixels.StandardDeviation();
            DarkDeviation = DarkPixels.StandardDeviation();
            BrightMean = (double)brightPixels.Sum() / brightPixels.Count;
            DarkMean = (double)darkPixels.Sum() / darkPixels.Count;
        }
    }

    public class PianoKey
    {
        public Pitch Pitch { get; set; }
        public Point Point { get; }
        public double DistanceToNext { get; set; }
        public Color NotPressedColor { get; }

        public PianoKey(Point point, DirectBitmap bitmap)
        {
            Point = point;
            NotPressedColor = bitmap.GetPixel(Point);
        }

        public override string ToString()
        {
            return $"({Point.X} | {Point.Y}): {Pitch}";
        }
    }
}
