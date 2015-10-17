namespace WebDAVSharp.Server.Utilities
{
    /// <summary>
    /// </summary>
    public static class FlagsHelper
    {
        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool IsSet<T>(T flags, T flag) where T : struct
        {
            int flagsValue = (int) (object) flags;
            int flagValue = (int) (object) flag;

            return (flagsValue & flagValue) != 0;
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <param name="flag"></param>
        public static void Set<T>(ref T flags, T flag) where T : struct
        {
            int flagsValue = (int) (object) flags;
            int flagValue = (int) (object) flag;

            flags = (T) (object) (flagsValue | flagValue);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <param name="flag"></param>
        public static void Unset<T>(ref T flags, T flag) where T : struct
        {
            int flagsValue = (int) (object) flags;
            int flagValue = (int) (object) flag;

            flags = (T) (object) (flagsValue & (~flagValue));
        }
    }
}