using GMADFileFormat;
using Newtonsoft.Json;
using System;

namespace WorkshopUtils
{
	public class WorkshopAddon
	{
		[JsonProperty ( PropertyName = "publishedfileid" )]
		public Int64 ID;

		[JsonProperty ( PropertyName = "creator" )]
		public Int64 SteamID64;

		[JsonProperty ( PropertyName = "file_size" )]
		public Int32 Size;

		[JsonProperty ( PropertyName = "file_url" )]
		public String URL;

		[JsonProperty ( PropertyName = "preview_url" )]
		public String Thumbnail;

		[JsonProperty ( PropertyName = "title" )]
		public String Title;

		[JsonProperty ( PropertyName = "description" )]
		public String Description;

		[JsonProperty ( PropertyName = "time_created" )]
		[JsonConverter ( typeof ( WorkshopTimeConverter ) )]
		public DateTime Created;

		[JsonProperty ( PropertyName = "time_updated" )]
		[JsonConverter ( typeof ( WorkshopTimeConverter ) )]
		public DateTime Updated;

		[JsonProperty ( PropertyName = "subscriptions" )]
		public UInt32 Subscriptions;

		[JsonProperty ( PropertyName = "lifetime_subscriptions" )]
		public UInt32 LifetimeSubs;

		[JsonIgnore]
		public GMADAddon GMAD;
	}

	internal class WorkshopTimeConverter : Newtonsoft.Json.Converters.DateTimeConverterBase
	{
		private static readonly DateTime epoch = new DateTime ( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );

		public override Boolean CanConvert ( Type objectType )
		{
			return objectType == typeof ( DateTime );
		}

		public override Object ReadJson ( JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer )
		{
			if ( reader.Value == null )
				return null;
			return epoch.AddMilliseconds ( ( Int64 ) reader.Value );
		}

		public override void WriteJson ( JsonWriter writer, Object value, JsonSerializer serializer )
		{
			throw new NotImplementedException ( );
		}
	}
}
