using System;

namespace GMADFileFormat
{
    public struct GMADAddon
    {
        public _Author Author;
        public String Description;
        public File[] Files;
        public Int16 FormatVersion;
        public String Name;
        public UInt64 Timestamp;
        public Int32 Version;

        #region Structs

        public struct _Author
        {
            public String Name;
            public UInt64 SteamID64;
        }

        public struct File
        {
            public String Path;
            public Byte[] Data;
            public UInt32 CRC; // GMOD doesn't checks this
            public Int64 Size;
        }

        #endregion Structs
    }
}