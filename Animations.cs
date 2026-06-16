using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace YAESU_FT_891_Front_End
{
    public static class Animations
    {
        public static void AnimateBlur(BlurEffect blurEffect, double target)
        {
            DoubleAnimation anim = new DoubleAnimation
            {
                To = target,
                Duration = TimeSpan.FromMilliseconds(80),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            blurEffect.BeginAnimation(
                BlurEffect.RadiusProperty,
                anim,
                HandoffBehavior.SnapshotAndReplace);
        }
        public static void AnimateButtonClick(FrameworkElement element, Action onComplete)
        {
            // 1. Setup the Brush
            SolidColorBrush animatedBrush = new SolidColorBrush(Colors.Black) { Opacity = 0 };

            // Safely assign the brush based on the actual type of the control
            if (element is Panel panel)
            {
                panel.Background = animatedBrush;
            }
            else if (element is Border border)
            {
                border.Background = animatedBrush;
            }
            else if (element is Control control)
            {
                control.Background = animatedBrush;
            }
            else
            {
                // Fallback or safety check: If it doesn't support a background, 
                // just run the callback immediately and exit.
                onComplete?.Invoke();
                return;
            }

            // 2. Define the Fade In
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                To = 0.35,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // 3. Define the Fade Out
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // 4. Chain them: When Fade In finishes, start Fade Out
            fadeIn.Completed += (s, ev) =>
            {
                animatedBrush.BeginAnimation(SolidColorBrush.OpacityProperty, fadeOut);
            };

            // Chain: Fade Out -> Execute the Callback
            fadeOut.Completed += (s, e) => onComplete?.Invoke();

            // Start the sequence
            animatedBrush.BeginAnimation(SolidColorBrush.OpacityProperty, fadeIn);
        }
        public static async void FadoutBorderWindow(Border borderWindow, int initalHoldValue = 260)
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
