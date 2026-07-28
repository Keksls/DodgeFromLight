using System;

/// <summary>
/// Stores the local metadata associated with an authored puzzle map.
/// </summary>
[Serializable]
public sealed class Map
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public int AuthorID { get; set; }
    public string UploadDate { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public MapState State { get; set; }
    public int AlreadyFinished { get; set; }

    /// <summary>
    /// Creates an empty metadata object for JSON deserialization.
    /// </summary>
    public Map()
    {
        // Supply safe local defaults when optional legacy fields are absent.
        ID = string.Empty;
        Name = string.Empty;
        Author = "Player";
        UploadDate = DateTime.UtcNow.ToString("O");
        State = MapState.Draft;
    }

    /// <summary>
    /// Creates metadata for a new local puzzle map.
    /// </summary>
    public Map(string name, int width, int height)
        : this()
    {
        // Generate a local stable identifier independent of any remote service.
        ID = Guid.NewGuid().ToString("N");
        Name = name;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Determines whether the author has completed the current revision.
    /// </summary>
    public bool IsFinished()
    {
        // Keep the established integer representation used by existing map JSON.
        return AlreadyFinished == 1;
    }

    /// <summary>
    /// Updates the local author identity.
    /// </summary>
    public void SetAuthor(string name)
    {
        // Local maps no longer require a server-side numeric account identifier.
        Author = string.IsNullOrWhiteSpace(name) ? "Player" : name;
        AuthorID = -1;
    }

    /// <summary>
    /// Updates the authored map state.
    /// </summary>
    public void SetState(MapState state)
    {
        // Store validation state alongside the map metadata.
        State = state;
    }
}
