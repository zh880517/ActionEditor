
using Newtonsoft.Json;
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

    private static JsonSerializerSettings _jsonSerializerWriteableSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ContractResolver = new JsonPropertyContractResolver()
    };


    class JsonPublicContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, MemberSerialization.OptOut);
            if (member.MemberType != System.Reflection.MemberTypes.Field)
            {
                property.ShouldSerialize =  _ => false;
            }
            return property;
        }
    }

    private static JsonSerializerSettings _jsonSerializerPublicFieldSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ContractResolver = new JsonPublicContractResolver()
    };

    public static string ToJson<T>(T val, bool indented = true , bool onlyPublicField = true)
    {
        return JsonConvert.SerializeObject(val, indented ? Formatting.Indented : Formatting.None, onlyPublicField ? _jsonSerializerPublicFieldSettings : _jsonSerializerWriteableSettings);
    }

    public static T FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
}
