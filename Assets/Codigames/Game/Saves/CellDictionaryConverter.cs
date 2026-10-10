using System;
using System.Collections;
using System.Collections.Generic;
using Codigames.Modules.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codigames.Game.Saves
{
    // Anything by cell, written as a list of { x, y, value }: JSON keys are text, and a cell is not. One converter for
    // every Dictionary<Vector2Int, T>, so a new map by cell saves without anyone remembering to register it.
    public class CellDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                                        && objectType.GetGenericArguments()[0] == typeof(Vector2Int);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (DictionaryEntry entry in (IDictionary)value)
            {
                var cell = (Vector2Int)entry.Key;
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(cell.X);
                writer.WritePropertyName("y");
                writer.WriteValue(cell.Y);
                writer.WritePropertyName("value");
                serializer.Serialize(writer, entry.Value);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = (IDictionary)Activator.CreateInstance(objectType);
            if (reader.TokenType == JsonToken.Null) return result;

            var valueType = objectType.GetGenericArguments()[1];
            // An older build wrote some maps keyed "x,y": read those too.
            if (reader.TokenType == JsonToken.StartObject)
            {
                foreach (var property in JObject.Load(reader).Properties())
                {
                    var parts = property.Name.Split(',');
                    result[new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]))] = property.Value.ToObject(valueType, serializer);
                }

                return result;
            }

            foreach (var item in JArray.Load(reader))
                result[new Vector2Int(item.Value<int>("x"), item.Value<int>("y"))] = item["value"].ToObject(valueType, serializer);
            return result;
        }
    }
}
