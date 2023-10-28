using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHelper : MonoBehaviour
{
    public static UIHelper Instance;

    public Color VeryHugeGray;
    public Color BackgroundPanel;
    public Color ContextMenuBack;
    public Color HugeGray;
    public Color DarkGray;
    public Color MediumDarkGray;
    public Color Gray;
    public Color LightGray;
    public Color Hover;
    public Color Interactible;
    public Color InteractibleHover;
    public Color Active;
    public Color ActiveHover;
    public Color PureBlack;
    public Color PureWhite;
    public Color Red;
    public Color ContextMenuSelectedBack;
    public Color ContextMenuSelectedText;
    public Color ContextMenuText;

    void Awake()
    {
        Instance = this;
    }
}