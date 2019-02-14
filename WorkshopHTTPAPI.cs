using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace WorkshopUtils
{
    public enum EPublishedFileInfoMatchingFileType
    {
        FileTypeCommunity,
        FileTypeMicrotransaction,
        FileTypeCollection,
        FileTypeArt,
        FileTypeVideo,
        FileTypeScreenshot,
        FileTypeGame,
        FileTypeSoftware,
        FileTypeConcept,
        FileTypeWebGuide,
        FileTypeIntegratedGuide,
        FileTypeMerch,
        FileTypeControllerBinding,
        FileTypeSteamworksAccessInvite,
        FileTypeSteamVideo,
        FileTypeGameManagedItem
    }

    public enum EPublishedFileQueryType
    {
        RankedByVote,
        RankedByPublicationDate,
        AcceptedForGameRankedByAcceptanceDate,
        RankedByTrend,
        FavoritedByFriendsRankedByPublicationDate,
        CreatedByFriendsRankedByPublicationDate,
        RankedByNumTimesReported,
        CreatedByFollowedUsersRankedByPublicationDate,
        NotYetRated,
        RankedByTotalVotesAsc,
        RankedByVotesUp,
        RankedByTextSearch,
        RankedByTotalUniqueSubscriptions,
        RankedByPlaytimeTrend,
        RankedByTotalPlaytime,
        RankedByAveragePlaytimeTrend,
        RankedByLifetimeAveragePlaytime,
        RankedByPlaytimeSessionsTrend,
        RankedByLifetimePlaytimeSessions
    }

    public static class WorkshopHTTPAPI
    {
        public static Task<Byte[]> DownloadAddonDataAsync ( WorkshopAddon Addon )
        {
            using (var wc = new WebClient())
                return wc.DownloadDataTaskAsync(Addon.URL);
        }

        public static Task DownloadAddonFileAsync ( WorkshopAddon Addon, String path )
        {
            using ( var wc = new WebClient ( ) )
                return wc.DownloadFileTaskAsync ( Addon.URL, path );
        }

        public async static Task<WorkshopAddon> GetAddonByIDAsync ( Int32 ID, String APIKey )
        {
            var resp = await WebClientGETAsync (
                "https://api.steampowered.com/IPublishedFileService/GetDetails/v1/",
                new NameValueCollection
                {
                    { "key", APIKey },
                    { "publishedfileids[0]", ID.ToString ( ) },
                    { "includetags", "0" },
                    { "includeadditionalpreviews", "0" },
                    { "includechildren", "0" },
                    { "includekvtags", "0" },
                    { "includevotes", "0" },
                    { "short_description", "0" },
                    { "includeforsaledata", "0" },
                    { "includemetadata", "0" },
                    { "return_playtime_stats", "30" },
                    { "appid", "4000" },
                    { "strip_description_bbcode", "1" }
                }
            ).ConfigureAwait(false);

            WorkshopAddon addon = JToken.Parse ( resp )
                ["response"]
                ["publishedfiledetails"]
                [0]
                .ToObject<WorkshopAddon> ( );
            addon.ID = ID;
            return addon;
        }

        public static async Task<WorkshopAddon[]> GetWorkshopAddonsAsync ( String APIKey, EPublishedFileQueryType query_type, UInt32 page, Int32 numperpage = 50 )
        {
            using ( var wc = new WebClient
            {
                QueryString = new NameValueCollection
                {
                    { "key", APIKey },
                    { "creator_appid", "4000" },
                    { "page", page.ToString ( ) },
                    { "numperpage", numperpage.ToString ( ) },
                    { "appid", "4000" },
                    { "return_tags", "1" },
                    { "return_kv_tags", "0" },
                    { "return_previews", "1" },
                    { "return_children", "0" },
                    { "return_metadata", "1" },
                    { "return_vote_data", "0" },
                    { "query_type", ( ( Int32 ) query_type ).ToString ( ) },
                    { "excludedtags[0]", "Dupe" },
                    { "excludedtags[1]", "Save" },
                    { "excludedtags[2]", "Demo" }
                }
            } )
            {
                var data = JObject.Parse ( await wc.DownloadStringTaskAsync (
                        "http://api.steampowered.com/IPublishedFileService/QueryFiles/v1/" ).ConfigureAwait(false) );
                return data["response"]["publishedfiledetails"]
                    .Select ( tok => tok.ToObject<WorkshopAddon> ( ) )
                    .ToArray ( );
            }
        }

        public static async Task<String> WebClientPOSTAsync ( String URL, NameValueCollection Data )
        {
            using ( var wc = new WebClient ( ) )
                return Encoding.UTF8.GetString ( await wc.UploadValuesTaskAsync ( URL, Data ).ConfigureAwait ( false ) );
        }

        public static async Task<String> WebClientGETAsync ( String URL, NameValueCollection Data )
        {
            NameValueCollection qstr = HttpUtility.ParseQueryString("");
            foreach ( String k in Data.AllKeys )
                qstr.Add ( k, Data[k] );

            using ( var wc = new WebClient ( ) )
                return await wc.DownloadStringTaskAsync ( $"{URL}?{qstr}" );
        }
    }
}
