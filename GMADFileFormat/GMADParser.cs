using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GMADFileFormat
{
    public class GMADParser
    {
        /// <summary>
        /// Parses a GMAD file taking into account LZMA
        /// compression and possible JSON encoding
        /// </summary>
        /// <param name="Data">the file contents as a byte array</param>
        /// <returns></returns>
        public static GMADAddon Parse ( Byte[] Data )
        {
            if ( !HasValidHeader ( ref Data ) )
                Data = UnLZMA ( ref Data );

            var addon = new GMADAddon { Author = new GMADAddon._Author ( ) };
            using ( var mem = new MemoryStream ( Data ) )
            using ( var reader = new BinaryReader ( mem, Encoding.UTF8, true ) )
            {
                // TODO: test if will work w/o this
                mem.Seek ( 0, SeekOrigin.Begin );

                // Check if LZMA decompressed file is valid
                if ( reader.ReadChar ( ) != 'G' || reader.ReadChar ( ) != 'M' ||
                     reader.ReadChar ( ) != 'A' || reader.ReadChar ( ) != 'D' )
                    throw new Exception ( "Invalid GMAD file." );

                // We only support up to v3
                addon.FormatVersion = ( Int16 ) reader.ReadChar ( );
                if ( addon.FormatVersion > 3 )
                    throw new Exception ( "Unsupported GMAD file version." );

                // These stuff is almost always wrong (aka SID64 = 0)
                addon.Author.SteamID64 = reader.ReadUInt64 ( );
                addon.Timestamp = reader.ReadUInt64 ( );

                // required content ( not used )
                if ( addon.FormatVersion > 1 )
                {
                    var content = ReadNullTerminatedString ( reader );
                    while ( content != "" )
                    {
                        content = ReadNullTerminatedString ( reader );
                    }
                }

                addon.Name = ReadNullTerminatedString ( reader );
                addon.Description = ReadNullTerminatedString ( reader );
                addon.Author.Name = ReadNullTerminatedString ( reader );
                addon.Version = reader.ReadInt32 ( );

                var files = new List<GMADAddon.File> ( );

                // Retrieve file metadata
                while ( reader.ReadUInt32 ( ) != 0 )
                {
                    var file = new GMADAddon.File
                    {
                        Path = ReadNullTerminatedString ( reader ),
                        Size = reader.ReadInt64 ( ),
                        CRC  = reader.ReadUInt32 ( )
                    };
                    files.Add ( file );
                }

                addon.Files = files.ToArray ( );
                // Addons data is stored after the metadata
                for ( var i = 0 ; i < addon.Files.Length ; i++ )
                    addon.Files[i].Data = reader.ReadBytes ( ( Int32 ) addon.Files[i].Size );

                var desc = addon.Description;
                // Description *might* be in JSON, because you
                // know..... "consistency"
                try
                {
                    addon.Description = JObject
                        .Parse ( addon.Description )
                        .Value<String> ( "description" );
                }
                catch ( Exception )
                {
                    addon.Description = desc;
                }

                return addon;
            }
        }

        /// <summary>
        /// Returns wether a byte array has a valid GMAD header
        /// </summary>
        /// <param name="Data">
        /// the file content as an array of bytes
        /// </param>
        /// <returns></returns>
        public static Boolean HasValidHeader ( ref Byte[] Data )
        {
            return
                Data[0] == 0x47 && // G
                Data[1] == 0x4D && // M
                Data[2] == 0x41 && // A
                Data[3] == 0x44;   // D
        }

        /// <summary>
        /// Decompresses a LZMA compressed file
        /// </summary>
        /// <param name="Data">File contents as bytes</param>
        /// <returns></returns>
        private static Byte[] UnLZMA ( ref Byte[] Data )
        {
            var dec = new SevenZip.Compression.LZMA.Decoder ( );
            using ( var @in = new MemoryStream ( Data ) )
            using ( var @out = new MemoryStream ( ) )
            {
                // Read the decoder properties
                var props = new byte[5];
                @in.Read ( props, 0, 5 );

                // Read in the decompress file size.
                var fsb = new byte[8];
                @in.Read ( fsb, 0, 8 );
                var fs = BitConverter.ToInt64 ( fsb, 0 );

                dec.SetDecoderProperties ( props );
                dec.Code ( @in, @out, @in.Length, fs, null );
                return @out.ToArray ( );
            }
        }

        /// <summary>
        /// Reads a null-terminated string
        /// </summary>
        /// <param name="red"></param>
        /// <returns></returns>
        private static String ReadNullTerminatedString ( BinaryReader red )
        {
            var build = new StringBuilder ( );
            var ch = red.ReadChar ( );
            while ( ch != 0x00 )
            {
                build.Append ( ch );
                ch = red.ReadChar ( );
            }
            return build.ToString ( );
        }
    }
}
