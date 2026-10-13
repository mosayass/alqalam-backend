using System.Text.Json.Serialization;

namespace AlQalam.Users.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MemorizationLevel
{
    Beginner,
    Intermediate,
    Advanced
}
