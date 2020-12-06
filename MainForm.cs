﻿using Accord.Video.FFMPEG;
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
using WMPLib;
using System.Runtime.ExceptionServices;

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
    ///   - Warum wird Frame anzeigen immer langsamer? :o
    /// </summary>
    public partial class MainForm : Form
    {
        // Serialize List<Tone> to file
        private const string TonesPath = @"C:\Users\Dustin\Desktop\tones";

        // Video file
        //private const string VideoPath = @"C:\Users\Dustin\Desktop\Slider Yellow.mp4";
        private const string VideoPath = @"C:\Users\Dustin\Desktop\Slider Yellow 360.mp4";

        // Settings
        private const ColorMode KeyColorMode = ColorMode.Green; //Blue = left, green = right
        private const AppMode Mode = AppMode.Save;
        //private const double StartTime = 0;
        private const double StartTime = 4.7;
        private const double EndTime = double.MaxValue;
        private const bool IsPlayingDefault = false;
        private const bool PlayRealtimeDefault = true;
        private const bool ShowVisualsDefault = true;
        private const bool PrintHandledTonesAgain = true;

        private bool isPlaying = IsPlayingDefault;
        private double maximumPlayedTime;
        private long frameIndex = -1;
        private bool Initializing { get; set; } = false;
        private VideoFileReader frameViewer = new VideoFileReader();
        private Bitmap frame;

        private List<Tone> tonesActive = new List<Tone>();
        private List<Tone> tonesPast = new List<Tone>();
        private List<PianoKey> pianoKeys;

        // For manual inserting beat beginnings
        private List<double> beatTimes = new List<double>();

        // TimeChangedEvent
        private delegate void TimeChangedEvent(double time);
        private event TimeChangedEvent TimeChanged;

        public MainForm()
        {
            //SaveYoutubeVideo(@"https://www.youtube.com/watch?v=lPtl-gBpGG8");

            InitializeComponent();
            InitializeUI();

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
            else
            {
                Shown += (s, e) =>
                {
                    InitVideoPlayerAndFrameViewer(VideoPath);

                    // Video play thread
                    TimeChanged += SetFrameByTime;
                    new Thread(() =>
                    {
                        while (frameIndex < frameViewer.FrameCount
                            && CurrentTime < EndTime)
                        {
                            try
                            {
                                CurrentTime = mediaPlayer.Ctlcontrols.currentPosition;
                                TimeChanged?.Invoke(CurrentTime);
                            }
                            catch { }
                            Thread.Sleep(1);
                        }

                        // EVALUATE
                        Evaluate();
                    }).Start();

                    RefreshUI();
                };
            }
        }

        private void SaveYoutubeVideo(string link)
        {
            YouTube youTube = YouTube.Default; // starting point for YouTube actions
            YouTubeVideo video = youTube.GetVideo(link); // gets a Video object with info about the video
            File.WriteAllBytes(@"C:\Users\Dustin\Desktop\" + video.FullName, video.GetBytes());
        }

        private bool ShowVisuals => checkBoxShowVisuals.Checked;
        private int FrameWidth => Frame.Width;
        private int FrameHeight => Frame.Height;
        private double FrameRate => frameViewer.FrameRate.Value;
        private double FrameDuration => 1.0 / frameViewer.FrameRate.Value;
        private long FrameCount => frameViewer.FrameCount;
        private double VideoDuration => frameViewer.FrameCount / frameViewer.FrameRate.Value;
        private double CurrentFrameViewerTime => frameIndex * FrameDuration;
        private double CurrentTime;
        IWMPControls2 Controls2 => (IWMPControls2)(mediaPlayer.Ctlcontrols);

        private Bitmap Frame
        {
            get => frame;
            set
            {
                frame?.Dispose();
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
            buttonNextFrame.Click += (s, e) => PlayNextFrameMediaPlayer();
            buttonPreviousFrame.Click += (s, e) => PlayPreviousFrameMediaPlayer();
            buttonClearNotesPast.Click += (s, e) => ClearNotesPast();
            buttonClearNotesActive.Click += (s, e) => textBoxTonesActiveLeft.Clear();
            textBoxJumpTo.KeyDown += TextBoxJumpTo_KeyDown;
            buttonClearBeatTimes.Click += (s, e) => ClearBeatTimes();
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
        }

        private void ResetToProgramStart()
        {
            InitVideoPlayerAndFrameViewer(VideoPath);

            tonesActive.Clear();
            tonesPast.Clear();
            ClearBeatTimes();

            textBoxLog.Clear();
            textBoxTonesPastLeft.Clear();
            textBoxTonesActiveLeft.Clear();
            textBoxTonesRight.Clear();
            textBoxTonesRightActive.Clear();

            SetStartFrame();
        }

        private void InitVideoPlayerAndFrameViewer(string path)
        {
            // VideoPlayer
            mediaPlayer.URL = VideoPath;
            mediaPlayer.Ctlcontrols.currentPosition = StartTime;
            mediaPlayer.settings.mute = true;
            CurrentTime = StartTime;

            // Stop playing (no frame shown at beginning)
            //mediaPlayer.Ctlcontrols.stop();

            // Immediately pause playing
            new Thread(() =>
            {
                mediaPlayer.Ctlcontrols.play();
                Thread.Sleep(100);
                while (true)
                {
                    try
                    {
                        if (mediaPlayer.playState == WMPPlayState.wmppsPlaying)
                        {
                            mediaPlayer.Ctlcontrols.pause();
                            break;
                        }
                    }
                    catch { }
                    Thread.Sleep(1);
                }
            }).Start();

            // FrameViewer
            frameViewer.Open(path);

            //SetStartFrame();
            isPlaying = false;
            Initializing = true;

            RefreshUI();
        }

        private void SetStartFrame()
        {
            // Get reset time from textbox (or use StartTime)
            double resetTime = double.TryParse(textBoxResetTime.Text, out double textBoxTime) 
                ? textBoxTime 
                : StartTime;
            SetFrameByTime(resetTime);
        }

        private void TextBoxJumpTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (double.TryParse(textBoxJumpTo.Text, out double time))
                {
                    SetFrameByTime(time);
                }
            }
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
                //case Keys.Q:
                //    PlayPreviousFrame();
                //    e.Handled = true;
                //    break;
                //case Keys.W:
                //    PlayNextFrame();
                //    e.Handled = true;
                //    break;
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
            MessageBox.Show("Saved tones to " + TonesPath);
            //Close();
        }


        private void RefreshUI()
        {
            if (Visible && InvokeRequired)
            {
                Invoke(new Action(() => RefreshUI()));
                return;
            }

            // Set UI
            Text = $"{CurrentTime.ToShortString()} / {(frameViewer.FrameCount * FrameDuration)} ({frameIndex})";
            textBoxJumpTo.Text = CurrentTime.ToShortString();
            buttonPlay.Text = isPlaying ? "Pause" : "Play";
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

        private void PlayPause()
        {
            isPlaying = !isPlaying;

            if (isPlaying)
            {
                mediaPlayer.Ctlcontrols.play();
            }
            else
            {
                 mediaPlayer.Ctlcontrols.pause();
            }
        }

        private void PlayPreviousFrameMediaPlayer()
        {
            //mediaPlayer.Ctlcontrols.currentPosition -= FrameDuration;
            //mediaPlayer.Ctlcontrols.pause();
            Controls2.step(-1);
        }

        private void PlayNextFrameMediaPlayer()
        {
            //mediaPlayer.Ctlcontrols.currentPosition += FrameDuration;
            //mediaPlayer.Ctlcontrols.pause();
            Controls2.step(1);
        }

   

        private void SetFrameByTime(double time)
        {
            long newFrameIndex = (long)(time / FrameDuration + 0.5);
            long frameProgress = newFrameIndex - frameIndex; // Number of frames to go
            if (Initializing || frameProgress != 0)
            {
                //Log("New Frame: " + newFrameIndex + " (" + time + "), init = " + Initializing);
                frameIndex = newFrameIndex;

                //Log("Frame = " + this.frameIndex + ", frameTime = " + (frameIndex * FrameDuration).ToShortString() + ", videoTime = " + CurrentTime.ToShortString());
                Watch.Start();
                Frame = frameViewer.ReadVideoFrame((int)newFrameIndex);
                Watch.Measure(Log);

                // Colorize recognized keys
                if (Initializing)
                {
                    pianoKeys = InitializePianoKeys();
                    Initializing = false;
                }

                if (!Initializing && frameProgress > 1)
                {
                    Log(@"WARNING: Skipped " + frameProgress + " frames");
                    //MessageBox.Show("Skipping " + frameDelta + " frames");
                }
                else if (frameProgress == 1)
                {
                    AnalyseTones();
                }

                // Set taskbar progress
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                TaskbarManager.Instance.SetProgressValue((int)newFrameIndex, (int)FrameCount);

                maximumPlayedTime = CurrentTime;

                RefreshUI();
            }
        }

        private void AnalyseTones()
        {
            // COLLECT TONES
            /////////////////

            if (CurrentTime > maximumPlayedTime || PrintHandledTonesAgain)
            {
                foreach (PianoKey key in pianoKeys)
                {
                    Color color = Frame.GetPixel(key.Point.X, key.Point.Y);
                    float hue = color.GetHue();
                    float sat = color.GetSaturation(); // Active keys have saturation (colorized)

                    int colorDelta = color.DeltaTo(key.NotPressedColor);
                    if (colorDelta > 100)
                    {
                        // Only record tone if:
                        if (sat > 0.2 &&
                              (KeyColorMode == ColorMode.All
                            || KeyColorMode == ColorMode.Blue && hue > 180
                            || KeyColorMode == ColorMode.Green && hue < 180))
                        {
                            if (tonesActive.TryGet(tone => tone.ToneHeight == key.ToneHeight, out Tone activeTone))
                            {
                                Log("Hold " + key.ToneHeight);

                                // KEY HOLD
                                if (ShowVisuals)
                                {
                                    Frame.SetPixel4(key.Point.X, key.Point.Y, Color.Red);
                                }
                                activeTone.EndTime = CurrentTime;
                            }
                            else
                            {
                                Log("Pressed new " + key.ToneHeight);

                                // KEY PRESSED DOWN
                                if (ShowVisuals)
                                {
                                    Frame.SetPixel16(key.Point.X, key.Point.Y, Color.Red);
                                }
                                Tone tone = new Tone(key.ToneHeight, color, CurrentTime)
                                {
                                    EndTime = CurrentTime
                                };
                                tonesActive.Add(tone);
                            }
                        }
                    }
                    else
                    {
                        // KEY RELEASED: If it was active, shift to tonesPast
                        if (tonesActive.TryGet(prev => prev.ToneHeight == key.ToneHeight, out Tone activeTone))
                        {
                            //Log("Released new " + key.ToneHeight);
                            activeTone.EndTime = CurrentTime;
                            tonesActive.Remove(activeTone);
                            tonesPast.Add(activeTone);
                        }

                        // Add tone to bitmap
                        //pictureBoxNotes.Image
                    }
                }
            }

            // Update tone textboxes
            if (ShowVisuals)
            {
                this.Invoke(() =>
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
                    //bitmap.SetPixel4(xPos, brightnessLine.Y, Color.Brown);
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
                        //bitmap.SetPixel4(xPos, darkestLine.Y, Color.Yellow);
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


                // Assign ToneHeights to BLACK keys
                ////////////////////////////////////

                for (int index = 0; index < blackKeys.Count; index++)
                {
                    // These black key distances clearly identify the first Cis
                    if (blackKeys[index].DistanceToNext.IsAboutRel(shortDistance)
                        && blackKeys[index + 1].DistanceToNext.IsAboutRel(longDistance)
                        && blackKeys[index + 2].DistanceToNext.IsAboutRel(shortDistance)
                        && blackKeys[index + 3].DistanceToNext.IsAboutRel(shortDistance))
                    {
                        // Tone is C#
                        ToneHeight startToneHeight = new ToneHeight(Pitch.Cis, 2);
                        blackKeys[index].ToneHeight = startToneHeight;
                        int startIndex = index;

                        // Set lower blackKeys' toneHeights
                        index = startIndex;
                        ToneHeight previousToneHeight = startToneHeight;
                        while (index > 0)
                        {
                            previousToneHeight = ToneHeight.GetPreviousBlack(previousToneHeight);
                            blackKeys[--index].ToneHeight = previousToneHeight;
                        }

                        // Set higher blackKeys' toneHeights
                        index = startIndex;
                        ToneHeight nextTone = startToneHeight;
                        while (index < blackKeys.Count - 1)
                        {
                            nextTone = ToneHeight.GetNextBlack(nextTone);
                            blackKeys[++index].ToneHeight = nextTone;
                        }

                        break;
                    }
                }



                // Find white key - C1
                ///////////////////////

                PianoKey KeyCis2 = blackKeys.Where(p => p.ToneHeight.Pitch == Pitch.Cis && p.ToneHeight.Octave == 2).First();
                int xPositionC1 = (int)(KeyCis2.Point.X - 0.5 * keyDistance);

                // Find white key nearest to C1
                PianoKey keyC2 = null;
                foreach (PianoKey currentKey in whiteKeys)
                {
                    if (keyC2 == null
                        || Math.Abs(currentKey.Point.X - xPositionC1) < Math.Abs(keyC2.Point.X - xPositionC1))
                    {
                        // C1 = nearest key
                        keyC2 = currentKey;
                    }
                }
                keyC2.ToneHeight = new ToneHeight(Pitch.C, 2);

                bitmap.SetPixel4(keyC2.Point, ColorForTone(keyC2.ToneHeight.Pitch));

                // Set white key pitches
                FindWhiteKeys(whiteKeys, keyC2);

                // Colorize black keys
                foreach (PianoKey point in blackKeys)
                {
                    bitmap.SetPixel4(point.Point, ColorForPitchDict[point.ToneHeight.Pitch]);
                }
                // Colorize white keys
                foreach (PianoKey point in whiteKeys)
                {
                    bitmap.SetPixel4(point.Point, ColorForPitchDict[point.ToneHeight.Pitch]);
                }
                // Set bitmap
                Frame = bitmap.Bitmap.Copy();

                List<PianoKey> keys = whiteKeys.Concat(blackKeys).ToList();
                bool anySameToneHeight = keys.GroupBy(x => x.ToneHeight.TotalHeight).Any(key => key.Count() > 1);
                Log("Same tone heights = " + anySameToneHeight);
                return keys;
            }
        }

        private void FindWhiteKeys(List<PianoKey> whiteKeys, PianoKey keyC2)
        {
            // Tone is C#
            ToneHeight startTone = keyC2.ToneHeight;
            int startIndex = whiteKeys.IndexOf(keyC2);

            // Set lower whiteKeys' toneHeights
            int index = startIndex;
            ToneHeight previousTone = startTone;
            while (index > 0)
            {
                previousTone = ToneHeight.GetPreviousWhite(previousTone);
                whiteKeys[--index].ToneHeight = previousTone;
            }

            // Set higher whiteKeys' toneHeights
            index = startIndex;
            ToneHeight nextTone = startTone;
            while (index < whiteKeys.Count - 1)
            {
                nextTone = ToneHeight.GetNextWhite(nextTone);
                whiteKeys[++index].ToneHeight = nextTone;
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

            if (textBoxLog.TextLength > 500)
            {
                textBoxLog.Clear();
            }

            textBoxLog.AppendText(DateTime.Now.ToString("hh:mm:ss") + ":  " + text.ToString());
        }

        public Color ColorForTone(Pitch pitch)
        {
            // Normalize
            pitch = (Pitch)((int)pitch % ColorForPitchDict.Count);

            //pitch = (Pitch)((int)pitch - ColorByPitch.Count);

            return ColorForPitchDict[pitch];
        }

        private readonly static Dictionary<Pitch, Color> ColorForPitchDict = new Dictionary<Pitch, Color>()
        {
            // White keys
            { Pitch.C, Color.Black },
            { Pitch.D, Color.Gold },
            { Pitch.E, Color.Orange },
            { Pitch.F, Color.Red },
            { Pitch.G, Color.Violet },
            { Pitch.A, Color.SkyBlue },
            { Pitch.B, Color.Green },
            // Black keys
            { Pitch.Cis, Color.White },
            { Pitch.Es, Color.Yellow },
            { Pitch.Fis, Color.Orange },
            { Pitch.Gis, Color.Red },
            { Pitch.Bes, Color.Violet },
        };

        private static Color CreateColorless(byte brightness)
        {
            return Color.FromArgb(brightness, brightness, brightness);
        }

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
}
