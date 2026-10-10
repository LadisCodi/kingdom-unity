using Codigames.Kingdom;
using Codigames.Kingdom.Harvest.State;
using Codigames.Modules.Saves;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codigames.Game.Saves
{
    // The kingdom as JSON: { "version": n, "kingdom": { … } }. Migrations reshape the tree before it is read.
    public class KingdomSaveCodec : ISaveCodec<KingdomState, JObject>
    {
        private const string VERSION = "version";
        private const string KINGDOM = "kingdom";

        private readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Converters = { new CellDictionaryConverter() },
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        });

        public string Encode(KingdomState state, int version)
        {
            var document = new JObject
            {
                [VERSION] = version,
                [KINGDOM] = JObject.FromObject(state, _serializer),
            };
            return document.ToString(Formatting.None);
        }

        public JObject Parse(string text) => JObject.Parse(text);

        public int VersionOf(JObject raw) => raw.Value<int>(VERSION);

        public KingdomState Decode(JObject raw) => raw[KINGDOM].ToObject<KingdomState>(_serializer);
    }
}
