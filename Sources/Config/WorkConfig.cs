using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace WordTraining.Config
{
    [DataContract]
    public class WorkConfig
    {
        [XmlElement, DataMember]
        public WordShowingTimeout WordShowingTimeout { get; set; }

        [DataMember, XmlArray("DictionariesConfig"), XmlArrayItem("DictionaryConfig")]
        public List<DictionaryConfig> Dictionaries { get; set; }
    }

    [DataContract, DebuggerDisplay("First:{First}, Second:{Second}, Typing:{Typing}")]
    public class WordShowingTimeout
    {
        [XmlAttribute, DataMember]
        public int Value { get; set; }
    }

    [DataContract, DebuggerDisplay("{Name} (E:{Enabled})")]
    public class DictionaryConfig
    {
        [XmlAttribute, DataMember]
        public string Name { get; set; }

        [XmlAttribute, DataMember]
        public bool Enabled { get; set; }
    }
}