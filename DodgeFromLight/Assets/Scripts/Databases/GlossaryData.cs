using UnityEngine;

[CreateAssetMenu(fileName = "GlossaryData", menuName = "DodgeFromLight/GlossaryData", order = 3)]
public class GlossaryData : ScriptableObject
{
    public GlossaryDataItem[] GlossaryItems;
}

[System.Serializable]
public class GlossaryDataItem
{
    public string Name;
    public Sprite Image;
    [TextArea(3, 10)]
    public string Description;
}