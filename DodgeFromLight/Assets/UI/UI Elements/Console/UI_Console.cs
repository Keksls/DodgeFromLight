using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using System.Linq;

public class UI_Console : MonoBehaviour
{
    public static UI_Console Instance;
    private static bool isTyping = false;
    public static bool IsTyping { get { return Instance != null && isTyping; } }
    public ScrollRect scrollRect;
    public Transform Container;
    public GameObject LinePrefab;
    public TMP_InputField input;
    public List<CanalSettings> Cannaux;
    public ChatCanal CurrentCanal;

    public GameObject EmoteItemPrefab;
    public GameObject EmotesListPanel;
    public GameObject RightPanel;
    public Transform EmoteListContainer;
    private Vector3 EmoteListOpenPosition;
    public List<EmoteItemSettings> Emotes;

    public float AttitudesCooldown = 5f;
    public GameObject AttitudesListPanel;
    public GameObject AttitudeItemPrefab;
    public Transform AttitudesListContainer;
    public GameObject AttitudesCooldownPanel;
    public Image AttitudesCooldownImage;
    public List<AttitudesItemSettings> Attitudes;

    public ConsoleTabSettings CurrentTab;
    public int MaxMessagesBuffer = 1024;
    public float AnimationEmotePanelDuration = 0.75f;
    public GameObject Expanded;
    public GameObject Collapsed;

    public List<ConsoleTabItemSettings> Tabs;
    public Color SelectedColor;
    public Color UnselectedColor;
    public float SelectedHeight = 24;
    public float Unselectedheight = 20;
    private List<string> SendedMessages = new List<string>();
    private int currentSendedMessageNavigationIndex = 0;
    private Dictionary<ChatCanal, CanalSettings> dicCannaux;
    private List<ConsoleMessage> MessagesBuffer;
    private List<GameObject> MessagesLines = new List<GameObject>();
    private bool EmotesEnabled = false;

    private void Awake()
    {
        isTyping = false;
        dicCannaux = new Dictionary<ChatCanal, CanalSettings>();
        foreach (CanalSettings canal in Cannaux)
            dicCannaux.Add(canal.Canal, canal);
        Instance = this;
        ClearConsole();
        BindEmotes();
        BindAttitudes();
        RightPanel.SetActive(true);
        EmotesEnabled = false;
        EmoteListOpenPosition = RightPanel.GetComponent<RectTransform>().localPosition;
        RightPanel.SetActive(EmotesEnabled);

        MessagesBuffer = new List<ConsoleMessage>();

        foreach (var tab in Tabs)
            tab.Button.GetComponent<Button>().onClick.AddListener(() =>
            {
                ClickOnTab(tab);
            });
        ClickOnTab(Tabs[0]);
        Expand();
    }

    private void Start()
    {
        foreach (UI_Resizable resizable in GetComponentsInChildren<UI_Resizable>())
        {
            resizable.OnResize -= Resizable_OnResize;
            resizable.OnResize += Resizable_OnResize;
            resizable.OnSmash -= Resizable_OnSmash;
            resizable.OnSmash += Resizable_OnSmash;
        }

        input.GetComponentInChildren<TMP_SelectionCaret>().raycastTarget = false;
    }

    public void Expand()
    {
        Expanded.SetActive(true);
        Collapsed.SetActive(false);
    }

    public void Collapse()
    {
        Expanded.SetActive(false);
        Collapsed.SetActive(true);
    }

    private void Resizable_OnSmash()
    {
        Collapse();
    }

    private void ClickOnTab(ConsoleTabItemSettings tab)
    {
        // unselect other button
        foreach (var t in Tabs)
        {
            t.Button.GetComponent<Image>().color = UnselectedColor;
            t.Button.GetComponent<RectTransform>().sizeDelta = new Vector2(t.Button.GetComponent<RectTransform>().sizeDelta.x, Unselectedheight);
        }

        // select this button
        tab.Button.GetComponent<Image>().color = SelectedColor;
        tab.Button.GetComponent<RectTransform>().sizeDelta = new Vector2(tab.Button.GetComponent<RectTransform>().sizeDelta.x, SelectedHeight);

        SetCurrentTab(tab.Tab);
    }

    private void Resizable_OnResize()
    {
        if (EmotesEnabled)
        {
            RectTransform mainRect = Expanded.GetComponent<RectTransform>();
            RectTransform rect = RightPanel.GetComponent<RectTransform>();
            float expandedX = (mainRect.rect.width / 2f) - 8 + (rect.rect.width / 2f);
            Vector3 end = EmoteListOpenPosition;
            end.x = expandedX;
            rect.localPosition = end;
        }
    }

    private void Update()
    {
        if (!isTyping && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Space)))
        {
            input.Select();
            input.ActivateInputField();
        }

        else if (Input.GetKeyDown(KeyCode.Escape) && isTyping)
            EventSystem.current.SetSelectedGameObject(null);

        else if (isTyping && Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSendedMessageNavigationIndex--;
            if (currentSendedMessageNavigationIndex < 0)
                currentSendedMessageNavigationIndex = 0;
            input.text = SendedMessages[currentSendedMessageNavigationIndex];
        }
        else if (isTyping && Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSendedMessageNavigationIndex++;
            if (currentSendedMessageNavigationIndex > SendedMessages.Count - 1)
                currentSendedMessageNavigationIndex = SendedMessages.Count - 1;
            input.text = SendedMessages[currentSendedMessageNavigationIndex];
        }
        else if (isTyping && Input.GetKeyDown(KeyCode.Return))
        {
            Send();
        }

        isTyping = input.isFocused;
    }

    private void OnEnable()
    {
        input.Select();
        input.ActivateInputField();
    }

    #region Messages Handling
    public void SetCurrentTab(ConsoleTabSettings tab)
    {
        CurrentTab = tab;
        CurrentTab.Canals = new HashSet<ChatCanal>();
        foreach (ChatCanal canal in CurrentTab.ListCanals)
            CurrentTab.Canals.Add(canal);
        UpdateConsoleItems();
    }

    public void AddCanal(ChatCanal canal)
    {
        if (CurrentTab.Canals.Add(canal))
            CurrentTab.ListCanals.Add(canal);
        UpdateConsoleItems();
    }

    public void RemoveCanal(ChatCanal canal)
    {
        if (CurrentTab.Canals.Remove(canal))
            CurrentTab.ListCanals.Remove(canal);
        UpdateConsoleItems();
    }

    public void UpdateConsoleItems()
    {
        ClearConsole();

        List<ConsoleMessage> messages = new List<ConsoleMessage>();
        for (int i = MessagesBuffer.Count - 1; i >= 0; i--)
            if (CurrentTab.Canals.Contains(MessagesBuffer[i].Canal))
            {
                messages.Add(MessagesBuffer[i]);
                if (messages.Count >= CurrentTab.NbMessagesMax)
                    break;
            }
        messages.Reverse();
        foreach (ConsoleMessage consoleMessage in messages)
            AddLine(consoleMessage);

        LayoutRebuilder.ForceRebuildLayoutImmediate(Container.GetComponent<RectTransform>());
        StartCoroutine(AutoScroll());
    }

    public bool AddLine(ConsoleMessage message)
    {
        if (CurrentTab.Canals.Contains(message.Canal))
        {
            GameObject line = Instantiate(LinePrefab);
            MessagesLines.Add(line);
            line.transform.SetParent(Container, false);
            TextMeshProUGUI tmp = line.GetComponent<TextMeshProUGUI>();
            tmp.text = message.Message;
            if (message.Canal != ChatCanal.System)
            {
                Button btn = line.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    input.text = "/w " + message.ClientName + " ";
                    input.Select();
                });
            }
            else
            {
                Destroy(line.GetComponent<Button>());
                Destroy(line.GetComponent<CursorSetter>());
            }
            return true;
        }
        return false;
    }

    public void RemoveLastBufferMessage()
    {
        if (MessagesBuffer.Count > MaxMessagesBuffer)
            MessagesBuffer.RemoveAt(0); // remove first added
    }

    public void RemoveLastConsoleMessage()
    {
        if (MessagesLines.Count > CurrentTab.NbMessagesMax)
        {
            Destroy(MessagesLines[0]);
            MessagesLines.RemoveAt(0);
        }
    }

    public string AddMessage(ChatCanal canal, int senderID, string senderName, string message)
    {
        // create line
        CanalSettings canalSettings = dicCannaux[canal];

        // set text
        string msgHead = "<color=#" + canalSettings.Color.ToHexColor() + ">";
        string msgFoot = "</color>";
        if (canal == ChatCanal.System)
            msgHead += "<b>[System]</b> : ";
        else
            msgHead += "<b>" + senderName + "</b> : ";
        string fullMessage = msgHead + message + msgFoot;

        ConsoleMessage cm = new ConsoleMessage(canal, fullMessage, senderID, senderName);
        MessagesBuffer.Add(cm);
        AddLine(cm);
        RemoveLastBufferMessage();
        RemoveLastConsoleMessage();

        LayoutRebuilder.ForceRebuildLayoutImmediate(Container.GetComponent<RectTransform>());
        StartCoroutine(AutoScroll());
        return fullMessage;
    }

    IEnumerator AutoScroll()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(Container.GetComponent<RectTransform>());
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0;
    }

    public void ClearConsole()
    {
        foreach (Transform t in Container)
            Destroy(t.gameObject);
        RectTransform rect = Container.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0f);
        input.Select();
        input.ActivateInputField();
    }
    #endregion

    public void ClientSpeak()
    {
        byte canal = 0;
        int senderID = 0;
        string senderName = "", message = "";
        DFLClient.CurrentMessage.Get(ref canal);
        DFLClient.CurrentMessage.Get(ref senderID);
        DFLClient.CurrentMessage.Get(ref senderName);
        DFLClient.CurrentMessage.Get(ref message);

        AddMessage((ChatCanal)canal, senderID, senderName, message);

        if (LobbyManager.Instance != null && (ChatCanal)canal == ChatCanal.General)
            LobbyManager.Instance.ClientSpeak(senderID, message);
    }

    public void Send()
    {
        string message = input.text;
        SendedMessages.Add(message);
        if (SendedMessages.Count > MaxMessagesBuffer)
            SendedMessages.RemoveAt(0);
        currentSendedMessageNavigationIndex = SendedMessages.Count;

        if (message.StartsWith("/"))
            if (HandleLocalCommands(message))
            {
                input.text = "";
                input.Select();
                input.ActivateInputField();
                return;
            }

        if (!HasPrefix(message))
            message = GetMessagePrefix() + " " + message;
        DFLClient.SpeakLobby(message);
        input.text = "";
        input.Select();
        input.ActivateInputField();
    }

    private bool HandleLocalCommands(string message)
    {
        if (message == "/clear" || message == "/clr")
        {
            ClearConsole();
            return true;
        }

        else if (message.StartsWith("/canal ") || message.StartsWith("/layer "))
        {
            string[] spl = message.ToLower().Split(' ');
            try
            {
                if (spl[1] == "add")
                    AddCanal((ChatCanal)int.Parse(spl[2]));
                else if (spl[1] == "rem")
                    RemoveCanal((ChatCanal)int.Parse(spl[2]));
                else
                    AddMessage(ChatCanal.System, -1, "[System]", "/canal add | rem [canalID]");
            }
            catch { AddMessage(ChatCanal.System, -1, "[System]", "/canal add | rem [canalID]"); }
            return true;
        }

        else if (message == "/help" || message == "/?")
        {
            AddMessage(ChatCanal.System, -1, "[System]", "/layer add | rem [canalID]   => Add or Remove chat layer\n" +
                "/clear (or /clr)    => Clear this console\n");
            return true;
        }

        return false;
    }

    private string GetMessagePrefix()
    {
        return "/" + dicCannaux[CurrentCanal].Prefix;
    }

    private string GetMessagePrefix(string message)
    {
        string[] split = message.Split(' ');
        if (split.Length > 0)
        {
            if (split[0].Length == 2 && split[0][0] == '/')
                return split[0];
        }
        return string.Empty;
    }

    private bool HasPrefix(string message)
    {
        return !string.IsNullOrEmpty(GetMessagePrefix(message));
    }

    public void ClickOnEmotesButton(bool attitudes)
    {
        bool switchPanel = EmotesEnabled && ((EmotesListPanel.gameObject.activeInHierarchy && attitudes) || (AttitudesListPanel.gameObject.activeInHierarchy && !attitudes));
        if (switchPanel)
            EmotesEnabled = true;
        else
            EmotesEnabled = !EmotesEnabled;

        if (!switchPanel)
        {
            StopCoroutine("AnimateRightPanel");
            StartCoroutine(AnimateRightPanel(EmotesEnabled, attitudes));
        }
        else
        {
            EmotesListPanel.gameObject.SetActive(!attitudes);
            AttitudesListPanel.gameObject.SetActive(attitudes);
        }
    }

    IEnumerator AnimateRightPanel(bool open, bool attitudes)
    {
        RectTransform mainRect = Expanded.GetComponent<RectTransform>();
        RectTransform rect = RightPanel.GetComponent<RectTransform>();
        float expandedX = (mainRect.rect.width / 2f) - 8 + (rect.rect.width / 2f);
        float retractedX = (mainRect.rect.width / 2f) - 8 - (rect.rect.width / 2f);
        Vector3 start;
        Vector3 end;
        RightPanel.SetActive(true);
        EmotesListPanel.gameObject.SetActive(!attitudes);
        AttitudesListPanel.gameObject.SetActive(attitudes);
        if (open)
        {
            start = EmoteListOpenPosition;
            start.x = retractedX;
            end = EmoteListOpenPosition;
            end.x = expandedX;
        }
        else
        {
            start = EmoteListOpenPosition;
            start.x = expandedX;
            end = EmoteListOpenPosition;
            end.x = retractedX;
        }
        float enlapsed = 0.0f;
        while (enlapsed < AnimationEmotePanelDuration)
        {
            rect.localPosition = Vector3.Lerp(start, end, enlapsed / AnimationEmotePanelDuration);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        rect.localPosition = end;
        RightPanel.SetActive(open);
    }

    private void BindEmotes()
    {
        foreach (var emote in Emotes)
        {
            for (int i = 0; i < emote.MaxID; i++)
            {
                Sprite sprite = Resources.Load<Sprite>("Emotes/" + emote.Type.ToString() + "/" + i);
                if (sprite == null)
                    continue;
                GameObject item = Instantiate(EmoteItemPrefab);
                item.GetComponent<Image>().sprite = sprite;
                item.transform.SetParent(EmoteListContainer, false);
                int emoteID = i;
                EmoteType type = emote.Type;
                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    DFLClient.SpeakEmote(type, emoteID);
                });
            }
        }
    }

    private void BindAttitudes()
    {
        foreach (AttitudesItemSettings attitude in Attitudes.OrderBy(a => (int)a.Type))
        {
            if (attitude.Exclude)
                continue;
            GameObject item = Instantiate(AttitudeItemPrefab);
            item.transform.GetChild(1).GetComponent<Image>().sprite = attitude.Icon;
            item.transform.SetParent(AttitudesListContainer, false);
            item.transform.GetChild(0).GetComponent<Image>().color = attitude.Color;
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                DFLClient.PlayAnnimation(attitude.AnimationName);
                StartCoroutine(AnimationCooldown_Routine());
            });
            UITooltipSetter tt = item.AddComponent<UITooltipSetter>();
            tt.Message = "<i>[" + attitude.Type + "]</i>  " + attitude.Name;
            tt.UsePointerEvent = true;
        }
    }

    IEnumerator AnimationCooldown_Routine()
    {
        AttitudesCooldownPanel.SetActive(true);
        float enlapsed = 0.0f;
        while(enlapsed < AttitudesCooldown)
        {
            if(!AttitudesCooldownImage)
            {
                yield return null;
                continue;
            }
            AttitudesCooldownImage.fillAmount = enlapsed / AttitudesCooldown;
            yield return null;
            enlapsed += Time.deltaTime;
        }

        AttitudesCooldownPanel.SetActive(false);
    }
}

[Serializable]
public class CanalSettings
{
    public ChatCanal Canal;
    public Color Color;
    public string Prefix;
    public bool Selectable;
}

[Serializable]
public class ConsoleTabSettings
{
    public HashSet<ChatCanal> Canals;
    public List<ChatCanal> ListCanals;
    public string Name = "Default";
    public int NbMessagesMax = 50;
}

[Serializable]
public class ConsoleTabItemSettings
{
    public ConsoleTabSettings Tab;
    public GameObject Button;
}

[Serializable]
public class EmoteItemSettings
{
    public EmoteType Type;
    public int MaxID;
}
[Serializable]

public class AttitudesItemSettings
{
    public AttitudeType Type;
    public Sprite Icon;
    public Color Color;
    public string Name;
    public string AnimationName;
    public bool Exclude = false;
}

public struct ConsoleMessage
{
    public string Message { get; set; }
    public ChatCanal Canal { get; set; }
    public string ClientName { get; set; }
    public int ClientID { get; set; }

    public ConsoleMessage(ChatCanal canal, string message, int clientID, string clientName)
    {
        this.Canal = canal;
        this.Message = message;
        this.ClientID = clientID;
        this.ClientName = clientName;
    }

    public override string ToString()
    {
        return Message;
    }
}