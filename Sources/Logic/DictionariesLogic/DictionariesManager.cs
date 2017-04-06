using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using WordTraining.Config;
using WordTraining.Logic.Services;
using WordTraining.Windows;

namespace WordTraining.Logic.DictionariesLogic
{
    public class DictionariesManager
    {
        #region Private Fields

        private static readonly object DictionariesLocker;

        private static List<DictionaryInfo> _dictionaries; 
        private static List<WordInfo> _words; 

        #endregion Private Fields

        #region Initialisation

        static DictionariesManager()
        {
            DictionariesLocker = new object();
        }

        #endregion Initialisation

        #region Reading Dictionaries

        public static List<WordInfo> GetWords()
        {
            try
            {
                if (_words == null)
                {
                    UpdateWords();
                }

                return _words;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to read dictionaries.\n\n" + e, "Failed to read dictionaries", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static void UpdateWords()
        {
            try
            {
                _dictionaries = new List<DictionaryInfo>();
                _words = new List<WordInfo>();

                var path = FileSystemHelper.GetAbsolutePath("Dictionaries");
                var dictionaries = Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly).ToDictionary(Path.GetFileName, x => x);

                var disabledDictionaries = ConfigManager.WorkConfig.Dictionaries.Where(d => !d.Enabled).Select(x => x.Name);
                var enabledDictionaries = dictionaries.Where(x => !disabledDictionaries.Contains(x.Key)).Select(x => x.Value);

                foreach (var dictionary in enabledDictionaries)
                {
                    var currentDictionary = ReadOneDictionary(dictionary);
                    if (currentDictionary == null || currentDictionary.Words.Count == 0)
                    {
                        continue;
                    }

                    _dictionaries.Add(currentDictionary);
                    _words.AddRange(currentDictionary.Words);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to update words. " + e.Message);
            }
        }

        public static DictionaryInfo ReadOneDictionary(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show("Failed to read dictionary " + path + ".\n\nFile does not exists.", "Failed to read dictionary", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                XmlDocument doc = new XmlDocument();
                lock (DictionariesLocker)
                {
                    doc.Load(path);
                }

                return ParseOneDictionary(doc, path);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to read dictionary " + path + ".\n\n" + e, "Failed to read dictionary", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static DictionaryInfo ParseOneDictionary(string innerXmlContent, string dictionaryRelativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(innerXmlContent))
                {
                    throw new Exception("Inner XML content is null.");
                }

                XmlDocument doc = new XmlDocument();
                lock (DictionariesLocker)
                {
                    doc.LoadXml(innerXmlContent);
                }

                return ParseOneDictionary(doc, dictionaryRelativePath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to parse content of dictionary " + dictionaryRelativePath + ".\n\n" + e, "Failed to read dictionary", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private static DictionaryInfo ParseOneDictionary(XmlDocument doc, string path)
        {
            try
            {
                var result = new DictionaryInfo(Path.GetFileName(path));

                var wordElements = doc.GetElementsByTagName("Word").OfType<XmlElement>().ToList();
                foreach (XmlElement wordElement in wordElements)
                {
                    LanguageConfig nativeLang = App.CountriesConfig.FirstOrDefault(c => c.Alias.Equals(wordElement.GetAttribute("NativeLang"), StringComparison.InvariantCultureIgnoreCase));
                    string nativeWord = wordElement.GetAttribute("NativeWord");

                    LanguageConfig translatedLang = App.CountriesConfig.FirstOrDefault(c => c.Alias.Equals(wordElement.GetAttribute("TranslatedLang"), StringComparison.InvariantCultureIgnoreCase));
                    string translatedWord = wordElement.GetAttribute("TranslatedWord");

                    bool twoWays = Convert.ToBoolean(wordElement.GetAttribute("TwoWays").ToLowerInvariant());
                    int learnt = Convert.ToInt32(wordElement.GetAttribute("Learnt"));
                    int repeatedTimes = Convert.ToInt32(wordElement.GetAttribute("Repeated"));

                    if (nativeLang == null || translatedLang == null)
                    {
                        continue;
                    }

                    WordInfo wordInfo = new WordInfo
                    {
                        DictonaryName = Path.GetFileName(path),

                        NativeLang = nativeLang,
                        NativeWord = nativeWord,

                        TranslatedLang = translatedLang,
                        TranslatedWord = translatedWord,

                        TwoWays = twoWays,
                        Repeated = repeatedTimes,
                        Learnt = learnt,

                        Parent = result,
                    };

                    result.Words.Add(wordInfo);
                }

                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse dictionary. " + e.Message, e);
            }
        }

        #endregion Reading Dictionaries

        #region Writing Dictionaries

        public static void SaveDictionaries(List<WordInfo> words)
        {
            try
            {
                IEnumerable<IGrouping<string, WordInfo>> groupedWords = words.GroupBy(w => w.DictonaryName);
                foreach (IGrouping<string, WordInfo> groupedWord in groupedWords)
                {
                    SaveOneDictionary(groupedWord.Key, groupedWord);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to save dictionaries.\n\n" + e, "Failed to save dictionaries", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveOneDictionary(string name, IEnumerable<WordInfo> words)
        {
            try
            {
                string absolutePath = FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + name, false);
                if (!File.Exists(absolutePath))
                {
                    StringBuilder defaultDictionary = new StringBuilder();
                    defaultDictionary.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    defaultDictionary.AppendLine("<Words>");
                    defaultDictionary.AppendLine("</Words>");

                    File.WriteAllText(absolutePath, defaultDictionary.ToString());
                }

                XmlDocument doc = new XmlDocument();
                if (File.Exists(absolutePath))
                {
                    lock (DictionariesLocker)
                    {
                        doc.Load(absolutePath);
                    }

                    XmlElement wordsNode = doc.GetElementsByTagName("Words").OfType<XmlElement>().FirstOrDefault();
                    if (wordsNode != null)
                    {
                        wordsNode.RemoveAll();
                    }
                }
                else
                {
                    XmlDeclaration firstElement = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    doc.AppendChild(firstElement);

                    XmlElement rootElement = doc.CreateElement("Words");
                    doc.AppendChild(rootElement);
                }

                XmlNodeList wordNodes = doc.GetElementsByTagName("Word");
                List<XmlElement> wordElements = wordNodes.OfType<XmlElement>().ToList();

                foreach (WordInfo savingWord in words)
                {

                    IEnumerable<XmlElement> savingElementsEnumerable = from wordElement in wordElements
                                                                       let nativeLang = App.CountriesConfig.FirstOrDefault(c => c.Alias.Equals(wordElement.GetAttribute("NativeLang"), StringComparison.InvariantCultureIgnoreCase))
                                                                       let nativeWord = wordElement.GetAttribute("NativeWord")
                                                                       let translatedLang = App.CountriesConfig.FirstOrDefault(c => c.Alias.Equals(wordElement.GetAttribute("TranslatedLang"), StringComparison.InvariantCultureIgnoreCase))
                                                                       let translatedWord = wordElement.GetAttribute("TranslatedWord")
                                                                       where nativeLang != null && !string.IsNullOrEmpty(nativeWord) && translatedLang != null && !string.IsNullOrEmpty(translatedWord)
                                                                       where nativeLang.Alias == savingWord.NativeLang.Alias && translatedLang.Alias == savingWord.TranslatedLang.Alias && nativeWord == savingWord.NativeWord && translatedWord == savingWord.TranslatedWord
                                                                       select wordElement;

                    XmlElement savingElement = savingElementsEnumerable.FirstOrDefault();

                    if (savingElement == null)
                    {
                        savingElement = doc.CreateElement("Word");

                        savingElement.SetAttribute("Learnt", savingWord.Learnt.ToString(CultureInfo.InvariantCulture));
                        savingElement.SetAttribute("Repeated", savingWord.Repeated.ToString(CultureInfo.InvariantCulture));
                        savingElement.SetAttribute("TwoWays", savingWord.TwoWays.ToString());

                        savingElement.SetAttribute("NativeLang", savingWord.NativeLang.Alias);
                        savingElement.SetAttribute("NativeWord", savingWord.NativeWord);

                        savingElement.SetAttribute("TranslatedLang", savingWord.TranslatedLang.Alias);
                        savingElement.SetAttribute("TranslatedWord", savingWord.TranslatedWord);

                        XmlElement wordsRootElement = doc.LastChild as XmlElement;
                        if (wordsRootElement != null)
                        {
                            wordsRootElement.AppendChild(savingElement);
                        }
                    }
                    else
                    {
                        savingElement.SetAttribute("Learnt", savingWord.Learnt.ToString(CultureInfo.InvariantCulture));
                        savingElement.SetAttribute("Repeated", savingWord.Repeated.ToString(CultureInfo.InvariantCulture));
                    }
                }

                lock (DictionariesLocker)
                {
                    doc.Save(absolutePath);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to save dictionary '" + name + "'.\n\n" + e, "Failed to save dictionarie", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveOneWordToDictionary(WordInfo word)
        {
            try
            {
                string absolutePath = FileSystemHelper.GetAbsolutePath(@"Dictionaries\" + word.DictonaryName);
                if (!File.Exists(absolutePath))
                {
                    MessageBox.Show("Failed to save word to dictionary " + word.DictonaryName + ".\n\nFile does not exists.", "Failed to save word to dictionary", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                XmlDocument doc = new XmlDocument();
                if (!File.Exists(absolutePath))
                {
                    MessageBox.Show("Failed to save word to dictionary " + word.DictonaryName + ".\n\nFile does not exists.", "Failed to save word to dictionary", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                lock (DictionariesLocker)
                {
                    doc.Load(absolutePath);
                }

                XmlElement wordsNode = doc.GetElementsByTagName("Words").OfType<XmlElement>().FirstOrDefault();
                if (wordsNode == null)
                {
                    MessageBox.Show("Failed to save word to dictionary " + word.DictonaryName + ".\n\nWords collection does not exist.", "Failed to save word to dictionary", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                XmlNodeList wordNodes = doc.GetElementsByTagName("Word");
                List<XmlElement> wordElements = wordNodes.OfType<XmlElement>().ToList();

                bool isWordCreatedNow = false;
                XmlElement currentWord = wordElements.FirstOrDefault(e => e.GetAttribute("NativeWord") == word.NativeWord.ToString());
                if (currentWord == null)
                {
                    currentWord = doc.CreateElement("Word");
                    isWordCreatedNow = true;
                }

                currentWord.SetAttribute("NativeLang", word.NativeLang.Alias);
                currentWord.SetAttribute("NativeWord", word.NativeWord);

                currentWord.SetAttribute("TranslatedLang", word.TranslatedLang.Alias);
                currentWord.SetAttribute("TranslatedWord", word.TranslatedWord);

                currentWord.SetAttribute("Learnt", word.Learnt.ToString(CultureInfo.InvariantCulture));
                currentWord.SetAttribute("Repeated", word.Repeated.ToString(CultureInfo.InvariantCulture));
                currentWord.SetAttribute("TwoWays", word.TwoWays.ToString());

                XmlElement wordsRootElement = doc.LastChild as XmlElement;
                if (wordsRootElement != null && isWordCreatedNow)
                {
                    wordsRootElement.AppendChild(currentWord);
                }

                lock (DictionariesLocker)
                {
                    doc.Save(absolutePath);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to save word to dictionary '" + word.DictonaryName + "'.\n\n" + e, "Failed to save dictionarie", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Writing Dictionaries
    }
}