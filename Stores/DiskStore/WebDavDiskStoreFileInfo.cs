using System.IO;
using WebDAVSharp.Server.Stores.BaseClasses;

namespace WebDAVSharp.Server.Stores.DiskStore
{
    internal class WebDavDiskStoreFileInfo : WebDavFileInfoBase
    {
        public override void Apply()
        {
            FileInfo fi = new FileInfo(Path);
            if (fi.Exists)
            {
                fi.Attributes = GetAttributes();
            }
        }
    }
}