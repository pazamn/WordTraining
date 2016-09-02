using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using WordTraining.Config;
using WordTraining.Logic.DictionariesLogic;
using WordTraining.Windows;

namespace WordTraining.Logic.MainLogic
{
    public class WordTrainer
    {
        #region Private Fields

        private readonly List<WordInfo> _previousWords;

        private int _alreadyWaitedTimer;
        private Thread _mainWorker;
        private bool _waitForAnotherThread;

        #endregion Private Fields

        #region Initialisation

        public WordTrainer()
        {
            _previousWords = new List<WordInfo>();
            _mainWorker = new Thread(() => DoJob(null));
        }

        public void StartJob()
        {
            _mainWorker.Start();
        }

        public void RestartJob()
        {
            _mainWorker.Abort();

            while (_waitForAnotherThread)
            {
                Thread.Sleep(10);
            }

            _mainWorker = new Thread(() => DoJob(null));
            _mainWorker.Start();
        }

        public void ShowPreviousWord()
        {
            _mainWorker.Abort();

            while (_waitForAnotherThread)
            {
                Thread.Sleep(10);
            }

            WordInfo previousWord = null;
            try
            {
                _previousWords.Remove(_previousWords.LastOrDefault());
                previousWord = _previousWords.LastOrDefault();
                _previousWords.Remove(_previousWords.LastOrDefault());

                while (previousWord != null && previousWord.Learnt > 0)
                {
                    previousWord = _previousWords.LastOrDefault();
                    _previousWords.Remove(_previousWords.LastOrDefault());
                }
            }
            catch (Exception)
            {
                //Just do not crash, previous word should be null
            }

            _mainWorker = new Thread(() => DoJob(previousWord));
            _mainWorker.Start();
        }

        #endregion Initialisation

        #region Main Work

        private void DoJob(WordInfo showThisWord)
        {
            while (true)
            {
                WorkCycle(showThisWord);
            }
        }

        private void WorkCycle(WordInfo showThisWord)
        {
            _waitForAnotherThread = true;

            try
            {
                if (showThisWord == null)
                {
                    Random randomGenerator = new Random();
                    
                    List<WordInfo> words = DictionariesManager.GetWords();
                    List<WordInfo> appropriateWords = words.Where(x => x.Learnt == 0).ToList();
                    if (!appropriateWords.Any())
                    {
                        MessageBox.Show("Congratulations!\n\nYou have learned all words from selected dictionaries!", "Congratulations!", MessageBoxButton.OK, MessageBoxImage.Information);
                        while (true)
                        {
                            Thread.Sleep(100);
                        }
                    }

                    showThisWord = appropriateWords[randomGenerator.Next(appropriateWords.Count(w => w.Learnt == 0))];
                }

                App.ShowingWord = showThisWord;
                _previousWords.Add(showThisWord);

                if (_previousWords.Count > 100)
                {
                    _previousWords.RemoveAt(0);
                }
                
                _alreadyWaitedTimer = 0;
                int firstTimeout = ConfigManager.WorkConfig.WordShowingTimeout.Value;
                int secondTimeout = ConfigManager.WorkConfig.WordShowingTimeout.Value;

                while (_alreadyWaitedTimer < firstTimeout * 1000)
                {
                    double currentWordTimingProgress = (double)_alreadyWaitedTimer / (firstTimeout + secondTimeout) / 1000.0 * 1000.0;
                    App.CurrentWordTimingProgress = (int) currentWordTimingProgress;

                    Thread.Sleep(10);
                    _alreadyWaitedTimer += App.LearningSuspended ? 0 : 10;
                }

                App.ShowTranslation();

                _alreadyWaitedTimer = 0;
                while (_alreadyWaitedTimer < secondTimeout * 1000)
                {
                    double currentWordTimingProgress = (double)(firstTimeout * 1000 + _alreadyWaitedTimer) / (firstTimeout + secondTimeout) / 1000.0 * 1000.0;
                    App.CurrentWordTimingProgress = (int) currentWordTimingProgress;

                    Thread.Sleep(10);
                    _alreadyWaitedTimer += App.LearningSuspended ? 0 : 10;
                }

                GC.Collect();
            }
            finally
            {
                if (App.ShowingWord != null)
                {
                    App.ShowingWord.Repeated++;

                    Thread dictionarySaver = new Thread(() => DictionariesManager.SaveOneWordToDictionary(App.ShowingWord)) { Priority = ThreadPriority.AboveNormal };
                    dictionarySaver.Start();
                    dictionarySaver.Join();
                }

                _waitForAnotherThread = false;
            }
        }

        #endregion Main Work
    }
}