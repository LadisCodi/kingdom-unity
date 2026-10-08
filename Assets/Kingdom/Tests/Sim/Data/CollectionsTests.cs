using System.IO;
using System.Linq;
using Kingdom.Sim.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Kingdom.Tests.Sim.Data
{
    public class CollectionsTests
    {
        private static readonly string[] COLLECTIONS = typeof(Collections).GetProperties()
            .Select(p => p.GetCustomAttributes(typeof(JsonPropertyAttribute), false).Cast<JsonPropertyAttribute>().First().PropertyName)
            .ToArray();

        // Every field of every game file has a home in the generated classes, and nothing is lost on the way.
        [TestCaseSource(nameof(COLLECTIONS))]
        public void Load_ShouldRoundTripEveryField(string collection)
        {
            var property = typeof(Collections).GetProperties()
                .First(p => p.GetCustomAttributes(typeof(JsonPropertyAttribute), false).Cast<JsonPropertyAttribute>().First().PropertyName == collection);
            var json = JToken.Parse(File.ReadAllText(Path.Combine(TestPaths.Data, "Game", collection + ".json")));

            var loaded = json.ToObject(property.PropertyType);
            var written = JToken.FromObject(loaded, JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include }));

            var difference = Diff(json, written, collection);
            Assert.That(difference, Is.Null, difference);
        }

        private static string Diff(JToken expected, JToken actual, string path)
        {
            if (expected is JObject eo && actual is JObject ao)
            {
                foreach (var p in eo.Properties())
                {
                    if (!ao.TryGetValue(p.Name, out var av)) return $"{path}.{p.Name}: missing from the class";
                    var d = Diff(p.Value, av, path + "." + p.Name);
                    if (d != null) return d;
                }

                foreach (var p in ao.Properties())
                {
                    // An optional field the file leaves out reads as null, as it does in the web prototype.
                    if (!eo.ContainsKey(p.Name) && p.Value.Type != JTokenType.Null) return $"{path}.{p.Name}: absent in the file, written as {p.Value.ToString(Formatting.None)}";
                }

                return null;
            }

            if (expected is JArray ea && actual is JArray aa)
            {
                if (ea.Count != aa.Count) return $"{path}: {ea.Count} entries, {aa.Count} written";
                for (var i = 0; i < ea.Count; i++)
                {
                    var d = Diff(ea[i], aa[i], $"{path}[{i}]");
                    if (d != null) return d;
                }

                return null;
            }

            return JToken.DeepEquals(expected, actual) || (expected.Type is JTokenType.Integer or JTokenType.Float && actual.Type is JTokenType.Integer or JTokenType.Float && expected.Value<double>() == actual.Value<double>())
                ? null
                : $"{path}: {expected.ToString(Formatting.None)} written as {actual.ToString(Formatting.None)}";
        }
    }
}
