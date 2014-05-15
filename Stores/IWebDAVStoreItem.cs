using System;

namespace WebDAVSharp.Server.Stores
{
    /// <summary>
    /// This interface must be implemented by classes that will function as a store item,
    /// which is either a document (
    /// <see cref="IWebDavStoreDocument" />) or a
    /// collection of documents (
    /// <see cref="IWebDavStoreCollection" />.)
    /// </summary>
    public interface IWebDavStoreItem
    {
        /// <summary>
        /// Gets the parent <see cref="IWebDavStoreCollection" /> that owns this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <value>
        /// The parent collection.
        /// </value>
        IWebDavStoreCollection ParentCollection
        {
            get;
        }

        /// <summary>
        /// Gets or sets the name of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the ItemPath of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <value>
        /// The item path.
        /// </value>
        string ItemPath
        {
            get;
        }

        /// <summary>
        /// Gets if this <see cref="IWebDavStoreItem" /> is a collection.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is collection; otherwise, <c>false</c>.
        /// </value>
        bool IsCollection
        {
            get;
        }

        /// <summary>
        /// Gets the creation date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <value>
        /// The creation date.
        /// </value>
        DateTime CreationDate
        {
            get;
        }

        /// <summary>
        /// Gets the modification date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <value>
        /// The modification date.
        /// </value>
        DateTime ModificationDate
        {
            get;
        }

        /// <summary>
        /// Gets the hidden state of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <value>
        /// 1 if hidden, 0 if not.
        /// </value>
        int Hidden
        {
            get;
        }
    }
}