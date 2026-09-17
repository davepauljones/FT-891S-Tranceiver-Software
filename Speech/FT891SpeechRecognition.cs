using System;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace YAESU_FT_891_Front_End
{
    public class FT891SpeechRecognition
    {
        struct VoiceCommands
        {
            public const byte SetVolume = 0;
            public const byte EnableVoice = 1;
            public const byte DisableVoice = 2;
            public const byte WhatTimeIsIt = 3;
            public const byte SetWatts = 4;
            public const byte SetRF = 5;
        }

        MainWindow mainWindow;

        public SpeechRecognitionEngine recognizer =
            new SpeechRecognitionEngine(
                new System.Globalization.CultureInfo(
                    CultureInfo.CurrentUICulture.Name));

        public static bool VoiceCommandsCanStart = true;
        public static bool SpeechRecognizerEnabled = true;

        double ConfidenceLevel = 0.89;
        public int SpeechLevel = 60;

        bool AlreadyDisabled = false;
        bool AlreadyEnabled = false;

        DispatcherTimer SpeechRecognitionSleepTimer_dispatcherTimer =
            new DispatcherTimer();

        public FT891SpeechRecognition(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            if (VoiceCommandsCanStart)
            {
                Init_SpeechRecognitionSleepTimer();
                Init_SpeechRecognizer();
            }
        }

        private void Init_SpeechRecognitionSleepTimer()
        {
            SpeechRecognitionSleepTimer_dispatcherTimer.Tick +=
                new EventHandler(
                    SpeechRecognitionSleepTimer_dispatcherTimer_Tick);

            SpeechRecognitionSleepTimer_dispatcherTimer.Interval =
                TimeSpan.FromSeconds(4);
        }

        private void SpeechRecognitionSleepTimer_dispatcherTimer_Tick(
            object sender,
            EventArgs e)
        {
            SpeechRecognitionSleepTimer_dispatcherTimer.Stop();

            SpeechRecognitionListeningState =
                SpeechRecognitionStates.Asleep;

            mainWindow.buttonConsole.UpdateButton(
                StatusBarIndicatorConsole.Console.Recognition,
                true,
                true);

            mainWindow.CurrentConfidenceLevelTextBlock.Text = "0.00";
        }

        public void SwitchOnVoiceCommands(bool overrideIndicator = false)
        {
            if (!overrideIndicator)
            {
                mainWindow.buttonConsole.UpdateButton(
                    StatusBarIndicatorConsole.Console.Recognition,
                    true,
                    true);
            }

            SpeechRecognizerEnabled = true;
        }

        public void SwitchOffVoiceCommands(bool overrideIndicator = false)
        {
            if (!overrideIndicator)
            {
                mainWindow.buttonConsole.UpdateButton(
                    StatusBarIndicatorConsole.Console.Recognition,
                    false,
                    false);
            }

            SpeechRecognizerEnabled = false;
        }

        public void SetConfidenceLevel(int level)
        {
            double cl = 0.92;

            switch (level)
            {
                case 0: cl = 0.00; break;
                case 1: cl = 0.86; break;
                case 2: cl = 0.87; break;
                case 3: cl = 0.88; break;
                case 4: cl = 0.89; break;
                case 5: cl = 0.90; break;
                case 6: cl = 0.91; break;
                case 7: cl = 0.92; break;
                case 8: cl = 0.93; break;
                case 9: cl = 0.94; break;
                default: cl = 0.92; break;
            }

            ConfidenceLevel = cl;

            mainWindow.CurrentConfidenceLevelTextBlock.Text =
                ConfidenceLevel.ToString("F2");
        }

        public void SetSpeechLevel(int level)
        {
            int sl = 60;

            switch (level)
            {
                case 0: sl = 10; break;
                case 1: sl = 20; break;
                case 2: sl = 30; break;
                case 3: sl = 40; break;
                case 4: sl = 50; break;
                case 5: sl = 60; break;
                case 6: sl = 70; break;
                case 7: sl = 80; break;
                case 8: sl = 90; break;
                case 9: sl = 100; break;
                default: sl = 60; break;
            }

            SpeechLevel = sl;

            try
            {
                mainWindow.VolumeLevelTextBlock.Text = level.ToString();
            }
            catch
            {
                Console.WriteLine("Fix SetSpeechLevel at some point");
            }
        }

        private int SeeIfLastThreeWordsAreNumbers(List<string> words)
        {
            if (words.Count == 1)
            {
                words.Add("0");
                words.Add("0");
                words.Add("0");
            }
            else if (words.Count == 2)
            {
                words.Add("0");
                words.Add("0");

                words[3] = words[1];
                words[1] = "0";
            }
            else if (words.Count == 3)
            {
                words.Add("0");

                words[3] = words[2];
                words[2] = words[1];
                words[1] = "0";
            }

            return Functions.ValuesToInt(
                words[1],
                words[2],
                words[3]);
        }

        private void speechRecognizer_ActionHypothesized(
            object sender,
            SpeechHypothesizedEventArgs e)
        {
            if (e.Result == null || e.Result.Words.Count == 0)
                return;

            RecognizedWordUnit rwuw = e.Result.Words[0];

            mainWindow.CurrentConfidenceLevelTextBlock.Text =
                e.Result.Confidence.ToString("0.00");

            if (rwuw.Text.ToString().ToLower() == "yaysue" &&
                e.Result.Confidence > ConfidenceLevel)
            {
                mainWindow.buttonConsole.UpdateButton(
                    StatusBarIndicatorConsole.Console.Recognition,
                    true,
                    true,
                    true);

                SpeechRecognitionListeningState =
                    SpeechRecognitionStates.Awake;

                SpeechRecognitionSleepTimer_dispatcherTimer.Stop();
                SpeechRecognitionSleepTimer_dispatcherTimer.Start();

                if (e.Result.Words.Count > 2 &&
                    e.Result.Words.Count < 7)
                {
                    List<string> words = new List<string>();

                    foreach (RecognizedWordUnit rwu in e.Result.Words)
                    {
                        words.Add(
                            rwu.Text.ToString().ToLower());
                    }

                    if (words[0] == "yaysue" &&
                        SpeechRecognitionListeningState ==
                        SpeechRecognitionStates.Awake)
                    {
                        words.RemoveAt(0);

                        string text = string.Empty;

                        for (int w = 0; w < words.Count; w++)
                        {
                            text += words[w];

                            if (w < words.Count - 1)
                                text += " ";
                        }

                        mainWindow.StatusBarTextBlock.Text = text;

                        if (words.Count < 6)
                            EvaluateVoiceCommand(words, text);
                    }
                }
            }
        }

        private void ExecuteVoiceCommand(
            byte command,
            int value,
            bool AllEngines = false)
        {
            if (value > 255)
                return;

            if (AllEngines)
                value = 254;

            switch (command)
            {
                case VoiceCommands.SetVolume:

                    if (value > -1 && value < 10)
                    {
                        SetSpeechLevel(value);

                        Functions.PlayTextSynth(
                            mainWindow,
                            "Speech volume set to " +
                            value.ToString());

                        mainWindow.StatusBarTextBlock.Text =
                            "Speech volume set to " +
                            value.ToString();
                    }

                    break;

                case VoiceCommands.EnableVoice:

                    if (!SpeechRecognizerEnabled)
                    {
                        mainWindow.buttonConsole.UpdateButton(
                            StatusBarIndicatorConsole.Console.Recognition,
                            true,
                            true);

                        AlreadyDisabled = false;

                        mainWindow.StatusBarTextBlock.Text =
                            "Voice commands are now enabled!";

                        Functions.PlayTextSynth(
                            mainWindow,
                            "Voice commands are now enabled!");

                        SpeechRecognizerEnabled = true;
                    }
                    else
                    {
                        if (!AlreadyEnabled)
                        {
                            AlreadyEnabled = true;

                            Functions.PlayTextSynth(
                                mainWindow,
                                "Voice commands are already enabled!");

                            SpeechRecognizerEnabled = true;
                        }
                        else
                        {
                            Functions.PlayTextSynth(
                                mainWindow,
                                "Its enabled!");

                            SpeechRecognizerEnabled = true;
                        }
                    }

                    break;

                case VoiceCommands.DisableVoice:

                    if (SpeechRecognizerEnabled)
                    {
                        mainWindow.buttonConsole.UpdateButton(
                            StatusBarIndicatorConsole.Console.Recognition,
                            false,
                            false);

                        AlreadyEnabled = false;

                        mainWindow.StatusBarTextBlock.Text =
                            "Voice commands are now disabled!";

                        Functions.PlayTextSynth(
                            mainWindow,
                            "Voice commands are now disabled!");

                        SpeechRecognizerEnabled = false;
                    }
                    else
                    {
                        if (!AlreadyDisabled)
                        {
                            AlreadyDisabled = true;

                            Functions.PlayTextSynth(
                                mainWindow,
                                "Voice commands are already disabled!");

                            SpeechRecognizerEnabled = false;
                        }
                        else
                        {
                            Functions.PlayTextSynth(
                                mainWindow,
                                "Its disabled!");

                            SpeechRecognizerEnabled = false;
                        }
                    }

                    break;

                case VoiceCommands.WhatTimeIsIt:

                    Functions.PlayTextSynth(
                        mainWindow,
                        "The time is " +
                        DateTime.Now.ToString("hh:mm tt"));

                    break;

                case VoiceCommands.SetWatts:

                    if (value >= 5 && value <= 100)
                    {
                        Functions.PlayTextSynth(
                            mainWindow,
                            "The Watts has been set");
                    }

                    break;

                case VoiceCommands.SetRF:

                    if (value >= 0 && value <= 30)
                    {
                        Functions.PlayTextSynth(
                            mainWindow,
                            "The RF has been set");
                    }

                    break;
            }
        }

        private void EvaluateVoiceCommand(
            List<string> words,
            string text)
        {
            if (SpeechRecognitionListeningState ==
                SpeechRecognitionStates.Awake)
            {
                if (text == "enable voice commands")
                {
                    ExecuteVoiceCommand(
                        VoiceCommands.EnableVoice,
                        0);
                }
                else if (text == "disable voice commands")
                {
                    ExecuteVoiceCommand(
                        VoiceCommands.DisableVoice,
                        0);
                }

                if (SpeechRecognizerEnabled &&
                    words.Count > 2)
                {
                    if (text == "what time is it")
                    {
                        ExecuteVoiceCommand(
                            VoiceCommands.WhatTimeIsIt,
                            0);
                    }
                    else if (words[0] == "set")
                    {
                        if (words[1] == "volume")
                        {
                            ExecuteVoiceCommand(
                                VoiceCommands.SetVolume,
                                SeeIfLastThreeWordsAreNumbers(words));
                        }
                        else if (words[1] == "watts")
                        {
                            ExecuteVoiceCommand(
                                VoiceCommands.SetWatts,
                                SeeIfLastThreeWordsAreNumbers(words));
                        }
                        else if (words[1] == "rf")
                        {
                            ExecuteVoiceCommand(
                                VoiceCommands.SetRF,
                                SeeIfLastThreeWordsAreNumbers(words));
                        }
                    }
                }
            }
        }

        protected void Init_SpeechRecognizer()
        {
            CreateGrammar(
                SpeechRecognitionGrammars.EnableDisableVoiceCommands,
                new Choices(new string[] { "enable", "disable" }),
                new Choices(new string[] { "voice" }),
                new Choices(new string[] { "commands" }));

            CreateGrammar(
                SpeechRecognitionGrammars.WhatTimeIsIt,
                new Choices(new string[] { "what" }),
                new Choices(new string[] { "time" }),
                new Choices(new string[] { "is" }),
                new Choices(new string[] { "it" }));

            CreateGrammar(
                SpeechRecognitionGrammars.SetVolumeSkin,
                new Choices(new string[] { "set" }),
                new Choices(new string[] { "volume", "skin" }),
                Number_Param_Choices,
                Number_Param_Choices,
                Number_Param_Choices,
                Number_Param_Choices);

            CreateGrammar(
                SpeechRecognitionGrammars.SetWatts,
                new Choices(new string[] { "set" }),
                new Choices(new string[] { "Watts" }),
                Number_Param_Choices,
                Number_Param_Choices,
                Number_Param_Choices,
                Number_Param_Choices);

            CreateGrammar(
                SpeechRecognitionGrammars.SetRF,
                new Choices(new string[] { "set" }),
                new Choices(new string[] { "RF" }),
                Number_Param_Choices,
                Number_Param_Choices,
                Number_Param_Choices,
                Number_Param_Choices);

            recognizer.SpeechHypothesized +=
                speechRecognizer_ActionHypothesized;

            try
            {
                recognizer.SetInputToDefaultAudioDevice();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(
                    "An error occurred while trying to set the " +
                    "default audio device, check you have a device " +
                    "installed: " + e.Message);

                SpeechRecognizerEnabled = false;
                VoiceCommandsCanStart = false;

                SwitchOffVoiceCommands();

                return;
            }

            SwitchSpeechRecognitionListeningState(
                SpeechRecognitionStates.Asleep);

            try
            {
                recognizer.RecognizeAsync(
                    RecognizeMode.Multiple);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(
                    "An error occurred while starting speech " +
                    "recognition: " + e.Message);
            }
        }

        Choices Number_Param_Choices =
            new Choices(
                new string[]
                {
                    "zero",
                    "one",
                    "two",
                    "three",
                    "four",
                    "five",
                    "six",
                    "seven",
                    "eight",
                    "nine"
                });

        private void CreateGrammar(
            byte Grammars,
            Choices MainCommands,
            Choices SubCommands,
            Choices SubOptions1,
            Choices SubOptions2 = null,
            Choices SubOptions3 = null,
            Choices SubOptions4 = null)
        {
            GrammarBuilder _GrammarBuilder =
                new GrammarBuilder();

            _GrammarBuilder.Culture =
                recognizer.RecognizerInfo.Culture;

            _GrammarBuilder.Append(
                new Choices(
                    "yaysue",
                    "mac",
                    "very",
                    "which",
                    "test",
                    "river",
                    "attic"));

            _GrammarBuilder.Append(MainCommands);
            _GrammarBuilder.Append(SubCommands);

            if (SubOptions1 != null)
                _GrammarBuilder.Append(SubOptions1);

            if (SubOptions2 != null)
                _GrammarBuilder.Append(SubOptions2);

            if (SubOptions3 != null)
                _GrammarBuilder.Append(SubOptions3);

            if (SubOptions4 != null)
                _GrammarBuilder.Append(SubOptions4);

            switch (Grammars)
            {
                case SpeechRecognitionGrammars.EnableDisableVoiceCommands:

                    EnableDisableVoiceCommands =
                        new Grammar(_GrammarBuilder);

                    EnableDisableVoiceCommands.Name =
                        "EnableDisableVoiceCommands";

                    EnableDisableVoiceCommands.Enabled = false;

                    recognizer.LoadGrammar(
                        EnableDisableVoiceCommands);

                    break;

                case SpeechRecognitionGrammars.WhatTimeIsIt:

                    WhatTimeIsIt =
                        new Grammar(_GrammarBuilder);

                    WhatTimeIsIt.Name =
                        "WhatTimeIsIt";

                    WhatTimeIsIt.Enabled = true;

                    recognizer.LoadGrammar(
                        WhatTimeIsIt);

                    break;

                case SpeechRecognitionGrammars.SetVolumeSkin:

                    SetVolumeSkin =
                        new Grammar(_GrammarBuilder);

                    SetVolumeSkin.Name =
                        "SetVolumeSkin";

                    SetVolumeSkin.Enabled = false;

                    recognizer.LoadGrammar(
                        SetVolumeSkin);

                    break;

                case SpeechRecognitionGrammars.SetWatts:

                    SetWatts =
                        new Grammar(_GrammarBuilder);

                    SetWatts.Name =
                        "SetWatts";

                    // FIXED:
                    SetWatts.Enabled = true;

                    recognizer.LoadGrammar(
                        SetWatts);

                    break;

                case SpeechRecognitionGrammars.SetRF:

                    SetRF =
                        new Grammar(_GrammarBuilder);

                    SetRF.Name =
                        "SetRF";

                    SetRF.Enabled = true;

                    recognizer.LoadGrammar(
                        SetRF);

                    break;
            }

            // IMPORTANT:
            // Do NOT load another anonymous copy of the grammar here.
        }

        Grammar EnableDisableVoiceCommands;
        Grammar WhatTimeIsIt;
        Grammar SetVolumeSkin;
        Grammar SetWatts;
        Grammar SetRF;

        byte SpeechRecognitionListeningState =
            SpeechRecognitionStates.Asleep;

        private void SwitchSpeechRecognitionListeningState(
            byte state)
        {
            switch (state)
            {
                case SpeechRecognitionStates.Disabled:
                    break;

                case SpeechRecognitionStates.Asleep:

                    Console.WriteLine(
                        "SwitchSpeechRecognitionListeningState = " +
                        "SpeechRecognitionStates.Asleep");

                    SpeechRecognitionListeningState =
                        SpeechRecognitionStates.Asleep;

                    break;

                case SpeechRecognitionStates.Awake:

                    Console.WriteLine(
                        "SwitchSpeechRecognitionListeningState = " +
                        "SpeechRecognitionStates.Awake");

                    SpeechRecognitionListeningState =
                        SpeechRecognitionStates.Awake;

                    break;
            }

            recognizer.RequestRecognizerUpdate();
        }

        struct SpeechRecognitionStates
        {
            public const byte Disabled = 0;
            public const byte Asleep = 1;
            public const byte Awake = 2;
        }

        struct SpeechRecognitionGrammars
        {
            public const byte EnableDisableVoiceCommands = 0;
            public const byte WhatTimeIsIt = 1;
            public const byte SetVolumeSkin = 2;
            public const byte SetWatts = 3;
            public const byte SetRF = 4;
        }
    }
}