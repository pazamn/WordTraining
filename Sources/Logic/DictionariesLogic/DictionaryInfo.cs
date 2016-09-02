using System.Collections.Generic;

namespace WordTraining.Logic.DictionariesLogic
{
    public class DictionaryInfo
    {
        #region Public Properties

        public string Name { get; set; }
        public List<WordInfo> Words { get; set; }

        #endregion Public Properties

        #region Initialization

        public DictionaryInfo(string name)
        {
            Name = name;
            Words = new List<WordInfo>();
        }

        #endregion Initialization
    }
}