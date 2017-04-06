using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WordTraining.Config;
using WordTraining.Logic.BaseClasses;
using WordTraining.Windows;

namespace WordTraining.Logic.DictionariesLogic
{
    [DebuggerDisplay("{DictonaryName} {NativeLang}-{TranslatedLang} L:{Learnt} R:{Repeated} {NativeWord}-{TranslatedWord}")]
    public class WordInfo : NotificationObject
    {
        #region Private Fields and Initialization

        private LanguageConfig _nativeLang;
        private LanguageConfig _translationLang;
        private string _nativeWord;
        private string _translationWord;
        private bool _twoWays;
        private int _repeated;
        private int _learned;

        #endregion Private Fields and Initialization

        #region Public Properties

        public string DictonaryName { get; set; }

        public DictionaryInfo Parent { get; set; }

        public int DictonaryLearntWordsCount
        {
            get { return Parent.Words.Count(x => x.Learnt >= 1); }
        }

        public int DictonaryTotalWordsCount
        {
            get { return Parent.Words.Count; }
        }

        public List<LanguageConfig> Languages
        {
            get { return App.CountriesConfig; }
        }

        public int Number
        {
            get
            {
                if (Parent == null || Parent.Words == null || !Parent.Words.Any())
                {
                    return 0;
                }

                return Parent.Words.ToList().IndexOf(this) + 1;
            }
        }

        public LanguageConfig NativeLang
        {
            get { return _nativeLang; }
            set { SetField(ref _nativeLang, value, nameof(NativeLang)); }
        }

        public string NativeWord
        {
            get { return _nativeWord; }
            set { SetField(ref _nativeWord, value, nameof(NativeWord)); }
        }

        public LanguageConfig TranslatedLang
        {
            get { return _translationLang; }
            set { SetField(ref _translationLang, value, nameof(TranslatedLang)); }
        }

        public string TranslatedWord
        {
            get { return _translationWord; }
            set { SetField(ref _translationWord, value, nameof(TranslatedWord)); }
        }

        public bool TwoWays
        {
            get { return _twoWays; }
            set { SetField(ref _twoWays, value, nameof(TwoWays)); }
        }

        public int Repeated
        {
            get { return _repeated; }
            set { SetField(ref _repeated, value, nameof(Repeated)); }
        }

        public int Learnt
        {
            get { return _learned; }
            set { SetField(ref _learned, value, nameof(Learnt)); }
        }

        #endregion Public Properties
    }
}