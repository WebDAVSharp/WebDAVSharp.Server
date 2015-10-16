using System.Xml;

namespace WebDAVSharp.Server
{
    /// <summary>
    ///     This class implements the core WebDAV server.
    /// </summary>
    internal class WebDavProperty
    {
        #region Variables

        public enum StoreItemTypeFlag
        {
            Document,
            Collection,
            Any
        }

        /// <summary>
        ///     Used as a prefix
        /// </summary>
        public string Extended;

        /// <summary>
        ///     This class implements the core WebDAV server.
        /// </summary>
        public string Name;

        /// <summary>
        ///     This class implements the core WebDAV server.
        /// </summary>
        public string Namespace;

        public StoreItemTypeFlag StoreItemType;

        /// <summary>
        ///     This class implements the core WebDAV server.
        /// </summary>
        public string Value;

        #endregion

        #region Constructor

        /// <summary>
        ///     Standard constructor
        /// </summary>
        public WebDavProperty()
        {
            Namespace = string.Empty;
            Name = string.Empty;
            Value = string.Empty;
            Extended = "";
            StoreItemType = StoreItemTypeFlag.Any;
        }

        /// <summary>
        ///     Constructor for the WebDAVProperty class with "DAV:" as namespace and an empty value
        /// </summary>
        /// <param name="name">The name of the WebDAV property</param>
        /// <param name="type"></param>
        public WebDavProperty(string name, StoreItemTypeFlag? type = null)
        {
            Name = name;
            Value = string.Empty;
            Namespace = "DAV:";
            Extended = "";
            StoreItemType = type ?? StoreItemTypeFlag.Any;
        }

        /// <summary>
        ///     Constructor for the WebDAVProperty class with "DAV:" as namespace
        /// </summary>
        /// <param name="name">The name of the WebDAV property</param>
        /// <param name="value">The value of the WebDAV property</param>
        /// <param name="type"></param>
        public WebDavProperty(string name, string value, StoreItemTypeFlag? type = null)
        {
            Name = name;
            Value = value;
            Namespace = "DAV:";
            Extended = "";
            StoreItemType = type ?? StoreItemTypeFlag.Any;
        }

        /// <summary>
        ///     Constructor for the WebDAVProperty class
        /// </summary>
        /// <param name="name">The name of the WebDAV property</param>
        /// <param name="value">The value of the WebDAV property</param>
        /// <param name="ns">The namespace of the WebDAV property</param>
        /// <param name="type"></param>
        public WebDavProperty(string name, string value, string ns, StoreItemTypeFlag? type = null)
        {
            Name = name;
            Value = value;
            Namespace = ns;
            Extended = "";
            StoreItemType = type ?? StoreItemTypeFlag.Any;
        }

        public WebDavProperty(string name, string value, string ns, string extended, StoreItemTypeFlag? type = null)
        {
            Name = name;
            Value = value;
            Namespace = ns;
            Extended = extended;
            StoreItemType = type ?? StoreItemTypeFlag.Any;
        }

        #endregion

        #region Functions

        /// <summary>
        ///     This class implements the core WebDAV server.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return StartString() + Value + EndString();
        }

        /// <summary>
        ///     This class implements the core WebDAV server.
        /// </summary>
        /// <returns>The begin tag of an XML element as a string</returns>
        public string StartString()
        {
            if (Namespace == "DAV:")
                return "<D:" + Name + ">";

            if (!string.IsNullOrEmpty(Extended))
                return "<" + Extended + ":" + Name + ">";

            return "<" + Name + " xmlns=\"" + Namespace + "\">";
        }

        /// <summary>
        ///     This class implements the core WebDAV server.
        /// </summary>
        /// <returns>An empty XML element as a string</returns>
        public string EmptyString()
        {
            if (Namespace == "DAV:")
                return "<D:" + Name + "/>";
            if (!string.IsNullOrEmpty(Extended))
                return "<" + Extended + ":" + Name + "/>";

            return "<" + Name + " xmlns=\"" + Namespace + "\"/>";
        }

        /// <summary>
        ///     This class implements the core WebDAV server.
        /// </summary>
        /// <returns>The closing tag of an XML element as a string</returns>
        public string EndString()
        {
            if (Namespace == "DAV:")
                return "</D:" + Name + ">";
            if (string.IsNullOrEmpty(Extended))
                return "</" + Extended + ":" + Name + ">";
            return "</" + Name + ">";
        }

        ///// <summary>
        /////     Creates an XmlDocumentFragment from the current WebDAVProperty
        ///// </summary>
        ///// <param name="doc">The XmlDocument where a XmlDocumentFragment is needed</param>
        ///// <returns>
        /////     The XmlDocumentFragment of the current WebDAVProperty object
        ///// </returns>
        //public XmlDocumentFragment ToXmlDocumentFragment(XmlDocument doc)
        //{
        //    XmlDocumentFragment fragment = doc.CreateDocumentFragment();
        //    fragment.InnerXml = ToString();
        //    return fragment;
        //}

        /// <summary>
        ///     reates an XmlElement from the current WebDAVProperty
        /// </summary>
        /// <param name="doc">The XmlDocument where a XmlElement is needed</param>
        /// <returns>
        ///     The XmlElement of the current WebDAVProperty object
        /// </returns>
        public XmlElement ToXmlElement(XmlDocument doc)
        {
            // if the DocumentElement is not null, return the XmlElement with namespace
            if (doc.DocumentElement == null)
                return doc.CreateElement(Name);
            // Get the prefix of the namespace
            string prefix = doc.DocumentElement.GetPrefixOfNamespace(Namespace);

            // Create the element
            XmlElement element = doc.CreateElement(prefix, Name, Namespace);
            element.InnerText = Value;
            return element;
            // else, return XmlElement without namespace
        }

        ///// <summary>
        /////     reates an XmlElement from the current WebDAVProperty
        ///// </summary>
        ///// <param name="doc">The XmlDocument where a XmlElement is needed</param>
        ///// <returns>
        /////     The XmlElement of the current WebDAVProperty object
        ///// </returns>
        //public XmlElement XmlToXmlElement(XmlDocument doc)
        //{
        //    // if the DocumentElement is not null, return the XmlElement with namespace
        //    if (doc.DocumentElement == null)
        //        return doc.CreateElement(Name);
        //    // Get the prefix of the namespace
        //    string prefix = doc.DocumentElement.GetPrefixOfNamespace(Namespace);

        //    // Create the element
        //    XmlElement element = doc.CreateElement(prefix, Name, Namespace);
        //    element.InnerXml = Value;
        //    return element;
        //    // else, return XmlElement without namespace
        //}

        #endregion
    }
}