using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WordTraining.Config;
using WordTraining.Windows;

namespace WordTraining.Logic.DictionariesLogic
{
    [DebuggerDisplay("{DictonaryName} {NativeLang}-{TranslatedLang} L:{Learnt} R:{Repeated} {NativeWord}-{TranslatedWord}")]
    public class WordInfo
    {
        #region Common Dictionary Properties

        public string DictonaryName { get; set; }

        public int DictonaryLearntWordsCount
        {
            get { return Parent.Words.Count(x => x.Learnt >= 1); }
        }

        public int DictonaryTotalWordsCount
        {
            get { return Parent.Words.Count(); }
        }

        #endregion Common Dictionary Properties

        #region Current Word Properties
        
        public LanguageConfig NativeLang { get; set; }
        public string NativeWord { get; set; }

        public LanguageConfig TranslatedLang { get; set; }
        public string TranslatedWord { get; set; }

        public bool TwoWays { get; set; }
        public int Repeated { get; set; }
        public int Learnt { get; set; }
        
        #endregion Current Word Properties

        #region Current Word Number

        public DictionaryInfo Parent { get; set; } 

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

        #endregion Current Word Number

        #region All Supported Languages List

        public List<LanguageConfig> Languages
        {
            get { return App.CountriesConfig; }
        }

        #endregion All Supported Languages List
    }
}