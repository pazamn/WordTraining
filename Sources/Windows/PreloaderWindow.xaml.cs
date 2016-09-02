using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace WordTraining.Windows
{
    public partial class PreloaderWindow
    {
        #region Window Environment

        public static PreloaderWindow CurrentWindow { get; set; }

        public PreloaderWindow()
        {
            CurrentWindow = this;
            SetWindowPosition();
            InitializeComponent();

            Part1TextBlock.Visibility = Visibility.Collapsed;
            Part2TextBlock.Visibility = Visibility.Collapsed;
            Part3TextBlock.Visibility = Visibility.Collapsed;
            Part4TextBlock.Visibility = Visibility.Collapsed;
            Part5TextBlock.Visibility = Visibility.Collapsed;

            QuestionTextBlock.Visibility = Visibility.Collapsed;
            ButtonsPanel.Visibility = Visibility.Collapsed;
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void PreloaderWindowClosing(object sender, CancelEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void PreloaderWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void SetWindowPosition()
        {
            List<string> allArguments = Environment.GetCommandLineArgs().Select(a => a.ToLowerInvariant()).ToList();
            string leftArgument = allArguments.FirstOrDefault(a => a.Contains("/left=")) ?? string.Empty;
            string topArgument = allArguments.FirstOrDefault(a => a.Contains("/top=")) ?? string.Empty;
            if (string.IsNullOrEmpty(leftArgument) || string.IsNullOrEmpty(topArgument))
            {
                int defaultLeftPosition = (int)(Screen.PrimaryScreen.WorkingArea.Width / 2.0 - 400);
                int defaultTopPosition = (int)(Screen.PrimaryScreen.WorkingArea.Height / 2.0 - 150);
                SetWindowPosition(defaultLeftPosition, defaultTopPosition);
                return;
            }

            leftArgument = leftArgument.Replace("/left=", string.Empty);
            topArgument = topArgument.Replace("/top=", string.Empty);

            int leftPosition;
            int topPosition;
            bool leftPositionParsed = int.TryParse(leftArgument, out leftPosition);
            bool topPositionParsed = int.TryParse(topArgument, out topPosition);
            if (!leftPositionParsed || !topPositionParsed)
            {
                int defaultLeftPosition = (int)(Screen.PrimaryScreen.WorkingArea.Width / 2.0 - 400);
                int defaultTopPosition = (int)(Screen.PrimaryScreen.WorkingArea.Height / 2.0 - 150);
                SetWindowPosition(defaultLeftPosition, defaultTopPosition);
                return;
            }

            SetWindowPosition(leftPosition, topPosition);
        }

        private void SetWindowPosition(int leftPosition, int topPosition)
        {
            Left = leftPosition;
            Top = topPosition;
        }

        #endregion Window Environment

        #region Show Steps

        public static void ShowFirstStep()
        {
            Action action = delegate
                {
                    CurrentWindow.Part1TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part2TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part3TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part4TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part5TextBlock.Visibility = Visibility.Collapsed;
                };

            CurrentWindow.Dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        public static void ShowSecondStep()
        {
            Action action = delegate
                {
                    CurrentWindow.Part1TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part2TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part3TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part4TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part5TextBlock.Visibility = Visibility.Collapsed;
                };

            CurrentWindow.Dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        public static void ShowThirdStep()
        {
            Action action = delegate
                {
                    CurrentWindow.Part1TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part2TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part3TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part4TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part5TextBlock.Visibility = Visibility.Collapsed;
                };

            CurrentWindow.Dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        public static void ShowFourthStep()
        {
            Action action = delegate
                {
                    CurrentWindow.Part1TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part2TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part3TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part4TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part5TextBlock.Visibility = Visibility.Collapsed;
                };

            CurrentWindow.Dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        public static void ShowFifthStep()
        {
            Action action = delegate
                {
                    CurrentWindow.Part1TextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.Part2TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part3TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part4TextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.Part5TextBlock.Visibility = Visibility.Visible;
                };

            CurrentWindow.Dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        #endregion Show Steps

        #region Dialog Window Logic

        private static MessageBoxResult WindowResult { get; set; }

        public static MessageBoxResult ShowDialog(string message)
        {
            Action firstAction = delegate
                {
                    CurrentWindow.QuestionTextBlock.Text = message;
                    CurrentWindow.QuestionTextBlock.Visibility = Visibility.Visible;
                    CurrentWindow.ButtonsPanel.Visibility = Visibility.Visible;
                    WindowResult = MessageBoxResult.None;
                };

            CurrentWindow.Dispatcher.Invoke(firstAction, DispatcherPriority.Send);
            while (WindowResult == MessageBoxResult.None)
            {
                Thread.Sleep(100);
            }

            Action secondAction = delegate
                {
                    CurrentWindow.QuestionTextBlock.Visibility = Visibility.Collapsed;
                    CurrentWindow.ButtonsPanel.Visibility = Visibility.Collapsed;
                };

            CurrentWindow.Dispatcher.Invoke(secondAction, DispatcherPriority.Send);
            return WindowResult;
        }

        private void YesButtonClick(object sender, RoutedEventArgs e)
        {
            WindowResult = MessageBoxResult.Yes;
        }

        private void NoButtonClick(object sender, RoutedEventArgs e)
        {
            WindowResult = MessageBoxResult.No;
        }

        #endregion Dialog Window Logic

        #region Window Position

        public static int GetLeftPosition()
        {
            int result = 0;
            Action action = delegate
                {
                    result = (int) CurrentWindow.Left;
                };

            CurrentWindow.Dispatcher.Invoke(action, DispatcherPriority.Send);
            return result;
        }

        public static int GetTopPosition()
        {
            int result = 0;
            Action action = delegate
                {
                    result = (int) CurrentWindow.Top;
                };

            CurrentWindow.Dispatcher.Invoke(action, DispatcherPriority.Send);
            return result;
        }

        #endregion Window Position
    }
}