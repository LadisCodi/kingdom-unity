using System;
using System.Collections.Generic;
using System.IO;
using Kingdom.Sim.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kingdom.Tests.Sim.Parity
{
    // Compares what the C# sim builds with a golden the web prototype wrote (Tools/Parity). Every field the
    // golden holds must be there and equal; a null in the golden equals an absent field; numbers compare by
    // value; fields only the C# side has are not checked.
    public static class JsonParity
    {
        public static JToken Golden(string file)
            => JObject.Parse(File.ReadAllText(Path.Combine(TestPaths.Golden, file)))["body"];

        public static JToken ToJson(object value) => value == null ? JValue.CreateNull() : JToken.FromObject(value, SimJson.Serializer);

        // Null when they agree; otherwise the first difference, by path.
        public static string Diff(JToken expected, JToken actual, string path, ISet<string> ignored = null)
        {
            if (expected.Type == JTokenType.Null) return actual == null || actual.Type == JTokenType.Null ? null : $"{path}: null expected, got {Show(actual)}";
            if (actual == null) return $"{path}: missing (expected {Show(expected)})";

            if (expected is JObject eo)
            {
                if (actual is not JObject ao) return $"{path}: object expected, got {Show(actual)}";

                foreach (var p in eo.Properties())
                {
                    if (ignored != null && ignored.Contains(p.Name)) continue;
                    var d = Diff(p.Value, ao[p.Name], path + "." + p.Name, ignored);
                    if (d != null) return d;
                }

                return null;
            }

            if (expected is JArray ea)
            {
                if (actual is not JArray aa) return $"{path}: array expected, got {Show(actual)}";
                if (ea.Count != aa.Count) return $"{path}: {ea.Count} entries expected, got {aa.Count}";

                for (var i = 0; i < ea.Count; i++)
                {
                    var d = Diff(ea[i], aa[i], $"{path}[{i}]", ignored);
                    if (d != null) return d;
                }

                return null;
            }

            if (IsNumber(expected) && IsNumber(actual)) return expected.Value<double>() == actual.Value<double>() ? null : Mismatch(path, expected, actual);
            return JToken.DeepEquals(expected, actual) ? null : Mismatch(path, expected, actual);
        }

        private static bool IsNumber(JToken t) => t.Type is JTokenType.Integer or JTokenType.Float;

        private static string Mismatch(string path, JToken expected, JToken actual) => $"{path}: expected {Show(expected)}, got {Show(actual)}";

        private static string Show(JToken t)
        {
            var s = t.ToString(Formatting.None);
            return s.Length > 160 ? s.Substring(0, 160) + "…" : s;
        }

        // A copy of `source` with `extra` laid over it: the web's `{ ...data, id, … }`.
        public static JObject Merge(object source, object extra)
        {
            var merged = (JObject)ToJson(source);
            foreach (var p in ((JObject)ToJson(extra)).Properties()) merged[p.Name] = p.Value;
            return merged;
        }

        public static JObject Map<T>(IEnumerable<KeyValuePair<string, T>> entries, Func<T, JToken> project)
        {
            var o = new JObject();
            foreach (var e in entries) o[e.Key] = project(e.Value);
            return o;
        }
    }
}
