using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkshopUtils;
using GMADFileFormat;
using System.Text.RegularExpressions;
using System.IO;
using DiscordWebhook;

namespace BackdoorFinder
{
    class Backdoor
    {
        public struct FlagStruct
        {
            public int lineNumber;
            public String FlagStr;
            public GMADAddon Addon;
            public GMADAddon.File AddonFile;
            public String FlagDescription;
            public Regex CheckRegex;
            public int CheckType;
            public int Priority;

            public FlagStruct(int lineArg, String FlagStrArg, GMADAddon AddongArg, String Description, Regex CheckRegexArg, int CheckTypeArg, int PriorityArg, GMADAddon.File currentFile)
            {
                lineNumber = lineArg;
                FlagStr = FlagStrArg;
                Addon = AddongArg;
                FlagDescription = Description;
                CheckRegex = CheckRegexArg;
                CheckType = CheckTypeArg;
                Priority = PriorityArg;
                AddonFile = currentFile;
            }
        }

        public enum CheckTypes
        {
            NETWORK,
            DYNCODE,
            AUTHENT,
            BANMGMT,
            FILESYS,
            OBFUSC,
            MISC
        }

        public static Dictionary<Regex, ValueTuple<string, int, int>> backdoorPatterns = new Dictionary<Regex, ValueTuple<string, int, int>>()
        {
            {new Regex(@":SetUserGroup(.superadmin.)"), ("Setting User Group", 1, (int)CheckTypes.MISC) },
            {new Regex(@"STEAM_[0-9]+:[0-9]+:[0-9]+"), ("Presence of Steam ID", 2, (int)CheckTypes.AUTHENT) },
            {new Regex(@"http.Post"), ("HTTP server call (post)", 4, (int)CheckTypes.NETWORK) },
            {new Regex(@"http.Get"), ("HTTP server call (get)", 4, (int)CheckTypes.NETWORK) },
            {new Regex(@"_G[.](.)"), ("References global table", 1, (int)CheckTypes.MISC) },
            {new Regex(@":SteamID() {0,5}== {0,5}\WSTEAM_0:\d:\d{1,9}\W"), ("SteamID Check", 4, (int)CheckTypes.AUTHENT )},
            {new Regex(@"CompileString"), ("Dynamic code execution (CompileString)", 2, (int)CheckTypes.DYNCODE) },
            {new Regex(@"RunString"), ("Dynamic code execution (RunString)", 2, (int)CheckTypes.DYNCODE) },
            {new Regex(@"removeip"), ("Unban by IP address", 2, (int)CheckTypes.BANMGMT) },
            {new Regex(@"removeid"), ("Unabn by Steam ID", 2, (int)CheckTypes.BANMGMT) },
            {new Regex(@"banip"), ("Ban by IP address", 2, (int)CheckTypes.BANMGMT) },
            {new Regex(@"writeid"), ("Writing bans to disk", 1, (int)CheckTypes.BANMGMT) },
            {new Regex(@"file.Read"), ("Reading file contents", 1, (int)CheckTypes.FILESYS) },
            {new Regex(@"file.Delete"), ("File deletion", 1, (int)CheckTypes.FILESYS) },
            {new Regex(@"0[xX][0-9a-fA-F]+"), ("Obfuscated / encrypted code 1", 3, (int)CheckTypes.OBFUSC) },
            {new Regex(@"\\[0-9]+\\[0-9]+"), ("Obfuscated / encrypted code 2", 3, (int)CheckTypes.OBFUSC) },
            {new Regex(@"\\[xX][0-9a-fA-F][0-9a-fA-F]"), ("Obfuscated / encrypted code 3", 3, (int)CheckTypes.OBFUSC) },
            {new Regex(@"getfenv"), ("Call to getfenv()", 1, (int)CheckTypes.MISC) },
            {new Regex(@"backdoor"), ("Line containts 'backdoor'", 1, (int)CheckTypes.MISC) },
            {new Regex(@"superadmin"), ("Line containts 'superadmin'", 1, (int)CheckTypes.MISC) },
            {new Regex(@"game.ConsoleCommand"), ("Console Command", 2, (int)CheckTypes.MISC) }
        };

        private GMADAddon ADDON;

        public Backdoor(GMADAddon addon)
        {
            ADDON = addon;
        }

        public static Boolean includesFlag(string fileLine)
        {
            foreach(var Index in backdoorPatterns)
            {
                Regex pattern = Index.Key;

                if (pattern.IsMatch(fileLine))
                    return true;
            }
            return false;
        }


        public List<FlagStruct> getFlags(string fileLineStr, int fileLineInt, GMADAddon.File addonFile)
        {
            List<FlagStruct> checkArray = new List<FlagStruct>();
            int flagCount = 0;

            foreach (var Index in backdoorPatterns)
            {
                flagCount++;
                Regex Pattern = Index.Key;
                int CheckType = Index.Value.Item3;
                int Priority = Index.Value.Item2;

                if (Pattern.IsMatch(fileLineStr))
                {
                    String Description = Index.Value.Item1;
                    FlagStruct newFlag = new FlagStruct(
                        fileLineInt,
                        fileLineStr,
                        ADDON,
                        Description,
                        Pattern,
                        CheckType,
                        Priority,
                        addonFile
                    );
                    checkArray.Add(newFlag);
                }
            }
            return checkArray;
        }

        public static List<EmbedField> makeEmbedList(Dictionary<string, string> fieldsDict)
        {
            List<EmbedField> fieldsList = new List<EmbedField>();

            foreach(var field in fieldsDict)
            {
                string fieldName = field.Key;
                string fieldValue = field.Value;

                EmbedField newFieldObj = new EmbedField();
                newFieldObj.Name = fieldName;
                newFieldObj.Value = fieldValue;

                fieldsList.Add(newFieldObj);
            }
            return fieldsList;
        }

        public List<List<FlagStruct>> scanFile()
        {
            List<List<FlagStruct>> filesFlags = new List<List<FlagStruct>>();

            foreach(GMADAddon.File addonFile in ADDON.Files)
            {
                if (Path.GetExtension(addonFile.Path) != ".lua")
                    continue;

                byte[] fileData = addonFile.Data;
                String fileStrData = WorkshopDownload.GetString(fileData);

                string[] lines = fileStrData.Split('\n');
                
                for(int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    string lineStr = lines[lineNumber];

                    if (includesFlag(lineStr))
                    {
                        List<FlagStruct> flags = getFlags(lineStr, (lineNumber + 1), addonFile);
                        filesFlags.Add(flags);
                    }
                }
                
            }
            return filesFlags;
        }
    }
}
