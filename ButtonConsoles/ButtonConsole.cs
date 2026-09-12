using System;
using System.Collections.Generic;
using FontAwesome.WPF;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;

namespace YAESU_FT_891_Front_End
{
    public class ButtonConsoleClass : Button
    {
        public FontAwesomeIcon Icons;
        public FontAwesomeIcon IconAlts;
        public Color IconColors;
        public Color IconOnColors;
        public Color IconOn2Colors;
        public byte ButtonType;
        public bool IsThreeStep;
        public string ToolTipOff;
        public string ToolTipOn;
        public string NoviceToolTipOff;
        public string NoviceToolTipOn;
        public FontAwesome.WPF.FontAwesome Icon;
        public bool StateIsOn;
    }
    public struct ButtonTypes
    {
        public const byte PlaceHolder = 0;
        public const byte Indicator = 1;
        public const byte Button = 2;
    }
    public interface IButtonConsole
    {
        List<ButtonConsoleClass> ButtonConsoleList
        {
            get;
        }
    }
    public class ButtonConsole : IButtonConsole
    {
        public virtual List<ButtonConsoleClass> ButtonConsoleList
        {
            get;
            set;
        }

        StackPanel ParentStackPanel;
        double FontSize;
        Color Foreground;
        public delegate void ButtonClickCallBackFunction(byte id);
        ButtonClickCallBackFunction ButtonClickCBF;

        public List<ButtonConsoleClass> ButtonList = new List<ButtonConsoleClass>();
        
        public ButtonConsole(StackPanel ParentStackPanel, double FontSize, Color Foreground, ButtonClickCallBackFunction ButtonClickCBF)
        {
            this.ParentStackPanel = ParentStackPanel;
            this.FontSize = FontSize;
            this.Foreground = Foreground;
            this.ButtonClickCBF = ButtonClickCBF;

            Init();
        }

        private void Init()
        {
            int CurrentIndex = 0;
            foreach (ButtonConsoleClass bcc in ButtonConsoleList)
            {
                CreateButton(CurrentIndex, FontSize, bcc);

                //Set to off
                UpdateButton(CurrentIndex, false);

                CurrentIndex++;
            }
        }
        public void EnableButton(int IconID)
        {
            ButtonList[IconID].IsEnabled = true;
            ButtonList[IconID].Opacity = 1;
        }
        public void DisableButton(int IconID)
        {
            ButtonList[IconID].IsEnabled = false;
            ButtonList[IconID].Opacity = 0.3;
        }
        public void UpdateButton(int IconID, bool State, bool UseIconAlts = false, bool UseIconOn2Colors = false, bool Spin = false, bool ThirdState = false)
        {
            ButtonList[IconID].StateIsOn = State;
            ButtonList[IconID].IsThreeStep = ThirdState;

            if (!ButtonList[IconID].Dispatcher.CheckAccess())
            {
                ButtonList[IconID].Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action(delegate()
                {
                    if (UseIconAlts)
                        ButtonList[IconID].Icon.Icon = ButtonConsoleList[IconID].IconAlts;
                    else
                        ButtonList[IconID].Icon.Icon = ButtonConsoleList[IconID].Icons;

                    if (State)
                    {
                        if (UseIconOn2Colors)
                        {
                            if (ButtonConsoleList[IconID].IconOn2Colors != null)
                                ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOn2Colors);
                            else
                                ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOnColors);
                        }
                        else
                            ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOnColors);

                        if (ButtonConsoleList[IconID].ToolTipOn != string.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOn;
                    }
                    else
                    {
                        if (UseIconOn2Colors)
                        {
                            if (ButtonConsoleList[IconID].IconOn2Colors != null)
                                ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOn2Colors);
                            else
                                ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconColors);
                        }
                        else
                            ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconColors);

                        if (ButtonConsoleList[IconID].ToolTipOff != string.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOff;
                    }

                    if (Spin)
                        ButtonList[IconID].Icon.Spin = true;
                    else
                        ButtonList[IconID].Icon.Spin = false;
                }));
            }
            else
            {
                if (UseIconAlts)
                    ButtonList[IconID].Icon.Icon = ButtonConsoleList[IconID].IconAlts;
                else
                    ButtonList[IconID].Icon.Icon = ButtonConsoleList[IconID].Icons;

                if (State)
                {
                    if (UseIconOn2Colors)
                    {
                        if (ButtonConsoleList[IconID].IconOn2Colors != null)
                            ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOn2Colors);
                        else
                            ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOnColors);
                    }
                    else
                        ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOnColors);

                    if (ButtonConsoleList[IconID].ToolTipOn != string.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOn;
                }
                else
                {
                    if (UseIconOn2Colors)
                    {
                        if (ButtonConsoleList[IconID].IconOn2Colors != null)
                            ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconOn2Colors);
                        else
                            ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconColors);
                    }
                    else
                        ButtonList[IconID].Icon.Foreground = new SolidColorBrush(ButtonConsoleList[IconID].IconColors);

                    if (ButtonConsoleList[IconID].ToolTipOff != string.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOff;
                }

                if (Spin)
                    ButtonList[IconID].Icon.Spin = true;
                else
                    ButtonList[IconID].Icon.Spin = false;
            }
        }
        public void SwapToolTip(int IconID, bool IsActive)
        {
            if (!ButtonList[IconID].Dispatcher.CheckAccess())
            {
                ButtonList[IconID].Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                {
                    if (IsActive)
                        if (ButtonConsoleList[IconID].ToolTipOff != String.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOn;
                    else
                        if (ButtonConsoleList[IconID].ToolTipOff != String.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOff;
                }));
            }
            else
            {
                if (IsActive)
                    if (ButtonConsoleList[IconID].ToolTipOff != String.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOn;
                else
                    if (ButtonConsoleList[IconID].ToolTipOff != String.Empty) ButtonList[IconID].ToolTip = ButtonConsoleList[IconID].ToolTipOff;
            }
        }
        public void UpdateToolTip(int IconID, string NewToolTip)
        {
            if (!ButtonList[IconID].Dispatcher.CheckAccess())
            {
                ButtonList[IconID].Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                {
                    ButtonList[IconID].ToolTip = NewToolTip;
                }));
            }
            else
            {
                ButtonList[IconID].ToolTip = NewToolTip;
            }
        }
        private void CreateButton(int ID, Double FontSize, ButtonConsoleClass bcc)
        {
            StackPanel stackpanel = new StackPanel();

            ButtonConsoleClass button = new ButtonConsoleClass();

            stackpanel.Width = FontSize + 3;
            stackpanel.Height = FontSize + 1;

            switch (bcc.ButtonType)
            {
                case ButtonTypes.PlaceHolder:
                    stackpanel.Margin = new System.Windows.Thickness(1, 0, 1, 0);
                    stackpanel.Opacity = 0;
                    break;
                case ButtonTypes.Indicator:
                    stackpanel.Margin = new System.Windows.Thickness(1, 0, 1, 0);
                    break;
                case ButtonTypes.Button:
                    stackpanel.Margin = new System.Windows.Thickness(1, 0, 1, 0);

                    button.Click += (sender, e) => { ButtonClick(sender, e, Convert.ToByte(ID)); };

                    button.MouseEnter += (sender, e) => { Button_MouseEnter(sender, e, Convert.ToByte(ID)); };
                    button.MouseLeave += (sender, e) => { Button_MouseLeave(sender, e, Convert.ToByte(ID)); };
                    break;   
            }

            button.Padding = new Thickness(0);

            button.Name = "IconStatusBar" + ID;
            button.Tag = ID;
            button.Focusable = false;
            button.Background = new SolidColorBrush(Colors.Transparent);
            button.BorderThickness = new System.Windows.Thickness(0);
            button.BorderBrush = new SolidColorBrush(Colors.Transparent);

            button.SetResourceReference(Control.StyleProperty, "MyMetroFlatButtonStyle");

            button.Icon = new FontAwesome.WPF.FontAwesome();

            button.Icon.Icon = bcc.Icons;
            button.Icon.FontSize = FontSize;
            button.Icon.Foreground = new SolidColorBrush(bcc.IconColors);
            button.Icon.Margin = new Thickness(0, 1, 0, 0);

            stackpanel.Children.Add(button.Icon);

            button.Content = stackpanel;

            if (bcc.ToolTipOff != String.Empty) button.ToolTip = bcc.ToolTipOff;

            ButtonList.Add(button);

            ParentStackPanel.Children.Add(button);
        }
        private void ButtonClick(object sender, EventArgs e, byte id)
        {
            var button = sender as Button;

            if (ButtonClickCBF != null ) ButtonClickCBF(id);
        }
        void Button_MouseEnter(object sender, EventArgs e, byte IconID)
        {
            var button = sender as Button;

            ButtonList[IconID].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10FFFFFF"));

            Mouse.OverrideCursor = Cursors.Hand;
        }
        void Button_MouseLeave(object sender, EventArgs e, byte IconID)
        {
            var button = sender as Button;

            button.Effect = null;

            Mouse.OverrideCursor = Cursors.Arrow;

            ButtonList[IconID].Background = new SolidColorBrush(Colors.Transparent);
        }

    }
}
