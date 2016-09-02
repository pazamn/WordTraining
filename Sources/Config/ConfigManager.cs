using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using WordTraining.Logic.Services;

namespace WordTraining.Config
{
    public static class ConfigManager
    {
        #region Public Things

        public static WorkConfig WorkConfig { get; set; }

        public static void ReadConfig()
        {
            try
            {
                List<DictionaryConfig> dictionariesListBackup = null;
                if (WorkConfig != null && WorkConfig.Dictionaries != null)
                {
                    dictionariesListBackup = WorkConfig.Dictionaries;
                }

                string configPath = FileSystemHelper.GetAbsolutePath("Config\\Config.xml");
                WorkConfig = ReadConfig<WorkConfig>(configPath);
                WorkConfig.Dictionaries = dictionariesListBackup ?? WorkConfig.Dictionaries;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to read config file.\n\n" + e, "Failed to read config file", MessageBoxButton.OK, MessageBoxImage.Error);
                WorkConfig = null;
            }
        }

        public static void SaveConfig()
        {
            try
            {
                string configPath = FileSystemHelper.GetAbsolutePath("Config\\Config.xml");
                if (!Directory.Exists(FileSystemHelper.GetAbsolutePath("Config")))
                {
                    Directory.CreateDirectory(FileSystemHelper.GetAbsolutePath("Config"));
                }

                WorkConfig = WriteConfig(configPath, WorkConfig);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to save config file.\n\n" + e, "Failed to save config file", MessageBoxButton.OK, MessageBoxImage.Error);
                WorkConfig = null;
            }
        }

        #endregion Public Things

        #region Read Config

        private static T ReadConfig<T>(string path)
        {
            StreamReader reader = new StreamReader(path);

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                object result = serializer.Deserialize(reader);

                return (T)result;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to read config", e);
            }
            finally
            {
                reader.Close();
            }
        }

        #endregion Read Config

        #region Write Config

        private static T WriteConfig<T>(string path, T config)
        {
            XmlDocument document = new XmlDocument();

            if (File.Exists(path))
            {
                document.Load(path);
            }

            XmlElement root = document.DocumentElement;
            if (root == null)
            {
                root = document.CreateElement(GetConfigName(typeof(T)));
                document.AppendChild(root);
            }

            WriteElement(root, config);
            document.Save(path);

            return config;
        }

        private static string GetConfigName(Type type)
        {
            object[] xmlType = type.GetCustomAttributes(typeof(XmlTypeAttribute), false);
            return xmlType.Length != 0 ? ((XmlTypeAttribute)xmlType[0]).TypeName : type.Name;
        }

        private static void WriteElement(XmlElement node, object config)
        {
            IEnumerable<PropertyInformation<XmlAttributeAttribute>> attributes = GetProperties<XmlAttributeAttribute>(config);

            foreach (PropertyInformation<XmlAttributeAttribute> pi in attributes)
            {
                object defaultAttr = pi.Property.GetCustomAttributes(typeof(System.ComponentModel.DefaultValueAttribute), true).SingleOrDefault();
                object value = pi.Property.GetValue(config, null);

                if (defaultAttr != null)
                {
                    object defValue = ((System.ComponentModel.DefaultValueAttribute)defaultAttr).Value;
                    if (defValue == null && value == null)
                    {
                        node.RemoveAttribute(pi.Property.Name);
                        continue;
                    }
                    if (defValue != null && defValue.Equals(value))
                    {
                        node.RemoveAttribute(pi.Property.Name);
                        continue;
                    }
                }
                if (value == null)
                {
                    XmlAttribute attr = node.Attributes[pi.Property.Name];
                    if (attr != null)
                    {
                        node.Attributes.Remove(attr);
                    }
                }
                else
                {
                    string attrValue = String.Format(CultureInfo.InvariantCulture, "{0}", value);

                    if (value is bool)
                    {
                        attrValue = attrValue.ToLower();
                    }

                    node.SetAttribute(pi.Property.Name, attrValue);
                }
            }

            IEnumerable<PropertyInformation<XmlArrayItemAttribute>> boundedCollections = GetProperties<XmlArrayItemAttribute>(config); //there are "unbounded" too. see below
            foreach (var pi in boundedCollections)
            {
                string containerName = pi.Property.Name;
                XmlArrayAttribute xmlArray = (XmlArrayAttribute)pi.Property.GetCustomAttributes(typeof(XmlArrayAttribute), true).SingleOrDefault();
                if (xmlArray != null)
                {
                    containerName = xmlArray.ElementName;
                }

                XmlNode container = node.SelectSingleNode(containerName);
                if (container == null)
                {
                    container = node.OwnerDocument.CreateElement(containerName);
                    node.AppendChild(container);
                }

                IEnumerable collection = pi.Property.GetValue(config, null) as IEnumerable;
                WriteCollection(collection, pi.Attribute.ElementName, container);
            }

            IEnumerable<PropertyInformation<XmlElementAttribute>> selfCollection = GetProperties<XmlElementAttribute>(config);//config element contains collection of elements.
            foreach (PropertyInformation<XmlElementAttribute> pi in selfCollection)
            {
                string elementName = pi.Property.Name;
                if (!string.IsNullOrEmpty(pi.Attribute.ElementName))
                {
                    elementName = pi.Attribute.ElementName;
                }

                object value = pi.Property.GetValue(config, null);
                IEnumerable collection = value as IEnumerable;
                if (value is string)
                {
                    collection = null;
                }

                if (collection != null)
                {
                    WriteCollection(collection, elementName, node);
                }
                else
                {
                    XmlElement elementXml = (XmlElement)node.SelectSingleNode(elementName);
                    if (elementXml == null)
                    {
                        elementXml = node.OwnerDocument.CreateElement(elementName);
                        node.AppendChild(elementXml);
                    }

                    if (elementXml.Name == "Timeout" && value == null)
                    {
                        XmlNode parent = elementXml.ParentNode;
                        parent.RemoveChild(elementXml);
                    }

                    try
                    {
                        if (value == null)
                        {
                            elementXml.InnerText = string.Empty;
                            continue;
                        }
                        if (value is string || value is int || value is double || value is long)
                        {
                            elementXml.InnerText = value.ToString();
                            continue;
                        }
                        if (value is bool)
                        {
                            bool v = (bool)value;
                            elementXml.InnerText = v ? "true" : "false";
                            continue;
                        }
                    }
                    catch { }

                    WriteElement(elementXml, value);
                }
            }
        }

        private static void WriteCollection(IEnumerable collection, string elementName, XmlNode container)
        {
            if (collection == null)
            {
                container.RemoveAll();
                return;
            }

            XmlElement[] nodes = container.SelectNodes(elementName).Cast<XmlElement>().ToArray();

            for (int i = 0; i < nodes.Length; i++)
            {
                //remove all nodes. some will be added back again. This is done to maintain collection elements order.
                container.RemoveChild(nodes[i]);
            }

            object[] objects = collection.OfType<object>().ToArray();
            if (objects.Length == 0)
            {
                return;
            }

            foreach (object o in objects)
            {
                if (container.OwnerDocument == null)
                {
                    continue;
                }

                XmlElement itemNode = container.OwnerDocument.CreateElement(elementName);
                container.AppendChild(itemNode);
                WriteElement(itemNode, o);
            }
        }

        private static IEnumerable<PropertyInformation<TAttribute>> GetProperties<TAttribute>(object obj) where TAttribute : Attribute
        {
            return from property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   from attr in property.GetCustomAttributes(typeof(TAttribute), true)
                   select new PropertyInformation<TAttribute> { Property = property, Attribute = (TAttribute)attr };
        }

        private struct PropertyInformation<TAttribute> where TAttribute : Attribute
        {
            public PropertyInfo Property;
            public TAttribute Attribute;
        }

        #endregion Write Config
    }
}
