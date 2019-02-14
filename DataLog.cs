using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BackdoorFinder
{
    class BackdoorData
    {
        public string ID { get; set; }
        public string URL { get; set; }
    }

    class AddonData
    {
        public string ID { get; set; }
    }

    class ConfigData
    {
        public string APIKEY { get; set; }
        public string WEBHOOKURL { get; set; }
    }


    class DataLog
    {
        private static string jsonFile_1 = @"C:/backdoors/log.json";
        private static string jsonFile_2 = @"C:/backdoors/whitelist.json";
        private static string configFile = @"C:/backdoors/config.json";

        public static void addData(BackdoorData data)
        {
            if (!(File.Exists(jsonFile_1)))
            {
                JObject Object = new JObject();

                Object.Add(data.ID, JsonConvert.SerializeObject(data));
                string newJson = JsonConvert.SerializeObject(Object, Formatting.Indented);
                File.Create(jsonFile_1).Dispose();
                File.WriteAllText(jsonFile_1, newJson);
            }else
            {
                string json = File.ReadAllText(jsonFile_1);
                var Data = JObject.Parse(json);
                
                Data.Add(data.ID, JsonConvert.SerializeObject(data));

                string newJsonResult = JsonConvert.SerializeObject(Data, Formatting.Indented);

                File.WriteAllText(jsonFile_1, newJsonResult);
            }
        }

        public static Boolean hasBeenLogged(string ID)
        {
            if (!(File.Exists(jsonFile_1)))
                return false;

            string json = File.ReadAllText(jsonFile_1);
            var Data = JObject.Parse(json);

            return Data.ContainsKey(ID);
        }

        public static Boolean hasBeenWhitelisted(string ID)
        {
            if (!(File.Exists(jsonFile_2)))
                return false;

            string json = File.ReadAllText(jsonFile_2);
            var Data = JObject.Parse(json);

            return Data.ContainsKey(ID);
        }

        public static BackdoorData toData(string AddonID, string URL)
        {
            BackdoorData Data = new BackdoorData();

            Data.ID = AddonID;
            Data.URL = URL;

            return Data;
        }
        
        public static void whitelistAddon(BackdoorData Data)
        {
            if (!(File.Exists(jsonFile_2)))
            {
                JObject Object = new JObject();
                Object.Add(Data.ID, JsonConvert.SerializeObject(Data));
                string JsonDataStr = JsonConvert.SerializeObject(Object, Formatting.Indented);
                File.Create(jsonFile_2).Dispose();
                File.WriteAllText(jsonFile_2, JsonDataStr);
            }else
            {
                string json = File.ReadAllText(jsonFile_2);
                var jsonData = JObject.Parse(json);
                jsonData.Add(Data.ID, JsonConvert.SerializeObject(Data));
                string newJsonData = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                File.WriteAllText(jsonFile_2, newJsonData);
            }
        }
        
        public static void writeConfig(ConfigData confData)
        {
            string jsonStr = JsonConvert.SerializeObject(confData, Formatting.Indented);

            File.Create(configFile).Dispose();
            File.WriteAllText(configFile, jsonStr);
        }

        public static Boolean configExists()
        {
            return File.Exists(configFile);
        }

        public static string getConfigVar(string propertyName)
        {
            string json = File.ReadAllText(configFile);
            ConfigData jsonData = JsonConvert.DeserializeObject<ConfigData>(json);

            if (propertyName == "apikey")
                return jsonData.APIKEY;
            else if (propertyName == "webhookurl")
                return jsonData.WEBHOOKURL;
            else
                throw new Exception("Property not found");
        }

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
