
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

public class NetJsonUtil
{
    class JsonPropertyContractResolver : DefaultContractResolver
    {

        protected override IList<JsonProperty> CreateProperties(System.Type type, MemberSerialization memberSerialization)
        {
            return base.CreateProperties(type, memberSerialization).ToList().FindAll(it => it.Writable && !it.Ignored);
        }
    }

    private static readonly JsonSerializerSettings _jsonSerializerWriteableSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ContractResolver = new JsonPropertyContractResolver()
    };


    private static JsonSerializerSettings _jsonSerializerPublicFieldSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ContractResolver = new JsonPublicContractResolver()
    };

    private static readonly JsonSerializer _publicFieldSerializer = JsonSerializer.Create(_jsonSerializerPublicFieldSettings);
    private static readonly JsonSerializer _writeableSerializer = JsonSerializer.Create(_jsonSerializerWriteableSettings);


    class JsonPublicContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, MemberSerialization.OptOut);
            if (member.MemberType != System.Reflection.MemberTypes.Field)
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
    public static string ToJson<T>(T val, bool indented = true, bool onlyPublicField = true)
    {
        return JsonConvert.SerializeObject(val, indented ? Formatting.Indented : Formatting.None, onlyPublicField ? _jsonSerializerPublicFieldSettings : _jsonSerializerWriteableSettings);
    }

    public static T FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void SerializeToBson<T>(T obj, System.IO.Stream stream, bool onlyPublicField = true)
    {
#pragma warning disable CS0618
        using (var writer = new BsonWriter(stream))
        {
            var serializer = onlyPublicField ? _publicFieldSerializer : _writeableSerializer;
            serializer.Serialize(writer, obj);
        }
#pragma warning restore CS0618
    }

    public static byte[] SerializeToBson<T>(T obj, bool onlyPublicField = true)
    {
        using (var stream = new System.IO.MemoryStream())
        {
            SerializeToBson(obj, stream);
            return stream.ToArray();
        }
    }

    public static T DeserializeFromBson<T>(System.IO.Stream stream)
    {
#pragma warning disable CS0618
        using (var reader = new BsonReader(stream))
        {
            return _publicFieldSerializer.Deserialize<T>(reader);
        }
#pragma warning restore CS0618
    }

    public static T DeserializeFromBson<T>(byte[] data)
    {
        using (var stream = new System.IO.MemoryStream(data))
        {
            return DeserializeFromBson<T>(stream);
        }
    }
}
