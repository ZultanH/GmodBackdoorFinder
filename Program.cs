using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorkshopUtils;
using GMADFileFormat;
using System.Net;
using Nito.AsyncEx;
using DiscordWebhook;

namespace BackdoorFinder
{
    class Program
    {
        private static string steamApiKey = "";
        private static string webhookURL = "";

        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync());
        }

        static async Task MainAsync()
        {
            if (!DataLog.configExists())
            {
                Init();
                steamApiKey = DataLog.getConfigVar("apikey");
                webhookURL  = DataLog.getConfigVar("webhookurl");
            }else
            {
                steamApiKey = DataLog.getConfigVar("apikey");
                webhookURL  = DataLog.getConfigVar("webhookurl");
            }

            Console.Write("What do you want to do? ");
            String Action = Console.ReadLine();

            if (Action == "help")
            {
                Console.Write("Commands: \nhelp - print help text\nscanlink [workshopLink] - scans the individual file\nwhitelistaddon [workshopURL]\nscanworkshop - scans the entire workshop\nextractaddon [workshopLink] - extracts an addon\n");
                await MainAsync();

            }
            else if (Action.StartsWith("scanlink"))
            {
                string workshopURL = Action.Split(' ')[1];
                Int32 workshopID = WorkshopDownload.ParseID(workshopURL);

                await ScanLink(workshopID);
                await MainAsync();

            }
            else if (Action.StartsWith("whitelistaddon"))
            {
                string workshopURL = Action.Split(' ')[1];
                Int32 workshopID = WorkshopDownload.ParseID(workshopURL);
                WorkshopAddon Addon = await WorkshopHTTPAPI.GetAddonByIDAsync(workshopID, steamApiKey);
                whiteListAddon(Addon);
                await MainAsync();

            }
            else if (Action == "scanworkshop")
            {
                await ScanWorkshop();
            }
            else if (Action.StartsWith("extractaddon"))
            {
                string workshopURL = Action.Split(' ')[1];
                Int32 workshopID = WorkshopDownload.ParseID(workshopURL);
                WorkshopAddon workshopAddon = await WorkshopHTTPAPI.GetAddonByIDAsync(workshopID, steamApiKey);

                Console.Write("Downloading Addon: {0}\n", workshopAddon.URL);
                GMADAddon parsedAddon;

                using (var wc = new WebClient())
                {
                    Byte[] data = await wc.DownloadDataTaskAsync(workshopAddon.URL);
                    parsedAddon = GMADParser.Parse(data);
                    data = new Byte[0];
                }

                WorkshopDownload.Extract(parsedAddon);
                await MainAsync();
            }
        }

        static async Task ScanLink(Int32 workshopID)
        {
            WorkshopAddon Addon = await WorkshopHTTPAPI.GetAddonByIDAsync(workshopID, steamApiKey);
            Console.Write("Downloading Addon: {0}\n", Addon.URL);
            GMADAddon parsedAddon;

            using (var wc = new WebClient())
            {
                Byte[] data = await wc.DownloadDataTaskAsync(Addon.URL);
                parsedAddon = GMADParser.Parse(data);
                data = new Byte[0];
            }

            Backdoor backdoorFinder = new Backdoor(parsedAddon);

            try
            {
                List<List<Backdoor.FlagStruct>> flagList = backdoorFinder.scanFile();

                foreach (var flagFile in flagList)
                {
                    foreach (Backdoor.FlagStruct fileStruct in flagFile)
                    {
                        int lineNumber = fileStruct.lineNumber;
                        String FlagStr = fileStruct.FlagStr;
                        GMADAddon.File AddonFile = fileStruct.AddonFile;
                        GMADAddon._Author Author = parsedAddon.Author;
                        String FlagDescription = fileStruct.FlagDescription;
                        Regex CheckRegex = fileStruct.CheckRegex;
                        int CheckType = fileStruct.CheckType;
                        int Priority = fileStruct.Priority;
                        string AddonUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + Addon.ID.ToString();

                        Console.Write("Potential Backdoor found in Addon:\nline number: {0}\nAddon URL: {1}\nAuthor SteamID: {2}\nFlag: {3}\nCode: {4}\n",
                            lineNumber.ToString(),
                            AddonUrl,
                            Author.SteamID64,
                            FlagDescription,
                            FlagStr
                        );
                    }

                }
            }
            catch (Exception ex)
            {
                Console.Write("Something went wrong...");
                Console.Write(ex.Message + "\n");
                Console.ReadLine();
            }
        }

        static void whiteListAddon(WorkshopAddon Addon)
        {
            BackdoorData Data = DataLog.toData(Addon.ID.ToString(), Addon.URL);

            DataLog.whitelistAddon(Data);
        }

        static void Init()
        {
            if (!DataLog.configExists())
            {
                Console.Write("Config file does not exist. Creating new one...\n");

                Console.Write("Please enter a valid discord webhook URL ");
                string webhookURL = Console.ReadLine();
                Console.Write("Please enter a valid steam API key ");
                string apiKey = Console.ReadLine();
                

                ConfigData confData = new ConfigData();
                confData.APIKEY     = apiKey;
                confData.WEBHOOKURL = webhookURL;

                DataLog.writeConfig(confData);
            }
        }

        static async Task ScanWorkshop()
        {
            uint pageNumber = 0;

            while (true)
            {
                pageNumber++;
                Console.Write("Page Number = " + pageNumber.ToString() + "\n");

                WorkshopAddon[] Addons = await WorkshopHTTPAPI.GetWorkshopAddonsAsync(steamApiKey, EPublishedFileQueryType.RankedByPublicationDate, pageNumber, 100);

                Console.Write("# Addons: {0}\n", Addons.Length.ToString());

                if (Addons.Length < 1)
                {
                    Console.Write("Reached end of workshop.");
                    if (Console.ReadLine() == "ok")
                        Environment.Exit(1);
                }

                foreach (WorkshopAddon Addon in Addons)
                {
                    if (DataLog.hasBeenLogged(Addon.ID.ToString()) || DataLog.hasBeenWhitelisted(Addon.ID.ToString()))
                        continue;

                    Console.Write("Downloading Addon: {0}\n", Addon.URL);
                    GMADAddon parsedAddon;

                    try
                    {
                        using (var wc = new WebClient())
                        {
                            Byte[] data = await wc.DownloadDataTaskAsync(Addon.URL);
                            parsedAddon = GMADParser.Parse(data);
                            data = new Byte[0];
                        }

                        Backdoor backdoorFinder = new Backdoor(parsedAddon);

                        List<List<Backdoor.FlagStruct>> flagList = backdoorFinder.scanFile();

                        foreach (var flagFile in flagList)
                        {
                            foreach (Backdoor.FlagStruct fileStruct in flagFile)
                            {
                                int lineNumber = fileStruct.lineNumber;
                                String FlagStr = fileStruct.FlagStr;
                                GMADAddon.File AddonFile = fileStruct.AddonFile;
                                GMADAddon._Author Author = parsedAddon.Author;
                                String FlagDescription = fileStruct.FlagDescription;
                                Regex CheckRegex = fileStruct.CheckRegex;
                                int CheckType = fileStruct.CheckType;
                                int Priority = fileStruct.Priority;
                                string AddonUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + Addon.ID.ToString();

                                Webhook discordWebhook = new Webhook(webhookURL);

                                Embed discordEmbed = new Embed();
                                List<Embed> embedList = new List<Embed>();

                                Dictionary<string, string> fieldDict = new Dictionary<string, string>()
                                {
                                    {"line number", lineNumber.ToString() },
                                    {"Addon name", parsedAddon.Name },
                                    {"Current File", AddonFile.Path },
                                    {"Priority", Priority.ToString() },
                                    {"Flag", FlagDescription },
                                    {"Code", FlagStr.Length < 1024 ? FlagStr : "Code too long" },
                                    {"Author Name",  Author.Name},
                                    {"Author SteamID", Author.SteamID64.ToString() },
                                    {"Addon URL",  AddonUrl}
                                };


                                discordEmbed.Title = "FLAG FOUND";
                                discordEmbed.Fields = Backdoor.makeEmbedList(fieldDict);
                                discordEmbed.Color = Extensions.ToRgb(Color.FromName("purple"));

                                embedList.Add(discordEmbed);

                                await discordWebhook.Send(null, null, null, false, embedList);

                            }
                        }

                        if (flagList.Count < 1)
                            Console.Write("No flags found.\n");

                        BackdoorData toLog = DataLog.toData(Addon.ID.ToString(), Addon.URL);

                        DataLog.addData(toLog);

                        flagList = null;
                        backdoorFinder = null;
                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        Console.Write("Something went wrong...\n");
                        Console.Write(ex.Message + "\n");
                    }
                }
            }
        }
    }
}
