using System;
using UnityEngine;

/// <summary>
/// Associates a cosmetic slot with its character hierarchy container.
/// </summary>
[Serializable]
public sealed class PlayerSkinPartContainer
{
    public SkinType Type;
    public GameObject Container;
}
