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
using YoutubeExtractor;
using VideoLibrary;

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
        //private const string VideoPath = @"C:\Users\Dustin\Desktop\Slider Yellow.mp4";
        private const string VideoPath = @"C:\Users\Dustin\Desktop\Slider Yellow 360.mp4";

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
            //SaveYoutubeVideo(@"https://www.youtube.com/watch?v=lPtl-gBpGG8");

            InitializeComponent();
            InitializeUI();

            mediaPlayer.URL = VideoPath;
            mediaPlayer.Ctlcontrols.currentPosition = StartTime;
            mediaPlayer.Ctlcontrols.stop();
            //mediaPlayer.Ctlcontrols.play();

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
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        //while (watch.El)
                        Thread.Sleep(1000000);
                        //double mediaPlayerTime = 0;
                        //while (mediaPlayerTime < CurrentTime)
                        //{
                        //    try
                        //    {
                        //        mediaPlayerTime = mediaPlayer.Ctlcontrols.currentPosition;
                        //    }
                        //    catch { }
                        //    Thread.Sleep(1);
                        //}

                        this.InvokeIfRequired(()
                            => PlayNextFrame());
                    }
                }).Start();

                RefreshUI();
            };
        }

        void SaveYoutubeVideo(string link)
        {
            YouTube youTube = YouTube.Default; // starting point for YouTube actions
            YouTubeVideo video = youTube.GetVideo(link); // gets a Video object with info about the video
            File.WriteAllBytes(@"C:\Users\Dustin\Desktop\" + video.FullName, video.GetBytes());
        }

        private void PlayFrame()
        {
            int frameIndex = (int)(mediaPlayer.Ctlcontrols.currentPosition / VideoDuration * FrameCount);
            Frame = videoReader.ReadVideoFrame(frameIndex);
            mediaPlayer.Ctlcontrols.currentPosition = CurrentTime;
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
            pictureBox.Size = new Size(FrameWidth, FrameHeight);
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

            //if (isPlaying)
            //{
            //    mediaPlayer.Ctlcontrols.play();
            //}
            //else
            //{
            //    mediaPlayer.Ctlcontrols.pause();
            //}
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

            Watch.Start();
            Frame = new Bitmap(videoReader.ReadVideoFrame((int)frameIndex), pictureBox.Width, pictureBox.Height);
            Watch.Measure(Log);

            // Set taskbar progress
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            TaskbarManager.Instance.SetProgressValue((int)frameIndex, (int)FrameCount);

            RefreshUI();
        }

        private List<PianoKey> InitializePianoKeys()
        {
            const double BrightnessOffset = 0.7;
            const double MaxPercentageOfHeightToRecognizeKeys = 0.82;

            using (DirectBitmap bitmap = new DirectBitmap(Frame))
            {
                // Find y with lowest deviation of bright values
                List<LineWithStatistics> linesWithStatistics = new List<LineWithStatistics>();

                for (int y = (int)(MaxPercentageOfHeightToRecognizeKeys * FrameHeight); y < FrameHeight; y += 10)
                {
                    // Collect brightness values
                    List<double> brightnessValues = new List<double>();
                    for (int x = 0; x < FrameWidth; x += 1)
                    {
                        brightnessValues.Add(bitmap.GetPixel(x, y).GetBrightness());
                    }

                    linesWithStatistics.Add(new LineWithStatistics(y, brightnessValues));
                }

                // Find brightest + darkest line (for white + black keys)
                LineWithStatistics brightnessLine = null;
                LineWithStatistics darkestLine = null;
                foreach (LineWithStatistics currentLine in linesWithStatistics.Where(it => !double.IsNaN(it.WhiteKeyDistanceMean) && !double.IsNaN(it.DarkKeyDistanceMean)))
                {
                    if (brightnessLine == null
                        || currentLine.MeanBrightness > brightnessLine.MeanBrightness)
                    {
                        brightnessLine = currentLine;
                    }
                    if (darkestLine == null
                        || currentLine.MeanBrightness < darkestLine.MeanBrightness)
                    {
                        darkestLine = currentLine;
                    }
                }

                // Saves number of light and dark pixels in a row
                List<int> sectionsWidths = new List<int>(FrameWidth) { 0 }; // Number of dark and light pixels in a row
                bool previousPixelIsBright = brightnessLine.BrightnessValues[0] > BrightnessOffset;
                for (int x = 1; x < FrameWidth; x++)
                {
                    bool currentPixelIsBright = brightnessLine.BrightnessValues[x] > BrightnessOffset;
                    if (currentPixelIsBright == previousPixelIsBright)
                    {
                        sectionsWidths[sectionsWidths.Count - 1]++;
                    }
                    else
                    {
                        sectionsWidths.Add(1);
                    }
                    previousPixelIsBright = currentPixelIsBright;
                }

                // Calc key distance
                List<int> list1 = new List<int>();
                List<int> list2 = new List<int>();
                for (int i = 0; i < sectionsWidths.Count - 1; i += 2)
                {
                    list1.Add(sectionsWidths[i]); // White/Black pixels in a row
                    list2.Add(sectionsWidths[i + 1]); // Black/White pixels in a row
                }
                double keyDistance = list1.Sum() / list1.Count + list2.Sum() / list2.Count;


                // Set white key points
                List<Point> whiteKeyPoints = new List<Point>();

                int xPos = (int)(keyDistance / 2);
                //whiteKeyPoints.Add(new Point(xPos, brightnessLine.Y));
                for (int i = 0; i < sectionsWidths.Count - 1; i += 2)
                {
                    bitmap.SetPixel4(xPos, brightnessLine.Y, Color.Brown);
                    whiteKeyPoints.Add(new Point(xPos, brightnessLine.Y));
                    xPos += sectionsWidths[i] + sectionsWidths[i + 1];
                }

                // Set black key points
                List<Point> blackKeyPoints = new List<Point>();

                xPos = (int)keyDistance;
                for (int i = 0; i < whiteKeyPoints.Count - 1; i++)
                {
                    xPos = (whiteKeyPoints[i].X + whiteKeyPoints[i + 1].X) / 2;
                    // Left and right must be also dark (else its just the gap between two whites)
                    if (darkestLine.BrightnessValues[xPos - 1] < BrightnessOffset
                        && darkestLine.BrightnessValues[xPos + 1] < BrightnessOffset)
                    {
                        bitmap.SetPixel4(xPos, darkestLine.Y, Color.Yellow);
                        blackKeyPoints.Add(new Point(xPos, darkestLine.Y));
                    }
                }

                if (blackKeyPoints.Count == 0 || whiteKeyPoints.Count == 0)
                {
                    throw new Exception("Could not find keys.");
                }



                // Create white & black keye

                List<PianoKey> blackKeys = blackKeyPoints.Select(point => new PianoKey(point, bitmap)).ToList();
                List<PianoKey> whiteKeys = whiteKeyPoints.Select(point => new PianoKey(point, bitmap)).ToList();

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
                return null;
                //return whiteKeys.Concat(blackKeys).ToList();
            }
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

  

    /// <summary>
    ///   Represents a horizontal line with all light and dark keyboard keys and mathematical stuff
    /// </summary>
    public class LineWithStatistics
    {
        public int Y { private set; get; }
        public List<int> WhiteKeyDistances { private set; get; }
        public List<int> DarkKeyDistances { private set; get; }

        public double MeanBrightness { private set; get; }

        public double WhiteKeyStdDeviation { private set; get; }
        public double DarkDeviation { private set; get; }
        public double WhiteKeyDistanceMean { private set; get; }
        public double DarkKeyDistanceMean { private set; get; }

        public List<int> WhiteKeyPositions { private set; get; } = new List<int>();
        public List<int> DarkKeyPositions { private set; get; } = new List<int>();
        public List<double> BrightnessValues { private set; get; }

        public LineWithStatistics(int y, 
            //List<int> whiteKeyDistances, List<int> blackKeyDistances, 
            List<double> brightnessValues)
        {
            Y = y;
            //WhiteKeyDistances = whiteKeyDistances;
            //DarkKeyDistances = blackKeyDistances;
            BrightnessValues = brightnessValues;
            MeanBrightness = brightnessValues.Sum() / brightnessValues.Count;

            //WhiteKeyStdDeviation = WhiteKeyDistances.StandardDeviation();
            //DarkDeviation = DarkKeyDistances.StandardDeviation();
            //WhiteKeyDistanceMean = (double)whiteKeyDistances.Sum() / whiteKeyDistances.Count;
            //DarkKeyDistanceMean = (double)blackKeyDistances.Sum() / blackKeyDistances.Count;

            //int keyPosition = 0;
            //foreach (int whiteKeyDistance in whiteKeyDistances)
            //{
            //    keyPosition += whiteKeyDistance;
            //    WhiteKeyPositions.Add(keyPosition);
            //}

            //keyPosition = 0;
            //foreach (int whiteKeyDistance in whiteKeyDistances)
            //{
            //    keyPosition += whiteKeyDistance;
            //    DarkKeyPositions.Add(keyPosition);
            //}
        }

        public override string ToString()
        {
            return $"Y = {Y}, Brightness = {MeanBrightness.ToShortString()}, BrightDev = {WhiteKeyStdDeviation.ToShortString()}, DarkDev = {DarkDeviation.ToShortString()}";
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
