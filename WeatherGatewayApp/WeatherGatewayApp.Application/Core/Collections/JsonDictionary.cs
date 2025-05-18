using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeatherGatewayApp.Application.Core.Collections
{
    public class JsonDictionary : Dictionary<string, object>
    {
        public string JsonText { get; }

        public JsonDictionary(string json) : base(JsonToDictionary(json))
        {
            JsonText = json;
        }

        public JsonDictionary()
        { }

        private static Dictionary<string, object> JsonToDictionary(string data)
        {
            if (data.Trim().StartsWith("["))
            {
                data = $"{{\"root\":{data}}}";
            }
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            return DeserializeData(values);
        }

        private static Dictionary<string, object> DeserializeData(JObject data)
        {
            var dict = data.ToObject<Dictionary<string, object>>();
            return DeserializeData(dict);
        }

        private static Dictionary<string, object> DeserializeData(Dictionary<string, object> data)
        {
            foreach (var key in data.Keys.ToArray())
            {
                var value = data[key];

                if (value is JObject)
                    data[key] = DeserializeData(value as JObject);

                if (value is JArray)
                    data[key] = DeserializeData(value as JArray);
            }

            return data;
        }

        private static IList<object> DeserializeData(JArray data)
        {
            var list = data.ToObject<List<object>>();

            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];

                if (value is JObject)
                {
                    list[i] = DeserializeData(value as JObject);
                }
                else if (value is JArray)
                {
                    list[i] = DeserializeData(value as JArray);
                }
            }

            return list;
        }
    }
}
