using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HtmlAgilityPack;
using WordTraining.Config;
using WordTraining.Logic.DictionariesLogic;
using WordTraining.Logic.Services;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using DataGrid = System.Windows.Controls.DataGrid;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using TabControl = System.Windows.Controls.TabControl;

namespace WordTraining.Windows
{
    public partial class SettingsWindow
    {
        #region Private Fields

        private DictionaryConfig _selectedDictionary;
        private ObservableCollection<DictionaryConfig> _dictionariesList;
        private ObservableCollection<WordInfo> _wordsList;

        private WordInfo _selectingWordIn;
        private bool _selectingWordEventSwitch;

        #endregion Private Fields

        #region Public Properties

        public static SettingsWindow CurrentWindow { get; set; }

        #endregion Public Properties

        #region Initialisation

        public SettingsWindow()
        {
            InitializeComponent();

            ConfigManager.ReadConfig();
            InitialiseConfig();
            App.InitialiseCountriesConfig();

            CurrentWindow = this;
            Width = Screen.PrimaryScreen.WorkingArea.Width * 0.8;
            Height = Screen.PrimaryScreen.WorkingArea.Height * 0.8;
        }

        private void InitialiseConfig()
        {
            try
            {
                UpdateDictionariesList();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to show settings window.\n\n" + e, "Failed to show settings window", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Initialisation

        #region Dictionaries List Editing

        private void RenameDictionaryMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DictionaryConfig selectedDictionary = DictionariesDataGrid.SelectedItem as DictionaryConfig;
                if (selectedDictionary == null)
                {
                    return;
                }

                string currentName = Path.GetFileNameWithoutExtension(selectedDictionary.Name);

                RenameDictionaryWindow changeNameWindow = new RenameDictionaryWindow { Owner = this, DictionaryName = currentName };
                changeNameWindow.ShowDialog();

                if (string.IsNullOrEmpty(changeNameWindow.DictionaryName))
                {
                    throw new Exception("Dictionary name is invalid: empty.");
                }

                if (changeNameWindow.DictionaryName == currentName)
                {
                    return;
                }
                
                var obsoleteDictionaryPath = FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + selectedDictionary.Name, false);
                if (!File.Exists(obsoleteDictionaryPath))
                {
                    throw new FileNotFoundException(string.Format("Dictionary {0} not found. ", selectedDictionary.Name));
                }

                selectedDictionary.Name = changeNameWindow.DictionaryName + ".xml";
                var updatedDictionaryPath = FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + selectedDictionary.Name, false);
                if (File.Exists(updatedDictionaryPath))
                {
                    throw new Exception(string.Format("Dictionary {0} already exists.", selectedDictionary.Name));
                }

                File.Move(obsoleteDictionaryPath, updatedDictionaryPath);
                ConfigManager.SaveConfig();
                UpdateDictionariesList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to rename dictionary. " + ex.Message, "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateDictionariesList()
        {
            Action action = delegate
                {
                    var path = FileSystemHelper.GetAbsolutePath("Dictionaries");
                    var allDictionaries = Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);

                    var addedDictionaries = allDictionaries.Where(x => !ConfigManager.WorkConfig.Dictionaries.Select(c => c.Name).Contains(x));
                    foreach (var addedDictionary in addedDictionaries)
                    {
                        var config = new DictionaryConfig { Name = addedDictionary, Enabled = true };
                        ConfigManager.WorkConfig.Dictionaries.Add(config);
                    }

                    DictionariesDataGrid.DataContext = null;

                    _dictionariesList = new ObservableCollection<DictionaryConfig>(ConfigManager.WorkConfig.Dictionaries.OrderBy(x => x.Name));
                    DictionariesDataGrid.DataContext = _dictionariesList;
                };

            Dispatcher.Invoke(DispatcherPriority.Send, action);
        }

        private void RemoveDictionaryMouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you want to completely remove the dictionary?", "Remove dictionary", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            DictionaryConfig dictionary = DictionariesDataGrid.SelectedItem as DictionaryConfig;
            if (dictionary == null)
            {
                return;
            }

            string dictionaryPath = FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + dictionary.Name);
            if (!string.IsNullOrEmpty(dictionaryPath))
            {
                File.Delete(dictionaryPath);
            }
 
            DictionaryConfig removingDictionary = ConfigManager.WorkConfig.Dictionaries.FirstOrDefault(d => d.Name.Equals(dictionary.Name));
            if (removingDictionary != null)
            {
                ConfigManager.WorkConfig.Dictionaries.Remove(removingDictionary);
            }

            ConfigManager.SaveConfig();
            UpdateDictionariesList();
        }

        #endregion Dictionaries List Editing

        #region Dictionary Editing

        #region Tab Control Actions

        private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if (tabControl == null)
            {
                return;
            }

            TabItem selectedItem = tabControl.SelectedItem as TabItem;
            if (selectedItem == null)
            {
                return;
            }

            DictionarySettingsButtonsPanel.Visibility = selectedItem.Header.ToString().ToUpperInvariant().Contains("DICTIONARY SETTINGS") ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion Tab Control Actions

        #region Dictionaries and Words Datagrids Actions

        private void EnableDictionaryCheckBoxClick(object sender, RoutedEventArgs e)
        {
            DictionaryConfig selectedDictionary = DictionariesDataGrid.SelectedItem as DictionaryConfig;
            if (selectedDictionary == null)
            {
                return;
            }

            selectedDictionary.Enabled = !selectedDictionary.Enabled;
        }

        private void EditDictionaryItemClick(object sender, RoutedEventArgs e)
        {
            DictionaryConfig selectedDictionary = DictionariesDataGrid.SelectedItem as DictionaryConfig;
            if (selectedDictionary == null)
            {
                return;
            }

            _selectedDictionary = selectedDictionary;
            ShowDictionaryAndSelectWord(selectedDictionary.Name, null);
        }

        public void ShowDictionaryAndSelectWord(string dictionaryName, WordInfo wordToSelect)
        {
            var dictionaryPath = FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + dictionaryName);

            var allWords = DictionariesManager.ReadOneDictionary(dictionaryPath);
            _wordsList = new ObservableCollection<WordInfo>(allWords.Words);
            WordsDataGrid.DataContext = _wordsList;

            DictionarySettingsTabItem.Visibility = Visibility.Visible;
            DictionarySettingsButtonsPanel.Visibility = Visibility.Visible;

            TextBlock headerTextBlock = new TextBlock
                {
                    FontSize = 14,
                    Margin = new Thickness(1, 1, 1, 1),
                    Text = dictionaryName,
                };

            Button headerCloseButton = new Button
                {
                    Width = 16,
                    Height = 16,
                    Background = new ImageBrush(new BitmapImage(new Uri(FileSystemHelper.GetAbsolutePath("Resources\\Close.png")))),
                    BorderThickness = new Thickness(0, 0, 0, 0),
                };

            StackPanel headerStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            headerStackPanel.Children.Add(headerTextBlock);
            headerStackPanel.Children.Add(headerCloseButton);

            headerCloseButton.Click -= HideDictionary;
            headerCloseButton.Click += HideDictionary;

            DictionarySettingsTabItem.Header = headerStackPanel;
            SettingsTabControl.SelectedItem = DictionarySettingsTabItem;
            if (wordToSelect != null)
            {
                _selectedDictionary = ConfigManager.WorkConfig.Dictionaries.FirstOrDefault(d => d.Name.Equals(App.ShowingWord.DictonaryName, StringComparison.InvariantCultureIgnoreCase));
            }

            PrepareWordSelectingInDataGrid(wordToSelect);
        }

        public void HideDictionary(object sender, EventArgs args)
        {
            DictionarySettingsTabItem.Visibility = Visibility.Collapsed;
            DictionarySettingsButtonsPanel.Visibility = Visibility.Collapsed;
            SettingsTabControl.SelectedItem = DictionariesListTabItem;
        }

        public void PrepareWordSelectingInDataGrid(WordInfo wordToSelect)
        {
            if (wordToSelect == null)
            {
                return;
            }

            try
            {
                IEnumerable<WordInfo> appropriateWords = from currentWord in _wordsList
                                                                   where currentWord.DictonaryName == wordToSelect.DictonaryName
                                                                   where currentWord.NativeLang.Alias == wordToSelect.NativeLang.Alias
                                                                   where currentWord.NativeWord == wordToSelect.NativeWord
                                                                   where currentWord.TranslatedLang.Alias == wordToSelect.TranslatedLang.Alias
                                                                   where currentWord.TranslatedWord == wordToSelect.TranslatedWord
                                                                   select currentWord;

                WordInfo word = appropriateWords.FirstOrDefault();
                if (word == null)
                {
                    return;
                }

                _selectingWordIn = word;
                _selectingWordEventSwitch = true;

                WordsDataGrid.LoadingRow -= SelectWordInDataGrid;
                WordsDataGrid.LoadingRow += SelectWordInDataGrid;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to select word in DataGrid. " + e.Message);
            }
        }

        public void SelectWordInDataGrid(object sender, EventArgs a)
        {
            if (!_selectingWordEventSwitch)
            {
                return;
            }

            _selectingWordEventSwitch = false;

            WordsDataGrid.Focus();
            WordsDataGrid.SelectedItem = _selectingWordIn;
            WordsDataGrid.CurrentColumn = WordsDataGrid.Columns[0];
            WordsDataGrid.ScrollIntoView(WordsDataGrid.SelectedItem, WordsDataGrid.Columns[0]);
        }

        #endregion Dictionaries and Words Datagrids Actions

        #region Lingualeo Word Adding

        private void AddWordsUsingLingualeo(object sender, RoutedEventArgs args)
        {
            long addedWordsCount = 0;
            IEnumerable<WordInfo> allWordsFromLingualeo = GetWordsListFromLingualeo();

            foreach (WordInfo justAddedWord in allWordsFromLingualeo)
            {
                WordInfo existedWord = _wordsList.FirstOrDefault(w => w.NativeLang == justAddedWord.NativeLang && w.TranslatedLang == justAddedWord.TranslatedLang && w.NativeWord == justAddedWord.NativeWord && w.TranslatedWord == justAddedWord.TranslatedWord);
                if (existedWord != null)
                {
                    continue;
                }

                _wordsList.Add(justAddedWord);
                addedWordsCount++;
            }

            MessageBox.Show(addedWordsCount + " new words successfully added.", addedWordsCount + " words added", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private IEnumerable<WordInfo> GetWordsListFromLingualeo()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                                            {
                                                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                                Filter = "HTM and HTML pages (*.htm, *.html)|*.htm; *.HTML|All files (*.*)|*.*",
                                                Title = "Please select saved htm or html page",
                                            };

                if (dialog.ShowDialog() != true)
                {
                    return null;
                }

                if (!File.Exists(dialog.FileName))
                {
                    MessageBox.Show("File " + dialog.FileName + " not found.", "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                HtmlDocument doc = new HtmlDocument();
                doc.Load(dialog.FileName, Encoding.UTF8);

                HtmlNode htmlNode = doc.DocumentNode.ChildNodes.FirstOrDefault(n => n.Name.ToUpperInvariant() == "HTML");
                if (htmlNode == null)
                {
                    MessageBox.Show("Element 'html' was not found.", "Html file not supported", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                HtmlNode bodyNode = htmlNode.ChildNodes.FirstOrDefault(n => n.Name.ToUpperInvariant() == "BODY");
                if (bodyNode == null)
                {
                    MessageBox.Show("Element 'body' was not found.", "Html file not supported", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                HtmlNode tableNode = bodyNode.ChildNodes.FirstOrDefault(n => n.Name.ToUpperInvariant() == "TABLE");
                if (tableNode == null)
                {
                    MessageBox.Show("Element 'table' was not found.", "Html file not supported", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                List<WordInfo> result = new List<WordInfo>();

                foreach (HtmlNode trNode in tableNode.ChildNodes.Where(n => n.Name.ToUpperInvariant() == "TR"))
                {
                    try
                    {
                        HtmlNode tdNodeEnglish = trNode.ChildNodes.Where(n => n.Name.ToUpperInvariant() == "TD").ToList()[1];
                        HtmlNode tdNodeRussian = trNode.ChildNodes.Where(n => n.Name.ToUpperInvariant() == "TD").ToList()[3];

                        if (tdNodeEnglish == null || tdNodeRussian == null)
                        {
                            continue;
                        }

                        HtmlNode bNodeEnglish = tdNodeEnglish.ChildNodes.FirstOrDefault(n => n.Name.ToUpperInvariant() == "B");
                        if (bNodeEnglish == null)
                        {
                            continue;
                        }

                        string russianWord = tdNodeRussian.InnerText;
                        string englishWord = tdNodeEnglish.InnerText;

                        WordInfo word = new WordInfo
                        {
                            DictonaryName = _selectedDictionary.Name,

                            NativeLang = new LanguageConfig { Alias = "Eng" },
                            TranslatedLang = new LanguageConfig { Alias = "Rus" },
                            NativeWord = englishWord,
                            TranslatedWord = russianWord,

                            Learnt = 0,
                            Repeated = 0,

                            Parent = null,
                        };

                        result.Add(word);
                    }
                    catch (Exception)
                    {
                        //Just do not fail
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to add words to dictionary using lingualeo updater.\n\n" + e.Message, "Failed to add words to dictionary", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion Lingualeo Word Adding

        #region Add Words Using Google Translate

        private void AddWordsUsingGoogleTranslate(object sender, RoutedEventArgs args)
        {

        }

        #endregion Add Words Using Google Translate

        #region Add/Remove Item

        private void WordsDataGridAddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid == null)
            {
                return;
            }

            LanguageConfig nativeLang = App.CountriesConfig.FirstOrDefault(c => c.Alias.Contains("Rus"));
            LanguageConfig translatedLang = App.CountriesConfig.LastOrDefault(c => c.Alias.Contains("Eng"));

            if (_wordsList.Count > 0)
            {
                List<IGrouping<LanguageConfig, WordInfo>> nativeLangGrouping = _wordsList.GroupBy(w => w.NativeLang).ToList();
                List<IGrouping<LanguageConfig, WordInfo>> translatedLangGrouping = _wordsList.GroupBy(w => w.TranslatedLang).ToList();

                int nativeLangGroupingMaxElementsCount = nativeLangGrouping.Max(g => g.Count());
                int translatedLangGroupingMaxElementsCount = translatedLangGrouping.Max(g => g.Count());

                IGrouping<LanguageConfig, WordInfo> nativeLangusgeGroup = nativeLangGrouping.FirstOrDefault(tt => tt.Count() == nativeLangGroupingMaxElementsCount);
                IGrouping<LanguageConfig, WordInfo> translatedLangusgeGroup = translatedLangGrouping.FirstOrDefault(tt => tt.Count() == translatedLangGroupingMaxElementsCount);

                if (nativeLangusgeGroup != null)
                {
                    nativeLang = nativeLangusgeGroup.Key;
                }

                if (translatedLangusgeGroup != null)
                {
                    translatedLang = translatedLangusgeGroup.Key;
                }
            }

            WordInfo lastItem = _wordsList.LastOrDefault() ?? new WordInfo();

            e.NewItem = new WordInfo
            {
                NativeLang = nativeLang,
                TranslatedLang = translatedLang,

                TwoWays = lastItem.TwoWays,
                Parent = lastItem.Parent,
            };
        }

        private void OnRemoveWordMenuItemClick(object sender, RoutedEventArgs e)
        {
            var word = (WordInfo) WordsDataGrid.SelectedItem;

            word.Parent.Words.Remove(word);
            _wordsList.Remove(word);
        }

        #endregion Add/Remove Item

        #endregion Dictionary Editing

        #region OK Cancel Buttons Actions

        private void ApplyButtonClick(object sender, RoutedEventArgs e)
        {
            SaveRootConfig();
            SaveDictionary();

            DictionariesManager.UpdateWords();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            SaveRootConfig();
            SaveDictionary();

            DictionariesManager.UpdateWords();
            Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveRootConfig()
        {
            try
            {
                List<DictionaryConfig> dictionariesList = new List<DictionaryConfig>();
                foreach (object dictionaryElement in DictionariesDataGrid.Items)
                {
                    DictionaryConfig dictionaryConfig = dictionaryElement as DictionaryConfig;

                    if (dictionaryConfig == null || string.IsNullOrEmpty(dictionaryConfig.Name))
                    {
                        continue;
                    }

                    if (dictionariesList.Select(d => d.Name).Contains(dictionaryConfig.Name))
                    {
                        continue;
                    }

                    dictionariesList.Add(dictionaryConfig);
                }

                foreach (DictionaryConfig dictionaryConfig in dictionariesList)
                {
                    var dictionaryPath = FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + dictionaryConfig.Name);
                    if (!File.Exists(dictionaryPath))
                    {
                        DictionariesManager.SaveOneDictionary(dictionaryConfig.Name, new List<WordInfo>());  
                    }
                }

                ConfigManager.WorkConfig.Dictionaries = new List<DictionaryConfig>(dictionariesList);
                ConfigManager.SaveConfig();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to apply settings.\n\n" + e, "Failed to apply settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveDictionary()
        {
            if (_selectedDictionary == null || _wordsList == null)
            {
                return;
            }

            try
            {
                DictionariesManager.SaveOneDictionary(_selectedDictionary.Name, _wordsList);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to apply settings in dictionary.\n\n" + e, "Failed to apply settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion OK Cancel Buttons Actions

        #region Copy/paste translations

        private void OnNavigateToNextNotTranslatedWordMenuItemClick(object sender, RoutedEventArgs e)
        {
            var words = WordsDataGrid.DataContext.Of<IEnumerable>().OfType<WordInfo>().ToList();

            var selectedWord = WordsDataGrid.SelectedItems.OfType<WordInfo>().FirstOrDefault();
            var offset = selectedWord != null ? words.IndexOf(selectedWord) + 1 : 0;

            var firstNotTranslatedWord = words.Skip(offset).FirstOrDefault(x => string.IsNullOrEmpty(x.TranslatedWord));
            if (firstNotTranslatedWord == null)
            {
                MessageBox.Show("No more not translated words");
                return;
            }

            WordsDataGrid.ScrollIntoView(firstNotTranslatedWord);
            WordsDataGrid.SelectedItem = firstNotTranslatedWord;
        }

        private void OnCopyNativeWordsMenuItemClick(object sender, RoutedEventArgs e)
        {
            var words = WordsDataGrid.SelectedItems.OfType<WordInfo>();
            var text = string.Join("\r\n", words.Select(x => x.NativeWord));
            Clipboard.SetText(text);
        }

        private void OnPasteTranslationsMenuItemClick(object sender, RoutedEventArgs e)
        {
            var text = Clipboard.GetText();
            var lines = text.Split('\n').Select(x => x.Trim('\r').Trim()).ToList();

            var selectedWords = WordsDataGrid.SelectedItems.OfType<WordInfo>().ToList();
            if (selectedWords.Count != lines.Count)
            {
                MessageBox.Show("Count of pasted translations is not equal to selected rows.", "Translations are not valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                selectedWords[i].TranslatedWord = lines[i];
            }
        }

        #endregion Copy/paste translations
    }
}