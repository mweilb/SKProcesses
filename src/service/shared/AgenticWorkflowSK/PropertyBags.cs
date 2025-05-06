using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgenticWorkflowSK
{
    /// <summary>
    /// Strongly-typed property bags for storing arbitrary key-value pairs.
    /// </summary>
    public class PropertyBags : IDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _dict;

        /// <summary>
        /// Initializes an empty PropertyBags.
        /// </summary>
        public PropertyBags() => _dict = new();

        /// <summary>
        /// Initializes a PropertyBags with an existing dictionary.
        /// </summary>
        [JsonConstructor]
        public PropertyBags(Dictionary<string, object?> dict) => _dict = dict ?? new();

        /// <summary>
        /// Tries to get a value of type T by key.
        /// </summary>
        public bool TryGetValue<T>(string key, out T? value)
        {
            if (_dict.TryGetValue(key, out var obj))
            {
                if (obj is T t)
                {
                    value = t;
                    return true;
                }
                if (obj is System.Text.Json.JsonElement elem)
                {
                    value = System.Text.Json.JsonSerializer.Deserialize<T>(elem);
                    return value != null;
                }
                if (obj is string json)
                {
                    value = System.Text.Json.JsonSerializer.Deserialize<T>(json);
                    return value != null;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Sets a value of type T by key.
        /// </summary>
        public void Set<T>(string key, T value) => _dict[key] = value;

        /// <summary>
        /// Updates or adds a value of type T by key.
        /// </summary>
        public void Update<T>(string key, T value) => _dict[key] = value;

        /// <summary>
        /// Adds a value of type T by key.
        /// </summary>
        public void Add<T>(string key, T value) => _dict.Add(key, value);

        /// <summary>
        /// Removes code block markers (```json, ```yaml, ```, etc.) from a string.
        /// </summary>
        public static string CleanCodeBlock(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var cleaned = input.Trim();
            if (cleaned.StartsWith("```json")) cleaned = cleaned[7..].TrimStart();
            else if (cleaned.StartsWith("```yaml")) cleaned = cleaned[7..].TrimStart();
            else if (cleaned.StartsWith("```")) cleaned = cleaned[3..].TrimStart();
            if (cleaned.EndsWith("```")) cleaned = cleaned[..^3].TrimEnd();
            return cleaned;
        }

        /// <summary>
        /// Replaces '{{' and '}}' with safe tokens for Handlebars.
        /// </summary>
        public static string ProtectHandlebarsBraces(string input) =>
            string.IsNullOrEmpty(input) ? input : input.Replace("{{", "__LBRACE__").Replace("}}", "__RBRACE__");

        /// <summary>
        /// Restores Handlebars tokens to '{{' and '}}'.
        /// </summary>
        public static string RestoreHandlebarsBraces(string input) =>
            string.IsNullOrEmpty(input) ? input : input.Replace("__LBRACE__", "{{").Replace("__RBRACE__", "}}");

        /// <summary>
        /// Exposes the internal dictionary for serialization.
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, object?> Properties => _dict;

        // IDictionary<string, object?> implementation
        public object? this[string key] { get => _dict[key]; set => _dict[key] = value; }
        public ICollection<string> Keys => _dict.Keys;
        public ICollection<object?> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool IsReadOnly => false;
        public void Add(string key, object? value) => _dict.Add(key, value);
        public bool ContainsKey(string key) => _dict.ContainsKey(key);
        public bool Remove(string key) => _dict.Remove(key);
        public bool TryGetValue(string key, out object? value) => _dict.TryGetValue(key, out value);
        public void Add(KeyValuePair<string, object?> item) => _dict.Add(item.Key, item.Value);
        public void Clear() => _dict.Clear();
        public bool Contains(KeyValuePair<string, object?> item) => _dict.ContainsKey(item.Key) && Equals(_dict[item.Key], item.Value);
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((IDictionary<string, object?>)_dict).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<string, object?> item) => _dict.Remove(item.Key);
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}
