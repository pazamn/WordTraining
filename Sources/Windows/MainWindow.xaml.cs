using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;
using WordTraining.Logic.DictionariesLogic;
using WordTraining.Logic.Services;
using ContextMenu = System.Windows.Controls.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Size = System.Drawing.Size;
using TextBox = System.Windows.Controls.TextBox;

namespace WordTraining.Windows
{
    public partial class MainWindow
    {
        #region PrivateFields

        public static MainWindow CurrentWindow { get; set; }

        #endregion PrivateFields

        #region Initialisation

        public MainWindow()
        {
            InitializeComponent();

            CurrentWindow = this;

            InitialiseContextMenu();
            InitialiseDictionaryWordChanging();
            InitialiseWindowPositionAndSize();
        }

        private void InitialiseContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();
            ContextMenu = contextMenu;

            MenuItem itIsLearnedItem = new MenuItem();
            Separator separator1Item = new Separator();
            MenuItem showPreviousWordItem = new MenuItem();
            MenuItem showNextWordItem = new MenuItem();
            Separator separator2Item = new Separator();
            MenuItem changeWordItem = new MenuItem();
            MenuItem settingsItem = new MenuItem();
            Separator separator3Item = new Separator();
            MenuItem closeItem = new MenuItem();

            itIsLearnedItem.Header = "I know this word!";
            itIsLearnedItem.Margin = new Thickness(5, 0, 0, 0);
            itIsLearnedItem.Click += WordLearnedButtonClick;

            showPreviousWordItem.Header = "Show previous word";
            showPreviousWordItem.Margin = new Thickness(5, 0, 0, 0);
            showPreviousWordItem.Click += ShowPreviousWordButtonClick;

            showNextWordItem.Header = "Show next word";
            showNextWordItem.Margin = new Thickness(5, 0, 0, 0);
            showNextWordItem.Click += ShowNextWordButtonClick;

            changeWordItem.Header = "Change word...";
            changeWordItem.Margin = new Thickness(5, 0, 0, 0);
            changeWordItem.Click += ChangeWordButtonClick;

            settingsItem.Header = "Settings...";
            settingsItem.Margin = new Thickness(5, 0, 0, 0);
            settingsItem.Click += SettingsButtonClick;

            closeItem.Header = "Close";
            closeItem.Margin = new Thickness(5, 0, 0, 0);
            closeItem.Click += CloseButtonClick;

            contextMenu.Items.Add(itIsLearnedItem);
            contextMenu.Items.Add(separator1Item);
            contextMenu.Items.Add(showPreviousWordItem);
            contextMenu.Items.Add(showNextWordItem);
            contextMenu.Items.Add(separator2Item);
            contextMenu.Items.Add(changeWordItem);
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(separator3Item);
            contextMenu.Items.Add(closeItem);
        }

        private void InitialiseDictionaryWordChanging()
        {
            App.DictionaryWordChanged += DictionaryWordChanged;
            App.DictionaryWordShowTranslation += DictionaryWordShowTranslation;
            App.CurrentWordTimingProgressChanged += CurrentWordTimingProgressChanged;

            if (App.ShowingWord != null)
            {
                Task.Factory.StartNew(delegate { WordInfo = App.ShowingWord; });
            }
        }

        private void InitialiseWindowPositionAndSize()
        {
            try
            {
                object windowLeftObject = Registry.LocalMachine.GetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowTopPosition");
                object windowTopObject = Registry.LocalMachine.GetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowLeftPosition");
                object windowWidthObject = Registry.LocalMachine.GetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowWidth");
                object windowHeightObject = Registry.LocalMachine.GetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowHeight");

                if (windowLeftObject == null || windowTopObject == null || windowWidthObject == null || windowHeightObject == null)
                {
                    return;
                }

                double windowLeft = Convert.ToDouble(windowLeftObject);
                double windowTop = Convert.ToDouble(windowTopObject);
                double windowWidth = Convert.ToDouble(windowWidthObject);
                double windowHeight = Convert.ToDouble(windowHeightObject);

                if (windowLeft + windowWidth <= 0 || windowTop <= 0 || windowWidth <= 20 || windowHeight <= 20)
                {
                    return;
                }

                Left = windowLeft;
                Top = windowTop;
                Width = windowWidth;
                Height = windowHeight;
            }
            catch (Exception)
            {
                //Just do not crash
            }
        }

        #endregion Initialisation

        #region Form Closing

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            MessageBoxResult result = CloseWindow();
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
            else
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        private static MessageBoxResult CloseWindow()
        {
            MessageBoxResult result = MessageBox.Show("Close application?", "Closing...", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result;
        }

        #endregion Form Closing

        #region Form Dragging and Resizing

        private void WindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void WindowSizeChanged(object sender, EventArgs e)
        {
            TryToBindToScreenEdges();
            SavePositionAndSizeToRegistry();
            ResizeWords();
        }

        private void MainWindowLocationChanged(object sender, EventArgs e)
        {
            TryToBindToScreenEdges();
            SavePositionAndSizeToRegistry();
        }

        private void ResizeWords()
        {
            double fontSize = 14.0;

            if (string.IsNullOrEmpty(NativeWordTextBlock.Text) && string.IsNullOrEmpty(TranslatedWordTextBlock.Text))
            {
                NativeWordTextBlock.FontSize = fontSize;
                TranslatedWordTextBlock.FontSize = fontSize;

                return;
            }

            for (double currentFontSize = 8.0; currentFontSize < 128.0; currentFontSize += 0.5)
            {
                Font font = new Font(NativeWordTextBlock.FontFamily.Source, (float)currentFontSize);
                Size nativeWordSize = TextRenderer.MeasureText(NativeWordTextBlock.Text, font);
                Size translatedWordSize = TextRenderer.MeasureText(TranslatedWordTextBlock.Text, font);

                if (nativeWordSize.Width >= 0.98 * NativeWordTextBlock.ActualWidth ||
                    nativeWordSize.Height >= 0.98 * NativeWordTextBlock.ActualHeight ||
                    translatedWordSize.Width >= 0.98 * TranslatedWordTextBlock.ActualWidth ||
                    translatedWordSize.Height >= 0.98 * TranslatedWordTextBlock.ActualHeight)
                {
                    break;
                }

                fontSize = currentFontSize;
            }

            NativeWordTextBlock.FontSize = fontSize + 0.5;
            TranslatedWordTextBlock.FontSize = fontSize + 0.5;
        }

        private void TryToBindToScreenEdges()
        {
            Screen screen = Screen.AllScreens.LastOrDefault(currentScreen => Left >= currentScreen.WorkingArea.X);
            if (screen == null && Left >= -10 && Left < 10)
            {
                Left = 0;
                return;
            }

            if (screen == null)
            {
                return;
            }

            if (Left >= screen.WorkingArea.X - 10 && Left <= screen.WorkingArea.X + 10)
            {
                Left = screen.WorkingArea.X;
            }

            if (Top >= screen.WorkingArea.Y - 10 && Top <= screen.WorkingArea.Y + 10)
            {
                Top = screen.WorkingArea.Y;
            }

            if (Left >= screen.WorkingArea.X + screen.WorkingArea.Width - 10 && Left <= screen.WorkingArea.X + screen.WorkingArea.Width + 10)
            {
                Left = screen.WorkingArea.X + screen.WorkingArea.Width;
            }

            if (Left + Width >= screen.WorkingArea.X + screen.WorkingArea.Width - 10 && Left + Width <= screen.WorkingArea.X + screen.WorkingArea.Width + 10)
            {
                Left = screen.WorkingArea.X + screen.WorkingArea.Width - Width;
            }

            if (Top + Height >= screen.WorkingArea.Y + screen.WorkingArea.Height - 10 && Top + Height <= screen.WorkingArea.Y + screen.WorkingArea.Height + 10)
            {
                Top = screen.WorkingArea.Y + screen.WorkingArea.Height - Height;
            }
        }

        private void SavePositionAndSizeToRegistry()
        {
            try
            {
                Registry.LocalMachine.SetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowTopPosition", Left.ToString("F0"));
                Registry.LocalMachine.SetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowLeftPosition", Top.ToString("F0"));
                Registry.LocalMachine.SetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowWidth", Width.ToString("F0"));
                Registry.LocalMachine.SetValue("SOFTWARE\\Pazamn\\WordTraining\\WindowHeight", Height.ToString("F0"));
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to save window setings to Registry. " + e.Message, "Failed to save window setings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Form Dragging and Resizing

        #region Word Learnt Logic

        private void TranslatedWordTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            TextBox textBox = sender as TextBox;
            if (textBox == null || string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            const string prohibitedChars = ".,-_'\"\\/ {}[]()<>";
            string translation = textBox.Text.RemoveCharacters(prohibitedChars);
            string nativeAnswer = App.ShowingWord.NativeWord.RemoveCharacters(prohibitedChars);
            string translatedAnswer = App.ShowingWord.TranslatedWord.RemoveCharacters(prohibitedChars);

            bool succeeded = translation.Equals(nativeAnswer, StringComparison.InvariantCultureIgnoreCase) || translation.Equals(translatedAnswer, StringComparison.InvariantCultureIgnoreCase);
            MessageBoxResult result = MessageBox.Show(succeeded ? "Your translation is right. Is the word learnt?" : "Your translation is wrong.", "Word translation", succeeded ? MessageBoxButton.YesNo : MessageBoxButton.OK, MessageBoxImage.Asterisk);
            if (result == MessageBoxResult.Yes)
            {
                LearnCurrentWord();
            }

            e.Handled = true;
        }

        private static void LearnCurrentWord()
        {
            //todo Make DictionaryConfig as a parent of WordInfo and show learnt counts using it

            App.ShowingWord.Learnt++;
            App.ShowAnotherWord();
        }

        #endregion Word Learnt Logic

        #region ContextMenu Actions

        private static void WordLearnedButtonClick(object sender, RoutedEventArgs e)
        {
            if (App.ShowingWord == null)
            {
                return;
            }

            LearnCurrentWord();
        }

        private static void ShowPreviousWordButtonClick(object sender, RoutedEventArgs e)
        {
            if (App.ShowingWord != null)
            {
                App.ShowPreviousWord();
            }
        }

        private static void ShowNextWordButtonClick(object sender, RoutedEventArgs e)
        {
            if (App.ShowingWord != null)
            {
                App.ShowAnotherWord();
            }
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow();

            App.SuspendWordShowing();
            Hide();

            window.ShowDialog();

            App.ResumeWordShowing();
            Show();
        }

        private void ChangeWordButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow();

            window.ShowDictionaryAndSelectWord(_wordInfo.DictonaryName, _wordInfo);
            App.SuspendWordShowing();
            Hide();

            window.ShowDialog();

            App.ResumeWordShowing();
            Show();
        }

        private static void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = CloseWindow();
            if (result == MessageBoxResult.Yes)
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        #endregion ContextMenu Actions

        #region Word Changing

        private WordInfo _wordInfo;

        public WordInfo WordInfo
        {
            get
            {
                return _wordInfo;
            }
            set
            {
                _wordInfo = value;
                UpdateWordOnUi();
            }
        }

        private void DictionaryWordChanged(object sender, EventArgs args)
        {
            if (App.ShowingWord != null)
            {
                WordInfo = App.ShowingWord;
            }
        }

        private void DictionaryWordShowTranslation(object sender, EventArgs args)
        {
            if (App.ShowingWord == null)
            {
                return;
            }

            Action action = delegate
                {
                    string translatedWord = App.ShowingWord.NativeWord == NativeWordTextBlock.Text ? App.ShowingWord.TranslatedWord : App.ShowingWord.NativeWord;
                    TranslatedWordTextBlock.Text = translatedWord;

                    WindowSizeChanged(null, new EventArgs());
                };

            Dispatcher.Invoke(DispatcherPriority.Send, action);
        }

        private void CurrentWordTimingProgressChanged(object sender, EventArgs args)
        {
            UpdateProgressBar(App.CurrentWordTimingProgress);
        }

        private static string GetPicturePath(string language)
        {
            try
            {
                string configPath = FileSystemHelper.GetAbsolutePath("Resources\\CountriesConfig.xml");
                if (string.IsNullOrEmpty(configPath))
                {
                    return null;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(configPath);

                XmlNodeList countryNodes = doc.GetElementsByTagName("Country");
                List<XmlElement> countryElements = countryNodes.OfType<XmlElement>().ToList();

                IEnumerable<string> result = from country in countryElements
                                             let shortName = country.GetAttribute("ShortLanguageName")
                                             let pictureRelativePath = country.GetAttribute("Picture")
                                             where !string.IsNullOrEmpty(shortName) && !string.IsNullOrEmpty(pictureRelativePath)
                                             where shortName == language
                                             select pictureRelativePath;

                string relativePath = result.FirstOrDefault();
                if (string.IsNullOrEmpty(relativePath))
                {
                    return null;
                }

                string absolutePath = FileSystemHelper.GetAbsolutePath(relativePath);
                return File.Exists(absolutePath) ? absolutePath : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void UpdateWordOnUi()
        {
            try
            {
                string nativeImagePath = GetPicturePath(_wordInfo.NativeLang.Alias);
                string translatedImagePath = GetPicturePath(_wordInfo.TranslatedLang.Alias);

                string nativeWord = _wordInfo.NativeWord;
                string translatedWord = _wordInfo.TranslatedWord;

                Random randomGenerator = new Random();
                bool swapWords = _wordInfo.TwoWays && Convert.ToBoolean(randomGenerator.Next(0, 2));

                if (swapWords)
                {
                    string tempImagePath = nativeImagePath;

                    nativeWord = translatedWord;
                    nativeImagePath = translatedImagePath;

                    translatedImagePath = tempImagePath;
                }

                Action action = delegate
                {
                    NativeWordTextBlock.Text = nativeWord;
                    TranslatedWordTextBlock.Text = string.Empty;

                    NativeWordImage.Source = new BitmapImage(new Uri(nativeImagePath));
                    TranslatedWordImage.Source = new BitmapImage(new Uri(translatedImagePath));

                    DictionaryPathTextBlock.Text = string.Format("{0} ({1}/{2}", _wordInfo.DictonaryName, _wordInfo.DictonaryLearntWordsCount, _wordInfo.DictonaryTotalWordsCount + ")");
                    ToolTipService.SetToolTip(DictionaryPathTextBlock, FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + _wordInfo.DictonaryName));

                    WindowSizeChanged(null, new EventArgs());
                };

                Dispatcher.Invoke(DispatcherPriority.Send, action);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to update word on UI. " + e.Message);
            }
        }

        private void UpdateProgressBar(int progress)
        {
            try
            {
                Action action = delegate
                {
                    TimeProgressBar.IsIndeterminate = false;
                    TimeProgressBar.Minimum = 0;
                    TimeProgressBar.Maximum = 1000;
                    TimeProgressBar.Value = progress;

                    if (progress <= 500)
                    {
                        TimeProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else if (progress <= 900)
                    {
                        TimeProgressBar.Foreground = new SolidColorBrush(Colors.DarkOrange);
                    }
                    else
                    {
                        TimeProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                    }
                };

                Dispatcher.Invoke(DispatcherPriority.Send, action);
            }
            catch (ThreadAbortException)
            {
                
            }
            catch (Exception e)
            {
                throw new Exception("Failed to update progress bar. " + e.Message);
            }
        }

        #endregion Word Changing
    }
}