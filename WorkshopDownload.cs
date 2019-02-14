using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GMADFileFormat;
using System.IO;

namespace BackdoorFinder
{
    class WorkshopDownload
    {
        private static readonly Regex wsurl = new Regex(@"https?:\/\/steamcommunity\.com\/sharedfiles\/filedetails\/\?id=(\d+)");
        
        public static Int32 ParseID(string workshopURL)
        {
            Int32 id;
            if (wsurl.IsMatch(workshopURL))
            {
                id = Int32.Parse(wsurl.Match(workshopURL).Groups[1].ToString());
            }
            else if (!Int32.TryParse(workshopURL, out id))
            {
                throw new Exception("Invalid ID/URL.");
            }
            return id;
        }

        public static void Extract(GMADAddon addon)
        {
            string randomStr = DataLog.RandomString(5);
            foreach (GMADAddon.File file in addon.Files)
            {
                var path = file.Path;
                string dirName = Path.GetDirectoryName(path);

                var di = new DirectoryInfo($"C:/backdoors/addon_{randomStr}");
                var fi = new FileInfo($"C:/backdoors/addon_{randomStr}/{path}");

                di.Create();
                di.CreateSubdirectory(dirName);

                File.WriteAllText($"C:/backdoors/addon_{randomStr}/{path}", GetString(file.Data));
            }

        }

        public static String GetString(Byte[] data)
        {
            return String.Join("", data.Select(d => (Char)d).ToArray());
        }
    }
}
