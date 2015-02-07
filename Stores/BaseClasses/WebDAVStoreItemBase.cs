using System;
using WebDAVSharp.Server.Exceptions;

namespace WebDAVSharp.Server.Stores.BaseClasses
{
    /// <summary>
    /// This is the base class for <see cref="IWebDavStoreItem" /> implementations.
    /// </summary>
    public class WebDavStoreItemBase : IWebDavStoreItem
    {
        private readonly IWebDavStoreCollection _parentCollection;
        private string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDavStoreItemBase" /> class.
        /// </summary>
        /// <param name="parentCollection">The parent <see cref="IWebDavStoreCollection" /> that contains this <see cref="IWebDavStoreItem" /> implementation.</param>
        /// <param name="name">The name of this <see cref="IWebDavStoreItem" /></param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        protected WebDavStoreItemBase(IWebDavStoreCollection parentCollection, string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            _parentCollection = parentCollection;
            _name = name;
        }

        #region IWebDAVStoreItem Members

        /// <summary>
        /// Gets the parent <see cref="IWebDavStoreCollection" /> that owns this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public IWebDavStoreCollection ParentCollection
        {
            get
            {
                return _parentCollection;
            }
        }

        /// <summary>
        /// Gets or sets the name of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavForbiddenException"></exception>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                string fixedName = (value ?? string.Empty).Trim();
                if (fixedName == _name) return;
                if (!OnNameChanging(_name, fixedName))
                    throw new WebDavForbiddenException();
                string oldName = _name;
                _name = fixedName;
                OnNameChanged(oldName, _name);
            }
        }

        /// <summary>
        /// Gets the creation date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public virtual DateTime CreationDate
        {
            get
            {
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Gets the modification date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public virtual DateTime ModificationDate
        {
            get
            {
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Gets the path to this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public virtual string ItemPath
        {
            get { return String.Empty; }
        }

        /// <summary>
        /// Gets if this <see cref="IWebDavStoreItem" /> is a collection.
        /// </summary>
        public bool IsCollection
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the hidden state of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public int Hidden
        {
            get
            {
                return 0;
            }
        }

        #endregion

        /// <summary>
        /// Called before the name of this <see cref="IWebDavStoreItem" /> is changing.
        /// </summary>
        /// <param name="oldName">The old name of this <see cref="IWebDavStoreItem" />.</param>
        /// <param name="newName">The new name of this <see cref="IWebDavStoreItem" />.</param>
        /// <returns>
        /// <c>true</c> if the name change is allowed;
        /// otherwise, 
        /// <c>false</c>.
        /// </returns>
        protected virtual bool OnNameChanging(string oldName, string newName)
        {
            return true;
        }

        /// <summary>
        /// Called after the name of this <see cref="IWebDavStoreItem" /> has changed.
        /// </summary>
        /// <param name="oldName">The old name of this <see cref="IWebDavStoreItem" />.</param>
        /// <param name="newName">The new name of this <see cref="IWebDavStoreItem" />.</param>
        protected virtual void OnNameChanged(string oldName, string newName)
        {
        }
    }
}