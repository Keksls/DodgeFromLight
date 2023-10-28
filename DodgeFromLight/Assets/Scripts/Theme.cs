using System;
using UnityEngine;

[Serializable]
public class ThemeData
{
    public float GlobalRounding;
    public float TopHalf;
    public float BottomHalf;
    public float Intensity;
    public bool FlatBack;

    public Color GridColor;
    public Color BackColorUp;
    public Color BackColor;
    public Color BackColorDown;

    public Color Selected;
    public Color SelectedHover;
    public Color Unselected;
    public Color UnselectedHover;
    public Color Decline;
    public Color DeclineHover;

    public Color Text;
    public Color TextDisabled;
    public Color PlotLines;
    public Color PlotHistogram;

    public Color PlotLinesHovered;
    public Color PlotHistogramHovered;

    public Color WindowBg;
    public Color ChildBg;
    public Color Border;
    public Color BorderShadow;
    public Color FrameBg;
    public Color FrameBgHovered;
    public Color FrameBgActive;
    public Color FrameBgDisabled;

    public Color TitleBg;
    public Color TitleBgCollapsed;
    public Color TitleBgActive;

    public Color MenuBarBg;
    public Color ScrollbarBg;
    public Color ScrollbarGrab;
    public Color ScrollbarGrabHovered;
    public Color ScrollbarGrabActive;
    public Color CheckMark;

    public Color SliderGrab;
    public Color SliderGrabActive;

    public Color Button;
    public Color ButtonHovered;
    public Color ButtonActive;

    public Color SwitchButton;
    public Color SwitchButtonHovered;
    public Color SwitchButtonActive;

    public Color Toolbar;
    public Color ToolbarHovered;
    public Color ToolbarActive;

    public Color Header;
    public Color HeaderHovered;
    public Color HeaderActive;

    public Color Separator;
    public Color SeparatorHovered;
    public Color SeparatorActive;

    public Color ResizeGrip;
    public Color ResizeGripHovered;
    public Color ResizeGripActive;

    public Color CloseButton;
    public Color CloseButtonHovered;
    public Color CloseButtonActive;

    public Color TextSelectedBg;
    public Color PopupBg;
    public Color ModalWindowDarkening;

    public Color NewButtonColor;



    public ThemeJson toJSonable()
    {
        return new ThemeJson()
        {
            Border = Border.ToHexColor(),
            BorderShadow = BorderShadow.ToHexColor(),
            Button = Button.ToHexColor(),
            Text = Text.ToHexColor(),
            ButtonActive = ButtonActive.ToHexColor(),
            ButtonHovered = ButtonHovered.ToHexColor(),
            SwitchButton = SwitchButton.ToHexColor(),
            SwitchButtonHovered = SwitchButtonHovered.ToHexColor(),
            SwitchButtonActive = SwitchButtonActive.ToHexColor(),
            CheckMark = CheckMark.ToHexColor(),
            ChildBg = ChildBg.ToHexColor(),
            CloseButton = CloseButton.ToHexColor(),
            CloseButtonActive = CloseButtonActive.ToHexColor(),
            CloseButtonHovered = CloseButtonHovered.ToHexColor(),
            Decline = Decline.ToHexColor(),
            DeclineHover = DeclineHover.ToHexColor(),
            FrameBg = FrameBg.ToHexColor(),
            FrameBgActive = FrameBgActive.ToHexColor(),
            FrameBgHovered = FrameBgHovered.ToHexColor(),
            FrameBgDisabled = FrameBgDisabled.ToHexColor(),
            GlobalRounding = GlobalRounding,
            Header = Header.ToHexColor(),
            HeaderActive = HeaderActive.ToHexColor(),
            HeaderHovered = HeaderHovered.ToHexColor(),
            MenuBarBg = MenuBarBg.ToHexColor(),
            ModalWindowDarkening = ModalWindowDarkening.ToHexColor(),
            PlotHistogram = PlotHistogram.ToHexColor(),
            PlotHistogramHovered = PlotHistogramHovered.ToHexColor(),
            PlotLines = PlotLines.ToHexColor(),
            PlotLinesHovered = PlotLinesHovered.ToHexColor(),
            PopupBg = PopupBg.ToHexColor(),
            ResizeGrip = ResizeGrip.ToHexColor(),
            ResizeGripActive = ResizeGripActive.ToHexColor(),
            ResizeGripHovered = ResizeGripHovered.ToHexColor(),
            ScrollbarBg = ScrollbarBg.ToHexColor(),
            ScrollbarGrab = ScrollbarGrab.ToHexColor(),
            ScrollbarGrabActive = ScrollbarGrabActive.ToHexColor(),
            ScrollbarGrabHovered = ScrollbarGrabHovered.ToHexColor(),
            Selected = Selected.ToHexColor(),
            SelectedHover = SelectedHover.ToHexColor(),
            Separator = Separator.ToHexColor(),
            SeparatorActive = SeparatorActive.ToHexColor(),
            SeparatorHovered = SeparatorHovered.ToHexColor(),
            SliderGrab = SliderGrab.ToHexColor(),
            SliderGrabActive = SliderGrabActive.ToHexColor(),
            TextDisabled = TextDisabled.ToHexColor(),
            TextSelectedBg = TextSelectedBg.ToHexColor(),
            TitleBg = TitleBg.ToHexColor(),
            TitleBgActive = TitleBgActive.ToHexColor(),
            TitleBgCollapsed = TitleBgCollapsed.ToHexColor(),
            WindowBg = WindowBg.ToHexColor(),
            GridColor = GridColor.ToHexColor(),
            BackColor = BackColor.ToHexColor(),
            BackColorDown = BackColorDown.ToHexColor(),
            BackColorUp = BackColorUp.ToHexColor(),
            FlatBack = FlatBack,
            BottomHalf = BottomHalf,
            Intensity = Intensity,
            TopHalf = TopHalf,
            Toolbar = Toolbar.ToHexColor(),
            ToolbarActive = ToolbarActive.ToHexColor(),
            ToolbarHovered = ToolbarHovered.ToHexColor(),
            NewButtonColor = NewButtonColor.ToHexColor()
        };
    }

    public ThemeData FromJSonable(ThemeJson json)
    {
        GridColor = json.GridColor.ToColor();
        Border = json.Border.ToColor();
        BorderShadow = json.BorderShadow.ToColor();
        Button = json.Button.ToColor();
        Text = json.Text.ToColor();
        ButtonActive = json.ButtonActive.ToColor();
        ButtonHovered = json.ButtonHovered.ToColor();
        SwitchButton = json.SwitchButton.ToColor();
        SwitchButtonHovered = json.SwitchButtonHovered.ToColor();
        SwitchButtonActive = json.SwitchButtonActive.ToColor();
        CheckMark = json.CheckMark.ToColor();
        ChildBg = json.ChildBg.ToColor();
        CloseButton = json.CloseButton.ToColor();
        CloseButtonActive = json.CloseButtonActive.ToColor();
        CloseButtonHovered = json.CloseButtonHovered.ToColor();
        Decline = json.Decline.ToColor();
        DeclineHover = json.DeclineHover.ToColor();
        FrameBg = json.FrameBg.ToColor();
        FrameBgActive = json.FrameBgActive.ToColor();
        FrameBgHovered = json.FrameBgHovered.ToColor();
        FrameBgDisabled = json.FrameBgDisabled.ToColor();
        GlobalRounding = json.GlobalRounding;
        Header = json.Header.ToColor();
        HeaderActive = json.HeaderActive.ToColor();
        HeaderHovered = json.HeaderHovered.ToColor();
        MenuBarBg = json.MenuBarBg.ToColor();
        ModalWindowDarkening = json.ModalWindowDarkening.ToColor();
        PlotHistogram = json.PlotHistogram.ToColor();
        PlotHistogramHovered = json.PlotHistogramHovered.ToColor();
        PlotLines = json.PlotLines.ToColor();
        PlotLinesHovered = json.PlotLinesHovered.ToColor();
        PopupBg = json.PopupBg.ToColor();
        ResizeGrip = json.ResizeGrip.ToColor();
        ResizeGripActive = json.ResizeGripActive.ToColor();
        ResizeGripHovered = json.ResizeGripHovered.ToColor();
        ScrollbarBg = json.ScrollbarBg.ToColor();
        ScrollbarGrab = json.ScrollbarGrab.ToColor();
        ScrollbarGrabActive = json.ScrollbarGrabActive.ToColor();
        ScrollbarGrabHovered = json.ScrollbarGrabHovered.ToColor();
        Selected = json.Selected.ToColor();
        SelectedHover = json.SelectedHover.ToColor();
        Unselected = json.Unselected.ToColor();
        UnselectedHover = json.UnselectedHover.ToColor();
        Separator = json.Separator.ToColor();
        SeparatorActive = json.SeparatorActive.ToColor();
        SeparatorHovered = json.SeparatorHovered.ToColor();
        SliderGrab = json.SliderGrab.ToColor();
        SliderGrabActive = json.SliderGrabActive.ToColor();
        TextDisabled = json.TextDisabled.ToColor();
        TextSelectedBg = json.TextSelectedBg.ToColor();
        TitleBg = json.TitleBg.ToColor();
        TitleBgActive = json.TitleBgActive.ToColor();
        TitleBgCollapsed = json.TitleBgCollapsed.ToColor();
        WindowBg = json.WindowBg.ToColor();
        BackColor = json.BackColor.ToColor();
        FlatBack = json.FlatBack;
        BackColorDown = json.BackColorDown.ToColor();
        BackColorUp = json.BackColorUp.ToColor();
        BottomHalf = json.BottomHalf;
        Intensity = json.Intensity;
        TopHalf = json.TopHalf;
        Toolbar = json.Toolbar.ToColor();
        ToolbarActive = json.ToolbarActive.ToColor();
        ToolbarHovered = json.ToolbarHovered.ToColor();

        return this;
    }
}

public class ThemeJson
{
    public float GlobalRounding { get; set; }
    public float TopHalf { get; set; }
    public float BottomHalf { get; set; }
    public float Intensity { get; set; }
    public bool FlatBack { get; set; }

    public string BackColorUp { get; set; }
    public string BackColor { get; set; }
    public string BackColorDown { get; set; }

    public string GridColor { get; set; }

    public string Selected { get; set; }
    public string SelectedHover { get; set; }

    public string Unselected { get; set; }
    public string UnselectedHover { get; set; }
    public string Decline { get; set; }
    public string DeclineHover { get; set; }

    public string Text { get; set; }
    public string TextDisabled { get; set; }
    public string PlotLines { get; set; }
    public string PlotHistogram { get; set; }

    public string PlotLinesHovered { get; set; }
    public string PlotHistogramHovered { get; set; }

    public string WindowBg { get; set; }
    public string ChildBg { get; set; }
    public string Border { get; set; }
    public string BorderShadow { get; set; }
    public string FrameBg { get; set; }
    public string FrameBgHovered { get; set; }
    public string FrameBgActive { get; set; }
    public string FrameBgDisabled { get; set; }
    public string TitleBg { get; set; }
    public string TitleBgCollapsed { get; set; }
    public string TitleBgActive { get; set; }

    public string MenuBarBg { get; set; }
    public string ScrollbarBg { get; set; }
    public string ScrollbarGrab { get; set; }
    public string ScrollbarGrabHovered { get; set; }
    public string ScrollbarGrabActive { get; set; }
    public string CheckMark { get; set; }

    public string SliderGrab { get; set; }
    public string SliderGrabActive { get; set; }

    public string Button { get; set; }
    public string ButtonHovered { get; set; }
    public string ButtonActive { get; set; }

    public string SwitchButton { get; set; }
    public string SwitchButtonHovered { get; set; }
    public string SwitchButtonActive { get; set; }

    public string Toolbar { get; set; }
    public string ToolbarHovered { get; set; }
    public string ToolbarActive { get; set; }

    public string Header { get; set; }
    public string HeaderHovered { get; set; }
    public string HeaderActive { get; set; }

    public string Separator { get; set; }
    public string SeparatorHovered { get; set; }
    public string SeparatorActive { get; set; }

    public string ResizeGrip { get; set; }
    public string ResizeGripHovered { get; set; }
    public string ResizeGripActive { get; set; }

    public string CloseButton { get; set; }
    public string CloseButtonHovered { get; set; }
    public string CloseButtonActive { get; set; }

    public string TextSelectedBg { get; set; }
    public string PopupBg { get; set; }
    public string ModalWindowDarkening { get; set; }

    public string NewButtonColor { get; set; }

}

public static class ColorExtensions
{
    public static string ToHexColor(this Color col)
    {
        return ColorUtility.ToHtmlStringRGBA(col);
    }

    public static Color ToColor(this string colorcode)
    {

        colorcode = colorcode.WithStarting("#");
        Color colorDecode;
        if (ColorUtility.TryParseHtmlString(colorcode, out colorDecode))
            return new Color(colorDecode.r, colorDecode.g, colorDecode.b);
        else
            return Color.white;
    }

    public static string WithStarting(this string value, string starting)
    {
        if (value == null)
        {
            return starting;
        }
        for (int i = 0; i < starting.Length; i++)
        {
            string tmp = starting.Left(i) + value;
            if (tmp.StartsWith(starting))
            {
                return tmp;
            }
        }
        return starting + value;
    }

    public static string Left(this string value, int length)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length", length, "Length is less than zero.");
        }

        return (length < value.Length) ? value.Substring(0, length) : value;
    }
}