﻿using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;

namespace Nest
{
	public partial class ElasticClient
	{
		private static Regex StripIndex = new Regex(@"^index\.");

		/// <summary>
		/// Gets the index settings for the default index
		/// </summary>
		public IIndexSettingsResponse GetIndexSettings()
		{
			var index = this._connectionSettings.DefaultIndex;
			return this.GetIndexSettings(index);
		}
		/// <summary>
		/// Gets the index settings for the specified index
		/// </summary>
		public IIndexSettingsResponse GetIndexSettings(string index)
		{
			string path = this.PathResolver.CreateIndexPath(index, "_settings");
			var status = this.Connection.GetSync(path);

			var response = new IndexSettingsResponse();
			response.IsValid = false;
			try
			{
				var o = JObject.Parse(status.Result);
				var settingsObject = o.First.First.First.First;
				var settings = new IndexSettings(); //this.Deserialize<IndexSettings>(settingsObject.ToString());

				foreach (JProperty s in settingsObject.Children<JProperty>())
				{
					settings.Add(StripIndex.Replace(s.Name, ""), s.Value.ToString());
				}


				response.Settings = settings;
				response.IsValid = true;
			}
			catch { }
			response.ConnectionStatus = status;
			return response;
		}
		/// <summary>
		/// Update the index settings for the default index
		/// </summary>
		public ISettingsOperationResponse UpdateSettings(IndexSettings settings)
		{
			var index = this._connectionSettings.DefaultIndex;
			return this.UpdateSettings(index, settings);
		}
		/// <summary>
		/// Update the index settings for the specified index
		/// </summary>
		public ISettingsOperationResponse UpdateSettings(string index, IndexSettings settings)
		{

			string path = this.PathResolver.CreateIndexPath(index, "_settings");
			settings.Settings = settings.Settings
					.Where(kv => IndexSettings.UpdateWhiteList.Any(p =>
					{
						return kv.Key.StartsWith(p);
					}
					)).ToDictionary(kv => kv.Key, kv => kv.Value);

			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			using (JsonWriter jsonWriter = new JsonTextWriter(sw))
			{
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyName("index");
				jsonWriter.WriteStartObject();
				foreach (var kv in settings.Settings)
				{
					jsonWriter.WritePropertyName(kv.Key);
					jsonWriter.WriteValue(kv.Value);
				}

				jsonWriter.WriteEndObject();
			}
			string data = sb.ToString();

			var status = this.Connection.PutSync(path, data);

			var r = new SettingsOperationResponse();
			try
			{
				r = this.Deserialize<SettingsOperationResponse>(status.Result);
			}
			catch
			{

			}
			r.IsValid = status.Success;
			r.ConnectionStatus = status;
			return r;
		}

		/// <summary>
		/// Delete the default index
		/// </summary>
		public IIndicesResponse DeleteIndex<T>() where T : class
		{
			return this.DeleteIndex(this.Infer.IndexName<T>());
		}
		/// <summary>
		/// Delete the specified index
		/// </summary>
		public IIndicesResponse DeleteIndex(string index)
		{
			string path = this.PathResolver.CreateIndexPath(index);

			var status = this.Connection.DeleteSync(path);
			var r = this.Deserialize<IndicesResponse>(status);
			return r;
		}

	}
}