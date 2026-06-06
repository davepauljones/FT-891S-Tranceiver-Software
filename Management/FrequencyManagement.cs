using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static YAESU_FT_891_Front_End.MemorySlot;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigState;

namespace YAESU_FT_891_Front_End
{
    public class FrequencyManagement
    {
        MainWindow mainWindow;

        public RigState lastRigState = new RigState();

        public FrequencyManagement(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        //Required Functions
        public long GetFrequency(Int32 memorySlotIndex, byte frequencyLocation, TextBlock textBlock = null)
        {
            long returnFrequency = 0;

            RigState memorySlotToGet;

            if (mainWindow.memorySlot.MemorySlotDictionary.TryGetValue((int)memorySlotIndex, out memorySlotToGet))
            {
                switch (frequencyLocation)
                {
                    case FrequencyLocations.RXFrequencyHz:
                        returnFrequency = memorySlotToGet.RXFrequencyHz;
                        break;
                    case FrequencyLocations.TXFrequencyHz:
                        returnFrequency = memorySlotToGet.TXFrequencyHz;
                        break;
                    default:
                        returnFrequency = 0;
                        break;
                }

                if (textBlock != null)
                {
                    textBlock.Text = FormatFrequency(returnFrequency);
                }

                if (memorySlotIndex == MemorySlots.VFO_A) UpdateVFODialPosition(returnFrequency);
            }
            else
            {
                Console.WriteLine("FrequencyManagement.GetFrequency");
                Console.WriteLine("memorySlotDictionary.TryGetValue Error!");
            }

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine("FrequencyManagement.GetFrequency");
                Console.Write("memorySlotIndex set to ");
                Console.WriteLine(memorySlotIndex);
                Console.Write("frequencyLocation set to ");
                Console.WriteLine(frequencyLocation);
                Console.Write("Frequencey set to ");
                Console.WriteLine(returnFrequency);
            }

            lastRigState.RXFrequencyHz = returnFrequency;

            return returnFrequency;
        }
        public void SetFrequency(Int32 memorySlotIndex, byte frequencyLocation, long frequency, TextBlock textBlock = null)
        {
            RigState memorySlotToSet;

            if (mainWindow.memorySlot.MemorySlotDictionary.TryGetValue((int)memorySlotIndex, out memorySlotToSet))
            {
                switch (frequencyLocation)
                {
                    case FrequencyLocations.RXFrequencyHz:
                        memorySlotToSet.RXFrequencyHz = frequency;
                        break;
                    case FrequencyLocations.TXFrequencyHz:
                        memorySlotToSet.TXFrequencyHz = frequency;
                        break;
                }

                if (textBlock != null)
                {
                    textBlock.Text = FormatFrequency(frequency);
                }

                //added temp to get functions working in the intrim
                mainWindow.yAESU_FT_891_CAT_Dictionary.SetFrequency(frequency);

                if (memorySlotIndex == MemorySlots.VFO_A) UpdateVFODialPosition(frequency);
            }
            else
            {
                Console.WriteLine("FrequencyManagement.SetFrequency");
                Console.WriteLine("memorySlotDictionary.TryGetValue Error!");
            }

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine("FrequencyManagement.SetFrequency");
                Console.Write("memorySlotIndex set to ");
                Console.WriteLine(memorySlotIndex);
                Console.Write("frequencyLocation set to ");
                Console.WriteLine(frequencyLocation);
                Console.Write("Frequencey set to ");
                Console.WriteLine(frequency);
            }

            lastRigState.RXFrequencyHz = frequency;
        }

        private void UpdateVFODialPosition(long freq)
        {
            if (freq != lastRigState.RXFrequencyHz)
            {
                if (freq > lastRigState.RXFrequencyHz)
                    mainWindow.SpinDial(MyStructs.DialDirection.Clockwise);
                else if (freq < lastRigState.RXFrequencyHz)
                    mainWindow.SpinDial(MyStructs.DialDirection.AntiClockwise);
            }
        }
        public string FormatFrequency(long hz)
        {
            string s = hz.ToString();

            int firstGroup = s.Length % 3;
            if (firstGroup == 0) firstGroup = 3;

            var parts = new List<string>();
            parts.Add(s.Substring(0, firstGroup));

            for (int i = firstGroup; i < s.Length; i += 3)
            {
                parts.Add(s.Substring(i, 3));
            }

            return string.Join(".", parts);
        }
    }
}