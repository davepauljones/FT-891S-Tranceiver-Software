using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAESU_FT_891_Front_End
{
    public class SRRSManager
    {
        //SRRSManager Software Rig Rig Software Manager
        //Goal to strictly control all radio setting and variables, that apply default values, last DateTime etc
        //Start with Filter Screen, than expand to other areas.

        public struct SRRSManagedItems
        {
            public const int NotchFilterSwitch = 0;
            public const int NotchFilterValue = 1;

            public const int WidthFilter = 2;
            public const int ShiftFilter = 3;
        }
        public struct Origins
        {
            public const int Init = 0;
            public const int Software = 1;
            public const int Rig = 2;
        }
        public struct RequiredActions
        {
            public const int None = 0;
            public const int UpdateUI = 1;
            public const int UpdateUI_SendCAT = 2;
        }

        public class SRRSCriteria
        {
            //Main fields
            public Int32 ID { get; set; }
            public int CurrentValue { get; set; }
            public int LastValue { get; set; }
            public int DefaultValue { get; set; }
            public int MinValue { get; set; }
            public int MaxValue { get; set; }
            public int StepValue { get; set; }
            public DateTime LastUpdateDateTime { get; set; }
            public int Origin { get; set; }
            
            //Returned Fields
            public int RequiredAction { get; set; }
            public bool CATForceSend { get; set; }
        }

        MainWindow mainWindow;

        public List<SRRSCriteria> SRRSCriteriaList = new List<SRRSCriteria>();

        public SRRSManager(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            Init_SRRSManager();
        }

        private void Init_SRRSManager()
        {
            SRRSCriteriaList.Add(new SRRSCriteria { ID = SRRSManagedItems.NotchFilterSwitch, CurrentValue = 0, LastValue = FT891S_CatManager.currentRadioState.NotchSwitchValue, DefaultValue = 0, MinValue = 0, MaxValue = 1, StepValue = 0, LastUpdateDateTime = DateTime.Now, Origin = Origins.Init });
            SRRSCriteriaList.Add(new SRRSCriteria { ID = SRRSManagedItems.NotchFilterValue, CurrentValue = 1600, LastValue = FT891S_CatManager.currentRadioState.NotchValue, DefaultValue = 1600, MinValue = 10, MaxValue = 3200, StepValue = 10, LastUpdateDateTime = DateTime.Now, Origin = Origins.Init });
            
            SRRSCriteriaList.Add(new SRRSCriteria { ID = SRRSManagedItems.WidthFilter, CurrentValue = 14, LastValue = 14, DefaultValue = 14, MinValue = 0, MaxValue = 21, StepValue = 0, LastUpdateDateTime = DateTime.Now, Origin = Origins.Init });
            SRRSCriteriaList.Add(new SRRSCriteria { ID = SRRSManagedItems.ShiftFilter, CurrentValue = 0, LastValue = 0, DefaultValue = 0, MinValue = 0, MaxValue = 1200, StepValue = 20, LastUpdateDateTime = DateTime.Now, Origin = Origins.Init });
        }

        public SRRSCriteria SetSRRSManagedItem(Int32 ID, int CurrentValue, int Origin, bool CatForceSend)
        {
            SRRSCriteria _SRRSCriteria = new SRRSCriteria();

            var criteria = SRRSCriteriaList[ID];

            if (CurrentValue >= criteria.MinValue &&
                CurrentValue <= criteria.MaxValue &&
                (criteria.StepValue == 0 || CurrentValue % criteria.StepValue == 0))
            {
                // This runs if:
                // 1. It's within the Min/Max bounds, AND
                // 2. EITHER StepValue is 0 (ignored) OR CurrentValue divides evenly by StepValue.
                criteria.CurrentValue = CurrentValue;

                Console.Write("SetSRRSManagedItem_criteria.CurrentValue = ");
                Console.WriteLine(criteria.CurrentValue);
            }
            else
            {
                Console.Write("SetSRRSManagedItem_CurrentValue Not Valid = ");
                Console.WriteLine(criteria.CurrentValue);

                Console.Write("SetSRRSManagedItem_Trying LastValue = ");
                Console.WriteLine(criteria.LastValue);

                if (criteria.LastValue >= criteria.MinValue && criteria.LastValue <= criteria.MaxValue &&
                    (criteria.StepValue == 0 || criteria.LastValue % criteria.StepValue == 0))
                {
                    CurrentValue = criteria.LastValue;

                    Console.Write("SetSRRSManagedItem_criteria.LastValue is Good = ");
                    Console.WriteLine(criteria.LastValue);
                }
                else
                {
                    CurrentValue = criteria.DefaultValue;

                    Console.Write("SetSRRSManagedItem_criteria.LastValue is Bad = ");
                    Console.WriteLine(criteria.LastValue);

                    Console.Write("SetSRRSManagedItem_CurrentValue Set To Default = ");
                    Console.WriteLine(CurrentValue);
                }
            }

            SRRSCriteriaList[ID].Origin = Origin;

            if (CatForceSend)
            {
                Console.WriteLine("SetSRRSManagedItem_CatForceSend True");

                if (CurrentValue != SRRSCriteriaList[ID].LastValue)
                {
                    if (Origin == Origins.Software)
                        _SRRSCriteria.RequiredAction = RequiredActions.UpdateUI_SendCAT;
                    else if (Origin == Origins.Rig)
                        _SRRSCriteria.RequiredAction = RequiredActions.UpdateUI;
                    else
                        _SRRSCriteria.RequiredAction = RequiredActions.UpdateUI;

                    SRRSCriteriaList[ID].LastUpdateDateTime = DateTime.Now;
                    SRRSCriteriaList[ID].LastValue = CurrentValue;
                }
                else
                    _SRRSCriteria.RequiredAction = RequiredActions.None;
            }
            else if (DateTime.Now > SRRSCriteriaList[ID].LastUpdateDateTime + TimeSpan.FromMilliseconds(1000))
            {
                if (CurrentValue != SRRSCriteriaList[ID].LastValue)
                {
                    if (Origin == Origins.Software)
                        _SRRSCriteria.RequiredAction = RequiredActions.UpdateUI_SendCAT;
                    else if (Origin == Origins.Rig)
                        _SRRSCriteria.RequiredAction = RequiredActions.UpdateUI;

                    SRRSCriteriaList[ID].LastUpdateDateTime = DateTime.Now;
                    SRRSCriteriaList[ID].LastValue = CurrentValue;
                }
                else
                    _SRRSCriteria.RequiredAction = RequiredActions.None;
            }
            else
            {
                _SRRSCriteria.RequiredAction = RequiredActions.None;
                
                Console.WriteLine("SetSRRSManagedItem_SRRSCriteria.RequiredAction = RequiredActions.None");
            }

            return _SRRSCriteria;
        }
    }
}
