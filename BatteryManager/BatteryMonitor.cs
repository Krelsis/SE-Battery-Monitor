using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
//using Sandbox.Common.Components;
//using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game;
using VRageMath;
using System.Text.RegularExpressions;

namespace BatteryManager
{
    
    
    class Runtime
    {
        public static UpdateFrequency UpdateFrequency;
    }

    class API : Runtime
    {
        public IMyGridTerminalSystem GridTerminalSystem;
        public IMyProgrammableBlock Me;

        public void Echo(string message)
        {
            return;
        }
    }

    class Program : API
    {
        // Script Name (Programmable block gets appended with this)
        public static string ScriptName = " [BatteryMonitor]";
        // Time of last check
        public DateTime balanceTime = DateTime.Now;

        // Lists
        public List<IMyBatteryBlock> BatteryList = new List<IMyBatteryBlock>();
        public List<IMyReactor> FluidBatteryList = new List<IMyReactor>();
        public List<IMyCockpit> CockpitList = new List<IMyCockpit>();
        public List<IMyTerminalBlock> LcdList = new List<IMyTerminalBlock>();
        public List<IMyShipConnector> ConnectorList = new List<IMyShipConnector>();
        public List<IMyFunctionalBlock> FunctionalBlockList = new List<IMyFunctionalBlock>();

        // Battery Variables
        public double CurrentStoredPowerAll = 0;
        public double MaxStoredPowerAll = 0;
        public double CurrentInputAll = 0;
        public double MaxInputAll = 0;
        public double CurrentOutputAll = 0;
        public double MaxOutputAll = 0;
        public int RechargeModeAll = 0;
        public int DischargeModeAll = 0;
        public int AutoModeAll = 0;

        // Battery Strings
        public string StringCurrentStoredPowerAll = "";
        public string StringMaxStoredPowerAll = "";
        public string StringCurrentInputAll = "";
        public string StringMaxInputAll = "";
        public string StringCurrentOutputAll = "";
        public string StringMaxOutputAll = "";
        public string StringChargePotential = "";
        public string StringDischargePotential = "";
        public string StringBatteryChargeModes = "";

        // Percentage Strings
        public string StringCurrentStoredPowerAllPerc = "";
        public string StringCurrentInputAllPerc = "";
        public string StringCurrentOutputAllPerc = "";

        // Summary Strings
        public string BatterySummary = "";
        public string BatteryChargeSummary = "";
        public static string DashSeparator = "-------------------------------------------------------------";

        // Keywords
        public static string IgnoreKeyword = "[BatteryIgnore]";
        public static string AutoRechargeKeyword = "[AutoRecharge]";
        public static string AutoDischargeKeyword = "[AutoDischarge]";
        public static string PowerRequirementKeyword = "[PowerRequirement]";
        public static Dictionary<string, string> LCDKeywordsMap = new Dictionary<string, string>();

        //Group/Responsibility action stuff
        public static string ResponsibilityTagKeyword = "[BatteryResponsibility]";
        public enum GroupActionKeywordsEnum
        {
            None = 0,
            OffWhenTotalCapacityFull,
            OffWhenTotalCapacityMoreThan,
            OffWhenTotalCapacityMoreThanEqualTo,
            OnWhenTotalCapacityFull,
            OnWhenTotalCapacityMoreThan,
            OnWhenTotalCapacityMoreThanEqualTo,
            OffWhenTotalInputFull,
            OffWhenTotalInputMoreThan,
            OffWhenTotalInputMoreThanEqualTo,
            OnWhenTotalInputFull,
            OnWhenTotalInputMoreThan,
            OnWhenTotalInputMoreThanEqualTo,
            OffWhenTotalOutputFull,
            OffWhenTotalOutputMoreThan,
            OffWhenTotalOutputMoreThanEqualTo,
            OnWhenTotalOutputFull,
            OnWhenTotalOutputMoreThan,
            OnWhenTotalOutputMoreThanEqualTo

        }
        public enum GroupActionEnum
        {
            None = 0,
            TurnOn,
            TurnOff,
        }
        public enum GroupActionExpression
        {
            MoreThan = 0,
            MoreThanEqualTo
        }
        public enum GroupActionCriteria
        {
            None = 0,
            StoredCapacity,
            InputCapacity,
            OutputCapacity
        }
        public static Dictionary<GroupActionKeywordsEnum, string> GroupActionKeywordsMap;
        public static Dictionary<GroupActionKeywordsEnum, GroupActionEnum> GroupActionKeywordAction;
        public static Dictionary<GroupActionKeywordsEnum, GroupActionExpression> GroupActionsExpressionMap;
        public static Dictionary<GroupActionKeywordsEnum, GroupActionCriteria> GroupActionsCriteriaMap;
        public static Dictionary<GroupActionEnum, string> GroupActionsMap;

        // Misc
        public static List<string> OtherBatteryTypes = new List<string>
        {
            "FluidBattery",
            "Fluid Battery"
        };

        // Script Responsibility
        public string Responsibility = "";

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main()
        {
            Me.CustomName += !KeywordInName(Me, ScriptName) ? ScriptName : "";
            CheckBlockNameKeywordCapitalisation(Me, ScriptName);

            Responsibility = DetermineResponsibility(Me);

            HandleBatteries();

            LCDKeywordsMap = new Dictionary<string, string>
            {
                { "[BatteryStatus]", BatterySummary},
                { "[BatterySummary]", BatterySummary},
                { "[BatteryChargeSummary]", BatteryChargeSummary},
                { "[BatteryChargeStatus]", BatteryChargeSummary},
                { "[BatteryPower]", StringCurrentStoredPowerAll},
                { "[BatteryMaxPower]", StringMaxStoredPowerAll},
                { "[BatteryInput]", StringCurrentInputAll},
                { "[BatteryMaxInput]", StringMaxInputAll},
                { "[BatteryOuput]", StringCurrentOutputAll},
                { "[BatteryMaxOutput]", StringMaxOutputAll}
            };
            GroupActionKeywordsMap = new Dictionary<GroupActionKeywordsEnum, string>
            {
                { GroupActionKeywordsEnum.OffWhenTotalCapacityFull, "[OffWhenTotalCapacityFull]"},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThan, "[OffWhenTotalCapacityMoreThan]"},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThanEqualTo, "[OffWhenTotalCapacityMoreThanEqualTo]"},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityFull, "[OnWhenTotalCapacityFull]"},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThan, "[OnWhenTotalCapacityMoreThan]"},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThanEqualTo, "[OnWhenTotalCapacityMoreThanEqualTo]"},
                { GroupActionKeywordsEnum.OffWhenTotalInputFull, "[OffWhenTotalInputFull]"},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThan, "[OffWhenTotalInputMoreThan]"},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThanEqualTo, "[OffWhenTotalInputMoreThanEqualTo]"},
                { GroupActionKeywordsEnum.OnWhenTotalInputFull, "[OnWhenTotalInputFull]"},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThan, "[OnWhenTotalInputMoreThan]"},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThanEqualTo, "[OnWhenTotalInputMoreThanEqualTo]"},
                { GroupActionKeywordsEnum.OffWhenTotalOutputFull, "[OffWhenTotalOutputFull]"},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThan, "[OffWhenTotalOutputMoreThan]"},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThanEqualTo, "[OffWhenTotalOutputMoreThanEqualTo]"},
                { GroupActionKeywordsEnum.OnWhenTotalOutputFull, "[OnWhenTotalOutputFull]"},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThan, "[OnWhenTotalOutputMoreThan]"},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThanEqualTo, "[OnWhenTotalOutputMoreThanEqualTo]"}
            };
            GroupActionKeywordAction = new Dictionary<GroupActionKeywordsEnum, GroupActionEnum>
            {
                { GroupActionKeywordsEnum.OffWhenTotalCapacityFull, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThan, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThanEqualTo, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityFull, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThan, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThanEqualTo, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OffWhenTotalInputFull, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThan, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThanEqualTo, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OnWhenTotalInputFull, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThan, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThanEqualTo, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OffWhenTotalOutputFull, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThan, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThanEqualTo, GroupActionEnum.TurnOff},
                { GroupActionKeywordsEnum.OnWhenTotalOutputFull, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThan, GroupActionEnum.TurnOn},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThanEqualTo, GroupActionEnum.TurnOn}
            };
            GroupActionsMap = new Dictionary<GroupActionEnum, string>
            {
                { GroupActionEnum.TurnOff, "OnOff_Off" },
                { GroupActionEnum.TurnOn, "OnOff_On" }
            };
            GroupActionsExpressionMap = new Dictionary<GroupActionKeywordsEnum, GroupActionExpression>
            {
                { GroupActionKeywordsEnum.OffWhenTotalCapacityFull, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThan, GroupActionExpression.MoreThan},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityFull, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThan, GroupActionExpression.MoreThan},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OffWhenTotalInputFull, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThan, GroupActionExpression.MoreThan},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OnWhenTotalInputFull, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThan, GroupActionExpression.MoreThan},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OffWhenTotalOutputFull, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThan, GroupActionExpression.MoreThan},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OnWhenTotalOutputFull, GroupActionExpression.MoreThanEqualTo},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThan, GroupActionExpression.MoreThan},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo}
            };
            GroupActionsCriteriaMap = new Dictionary<GroupActionKeywordsEnum, GroupActionCriteria>
            {
                { GroupActionKeywordsEnum.OffWhenTotalCapacityFull, GroupActionCriteria.StoredCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThan, GroupActionCriteria.StoredCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalCapacityMoreThanEqualTo, GroupActionCriteria.StoredCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityFull, GroupActionCriteria.StoredCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThan, GroupActionCriteria.StoredCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalCapacityMoreThanEqualTo, GroupActionCriteria.StoredCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalInputFull, GroupActionCriteria.InputCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThan, GroupActionCriteria.InputCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalInputMoreThanEqualTo, GroupActionCriteria.InputCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalInputFull, GroupActionCriteria.InputCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThan, GroupActionCriteria.InputCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalInputMoreThanEqualTo, GroupActionCriteria.InputCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalOutputFull, GroupActionCriteria.OutputCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThan, GroupActionCriteria.OutputCapacity},
                { GroupActionKeywordsEnum.OffWhenTotalOutputMoreThanEqualTo, GroupActionCriteria.OutputCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalOutputFull, GroupActionCriteria.OutputCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThan, GroupActionCriteria.OutputCapacity},
                { GroupActionKeywordsEnum.OnWhenTotalOutputMoreThanEqualTo, GroupActionCriteria.OutputCapacity}
            };

            HandleLCDs();
            HandleCockpits();
            HandleBatteryCharge();

            HandleAllMiscBlocks();
            WriteStorageSummaryToProgrammeBlockDisplay();

            SetProgramBlockScreenText(Me as IMyProgrammableBlock, 0, BatterySummary);

        }

        string DetermineResponsibility(IMyTerminalBlock Block)
        {
            var responsibility = FindTaggedDataForKeyword(Block, ResponsibilityTagKeyword);

            if (string.IsNullOrWhiteSpace(responsibility))
            {
                responsibility = "Default";
            }

            return responsibility;
        }

        void HandleAllMiscBlocks()
        {
            GridTerminalSystem.GetBlocksOfType(FunctionalBlockList, IsRelevantMiscOnLocalGrid);

            if (FunctionalBlockList.Count > 0)
            {
                foreach (var FunctionalBlock in FunctionalBlockList)
                {
                    foreach (var Keyword in GroupActionKeywordsMap)
                    {
                        if (HasKeywordAndNotIgnored(FunctionalBlock, Keyword.Value))
                        {
                            ProcessMiscBlock(FunctionalBlock, Keyword.Value);
                        }
                    }
                }
                BatterySummary += $@"Managing {FunctionalBlockList.Count} miscellaneous devices
                    {DashSeparator}
                ";

                BatterySummary = BatterySummary.Replace("                    ", "");
                BatterySummary = BatterySummary.Replace("                ", "");
            }
        }

        void ProcessMiscBlock(IMyTerminalBlock terminalBlock, string Keyword)
        {
            double Percentage = 0;
            var DoAction = false;
            GroupActionEnum ActionToPerform = GroupActionEnum.None;
            GroupActionEnum InverseActionToPerform = GroupActionEnum.None;
            int PercentileCriteria = 0;
            var StrippedKeyword = Keyword.Replace("[", "");
            StrippedKeyword = StrippedKeyword.Replace("]", "");
            GroupActionKeywordsEnum KeywordEnum = GroupActionKeywordsEnum.None;


            if (GroupActionKeywordsMap.ContainsValue(Keyword))
            {
                DoAction = true;
                KeywordEnum = (GroupActionKeywordsEnum)Enum.Parse(typeof(GroupActionKeywordsEnum), StrippedKeyword);

                if (GroupActionKeywordAction[KeywordEnum] == GroupActionEnum.TurnOff)
                {
                    ActionToPerform = GroupActionEnum.TurnOff;
                    InverseActionToPerform = GroupActionEnum.TurnOn;
                }
                else if (GroupActionKeywordAction[KeywordEnum] == GroupActionEnum.TurnOn)
                {
                    ActionToPerform = GroupActionEnum.TurnOn;
                    InverseActionToPerform = GroupActionEnum.TurnOff;
                }

                if (GroupActionsCriteriaMap[KeywordEnum] == GroupActionCriteria.StoredCapacity)
                {
                    Percentage = ToPercentage(CurrentStoredPowerAll, MaxStoredPowerAll);
                }
                else if (GroupActionsCriteriaMap[KeywordEnum] == GroupActionCriteria.InputCapacity)
                {
                    Percentage = ToPercentage(CurrentInputAll, MaxInputAll);
                }
                else if (GroupActionsCriteriaMap[KeywordEnum] == GroupActionCriteria.OutputCapacity)
                {
                    Percentage = ToPercentage(CurrentOutputAll, MaxOutputAll);
                }

                var fullKeywords = new List<string>
                {
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OffWhenTotalCapacityFull],
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OffWhenTotalInputFull],
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OffWhenTotalOutputFull],
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OnWhenTotalCapacityFull],
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OnWhenTotalInputFull],
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OnWhenTotalOutputFull]
                };
                if (fullKeywords.Contains(Keyword))
                {
                    PercentileCriteria = 98;
                }
                else
                {
                    PercentileCriteria = FindFirstNumberForKeyword(terminalBlock, Keyword);
                }
            }


            if (DoAction)
            {
                var ApplyAction = false;
                var DesiredAction = "";
                if (GroupActionsExpressionMap[KeywordEnum] == GroupActionExpression.MoreThan)
                {
                    ApplyAction = true;
                    DesiredAction = Percentage > PercentileCriteria ? GroupActionsMap[ActionToPerform] : GroupActionsMap[InverseActionToPerform];
                }
                else if (GroupActionsExpressionMap[KeywordEnum] == GroupActionExpression.MoreThanEqualTo)
                {
                    ApplyAction = true;
                    DesiredAction = Percentage >= PercentileCriteria ? GroupActionsMap[ActionToPerform] : GroupActionsMap[InverseActionToPerform];
                }

                if (ApplyAction)
                {
                    var ActionList = new List<ITerminalAction>();
                    terminalBlock.GetActions(ActionList, (x) => x.Id.Equals(DesiredAction));
                    if (ActionList.Count > 0)
                    {
                        ActionList[0].Apply(terminalBlock);
                    }
                }
            }
        }

        void HandleLCDs()
        {
            GridTerminalSystem.GetBlocksOfType(LcdList, IsRelevantLCDsOnLocalGrid);

            if (LcdList.Count > 0)
            {
                foreach (var lcd in LcdList)
                {
                    foreach (var Keyword in LCDKeywordsMap)
                    {
                        if (HasKeywordAndNotIgnored(lcd, Keyword.Key))
                        {
                            SetLCDText(lcd, Keyword.Key, Keyword.Value);
                        }
                    }

                }
            }

        }

        void HandleBatteries()
        {
            List<IMyBatteryBlock> batteriesTemp = new List<IMyBatteryBlock>();
            Echo("Searching power storage devices.");
            GridTerminalSystem.GetBlocksOfType(batteriesTemp, IsBatteryOnLocalGridNotIgnored);
            List<IMyReactor> FluidBatteriesTemp = new List<IMyReactor>();
            GridTerminalSystem.GetBlocksOfType(FluidBatteriesTemp, IsFluidBatteryOnLocalGridNotIgnored);


            if (batteriesTemp.Count == 0)
            {
                Echo("No batteries found.");
            }
            else
            {
                Echo($"Found {batteriesTemp.Count} batteries.");
            }

            if (FluidBatteriesTemp.Count == 0)
            {
            }
            else
            {
                Echo($"Found {FluidBatteriesTemp.Count} Fluid Batteries.");
            }


            // Order the batteries from lowest to highest charge
            TimeSpan balanceTimeSpan = DateTime.Now - balanceTime;
            if (balanceTimeSpan.TotalSeconds >= 1 || BatteryList.Count != batteriesTemp.Count || FluidBatteryList.Count != FluidBatteriesTemp.Count)
            {
                BatteryList = batteriesTemp;
                FluidBatteryList = FluidBatteriesTemp;


                CheckPowerDevices();
                SetupBatteryStringVariables();

                if (BatteryList.Count >= 2 && CurrentStoredPowerAll < MaxStoredPowerAll * 0.999)
                {
                    BatteryList.Sort((a, b) => b.CurrentStoredPower.CompareTo(a.CurrentStoredPower));
                }
                balanceTime = DateTime.Now;


                RenameAllBatteries();
            }
        }

        void HandleCockpits()
        {
            GridTerminalSystem.GetBlocksOfType(CockpitList, IsRelevantCockpitsOnLocalGrid);

            if (CockpitList.Count > 0)
            {
                foreach (var cockpit in CockpitList)
                {
                    foreach (var Keyword in LCDKeywordsMap)
                    {
                        if (HasKeywordAndNotIgnored(cockpit, Keyword.Key))
                        {
                            var screenToChange = FindFirstNumberForKeyword(cockpit, Keyword.Key);

                            if (screenToChange != -1)
                            {
                                SetCockpitScreenText(cockpit, screenToChange, Keyword.Value);
                            }
                        }
                    }
                }
            }
        }

        void SetLCDText(IMyTerminalBlock lcd, string title, string text)
        {
            try
            {
                IMyTextSurface lcdControl = lcd as IMyTextSurface;
                lcdControl.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                var lcdBlock = lcd as IMyTextPanel;
                lcdBlock.WritePublicTitle(title);
                lcdControl.WriteText(text);
            }
            catch
            {

            }
        }

        void SetCockpitScreenText(IMyCockpit cockpit, int screenNumber, string text)
        {
            try
            {
                if (screenNumber + 1 <= cockpit.SurfaceCount)
                {
                    IMyTextSurface lcdControl = cockpit.GetSurface(screenNumber);
                    lcdControl.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    lcdControl.WriteText(text);
                }
            }
            catch
            {

            }
            
        }

        void SetProgramBlockScreenText(IMyProgrammableBlock block, int screenNumber, string text)
        {
            try
            {
                if (screenNumber + 1 <= block.SurfaceCount)
                {
                    IMyTextSurface lcdControl = block.GetSurface(screenNumber);
                    lcdControl.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    lcdControl.WriteText(text);
                }
            }
            catch
            {

            }

        }

        void HandleBatteryCharge()
        {
            if (HasKeywordAndNotIgnored(Me, AutoRechargeKeyword) || HasKeywordAndNotIgnored(Me, AutoDischargeKeyword))
            {
                GridTerminalSystem.GetBlocksOfType(ConnectorList, IsConnectorOnLocalGridNotIgnored);


                if (ConnectorList.Count > 0)
                {
                    var connected = false;
                    foreach (var connector in ConnectorList)
                    {
                        if (connector.Status == MyShipConnectorStatus.Connected)
                        {
                            //Echo(connector.OtherConnector.CubeGrid.IsStatic.ToString());
                            connected = true;
                        }
                    }

                    if (connected)
                    {
                        if (HasKeywordAndNotIgnored(Me, AutoRechargeKeyword))
                        {
                            SetBatteriesToRecharge();
                        }
                        else if (HasKeywordAndNotIgnored(Me, AutoDischargeKeyword))
                        {
                            SetBatteriesToDischarge();
                        }
                    }
                    else
                    {
                        SetBatteriesToAuto();
                    }
                }
                else
                {
                    SetBatteriesToAuto();
                }
            }
        }

        bool IsABatteryMonitorProgramBlock(IMyProgrammableBlock ProgramBlock)
        {
            if (!ProgramBlock.IsSameConstructAs(Me))
            {
                var KeywordPresent = false;
                if (HasKeywordAndNotIgnored(ProgramBlock, ScriptName))
                {
                    KeywordPresent = true;
                }
                if (KeywordPresent)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsRelevantCockpitsOnLocalGrid(IMyTerminalBlock cockpit)
        {
            if (cockpit.IsSameConstructAs(Me))
            {
                var KeywordPresent = false;
                foreach (var Keyword in LCDKeywordsMap)
                {
                    if (HasKeywordAndNotIgnored(cockpit, Keyword.Key))
                    {
                        if (string.IsNullOrWhiteSpace(Responsibility) || Responsibility == "Default" || DetermineResponsibility(cockpit) == Responsibility)
                        {
                            KeywordPresent = true;
                        }
                    }
                }
                if (KeywordPresent)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsRelevantLCDsOnLocalGrid(IMyTerminalBlock lcd)
        {
            if (lcd.IsSameConstructAs(Me))
            {
                var KeywordPresent = false;
                foreach (var Keyword in LCDKeywordsMap)
                {
                    if (HasKeywordAndNotIgnored(lcd, Keyword.Key))
                    {
                        if (string.IsNullOrWhiteSpace(Responsibility) || Responsibility == "Default" || DetermineResponsibility(lcd) == Responsibility)
                        {
                            KeywordPresent = true;
                        }
                    }
                }
                if (KeywordPresent)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsRelevantMiscOnLocalGrid(IMyFunctionalBlock FunctionalBlock)
        {
            List<IMyFunctionalBlock> alreadyControlled = new List<IMyFunctionalBlock>();

            alreadyControlled.AddRange(BatteryList);
            alreadyControlled.AddRange(FluidBatteryList);
            alreadyControlled.AddRange(ConnectorList);

            if (FunctionalBlock.IsSameConstructAs(Me) && FunctionalBlock != Me && !alreadyControlled.Contains(FunctionalBlock) && !LcdList.Contains(FunctionalBlock))
            {
                if (HasKeywordAndNotIgnored(FunctionalBlock, ScriptName.Trim()))
                {
                    if (DetermineResponsibility(FunctionalBlock) == Responsibility)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool IsBatteryOnLocalGridNotIgnored(IMyBatteryBlock battery)
        {
            if (battery.IsSameConstructAs(Me) && !IsIgnored(battery))
            {
                if (string.IsNullOrWhiteSpace(Responsibility) || Responsibility == "Default" || DetermineResponsibility(battery) == Responsibility)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsFluidBatteryOnLocalGridNotIgnored(IMyReactor fluidBattery)
        {
            try
            {
                if (fluidBattery.IsSameConstructAs(Me) && (HasKeywordAndNotIgnored(fluidBattery, OtherBatteryTypes[0]) || HasKeywordAndNotIgnored(fluidBattery, OtherBatteryTypes[1])))
                {
                    if (string.IsNullOrWhiteSpace(Responsibility) || Responsibility == "Default" || DetermineResponsibility(fluidBattery) == Responsibility)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                
            }
            return false;

        }

        bool IsConnectorOnLocalGridNotIgnored(IMyShipConnector connector)
        {
            if (connector.IsSameConstructAs(Me) && !IsIgnored(connector))
            {
                return true;
            }
            return false;
        }

        void CheckPowerDevices()
        {
            float _storedPowerAll = 0;
            float _storedPowerAllMax = 0;
            float _currentInputAll = 0;
            float _maxInputAll = 0;
            float _currentOutputAll = 0;
            float _maxOutputAll = 0;
            int _rechargeModeAll = 0;
            int _dischargeModeAll = 0;
            int _autoModeAll = 0;

            foreach (var battery in BatteryList)
            {
                _storedPowerAll += battery.CurrentStoredPower;
                _storedPowerAllMax += battery.MaxStoredPower;
                _currentInputAll += battery.CurrentInput;
                _maxInputAll += battery.MaxInput;
                _currentOutputAll += battery.CurrentOutput;
                _maxOutputAll += battery.MaxOutput;
                _rechargeModeAll += battery.ChargeMode == ChargeMode.Recharge ? 1 : 0;
                _dischargeModeAll += battery.ChargeMode == ChargeMode.Discharge ? 1 : 0;
                _autoModeAll += battery.ChargeMode == ChargeMode.Auto ? 1 : 0;
            }

            foreach (var fluidBattery in FluidBatteryList)
            {
                _storedPowerAll += fluidBattery.GetValueFloat("CurrentStoredPower");
                _storedPowerAllMax += fluidBattery.GetValueFloat("MaxStoredPower");
                _currentInputAll += fluidBattery.GetValueFloat("CurrentInput");
                _maxInputAll += fluidBattery.GetValueFloat("MaxInput");
                _currentOutputAll += fluidBattery.GetValueFloat("CurrentOutput");
                _maxOutputAll += fluidBattery.GetValueFloat("MaxOutput");
            }

            CurrentStoredPowerAll = Math.Round(_storedPowerAll, 2);
            MaxStoredPowerAll = Math.Round(_storedPowerAllMax, 2);
            CurrentInputAll = Math.Round(_currentInputAll, 2);
            MaxInputAll = Math.Round(_maxInputAll, 2);
            CurrentOutputAll = Math.Round(_currentOutputAll, 2);
            MaxOutputAll = Math.Round(_maxOutputAll, 2);
            RechargeModeAll = _rechargeModeAll;
            DischargeModeAll = _dischargeModeAll;
            AutoModeAll = _autoModeAll;
        }

        void WriteStorageSummaryToProgrammeBlockDisplay()
        {
            Echo(BatterySummary.Replace(DashSeparator, "----------------------------------"));
            Echo(BatteryChargeSummary.Replace(DashSeparator, "----------------------------------"));
        }

        int FindFirstNumberForKeyword(IMyTerminalBlock Block, string Keyword)
        {
            var attachedNumber = 0;
            if (KeywordInName(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomName, Keyword);
                attachedNumber = FindFirstNumberInString(Block.CustomName.Substring(endOfKeyword, Block.CustomName.Length - endOfKeyword));
            }
            else if (KeywordInData(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomData, Keyword);
                attachedNumber = FindFirstNumberInString(Block.CustomData.Substring(endOfKeyword, Block.CustomData.Length - endOfKeyword));
            }

            return attachedNumber;
        }

        int FindFirstNumberInString(string Haystack)
        {
            int number;
            string collection = "";
            bool foundFirstNumber = false;

            foreach (var character in Haystack)
            {

                if (int.TryParse(character.ToString(), out number))
                {
                    collection += character;
                    foundFirstNumber = true;
                }
                else
                {
                    if (foundFirstNumber)
                    {
                        break;
                    }
                }
            }


            if (int.TryParse(collection, out number))
            {
                return number;
            }
            return -1;
        }

        string FindTaggedDataForKeyword(IMyTerminalBlock Block, string Keyword)
        {
            var attachedData = "";
            if (KeywordInName(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomName, ResponsibilityTagKeyword);
                attachedData = FindTaggedDataInString(Block.CustomName.Substring(endOfKeyword, Block.CustomName.Length - endOfKeyword));
            }
            else if (KeywordInData(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomData, ResponsibilityTagKeyword);
                attachedData = FindTaggedDataInString(Block.CustomData.Substring(endOfKeyword, Block.CustomData.Length - endOfKeyword));
            }

            return attachedData;
        }

        string FindTaggedDataInString(string Haystack)
        {
            string collection = "";
            bool foundOpeningTag = false;
            bool foundClosingTag = false;

            foreach (var character in Haystack)
            {
                if (!foundOpeningTag)
                {
                    if (character == '[')
                    {
                        foundOpeningTag = true;
                    }
                }
                else
                {
                    if (character == ']')
                    {
                        foundClosingTag = true;
                        break;
                    }
                    else
                    {
                        collection += character;
                    }
                }
            }

            return foundClosingTag ? collection : "";
        }

        bool HasKeywordAndNotIgnored(IMyTerminalBlock Block, string Keyword)
        {
            if (KeywordInName(Block, Keyword) || KeywordInData(Block, Keyword))
            {
                if (KeywordInName(Block, Keyword))
                {
                    CheckBlockNameKeywordCapitalisation(Block, Keyword);
                }
                else if (KeywordInData(Block, Keyword))
                {
                    CheckBlockDataKeywordCapitalisation(Block, Keyword);
                }
                if (!IsIgnored(Block))
                {
                    return true;
                }
            }
            return false;
        }

        bool KeywordInName(IMyTerminalBlock Block, string Keyword)
        {
            if (Block.CustomName.ToLower().Contains(Keyword.ToLower()))
            {
                return true;
            }
            return false;
        }

        bool KeywordInData(IMyTerminalBlock Block, string Keyword)
        {
            if (Block.CustomData.ToLower().Contains(Keyword.ToLower()))
            {
                return true;
            }
            return false;
        }

        bool IsIgnored(IMyTerminalBlock Block)
        {
            if (KeywordInName(Block, IgnoreKeyword) || KeywordInData(Block, IgnoreKeyword))
            {
                if (KeywordInName(Block, IgnoreKeyword))
                {
                    CheckBlockNameKeywordCapitalisation(Block, IgnoreKeyword);
                }
                else if (KeywordInData(Block, IgnoreKeyword))
                {
                    CheckBlockDataKeywordCapitalisation(Block, IgnoreKeyword);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        void CheckBlockNameKeywordCapitalisation(IMyTerminalBlock Block, string Keyword)
        {
            if (KeywordInName(Block, Keyword) && !Block.CustomName.Contains(Keyword))
            {
                var keywordStartsAt = GetKeywordStartPos(Block.CustomName.ToLower(), Keyword.ToLower());
                var keywordEndsAt = GetKeywordEndPos(Block.CustomName.ToLower(), Keyword.ToLower());
                var nameArray = Block.CustomName.ToArray();
                var keywordArray = Keyword.ToArray();

                var iterations = 0;
                for (int i = keywordStartsAt; i < keywordEndsAt; i++)
                {
                    nameArray[i] = keywordArray[iterations];
                    iterations++;
                }

                var newName = new string(nameArray);
                Block.CustomName = newName;
            }
        }

        void CheckBlockDataKeywordCapitalisation(IMyTerminalBlock Block, string Keyword)
        {
            if (KeywordInData(Block, Keyword) && !Block.CustomData.Contains(Keyword))
            {
                var keywordStartsAt = GetKeywordStartPos(Block.CustomData.ToLower(), Keyword.ToLower());
                var keywordEndsAt = GetKeywordEndPos(Block.CustomData.ToLower(), Keyword.ToLower());
                var dataArray = Block.CustomData.ToArray();
                var keywordArray = Keyword.ToArray();

                var iterations = 0;
                for (int i = keywordStartsAt; i < keywordEndsAt; i++)
                {
                    dataArray[i] = keywordArray[iterations];
                    iterations++;
                }

                var newData = new string(dataArray);
                Block.CustomData = newData;

            }
        }

        int GetKeywordStartPos(string Haystack, string Keyword)
        {
            return Haystack.IndexOf(Keyword);
        }

        int GetKeywordEndPos(string Haystack, string Keyword)
        {
            return Haystack.Length - (new string(Haystack.Reverse().ToArray())).LastIndexOf(new string(Keyword.Reverse().ToArray()));
        }

        string Pluralise(int ValueToCheck)
        {
            return ValueToCheck > 1 ? "s" : "";
        }

        string ChargeStatus()
        {
            var Charge = CurrentInputAll - CurrentOutputAll;

            var ChargeValue = Charge < 0 ? Charge * -1 : Charge;
            var Seconds = Charge > 0 ? ((MaxStoredPowerAll - CurrentStoredPowerAll) / ChargeValue) * 60 * 60 : ((CurrentStoredPowerAll) / ChargeValue) * 60 * 60;
            var now = DateTime.Now;
            TimeSpan span;
            try
            {
                span = now - now.AddSeconds(-Seconds);

                var DaysArgument = span.Days > 0 ? $"{span.Days} Day{Pluralise(span.Days)} " : "";
                var HoursArgument = span.Hours > 0 ? $"{span.Hours} Hour{Pluralise(span.Hours)} " : "";
                var MinutesArgument = span.Minutes > 0 ? $"{span.Minutes} Minute{Pluralise(span.Minutes)} " : "";
                var SecondsArgument = span.Seconds > 0 ? $"{span.Seconds} Second{Pluralise(span.Seconds)}" : "";
                var Duration = $"{DaysArgument}{HoursArgument}{MinutesArgument}{SecondsArgument}";

                return !string.IsNullOrWhiteSpace(Duration) ? Charge > 0 ? $"Full Charge in : {Duration}" : Charge < 0 ? $"Full Discharge in : {Duration}" : "Stable Charge" : "Stable Charge";

            }
            catch
            {
                return "Stable Charge";
            }

            
        }

        public void RenameAllBatteries()
        {

            foreach (var battery in BatteryList)
            {
                string newName = battery.CustomName;
                string oldStatus = System.Text.RegularExpressions.Regex.Match(battery.CustomName, @" *\((.*?)\)").Value;
                double oldPercentage = 0;
                var status = "";
                var percentage = ToPercentage(battery.CurrentStoredPower, battery.MaxStoredPower);

                if (oldStatus != String.Empty)
                {
                    newName = battery.CustomName.Replace(oldStatus, "");
                    oldPercentage = Convert.ToDouble(oldStatus.Split('%')[0].Replace("(", ""));

                    if (percentage > oldPercentage)
                    {
                        status = " - Recharging";
                    }
                    else if (percentage < oldPercentage)
                    {
                        status = " - Discharging";
                    }
                    else
                    {
                        status = " - Stable";
                    }
                }
                else
                {
                    status = " - Stable";
                }


                newName = $"{newName} ( {percentage}%{status} )";

                // Rename the block if the name has changed
                if (battery.CustomName != newName)
                {
                    battery.CustomName = newName;
                }

            }


        }

        string GetPowerRepresentation(double PowerInMegaWatts)
        {
            //Smaller Units
            var PowerInKiloWatts = PowerInMegaWatts * 1000;
            var PowerInWatts = PowerInKiloWatts * 1000;
            //Bigger Units
            var PowerInGigaWatts = PowerInMegaWatts / 1000;
            var PowerInTeraWatts = PowerInGigaWatts / 1000;
            var PowerInPetaWatts = PowerInTeraWatts / 1000;
            var PowerInExaWatts = PowerInTeraWatts / 1000;
            var PowerInZettaWatts = PowerInExaWatts / 1000;

            if (PowerInZettaWatts >= 1)
            {
                return $"{PowerInZettaWatts}ZW";
            }
            else if (PowerInExaWatts >= 1)
            {
                return $"{PowerInExaWatts}EW";
            }
            else if (PowerInPetaWatts >= 1)
            {
                return $"{PowerInPetaWatts}PW";
            }
            else if (PowerInTeraWatts >= 1)
            {
                return $"{PowerInTeraWatts}TW";
            }
            else if (PowerInGigaWatts >= 1)
            {
                return $"{PowerInGigaWatts}GW";
            }
            else if (PowerInMegaWatts >= 1)
            {
                return $"{PowerInMegaWatts}MW";
            }
            else if (PowerInKiloWatts >= 1)
            {
                return $"{PowerInKiloWatts}KW";
            }
            else
            {
                return $"{PowerInWatts}W";
            }
        }

        void SetupBatteryStringVariables()
        {
            StringCurrentStoredPowerAllPerc = $" ( {ToPercentage(CurrentStoredPowerAll, MaxStoredPowerAll)}% )";
            StringCurrentInputAllPerc = $" ( {ToPercentage(CurrentInputAll, MaxInputAll)}% )";
            StringCurrentOutputAllPerc = $" ( {ToPercentage(CurrentOutputAll, MaxOutputAll)}% )";

            StringCurrentStoredPowerAll = $"Power Available : {GetPowerRepresentation(CurrentStoredPowerAll)}h{StringCurrentStoredPowerAllPerc}";
            StringMaxStoredPowerAll = $"Max Power : {GetPowerRepresentation(MaxStoredPowerAll)}h";
            StringCurrentInputAll = $"Current Input : {GetPowerRepresentation(CurrentInputAll)}{StringCurrentInputAllPerc}";
            StringMaxInputAll = $"Max Input : {GetPowerRepresentation(MaxInputAll)}";
            StringCurrentOutputAll = $"Current Output : {GetPowerRepresentation(CurrentOutputAll)}{StringCurrentOutputAllPerc}";
            StringMaxOutputAll = $"Max Output : {GetPowerRepresentation(MaxOutputAll)}";
            
            BatterySummary = $@"{DashSeparator}
                    {StringCurrentStoredPowerAll}
                    {StringMaxStoredPowerAll}
                    {DashSeparator}
                    {StringCurrentInputAll}
                    {StringMaxInputAll}
                    {DashSeparator}
                    {StringCurrentOutputAll}
                    {StringMaxOutputAll}
                    {DashSeparator}
                    Responsibility :  {Responsibility}
                    {DashSeparator}
                ";


            var AmountOfAutoMode = $"Batteries in Auto Mode : {AutoModeAll}";
            var AmountOfRechargeMode = $"Batteries in Recharge Mode : {RechargeModeAll}";
            var AmountOfDischargeMode = $"Batteries in Discharge Mode : {DischargeModeAll}";
            StringBatteryChargeModes = $"";
            StringChargePotential = $"Full Charge : {Math.Round((MaxStoredPowerAll / MaxInputAll) * 60)} Mins @ {GetPowerRepresentation(MaxInputAll)}";
            StringDischargePotential = $"Full Discharge : {Math.Round((MaxStoredPowerAll / MaxOutputAll) * 60)} Mins @ {GetPowerRepresentation(MaxInputAll)}";
            BatteryChargeSummary = $@"{DashSeparator}
                    {AmountOfAutoMode}
                    {AmountOfRechargeMode}
                    {AmountOfDischargeMode}
                    {DashSeparator}
                    {ChargeStatus()}
                    {DashSeparator}
                    Potential Throughput : 
                    {StringChargePotential}
                    {StringDischargePotential}
                    {DashSeparator}
                ";
            BatterySummary = BatterySummary.Replace("                    ", "");
            BatterySummary = BatterySummary.Replace("                ", "");
            BatteryChargeSummary = BatteryChargeSummary.Replace("                    ", "");
            BatteryChargeSummary = BatteryChargeSummary.Replace("                ", "");
            
        }

        double ToPercentage(double value, double comparisonValue)
        {
            
            if (comparisonValue == 0)
            {
                return 0;
            }

            double percentage = Math.Round(value / comparisonValue * 100, 2);
            return percentage ;
        }

        void SetPowerRequirement(double RequirementInMW)
        {
            if (KeywordInData(Me, PowerRequirementKeyword))
            {
                var endOfKeyword = GetKeywordEndPos(Me.CustomData, PowerRequirementKeyword);
                var PreviousPowerRequirement = FindFirstNumberInString(Me.CustomData.Substring(endOfKeyword, Me.CustomData.Length - endOfKeyword));
                if (PreviousPowerRequirement != -1)
                {
                    Me.CustomData = Me.CustomData.Replace($"{PowerRequirementKeyword}:{PreviousPowerRequirement.ToString()}", $"{PowerRequirementKeyword}:{Math.Ceiling(RequirementInMW).ToString()}");
                }
            }
            else
            {
                Me.CustomData += $"\n{PowerRequirementKeyword}:{Math.Ceiling(RequirementInMW)}";
            }
        }

        int GetPowerRequirement()
        {
            List<IMyProgrammableBlock> ProgramBlockList = new List<IMyProgrammableBlock>();

            GridTerminalSystem.GetBlocksOfType(ProgramBlockList, IsABatteryMonitorProgramBlock);
            var PowerRequirementInMW = 0;

            foreach (var ProgramBlock in ProgramBlockList)
            {
                if(KeywordInData(ProgramBlock, PowerRequirementKeyword))
                {
                    var endOfKeyword = GetKeywordEndPos(ProgramBlock.CustomData, PowerRequirementKeyword);
                    var PowerRequirement = FindFirstNumberInString(ProgramBlock.CustomData.Substring(endOfKeyword, ProgramBlock.CustomData.Length - endOfKeyword));
                    if (PowerRequirement != -1)
                    {
                        PowerRequirementInMW += PowerRequirement;
                    }
                }
            }
            return PowerRequirementInMW;
        }

        void SetBatteriesToRecharge()
        {
            if (ToPercentage(CurrentStoredPowerAll, MaxStoredPowerAll) < 100)
            {
                BatteryList[0].ChargeMode = ChargeMode.Auto;
                double MWRequired = 0;
                for (int i = 1; i < BatteryList.Count; i++)
                {
                    BatteryList[i].ChargeMode = ChargeMode.Recharge;
                    MWRequired += BatteryList[i].MaxInput;
                }
                SetPowerRequirement(MWRequired);
            }
            else
            {
                for (int i = 0; i < BatteryList.Count; i++)
                {
                    BatteryList[i].ChargeMode = ChargeMode.Auto;
                }
                SetPowerRequirement(0);
            }       
        }

        void SetBatteriesToAuto()
        {
            for (int i = 0; i < BatteryList.Count; i++)
            {
                BatteryList[i].ChargeMode = ChargeMode.Auto;
            }
        }

        void SetBatteriesToDischarge()
        {
            var HalfOfBatteries = (int)BatteryList.Count / 2;
            var PowerRequired = GetPowerRequirement();
            double PowerProvided = 0;

            for (int i =  0; i < BatteryList.Count; i++)
            {
                if (i < HalfOfBatteries)
                {
                    if (PowerProvided < PowerRequired)
                    {
                        BatteryList[i].ChargeMode = ChargeMode.Discharge;
                        PowerProvided += BatteryList[i].MaxOutput;
                    }
                    else
                    {
                        BatteryList[i].ChargeMode = ChargeMode.Auto;
                    }
                }
                else
                {
                    BatteryList[i].ChargeMode = ChargeMode.Auto;
                }
            }
        }

    }
}
