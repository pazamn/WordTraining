using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using WordTraining.Config;
using WordTraining.Logic.DictionariesLogic;
using WordTraining.Logic.MainLogic;
using WordTraining.Logic.Services;

namespace WordTraining.Windows
{
    public partial class App
    {
        #region Public Fields

        public static readonly WordTrainer WordTrainer;

        public static List<LanguageConfig> CountriesConfig;
        public static bool LearningSuspended;

        #endregion Public Fields

        #region Initialisation

        static App()
        {
            //PreloaderWindow preloader = null;
            //ThreadStart preloaderThreadStart = () =>
            //    {
            //        preloader = new PreloaderWindow();
            //        preloader.Show();
            //        Dispatcher.Run();
            //    };

            //Thread preloaderThread = new Thread(preloaderThreadStart) { IsBackground = true };
            //preloaderThread.SetApartmentState(ApartmentState.STA);
            //preloaderThread.Start();

            //Action hidePreloaderAction = delegate
            //    {
            //        if (preloader == null)
            //        {
            //            return;
            //        }

            //        preloader.Hide();
            //    };

            //UpdateManager.CheckForUpdatesAndServiceArguments();

            //preloader.Dispatcher.Invoke(hidePreloaderAction, DispatcherPriority.Send);
            //preloaderThread.Abort();

            InitialiseCountriesConfig();
            ConfigManager.ReadConfig();

            WordTrainer = new WordTrainer();
            WordTrainer.StartJob();
        }

        public static void InitialiseCountriesConfig()
        {
            try
            {
                string configPath = FileSystemHelper.GetAbsolutePath("Resources\\CountriesConfig.xml");
                if (string.IsNullOrEmpty(configPath))
                {
                    return;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(configPath);

                XmlNodeList countryNodes = doc.GetElementsByTagName("Country");
                List<XmlElement> countryElements = countryNodes.OfType<XmlElement>().ToList();

                IEnumerable<LanguageConfig> allLanguages = from countryElement in countryElements
                                                           let shortName = countryElement.GetAttribute("ShortLanguageName")
                                                           let pictureRelativePath = countryElement.GetAttribute("Picture")
                                                           let name = countryElement.GetAttribute("Name")
                                                           where !string.IsNullOrWhiteSpace(shortName)
                                                           where !string.IsNullOrWhiteSpace(pictureRelativePath)
                                                           where !string.IsNullOrWhiteSpace(name)
                                                           let pictureAbsolutePath = FileSystemHelper.GetAbsolutePath(pictureRelativePath)
                                                           where !string.IsNullOrWhiteSpace(pictureAbsolutePath)
                                                           select new LanguageConfig
                                                               {
                                                                   Alias = shortName,
                                                                   IconPath = pictureAbsolutePath,
                                                                   Name = name,
                                                               };


                CountriesConfig = allLanguages.ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to initialise countries config. " + e.Message);
            }
        }

        #endregion Initialisation

        #region Word Changing

        private static WordInfo _showingWord;
        private static int _currentWordTimingProgress;

        public delegate void DictionaryWordChangedEventHandler(object sender, EventArgs e);
        public static event DictionaryWordChangedEventHandler DictionaryWordChanged;

        public delegate void DictionaryWordShowTranslationEventHandler(object sender, EventArgs e);
        public static event DictionaryWordShowTranslationEventHandler DictionaryWordShowTranslation;

        public delegate void CurrentWordTimingProgressChangedEventHandler(object sender, EventArgs e);
        public static event CurrentWordTimingProgressChangedEventHandler CurrentWordTimingProgressChanged;

        public static WordInfo ShowingWord
        {
            get
            {
                return _showingWord;
            }
            set
            {
                _showingWord = value;

                if (DictionaryWordChanged != null)
                {
                    DictionaryWordChanged.BeginInvoke(null, new EventArgs(), delegate {  }, null);
                }
            }
        }

        public static int CurrentWordTimingProgress
        {
            get
            {
                return _currentWordTimingProgress;
            }
            set
            {
                _currentWordTimingProgress = value;

                if (CurrentWordTimingProgressChanged != null)
                {
                    CurrentWordTimingProgressChanged.Invoke(null, new EventArgs());
                }
            }
        }

        public static void ShowTranslation()
        {
            if (DictionaryWordShowTranslation != null)
            {
                DictionaryWordShowTranslation.Invoke(null, new EventArgs());
            }
        }

        public static void ShowPreviousWord()
        {
            WordTrainer.ShowPreviousWord();
        }

        public static void ShowAnotherWord()
        {
            WordTrainer.RestartJob();
        }

        public static void SuspendWordShowing()
        {
            LearningSuspended = true;
        }

        public static void ResumeWordShowing()
        {
            LearningSuspended = false;
        }

        #endregion Word Changing
    }
}