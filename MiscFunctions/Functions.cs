using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace YAESU_FT_891_Front_End
{
    public class Functions
    {
        public static int ConvertRecognizedTextToInt(string recognizedText)
        {
            int numericCommand;

            if (int.TryParse(recognizedText, out numericCommand))
                return numericCommand;
            else
                return -1;
        }

        public static int ValuesToInt(string value1, string value2, string value3)
        {
            string d1 = WrittenNumberToDigit(value1);
            string d2 = WrittenNumberToDigit(value2);
            string d3 = WrittenNumberToDigit(value3);

            string digitsString = string.Empty;

            int n1 = ConvertRecognizedTextToInt(d1);
            int n2 = ConvertRecognizedTextToInt(d2);
            int n3 = ConvertRecognizedTextToInt(d3);

            digitsString = n1.ToString() + n2.ToString() + n3.ToString();

            int r = ConvertRecognizedTextToInt(digitsString);

            //Console.Write("ConvertRecognizedTextToInt = ");
            //Console.WriteLine(r);

            return r;
        }
        public static string WrittenNumberToDigit(string writtenNumber)
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
                //case "ten": result = "10"; break;
                //case "eleven": result = "11"; break;
                //case "twelve": result = "12"; break;
                //case "thirteen": result = "13"; break;
                //case "fourteen": result = "14"; break;
                //case "fithteen": result = "15"; break;
                //case "sixteen": result = "16"; break;
                //case "seventeen": result = "17"; break;
                //case "eighteen": result = "18"; break;
                //case "nineteen": result = "19"; break;
                //case "twenty": result = "20"; break;
                //case "thirty": result = "30"; break;
                //case "fourty": result = "40"; break;
                //case "fithty": result = "50"; break;
                //case "sixty": result = "60"; break;
                //case "seventy": result = "70"; break;
                //case "eighty": result = "80"; break;
                //case "ninety": result = "90"; break;
                //case "hundred": result = "100"; break;
                default:
                    result = "0";
                    break;
            }

            //Console.Write("WrittenNumberToDigit in = ");
            //Console.Write(writtenNumber);
            //Console.Write(" out = ");
            //Console.WriteLine(result);

            return result;
        }

        public static void PlayTextSynth(MainWindow mainWindow, string text, int volumeLevel = 60, bool overrideAsync = false)
        {
            if (mainWindow.fT891SpeechRecognition != null) mainWindow.fT891SpeechRecognition.SwitchOffVoiceCommands(true);

            SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();

            volumeLevel = mainWindow.fT891SpeechRecognition.SpeechLevel;

            speechSynthesizer.Volume = volumeLevel;

            if (overrideAsync)
                speechSynthesizer.Speak(text);
            else
                speechSynthesizer.SpeakAsync(text);

            if (mainWindow.fT891SpeechRecognition != null) mainWindow.fT891SpeechRecognition.SwitchOnVoiceCommands(true);
        }

    }
}
