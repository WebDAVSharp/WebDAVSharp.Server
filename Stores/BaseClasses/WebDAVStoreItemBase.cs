using System;
using System.Security.Principal;
using System.Threading;
using System.Web;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Utilities;
using static System.String;

namespace WebDAVSharp.Server.Stores.BaseClasses
{
    /// <summary>
    ///     This is the base class for <see cref="IWebDavStoreItem" /> implementations.
    /// </summary>
    public class WebDavStoreItemBase : IWebDavStoreItem
    {
        #region Constuctor

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavStoreItemBase" /> class.
        /// </summary>
        /// <param name="parentCollection">
        ///     The parent <see cref="IWebDavStoreCollection" /> that contains this
        ///     <see cref="IWebDavStoreItem" /> implementation.
        /// </param>
        /// <param name="name">The name of this <see cref="IWebDavStoreItem" /></param>
        /// <param name="store"></param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        protected WebDavStoreItemBase(IWebDavStoreCollection parentCollection, string name, IWebDavStore store)
        {
            Store = store;
            if (IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            ParentCollection = parentCollection;
            _name = name;
            UserIdentity = (WindowsIdentity)Thread.GetData(Thread.GetNamedDataSlot(WebDavServer.HttpUser));
        }

        #endregion

        /// <summary>
        /// </summary>
        public Uri Href { get; set; }

        #region Variables

        private string _name;

        /// <summary>
        /// </summary>
        public WebDavStoreItemBase()
        {
            UserIdentity = (WindowsIdentity) Thread.GetData(Thread.GetNamedDataSlot(WebDavServer.HttpUser));
        }

        /// <summary>
        /// </summary>
        public WindowsIdentity UserIdentity { get; set; }


        /// <summary>
        /// </summary>
        public virtual long Size
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// </summary>
        public virtual string MimeType => MimeMapping.GetMimeMapping(Name);


        /// <summary>
        /// </summary>
        public virtual string Etag => Md5Util.Md5HashStringForUtf8String(ItemPath + ModificationDate + GetFileInfo().Hidden + Size);

        /// <summary>
        ///     Gets the The Repl:repl-uid property is a read-only property that contains a string with the document's Repl-uid.
        ///     This property appears within the DAV:prop element collection.
        /// </summary>
        /// <returns></returns>
        public virtual Guid GetRepl_uId()
        {
            return new Guid();
        }

        /// <summary>
        /// </summary>
        public IWebDavStore Store { get; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the parent <see cref="IWebDavStoreCollection" /> that owns this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public IWebDavStoreCollection ParentCollection { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public virtual IWebDavFileInfo GetFileInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets or sets the name of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavForbiddenException"></exception>
        public string Name
        {
            get { return _name; }

            set
            {
                string fixedName = (value ?? Empty).Trim();
                if (fixedName == _name)
                    return;
                if (!OnNameChanging(_name, fixedName))
                    throw new WebDavForbiddenException();
                string oldName = _name;
                _name = fixedName;
                OnNameChanged(oldName, _name);
            }
        }

        /// <summary>
        ///     Gets the creation date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public virtual DateTime CreationDate => DateTime.Now;

        /// <summary>
        ///     Gets the modification date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public virtual DateTime ModificationDate => DateTime.Now;

        /// <summary>
        ///     Gets the path to this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public virtual string ItemPath => Empty;

        /// <summary>
        ///     Gets if this <see cref="IWebDavStoreItem" /> is a collection.
        /// </summary>
        public virtual bool IsCollection => true;

        #endregion

        #region Functions

        /// <summary>
        ///     Called before the name of this <see cref="IWebDavStoreItem" /> is changing.
        /// </summary>
        /// <param name="oldName">The old name of this <see cref="IWebDavStoreItem" />.</param>
        /// <param name="newName">The new name of this <see cref="IWebDavStoreItem" />.</param>
        /// <returns>
        ///     <c>true</c> if the name change is allowed;
        ///     otherwise,
        ///     <c>false</c>.
        /// </returns>
        protected virtual bool OnNameChanging(string oldName, string newName)
        {
            return true;
        }

        /// <summary>
        ///     Called after the name of this <see cref="IWebDavStoreItem" /> has changed.
        /// </summary>
        /// <param name="oldName">The old name of this <see cref="IWebDavStoreItem" />.</param>
        /// <param name="newName">The new name of this <see cref="IWebDavStoreItem" />.</param>
        protected virtual void OnNameChanged(string oldName, string newName)
        {
        }

        #endregion
    }
}