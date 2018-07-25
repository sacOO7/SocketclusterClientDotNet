using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ScClient
{
    public class NewtonSoftJsonConverter : IJsonConverter
    {
        private readonly JsonSerializerSettings _jsonSerializationSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public string Serialize(object model)
        {
            return JsonConvert.SerializeObject(model, _jsonSerializationSettings);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _jsonSerializationSettings);
        }
    }
}