using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenPrestige
{
    /// <summary>
    /// Minimal, self-contained JSON parser and serializer with zero external dependencies.
    /// Supports objects (Dictionary<string, object>), arrays (List<object>), strings, numbers, bools, and null.
    /// </summary>
    public static class SimpleJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int index = 0;
            return ParseValue(json, ref index);
        }

        public static Dictionary<string, object> DeserializeObject(string json)
        {
            return Deserialize(json) as Dictionary<string, object>;
        }

        public static int? ExtractId(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var dict = DeserializeObject(json);
                if (dict == null) return null;

                if (dict.ContainsKey("id") && dict["id"] != null)
                {
                    int val;
                    if (int.TryParse(dict["id"].ToString(), out val))
                        return val;
                }

                foreach (var kvp in dict)
                {
                    if (kvp.Value is long || kvp.Value is int)
                    {
                        return Convert.ToInt32(kvp.Value);
                    }
                }
            }
            catch { }
            return null;
        }

        public static string Serialize(object obj)
        {
            StringBuilder sb = new StringBuilder();
            SerializeValue(obj, sb);
            return sb.ToString();
        }

        private static void SerializeValue(object obj, StringBuilder sb)
        {
            if (obj == null)
            {
                sb.Append("null");
            }
            else if (obj is string)
            {
                SerializeString((string)obj, sb);
            }
            else if (obj is bool)
            {
                sb.Append((bool)obj ? "true" : "false");
            }
            else if (obj is IDictionary)
            {
                IDictionary dict = (IDictionary)obj;
                sb.Append("{");
                bool first = true;
                foreach (DictionaryEntry de in dict)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    SerializeString(de.Key.ToString(), sb);
                    sb.Append(":");
                    SerializeValue(de.Value, sb);
                }
                sb.Append("}");
            }
            else if (obj is IEnumerable && !(obj is string))
            {
                IEnumerable list = (IEnumerable)obj;
                sb.Append("[");
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    SerializeValue(item, sb);
                }
                sb.Append("]");
            }
            else if (obj is int || obj is long || obj is short || obj is byte)
            {
                sb.Append(Convert.ToInt64(obj).ToString(CultureInfo.InvariantCulture));
            }
            else if (obj is float || obj is double || obj is decimal)
            {
                sb.Append(Convert.ToDouble(obj).ToString("R", CultureInfo.InvariantCulture));
            }
            else
            {
                SerializeString(obj.ToString(), sb);
            }
        }

        private static void SerializeString(string s, StringBuilder sb)
        {
            sb.Append("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append("\"");
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '\"') return ParseString(json, ref index);
            if (c == 't' || c == 'f') return ParseBool(json, ref index);
            if (c == 'n') return ParseNull(json, ref index);
            if (c == '-' || char.IsDigit(c)) return ParseNumber(json, ref index);

            throw new FormatException("Invalid JSON token at index " + index + ": '" + c + "'");
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var dict = new Dictionary<string, object>();
            index++; // skip '{'

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == '}')
                {
                    index++;
                    return dict;
                }

                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);

                if (index >= json.Length || json[index] != ':')
                    throw new FormatException("Expected ':' at index " + index);
                index++; // skip ':'

                object val = ParseValue(json, ref index);
                dict[key] = val;

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                }
                else if (index < json.Length && json[index] == '}')
                {
                    index++;
                    return dict;
                }
            }

            return dict;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var list = new List<object>();
            index++; // skip '['

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == ']')
                {
                    index++;
                    return list;
                }

                object val = ParseValue(json, ref index);
                list.Add(val);

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                }
                else if (index < json.Length && json[index] == ']')
                {
                    index++;
                    return list;
                }
            }

            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            if (json[index] != '\"') throw new FormatException("Expected '\"' at index " + index);
            index++; // skip opening '\"'
            StringBuilder sb = new StringBuilder();

            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '\"') return sb.ToString();
                if (c == '\\')
                {
                    if (index >= json.Length) break;
                    char esc = json[index++];
                    switch (esc)
                    {
                        case '\"': sb.Append('\"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 <= json.Length)
                            {
                                string hex = json.Substring(index, 4);
                                sb.Append((char)Convert.ToInt32(hex, 16));
                                index += 4;
                            }
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            if (json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-'))
            {
                index++;
            }

            string numStr = json.Substring(start, index - start);
            long lVal;
            if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out lVal))
            {
                if (lVal >= int.MinValue && lVal <= int.MaxValue) return (int)lVal;
                return lVal;
            }
            double dVal;
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out dVal))
                return dVal;

            return numStr;
        }

        private static bool ParseBool(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("true"))
            {
                index += 4;
                return true;
            }
            if (json.Substring(index).StartsWith("false"))
            {
                index += 5;
                return false;
            }
            throw new FormatException("Invalid boolean at index " + index);
        }

        private static object ParseNull(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("null"))
            {
                index += 4;
                return null;
            }
            throw new FormatException("Invalid null at index " + index);
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && (json[index] == ' ' || json[index] == '\t' || json[index] == '\r' || json[index] == '\n'))
                index++;
        }
    }
}
