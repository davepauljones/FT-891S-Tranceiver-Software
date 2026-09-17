using System;
using System.Speech.Synthesis;

namespace YAESU_FT_891_Front_End
{
    public class Functions
    {
        public static int ConvertRecognizedTextToInt(
            string recognizedText)
        {
            int numericCommand;

            if (int.TryParse(
                recognizedText,
                out numericCommand))
            {
                return numericCommand;
            }
            else
            {
                return -1;
            }
        }

        public static int ValuesToInt(
            string value1,
            string value2,
            string value3)
        {
            string d1 = WrittenNumberToDigit(value1);
            string d2 = WrittenNumberToDigit(value2);
            string d3 = WrittenNumberToDigit(value3);

            string digitsString = string.Empty;

            int n1 = ConvertRecognizedTextToInt(d1);
            int n2 = ConvertRecognizedTextToInt(d2);
            int n3 = ConvertRecognizedTextToInt(d3);

            digitsString =
                n1.ToString() +
                n2.ToString() +
                n3.ToString();

            return ConvertRecognizedTextToInt(digitsString);
        }

        public static string WrittenNumberToDigit(
            string writtenNumber)
        {
            string result = string.Empty;

            switch (writtenNumber.ToLower())
            {
                case "zero": result = "0"; break;
                case "one": result = "1"; break;
                case "two": result = "2"; break;
                case "three": result = "3"; break;
                case "four": result = "4"; break;
                case "five": result = "5"; break;
                case "six": result = "6"; break;
                case "seven": result = "7"; break;
                case "eight": result = "8"; break;
                case "nine": result = "9"; break;

                default:
                    result = "0";
                    break;
            }

            return result;
        }

        public static void PlayTextSynth(
            MainWindow mainWindow,
            string text,
            int volumeLevel = 60,
            bool overrideAsync = false)
        {
            if (mainWindow.fT891SpeechRecognition != null)
            {
                mainWindow.fT891SpeechRecognition
                    .SwitchOffVoiceCommands(true);
            }

            SpeechSynthesizer speechSynthesizer =
                new SpeechSynthesizer();

            volumeLevel =
                mainWindow.fT891SpeechRecognition.SpeechLevel;

            speechSynthesizer.Volume = volumeLevel;

            if (overrideAsync)
            {
                // Synchronous speech:
                // recognition remains disabled until the
                // synthesizer has finished speaking.
                speechSynthesizer.Speak(text);

                speechSynthesizer.Dispose();

                if (mainWindow.fT891SpeechRecognition != null)
                {
                    mainWindow.fT891SpeechRecognition
                        .SwitchOnVoiceCommands(true);
                }
            }
            else
            {
                // Asynchronous speech.

                speechSynthesizer.SpeakCompleted +=
                    (sender, e) =>
                    {
                        speechSynthesizer.Dispose();

                        if (mainWindow.fT891SpeechRecognition != null)
                        {
                            mainWindow.fT891SpeechRecognition
                                .SwitchOnVoiceCommands(true);
                        }
                    };

                speechSynthesizer.SpeakAsync(text);
            }
        }
    }
}