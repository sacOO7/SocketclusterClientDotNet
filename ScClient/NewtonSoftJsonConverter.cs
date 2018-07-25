using Newtonsoft.Json;

namespace ScClient
{
    public class NewtonSoftJsonConverter : IJsonConverter
    {
        public string Serialize(object model)
        {
            return JsonConvert.SerializeObject(model);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
