using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static YAESU_FT_891_Front_End.RigState;

namespace YAESU_FT_891_Front_End
{
    public class SimulatedWaterfall
    {
        public struct SimulatedWaterfallButtons
        {
            public const byte CENTER = 0;
            public const byte _3DSS = 1;
            public const byte MULTI = 2;
            public const byte EXPAND = 3;
            public const byte SPAN = 4;
            public const byte SPEED = 5;
            public const byte START = 6;
            public const byte STOP = 7;
        }

        public struct FrequencySpans
        {
            public const byte _1K = 0;
            public const byte _2K = 1;
            public const byte _5K = 2;
            public const byte _10K = 3;
            public const byte _20K = 4;
            public const byte _50K = 5;
            public const byte _100K = 6;
            public const byte _200K = 7;
            public const byte _500K = 8;
            public const byte _1000K = 9;
        }
        public struct Speeds
        {
            public const byte Slow1 = 0;
            public const byte Slow2 = 1;
            public const byte Fast1 = 2;
            public const byte Fast2 = 3;
            public const byte Fast3 = 4;
            public const byte Stop = 5;
        }

        public struct CursorModes
        {
            public const byte Center = 0;
            public const byte Cursor = 1;
            public const byte Fix = 2;
        }

        public struct Cursors
        {
            public const byte GreenCursor = 0;
            public const byte RedCursor = 1;
        }

        public static byte currentFrequencySpan = FrequencySpans._50K;
        public static byte currentSpeed = Speeds.Fast1;
        public static byte currentCursorMode = CursorModes.Center;
        public const Double GreenCursorCenterPosition = 313;
        public const Double RedCursorCenterPosition = 313;
        public static Double currentGreenCursorPosition = GreenCursorCenterPosition;
        public static Double currentRedCursorPosition = RedCursorCenterPosition;
        public bool maxLeftPositionReached = false;
        public bool maxRightPositionReached = false;

        MainWindow mainWindow;
        FrequencyManagement frequencyManagement;

        public SimulatedWaterfall(MainWindow mainWindow, FrequencyManagement frequencyManagement)
        {
            this.mainWindow = mainWindow;
            this.frequencyManagement = frequencyManagement;

            ChangeSpanFrequency(currentFrequencySpan);
            ChangeSpeed(currentSpeed);
            ChangeCursorMode(CursorModes.Cursor);
            ChangeCursorPosition(Cursors.GreenCursor, currentGreenCursorPosition);
            ChangeCursorPosition(Cursors.RedCursor, currentRedCursorPosition);
            ChangeCursorMode(CursorModes.Center);
        }

        public void DoScrollBasedOnCursorMode(Double delta)
        {
            switch(currentCursorMode)
            {
                case CursorModes.Center:
                    ChangeSpanLegends(currentFrequencySpan);
                    break;
                case CursorModes.Cursor:
                    ScrollPointer(delta);
                    break;
            }
        }
        private void ScrollPointer(Double delta)
        {
            double step = currentCursorPixelStep;

            double nextPosition = currentRedCursorPosition;

            if (delta > 0)
                nextPosition += step;
            else
                nextPosition -= step;

            bool hitLeftEdge = false;
            bool hitRightEdge = false;

            if (nextPosition <= 0)
            {
                nextPosition = 0;
                hitLeftEdge = true;
            }
            else if (nextPosition >= SpectrumWidthPixels)
            {
                nextPosition = SpectrumWidthPixels;
                hitRightEdge = true;
            }

            currentRedCursorPosition = nextPosition;

            ChangeCursorPosition(Cursors.GreenCursor, currentRedCursorPosition);

            ChangeCursorPosition(Cursors.RedCursor, currentRedCursorPosition);

            long spanHz = GetSpanHz(currentFrequencySpan);

            long freqStepHz = spanHz / 4;

            if (delta > 0 && hitRightEdge)
            {
                long _currentFrequency = frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, null);
                _currentFrequency += freqStepHz;
                frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, _currentFrequency, mainWindow.MainFrequencyTextBlock);
                maxRightPositionReached = true;
                maxLeftPositionReached = false;
            }
            else if (delta < 0 && hitLeftEdge)
            {
                long _currentFrequency = frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, null);
                _currentFrequency -= freqStepHz;
                frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, _currentFrequency, mainWindow.MainFrequencyTextBlock);
                maxLeftPositionReached = true;
                maxRightPositionReached = false;
            }
            else
            {
                maxLeftPositionReached = false;
                maxRightPositionReached = false;
            }

            ChangeSpanLegends(currentFrequencySpan);
        }

        public long GetSpanHz(byte span)
        {
            switch (span)
            {
                case FrequencySpans._1K: return 1000;
                case FrequencySpans._2K: return 2000;
                case FrequencySpans._5K: return 5000;
                case FrequencySpans._10K: return 10000;
                case FrequencySpans._20K: return 20000;
                case FrequencySpans._50K: return 50000;
                case FrequencySpans._100K: return 100000;
                case FrequencySpans._200K: return 200000;
                case FrequencySpans._500K: return 500000;
                case FrequencySpans._1000K: return 1000000;
                default: return 50000;
            }
        }
        public void ButtonSelection(byte buttonClicked)
        {
            switch (buttonClicked)
            {
                case SimulatedWaterfallButtons.CENTER:
                    if (currentCursorMode < 2)
                        currentCursorMode++;
                    else
                        currentCursorMode = 0;

                    ChangeCursorMode(currentCursorMode);
                    break;
                case SimulatedWaterfallButtons._3DSS:
                    break;
                case SimulatedWaterfallButtons.MULTI:
                    break;
                case SimulatedWaterfallButtons.EXPAND:
                    break;
                case SimulatedWaterfallButtons.SPAN:
                    mainWindow._1kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._1kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._2kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._2kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._5kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._5kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._10kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._10kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._20kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._20kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._50kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._50kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._100kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._100kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._200kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._200kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._500kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._500kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow._1000kBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow._1000kTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    
                    switch (currentFrequencySpan)
                    {
                        case FrequencySpans._1K:
                            mainWindow._1kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._1kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._2K:
                            mainWindow._2kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._2kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._5K:
                            mainWindow._5kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._5kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._10K:
                            mainWindow._10kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._10kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._20K:
                            mainWindow._20kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._20kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._50K:
                            mainWindow._50kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._50kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._100K:
                            mainWindow._100kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._100kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._200K:
                            mainWindow._200kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._200kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._500K:
                            mainWindow._500kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._500kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case FrequencySpans._1000K:
                            mainWindow._1000kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._1000kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        default:
                            currentFrequencySpan = FrequencySpans._50K;
                            mainWindow._50kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow._50kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                    }

                    if (mainWindow.SpanPopupWindowBorder.Visibility != Visibility.Visible)
                    {
                        // 1. Instant Show: Make it visible and reset opacity to full
                        mainWindow.SpanPopupWindowBorder.Visibility = Visibility.Visible;
                        mainWindow.SpanPopupWindowBorder.Opacity = 1.0;
                    }
                    else
                    {
                        FadoutBorderWindow(mainWindow.SpanPopupWindowBorder, 0);
                    }
                    break;
                case SimulatedWaterfallButtons.SPEED:
                    mainWindow.Slow1Border.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow.Slow1TextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow.Slow2Border.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow.Slow2TextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow.Fast1Border.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow.Fast1TextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow.Fast2Border.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow.Fast2TextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow.Fast3Border.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow.Fast3TextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    mainWindow.StopBorder.Background = new SolidColorBrush(Colors.LightGray);
                    mainWindow.StopTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    switch (currentSpeed)
                    {
                        case Speeds.Slow1:
                            mainWindow.Slow1Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow.Slow1TextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case Speeds.Slow2:
                            mainWindow.Slow2Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow.Slow2TextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case Speeds.Fast1:
                            mainWindow.Fast1Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow.Fast1TextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case Speeds.Fast2:
                            mainWindow.Fast2Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow.Fast2TextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case Speeds.Fast3:
                            mainWindow.Fast3Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow.Fast3TextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        case Speeds.Stop:
                            mainWindow.StopBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow.StopTextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                        default:
                            currentSpeed = Speeds.Fast1;
                            mainWindow.Fast1Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                            mainWindow.Fast1TextBlock.Foreground = new SolidColorBrush(Colors.White);
                            break;
                    }

                    if (mainWindow.SpeedPopupWindowBorder.Visibility != Visibility.Visible)
                    {
                        // 1. Instant Show: Make it visible and reset opacity to full
                        mainWindow.SpeedPopupWindowBorder.Visibility = Visibility.Visible;
                        mainWindow.SpeedPopupWindowBorder.Opacity = 1.0;
                    }
                    else
                    {
                        FadoutBorderWindow(mainWindow.SpeedPopupWindowBorder, 0);
                    }
                    break;
                case SimulatedWaterfallButtons.START:
                    break;
                case SimulatedWaterfallButtons.STOP:
                    break;
                default:

                    break;
            }
        }

        public void ChangeSpanFrequency(byte frequencySpan)
        {
            mainWindow._1kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._1kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._2kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._2kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._5kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._5kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._10kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._10kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._20kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._20kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._50kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._50kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._100kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._100kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._200kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._200kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._500kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._500kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow._1000kBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow._1000kTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            switch (frequencySpan)
            {
                case FrequencySpans._1K:
                    currentFrequencySpan = FrequencySpans._1K;
                    mainWindow._1kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._1kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "1kHz";
                    break;
                case FrequencySpans._2K:
                    currentFrequencySpan = FrequencySpans._2K;
                    mainWindow._2kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._2kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "2kHz";
                    break;
                case FrequencySpans._5K:
                    currentFrequencySpan = FrequencySpans._5K;
                    mainWindow._5kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._5kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "5kHz";
                    break;
                case FrequencySpans._10K:
                    currentFrequencySpan = FrequencySpans._10K;
                    mainWindow._10kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._10kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "10kHz";
                    break;
                case FrequencySpans._20K:
                    currentFrequencySpan = FrequencySpans._20K;
                    mainWindow._20kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._20kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "20kHz";
                    break;
                case FrequencySpans._50K:
                    currentFrequencySpan = FrequencySpans._50K;
                    mainWindow._50kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._50kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "50kHz";
                    break;
                case FrequencySpans._100K:
                    currentFrequencySpan = FrequencySpans._100K;
                    mainWindow._100kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._100kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "100kHz";
                    break;
                case FrequencySpans._200K:
                    currentFrequencySpan = FrequencySpans._200K;
                    mainWindow._200kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._200kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "200kHz";
                    break;
                case FrequencySpans._500K:
                    currentFrequencySpan = FrequencySpans._500K;
                    mainWindow._500kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._500kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "500kHz";
                    break;
                case FrequencySpans._1000K:
                    currentFrequencySpan = FrequencySpans._1000K;
                    mainWindow._1000kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._1000kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "1000kHz";
                    break;
                default:
                    currentFrequencySpan = FrequencySpans._50K;
                    mainWindow._50kBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow._50kTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.FrequencySpanTextBlock.Text = "50kHz";
                    break;
            }

            ChangeSpanLegends(currentFrequencySpan);

            FadoutBorderWindow(mainWindow.SpanPopupWindowBorder);
        }
        public void ChangeSpeed(byte speed)
        {
            mainWindow.Slow1Border.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow.Slow1TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow.Slow2Border.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow.Slow2TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow.Fast1Border.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow.Fast1TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow.Fast2Border.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow.Fast2TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow.Fast3Border.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow.Fast3TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            mainWindow.StopBorder.Background = new SolidColorBrush(Colors.LightGray);
            mainWindow.StopTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            switch (speed)
            {
                case Speeds.Slow1:
                    currentSpeed = Speeds.Slow1;
                    mainWindow.Slow1Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow.Slow1TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.SpeedTextBlock.Text = "SLOW1";
                    break;
                case Speeds.Slow2:
                    currentSpeed = Speeds.Slow2;
                    mainWindow.Slow2Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow.Slow2TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.SpeedTextBlock.Text = "SLOW2";
                    break;
                case Speeds.Fast1:
                    currentSpeed = Speeds.Fast1;
                    mainWindow.Fast1Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow.Fast1TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.SpeedTextBlock.Text = "FAST1";
                    break;
                case Speeds.Fast2:
                    currentSpeed = Speeds.Fast2;
                    mainWindow.Fast2Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow.Fast2TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.SpeedTextBlock.Text = "FAST2";
                    break;
                case Speeds.Fast3:
                    currentSpeed = Speeds.Fast3;
                    mainWindow.Fast3Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow.Fast3TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.SpeedTextBlock.Text = "FAST3";
                    break;
                case Speeds.Stop:
                    currentSpeed = Speeds.Stop;
                    mainWindow.StopBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow.StopTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.SpeedTextBlock.Text = "STOP";
                    break;
                default:
                    currentSpeed = Speeds.Fast1;
                    mainWindow.Fast1Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    mainWindow.Fast1TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.SpeedTextBlock.Text = "FAST1";
                    break;
            }

            FadoutBorderWindow(mainWindow.SpeedPopupWindowBorder);
        }

        public const double SpectrumWidthPixels = 600.0;
        public double currentCursorPixelStep;

        public void ChangeSpanLegends(byte span)
        {
            // 1. Map the span byte to its actual numeric Hz value
            long spanHz;
            switch (span)
            {
                case FrequencySpans._1K: spanHz = 1000; break;
                case FrequencySpans._2K: spanHz = 2000; break;
                case FrequencySpans._5K: spanHz = 5000; break;
                case FrequencySpans._10K: spanHz = 10000; break;
                case FrequencySpans._20K: spanHz = 20000; break;
                case FrequencySpans._50K: spanHz = 50000; break;
                case FrequencySpans._100K: spanHz = 100000; break;
                case FrequencySpans._200K: spanHz = 200000; break;
                case FrequencySpans._500K: spanHz = 500000; break;
                case FrequencySpans._1000K: spanHz = 1000000; break;
                default: spanHz = 50000; break;
            }

            // 2. Calculate the interval step size using integer math
            long stepHz = spanHz / 4;

            // 3. Collect UI references into an array
            var labels = new[] {
                mainWindow.SpanLabel1TextBlock,
                mainWindow.SpanLabel2TextBlock,
                mainWindow.SpanLabel3TextBlock,
                mainWindow.SpanLabel4TextBlock,
                mainWindow.SpanLabel5TextBlock
            };

            if (currentCursorMode == CursorModes.Center)
            {
                // Generates relative offsets: -400, -200, 14.252.000, +200, +400 (for 1K span)
                labels[0].Text = string.Format("-{0}", FormatOffset(stepHz * 2));
                labels[1].Text = string.Format("-{0}", FormatOffset(stepHz));
                labels[2].Text = frequencyManagement.FormatFrequency(frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, null));
                labels[3].Text = string.Format("+{0}", FormatOffset(stepHz));
                labels[4].Text = string.Format("+{0}", FormatOffset(stepHz * 2));
            }
            else if (currentCursorMode == CursorModes.Cursor)
            {
                long freq = frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, null);

                // Which label index should display the current frequency?
                // normal = center, left edge = left-most, right edge = right-most
                int anchorIndex = 2;

                if (maxLeftPositionReached)
                    anchorIndex = 0;
                else if (maxRightPositionReached)
                    anchorIndex = 4;

                for (int i = 0; i < labels.Length; i++)
                {
                    long offset = (i - anchorIndex) * stepHz;
                    long targetFreq = freq + offset;

                    labels[i].Text = frequencyManagement.FormatFrequency(targetFreq);
                }
            }

            // 4. Update the cursor pixel step (keeps double precision for the UI calculation)
            currentCursorPixelStep = (SpectrumWidthPixels / (double)spanHz) * 1000.0;
        }

        // Helper method adjusted for long inputs
        private string FormatOffset(long hz)
        {
            return hz >= 1000 ? string.Format("{0}k", hz / 1000) : hz.ToString();
        }

        private void ChangeCursorMode(byte cursorMode)
        {
            switch (cursorMode)
            {
                case CursorModes.Center:
                    currentCursorMode = CursorModes.Center;
                    mainWindow.CursorModeTextBlock.Text = "CENTER";
                    mainWindow.GreenCursorCanvas.Visibility = Visibility.Collapsed;
                    mainWindow.RedCursorCanvas.Visibility = Visibility.Visible;
                    break;
                case CursorModes.Cursor:
                    currentCursorMode = CursorModes.Cursor;
                    mainWindow.CursorModeTextBlock.Text = "CURSOR";
                    mainWindow.GreenCursorCanvas.Visibility = Visibility.Visible;
                    mainWindow.RedCursorCanvas.Visibility = Visibility.Visible;
                    break;
                case CursorModes.Fix:
                    currentCursorMode = CursorModes.Fix;
                    mainWindow.CursorModeTextBlock.Text = "FIX";
                    mainWindow.GreenCursorCanvas.Visibility = Visibility.Collapsed;
                    mainWindow.RedCursorCanvas.Visibility = Visibility.Collapsed;
                    break;
                default:
                    currentCursorMode = CursorModes.Center;
                    mainWindow.CursorModeTextBlock.Text = "CENTER";
                    mainWindow.GreenCursorCanvas.Visibility = Visibility.Collapsed;
                    mainWindow.RedCursorCanvas.Visibility = Visibility.Visible;
                    break;
            }
            ChangeCursorPosition(SimulatedWaterfall.Cursors.GreenCursor, GreenCursorCenterPosition);
            ChangeCursorPosition(SimulatedWaterfall.Cursors.RedCursor, RedCursorCenterPosition);
        }
        public void ChangeCursorPosition(byte cursor, Double position)
        {
            Double maxLeftPosition = 14;
            Double maxRightPosition = 613;
            Double finalPosition = position;

            maxLeftPositionReached = false;
            maxRightPositionReached = false;

            mainWindow.LeftHandStopIndicatorCanvas.Visibility = Visibility.Collapsed;
            mainWindow.RightHandStopIndicatorCanvas.Visibility = Visibility.Collapsed;

            if (position <= maxLeftPosition)
            { 
                mainWindow.LeftHandStopIndicatorCanvas.Visibility = Visibility.Visible;
                maxLeftPositionReached = true;
            }
            else if (position >= maxRightPosition)
            {    
                mainWindow.RightHandStopIndicatorCanvas.Visibility = Visibility.Visible;
                maxRightPositionReached = true;
            }

            switch (cursor)
            {
                case Cursors.GreenCursor: 
                    if (maxLeftPositionReached == false && maxRightPositionReached == false)
                    {
                        Canvas.SetLeft(mainWindow.GreenCursorCanvas, finalPosition);
                        currentGreenCursorPosition = finalPosition;
                    }
                    // Update frequency based on Green Cursor's new location
                    UpdateCurrentFrequencyFromCursor(currentGreenCursorPosition);  
                    break;
                case Cursors.RedCursor:       
                    if (maxLeftPositionReached == false && maxRightPositionReached == false)
                    {
                        Canvas.SetLeft(mainWindow.RedCursorCanvas, finalPosition);
                        currentRedCursorPosition = finalPosition;
                    }
                    // Update frequency based on Red Cursor's new location
                    UpdateCurrentFrequencyFromCursor(currentRedCursorPosition);     
                    break;
                default:
                    if (maxLeftPositionReached == false && maxRightPositionReached == false)
                    {
                        Canvas.SetLeft(mainWindow.GreenCursorCanvas, GreenCursorCenterPosition);
                        Canvas.SetLeft(mainWindow.RedCursorCanvas, RedCursorCenterPosition);
                        currentGreenCursorPosition = GreenCursorCenterPosition;
                        currentRedCursorPosition = RedCursorCenterPosition;
                    }
                    // Reset back to center frequency
                    UpdateCurrentFrequencyFromCursor(RedCursorCenterPosition);
                    break;
            }
            AnimateRedCursorExtension(mainWindow.RedCursorCanvas, 60);
        }
        public void ChangeSpanCenterFrequency(long centerFrequency)
        {
            if(currentCursorMode == CursorModes.Center)
                mainWindow.SpanLabel3TextBlock.Text = frequencyManagement.FormatFrequency(centerFrequency);

            if (frequencyManagement.lastRigState.RXFrequencyHz != frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, null))
                AnimateRedCursorExtension(mainWindow.RedCursorCanvas, 60);
        }

        private const double CenterPixel = 313.0;
        // 14.252.000 Hz center baseline from your labels
        private const double CenterFrequencyHz = 14252000.0;

        public void UpdateCurrentFrequencyFromCursor(double cursorPosition)
        {
            // 1. How far is the cursor from the center in pixels?
            double pixelOffset = cursorPosition - CenterPixel;

            // 2. Prevent division by zero if currentCursorPixelStep isn't initialized
            if (currentCursorPixelStep <= 0) return;

            // 3. Calculate how many 1 kHz steps this pixel distance represents
            double numberOfSteps = pixelOffset / currentCursorPixelStep;

            // 4. Multiply steps by 1000 Hz to get the actual frequency offset
            double frequencyOffsetHz = numberOfSteps * 1000.0;

            // 5. Update the current frequency
            frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, (long)Math.Round(CenterFrequencyHz + frequencyOffsetHz), mainWindow.MainFrequencyTextBlock);
            
            if (mainWindow.LeftHandStopIndicatorCanvas.Visibility == Visibility.Visible || mainWindow.RightHandStopIndicatorCanvas.Visibility == Visibility.Visible)
                ChangeSpanLegends(currentFrequencySpan);
        }
        
        public async void AnimateRedCursorExtension(Canvas redCursorCanvas, double extensionHeight = 60)
        {
            if (redCursorCanvas == null)
                return;

            var extraStem = new Polygon
            {
                Fill = Brushes.Red,
                Opacity = 1,
                Points = new PointCollection
        {
            new Point(0, 0),
            new Point(0, extensionHeight),
            new Point(1, extensionHeight),
            new Point(1, 0)
        }
            };

            // Continue exactly from your current stem
            Canvas.SetLeft(extraStem, 7);
            Canvas.SetTop(extraStem, 45);

            redCursorCanvas.Children.Add(extraStem);

            extraStem.BeginAnimation(
                UIElement.OpacityProperty,
                new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(2)
                });

            // Wait until fade completes
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Remove extension completely
            redCursorCanvas.Children.Remove(extraStem);
        }

        private async void FadoutBorderWindow(Border borderWindow, int initalHoldValue = 260)
        {
            // 1. THE HOLD: Wait for 1 second asynchronously without blocking the UI
            await Task.Delay(initalHoldValue);

            // 2. THE FADE: Create a direct, non-storyboard animation
            DoubleAnimation fadeAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(0.5) // Fades over 0.5 seconds
            };

            // This ensures the opacity stays at 0.0 when finished
            fadeAnimation.FillBehavior = FillBehavior.HoldEnd;

            // 3. START THE FADE
            borderWindow.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);

            // 4. THE HIDE: Wait for the 0.5-second fade to finish
            await Task.Delay(550);

            // 5. Hard-set the visibility and clear the animation to free up the property
            borderWindow.Visibility = Visibility.Hidden;
            borderWindow.BeginAnimation(UIElement.OpacityProperty, null);
        }
    }
}