using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
/// Serializes editor document history without Unity scene references or derived occupancy.
/// </summary>
public sealed class MapCreatorSnapshotResolver : DefaultContractResolver
{
    /// <summary>
    /// Excludes transient cell state while retaining every authored puzzle and cosmetic value.
    /// </summary>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
    {
        // Scene entities are rebuilt from their serializable enemy definitions when restoring a revision.
        JsonProperty property = base.CreateProperty(member, serialization);
        if (member.DeclaringType == typeof(Cell) &&
            (member.Name == nameof(Cell.EntityOnCell) || member.Name == nameof(Cell.HasElement) || member.Name == nameof(Cell.IsWalkable)))
            property.Ignored = true;
        return property;
    }
}
