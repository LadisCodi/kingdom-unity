using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Saves
{
    // Something by cell, written as a list of { x, y, value }: JSON keys are text, and a cell is not.
    public class CellDictionaryConverter<TValue> : JsonConverter<Dictionary<Vector2Int, TValue>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<Vector2Int, TValue> value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var entry in value)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(entry.Key.X);
                writer.WritePropertyName("y");
                writer.WriteValue(entry.Key.Y);
                writer.WritePropertyName("value");
                serializer.Serialize(writer, entry.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public override Dictionary<Vector2Int, TValue> ReadJson(JsonReader reader, Type objectType,
            Dictionary<Vector2Int, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<Vector2Int, TValue>();
            if (reader.TokenType == JsonToken.Null) return result;

            foreach (var item in JArray.Load(reader))
            {
                var cell = new Vector2Int(item.Value<int>("x"), item.Value<int>("y"));
                result[cell] = item["value"].ToObject<TValue>(serializer);
            }

            return result;
        }
    }
}
