using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kingdom.Sim.Core
{
    // How the sim's state is written and read: camelCase fields (the web prototype's shape), dictionary keys
    // untouched, absent for null.
    public static class SimJson
    {
        public static JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Double,
        };

        public static JsonSerializer Serializer { get; } = JsonSerializer.Create(Settings);

        public static string Write(object value) => JsonConvert.SerializeObject(value, Settings);

        public static T Read<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
    }
}
