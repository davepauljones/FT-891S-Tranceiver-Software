using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using static YAESU_FT_891_Front_End.Animations;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public static class HelperFunctions
    {
        public static void ProcessKnobRotation(ref double lastBlurSpeed, ref double blurImpulse, ref DateTime lastMoveTime, Canvas blurCanvas, double deltaY)
        {
            DateTime now = DateTime.Now;
            double dt = Math.Max(1, (now - lastMoveTime).TotalMilliseconds);

            // Update speed for blur effect
            double speed = Math.Abs(deltaY) / dt;
            lastBlurSpeed = (lastBlurSpeed * 0.8) + (speed * 0.2);

            // Increment the impulse (used by the timer loop)
            blurImpulse = Math.Min(6.0, blurImpulse + (Math.Abs(deltaY) * 0.8));

            lastMoveTime = now;
            blurCanvas.Visibility = Visibility.Visible;
        }
        public static void HandleBlurTimerTick(ref double lastBlurSpeed, ref double blurImpulse, ref DateTime lastMoveTime, BlurEffect effect, Canvas canvas, DispatcherTimer timer)
        {
            if (!MainWindow.isDragging) lastBlurSpeed = 0;

            bool isIdle = (DateTime.Now - lastMoveTime).TotalMilliseconds > 40;
            if (isIdle && MainWindow.isDragging) lastBlurSpeed *= 0.3;

            blurImpulse *= 0.65;
            double targetBlur = Math.Min(6, (lastBlurSpeed * 80) + blurImpulse);

            if (targetBlur < 0.40)
            {
                if (!MainWindow.isDragging)
                {
                    effect.BeginAnimation(BlurEffect.RadiusProperty, null);
                    effect.Radius = 0;
                    canvas.Visibility = Visibility.Collapsed;
                    timer.Stop();
                }
                blurImpulse = 0;
                lastBlurSpeed = 0;
            }
            else
            {
                AnimateBlur(effect, targetBlur);
            }
        }
        public static void SetRigLEDColor(MainWindow mainWindow, byte ledRigColor)
        {
            switch (ledRigColor)
            {
                case RigLEDColors.LightGray:
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightGray;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.LightGray;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.LightGray;
                    mainWindow.ClearWindowDropShadowEffect.Color = Colors.LightGray;
                    break;
                case RigLEDColors.Green:
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightGreen;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.Green;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.Green;
                    mainWindow.ClearWindowDropShadowEffect.Color = Colors.Green;
                    break;
                case RigLEDColors.Red:
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.IndianRed;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.Red;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.Red;
                    mainWindow.ClearWindowDropShadowEffect.Color = Colors.Red;
                    break;
                case RigLEDColors.Blue:
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightBlue;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.Blue;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.LightBlue;
                    mainWindow.ClearWindowDropShadowEffect.Color = Colors.Blue;
                    break;
                default:
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightGray;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.LightGray;
                    mainWindow.ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.LightGray;
                    mainWindow.ClearWindowDropShadowEffect.Color = Colors.LightGray;
                    break;
            }
        }
    }
}
