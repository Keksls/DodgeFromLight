using DFLCommonNetwork.GameEngine;
using DFLCommonNetwork.Protocole;
using DFLNetwork.Protocole;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class DFLClient_Controller : MonoBehaviour
{
    private ConcurrentQueue<Action> mainThreadCallback = new ConcurrentQueue<Action>();
    public TcpClient TcpClient { get; set; }
    private bool connected = false;
    private bool QueueStop { get; set; }
    public event EventHandler<EventArgs> Disconected;

    public void Update()
    {
        DFLClient.ProcessMessagesQueue();
        while (mainThreadCallback.Count > 0)
        {
            Action callback = null;
            if (mainThreadCallback.TryDequeue(out callback))
                callback?.Invoke();
        }
    }

    void Awake()
    {
        if (DFLClient.Client == null)
        {
            DFLClient.Client = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void Connect(string hostNameOrIpAddress, int port)
    {
        TcpClient = new TcpClient();
        TcpClient.Connect(hostNameOrIpAddress, port);
        ValidateConnection();
    }

    public void Disconnect()
    {
        if (TcpClient == null)
            return;
        TcpClient.Close();
        TcpClient.Dispose();
        TcpClient = null;
    }

    void ValidateConnection()
    {
        long timeEnd = DateTime.Now.AddSeconds(30).Ticks;
        int step = 0;
        int key = 0;
        while (TcpClient != null && TcpClient.Connected && DateTime.Now.Ticks < timeEnd)
        {
            // Handle Byte Avaliable
            if (step == 0 && TcpClient.Available >= 8)
            {
                ExecuteInMainThread(() =>
                {
                    DodgeFromLight.UI_WorkerNotifier.Show("HandShake server...");
                });
                byte[] array = new byte[8];
                TcpClient.Client.Receive(array, 0, 8, SocketFlags.None);
                int rnd1 = BitConverter.ToInt32(array, 0);
                int rnd2 = BitConverter.ToInt32(array, 4);
                key = HandShake.GetKey(rnd1, rnd2);
                byte[] rep = BitConverter.GetBytes(key);
                TcpClient.GetStream().Write(rep, 0, rep.Length);
                step = 1;
            }
            else if (step == 1 && TcpClient.Available >= 4)
            {
                byte[] array = new byte[4];
                TcpClient.Client.Receive(array, 0, 4, SocketFlags.None);
                int rep = BitConverter.ToInt32(array, 0);
                if (rep == key)
                {
                    ExecuteInMainThread(() =>
                    {
                        DodgeFromLight.UI_Notifications.Notify("Connected to server.");
                        DodgeFromLight.UI_WorkerNotifier.Hide();
                    });
                    Thread runLoopThread = new Thread(RunLoopStep);
                    runLoopThread.Start();
                    break;
                }
                else
                {
                    ExecuteInMainThread(() =>
                    {
                        DodgeFromLight.UI_WorkerNotifier.Show("Fail to Hand Shake server.");
                    });
                    break;
                }
            }
        }
    }

    void RunLoopStep()
    {
        byte[] bytesReceived = new byte[0];
        int currentLenght = -1;
        int nbBytesReceived = 0;
        while (!QueueStop)
        {
            // Handle Disconenction
            if (TcpClient == null || !TcpClient.Connected)
            {
                if (Disconected != null && connected)
                    Disconected(this, null);
                ExecuteInMainThread(() =>
                {
                    DodgeFromLight.UI_WorkerNotifier.Show("Disconnected to server.");
                });
                return;
            }
            connected = TcpClient.Connected;

            // Handle message lenght
            if (currentLenght == -1 && TcpClient.Available >= 4)
            {
                byte[] lenthArray = new byte[4];
                TcpClient.Client.Receive(lenthArray, 0, 4, SocketFlags.None);
                currentLenght = BitConverter.ToInt32(lenthArray, 0);
                bytesReceived = new byte[currentLenght];
                nbBytesReceived = 0;
            }

            // handle receive message data
            while (currentLenght != -1 && TcpClient.Available > 0 && TcpClient.Connected)
            {
                TcpClient.Client.Receive(bytesReceived, nbBytesReceived, 1, SocketFlags.None);
                nbBytesReceived++;

                // all message recieved
                if (nbBytesReceived == currentLenght)
                {
                    currentLenght = -1;
                    DFLClient.DataReceived(new NetworkMessage(bytesReceived));
                }
            }
            Thread.Sleep(1);
        }
    }

    public void Write(byte[] msg)
    {
        TcpClient.GetStream().Write(msg, 0, msg.Length);
    }

    public void ExecuteInMainThread(Action Callback)
    {
        lock (mainThreadCallback)
        {
            mainThreadCallback.Enqueue(Callback);
        }
    }
}

public class MessageEventArg : EventArgs
{
    public NetworkMessage Message;

    public MessageEventArg(NetworkMessage _Message)
    {
        Message = _Message;
    }
}

public static class DFLClient
{
    public static DFLClient_Controller Client;
    public static APIConfiguration APIConfiguration;
    public static GameClient CurrentUser { get; private set; }
    public static OnlineState OnlineState { get; private set; }
    public static LoginState LoginState { get; private set; }
    public static int NbReplyAsked { get; private set; }

    static DFLClient()
    {
        APIConfiguration = new APIConfiguration()
        {
            APIURL = "https://vrdtmstudio.com/DFL/API/API.php",
            MapsURL = "https://vrdtmstudio.com/DFL/API/Maps"
        };
        OnlineState = OnlineState.NotChecked;
        LoginState = LoginState.LoggedOut;
        NbReplyAsked = 1;
        Initialize();
    }

    #region TCP Client
    public static NetworkMessage CurrentMessage { get; private set; }
    public static int ID { get; private set; }
    private static Dictionary<HeadActions, Action> headToAction;
    private static Queue<NetworkMessage> MessagesQueue = new Queue<NetworkMessage>();
    private static Dictionary<int, Action<NetworkMessage>> ReplyCallBack = new Dictionary<int, Action<NetworkMessage>>();

    public static void Initialize()
    {
        ConfigurationManager.Initialize();
        ID = -1;
        headToAction = new Dictionary<HeadActions, Action>();

        // Debug
        headToAction.Add(HeadActions.DebugText, () =>
        {
            string text = null;
            CurrentMessage.Get(ref text);
            Debug.Log(text);
        });
        // Notify
        headToAction.Add(HeadActions.toClient_Notify, () =>
        {
            string text = null;
            CurrentMessage.Get(ref text);
            DodgeFromLight.UI_Notifications.Notify(text);
        });
        // Modal
        headToAction.Add(HeadActions.toClient_Modal, () =>
        {
            string text = null;
            CurrentMessage.Get(ref text);
            DodgeFromLight.UI_Modal.SetTitle("Server Information").Show(text);
        });

        // Console
        headToAction.Add(HeadActions.toClient_Speak, () => { UI_Console.Instance.ClientSpeak(); });
        headToAction.Add(HeadActions.toClient_Emote, () => { LobbyManager.Instance.ClientEmote(); });
        // player
        headToAction.Add(HeadActions.toClient_RefreshPlayerSave, () =>
        {
            RefreshPlayerSave();
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.RefreshSave();
        });

        // Lobbies
        headToAction.Add(HeadActions.toClient_EnterLobby, () => LobbyManager.Instance.ClientEnterLobby());
        headToAction.Add(HeadActions.toClient_LeaveLobby, () => LobbyManager.Instance.ClientLeaveLobby());
        headToAction.Add(HeadActions.toClient_ChangeCell, () => LobbyManager.Instance.ClientChangeCell());
        headToAction.Add(HeadActions.toClient_EquipSkin, () => LobbyManager.Instance.ClientEquipSkin());
        headToAction.Add(HeadActions.toClient_SetOrientation, () => LobbyManager.Instance.ClientSetOrientation());
        headToAction.Add(HeadActions.toClient_ForceEnterLobby, () => LobbyManager.Instance.ForceEnterLobby());
        headToAction.Add(HeadActions.toClient_AskEnterHub, () => LobbyManager.Instance.AskEnterHub());
        headToAction.Add(HeadActions.toClient_PlayAnimation, () => LobbyManager.Instance.ClientPlayAnimation());

        // Connection Lost  
        headToAction.Add(HeadActions.ConnectionLost, () => { DodgeFromLight.UI_Notifications.Notify("Server connexion Lost."); });
    }

    public static void Start(string IPAdress, int port)
    {
        Client.Connect(IPAdress, port);
        Client.Disconected += Client_Disconected;
    }

    private static void Client_Disconected(object sender, EventArgs e)
    {
        MessagesQueue.Enqueue(new NetworkMessage(HeadActions.ConnectionLost).Set(0));
    }

    public static void DataReceived(NetworkMessage msg)
    {
        MessagesQueue.Enqueue(msg);
    }

    public static void ProcessMessagesQueue()
    {
        while (MessagesQueue.Count > 0)
        {
            CurrentMessage = MessagesQueue.Dequeue();
            if (CurrentMessage.ReplyID != 0 && ReplyCallBack.ContainsKey(CurrentMessage.ReplyID))
            {
                ReplyCallBack[CurrentMessage.ReplyID](CurrentMessage);
                ReplyCallBack.Remove(CurrentMessage.ReplyID);
            }
            else
            {
                if (headToAction.ContainsKey(CurrentMessage.Head))
                {
                    headToAction[CurrentMessage.Head]();
                }
                else if (CurrentMessage.Head != HeadActions.None)
                    Debug.Log("Trying to Process message with head '" + CurrentMessage.Head.ToString() + "' but no action related... Message skipped.");
            }
            CurrentMessage = null;
        }
    }

    public static void SendMessage(NetworkMessage msg)
    {
        if (CurrentUser != null)
            msg.ClientID = CurrentUser.ID;
        Client.Write(msg.Serialize());
    }

    public static void AskForReply(NetworkMessage msg, Action<NetworkMessage> callback)
    {
        msg.ReplyTo(NbReplyAsked);
        ReplyCallBack.Add(msg.ReplyID, callback);
        NbReplyAsked++;
        SendMessage(msg);
    }
    #endregion

    #region Maps
    /// <summary>
    /// Get Add AvailableMap list
    /// </summary>
    /// <param name="Callback">arg1 : API Result, arg2 : List of maps</param>
    public static void GetMapsList(string keyWord, Action<APIResult, List<Map>> Callback)
    {
        DownloadAsyncString(route("GetMapsList", param("filter", keyWord)), (res) =>
        {
            if (!res.Error)
            {
                try
                {
                    ExecuteInMainThread(() => Callback(res, JsonConvert.DeserializeObject<List<Map>>(res.APIResponse)));
                }
                catch (Exception ex)
                {
                    res.LocalErrorMessage = ex.Message;
                    ExecuteInMainThread(() => Callback(res, null));
                }
            }
            else
                ExecuteInMainThread(() => Callback(res, null));
        });
    }
    /// <summary>
    /// Get Working Map list
    /// </summary>
    /// <param name="Callback">arg1 : API Result, arg2 : List of maps</param>
    public static void GetWorkingMapsList(int userID, Action<APIResult, List<Map>> Callback)
    {
        DownloadAsyncString(route("GetWorkingMapsList", param("userID", userID)), (res) =>
        {
            if (!res.Error)
            {
                try
                {
                    ExecuteInMainThread(() => Callback(res, JsonConvert.DeserializeObject<List<Map>>(res.APIResponse)));
                }
                catch (Exception ex)
                {
                    res.LocalErrorMessage = ex.Message;
                    ExecuteInMainThread(() => Callback(res, null));
                }
            }
            else
                ExecuteInMainThread(() => Callback(res, null));
        });
    }

    /// <summary>
    /// Upload a Map /!\ Handle Validation
    /// </summary>
    /// <param name="gridID"></param>
    /// <param name="Callback"></param>
    public static void UploadMap(string gridID, Action<APIResult> Callback)
    {
        if (LoginState != LoginState.LoggedIn)
        {
            Callback(new APIResult("You must be logged to upload a map", "You must be logged to upload a map"));
            return;
        }

        Thread t = new Thread(() =>
        {
            string mapJSON = File.ReadAllText(GridManager.GetMapPath());
            string zipPath = GridManager.Folder + ".zip";
            string folderPath = GridManager.Folder;
            ZipFile.CreateFromDirectory(folderPath, zipPath, System.IO.Compression.CompressionLevel.Fastest, false);
            Map map = JsonConvert.DeserializeObject<Map>(mapJSON);
            WebClient client = new WebClient();
            APIResult res = new APIResult();

            Uri url = route("UploadMap", param("ID", gridID), param("name", map.Name), param("authorID", CurrentUser.ID), param("width", map.Width), param("height", map.Height), param("state", (int)map.State), param("finished", map.AlreadyFinished));
            string repGrid = Encoding.UTF8.GetString(client.UploadFile(url.AbsoluteUri, "POST", zipPath));

            if (repGrid != "OK")
            {
                res.Error = true;
                res.APIErrorMessage += repGrid != "OK" ? repGrid : "";
                res.APIResponse = res.APIErrorMessage;
            }
            File.Delete(zipPath);

            ExecuteInMainThread(() => Callback(res));
        });
        t.Start();
    }

    public static void DownloadMap(string mapID, string folderPath, Action<APIResult> Callback)
    {
        Uri uri = route("DownloadMap", param("mapID", mapID));
        WebClient client = new WebClient();
        APIResult res = new APIResult();
        Thread t = new Thread(() =>
        {
            try
            {
                client.DownloadFile(uri.AbsoluteUri, folderPath + ".zip");
                if (File.Exists(folderPath + ".zip"))
                {
                    if (Directory.Exists(folderPath))
                        Directory.Delete(folderPath, true);
                    Directory.CreateDirectory(folderPath);
                    ZipFile.ExtractToDirectory(folderPath + ".zip", folderPath);
                    File.Delete(folderPath + ".zip");
                    res.Error = false;
                    res.APIResponse = "OK";
                }
                else
                {
                    res.Error = true;
                    res.LocalErrorMessage = "Download failed.";
                }
            }
            catch (Exception ex)
            {
                res.Error = true;
                res.LocalErrorMessage = ex.Message;
            }
            ExecuteInMainThread(() => Callback(res));
        });
        t.Start();
    }

    public static void GetDiscoveryMapID(Action<APIResult, string> Callback)
    {
        Uri uri = route("GetDiscoveryMapID", param("userID", CurrentUser.ID));
        DownloadAsyncString(uri, (res) =>
        {
            if (res.APIResponse.Length == 32)
            {
                ExecuteInMainThread(() => Callback(res, res.APIResponse));
            }
            else
            {
                res.Error = true;
                ExecuteInMainThread(() => Callback(res, null));
            }
        });
    }

    public static void GetMapCode(string mapID, Action<APIResult, string> Callback)
    {
        DownloadAsyncString(route("GetMapCode", param("mapID", mapID)), (res) =>
        {
            string code = "no code";
            res.Error |= res.APIResponse == null || res.APIResponse.Length != 6;
            if (!res.Error)
                code = res.APIResponse;
            Callback?.Invoke(res, code);
        });
    }

    public static void GetMap(string mapID, Action<APIResult, Map> Callback)
    {
        DownloadAsyncString(route("GetMap", param("mapID", mapID)), (res) =>
        {
            Map map = null;
            try
            {
                map = JsonConvert.DeserializeObject<Map>(res.APIResponse);
            }
            catch (Exception ex) { res.LocalErrorMessage = ex.Message; }
            Callback?.Invoke(res, map);
        });
    }

    public static void GetMapIDFromCode(string code, Action<APIResult, string> Callback)
    {
        DownloadAsyncString(route("GetMapIDFromCode", param("code", code)), (res) =>
        {
            res.Error |= res.APIResponse.Length != 32;
            Callback?.Invoke(res, res.APIResponse);
        });
    }

    public static void IsMapLocked(string mapID, Action<APIResult, bool> Callback)
    {
        DownloadAsyncString(route("IsMapLocked", param("mapID", mapID)), (res) =>
        {
            bool locked = true;
            res.Error |= bool.TryParse(res.APIResponse, out locked);
            Callback?.Invoke(res, locked);
        });
    }

    public static void StopWorkingOnMap(string mapID, Action<APIResult, bool> Callback)
    {
        DownloadAsyncString(route("StopWorkingOnMap", param("mapID", mapID)), (res) =>
        {
            res.Error |= res.APIResponse != "OK";
            Callback?.Invoke(res, !res.Error);
        });
    }

    public static void DeleteMap(string mapID, Action<APIResult, bool> Callback)
    {
        DownloadAsyncString(route("DeleteMap", param("mapID", mapID)), (res) =>
        {
            res.Error |= res.APIResponse != "OK";
            Callback?.Invoke(res, !res.Error);
        });
    }

    public static void WorkOnMap(string mapID, Action<APIResult, bool> Callback)
    {
        DownloadAsyncString(route("WorkOnMap", param("mapID", mapID), param("userID", CurrentUser.ID)), (res) =>
        {
            res.Error |= res.APIResponse != "OK";
            Callback?.Invoke(res, !res.Error);
        });
    }

    public static void DownloadMapPreview(string mapID, Action<APIResult, byte[]> Callback)
    {
        Uri uri = route("DownloadPreviewImage", param("ID", mapID));
        WebClient client = new WebClient();
        APIResult res = new APIResult();
        byte[] data = null;
        Thread t = new Thread(() =>
        {
            int nbTenta = 0;
            int nbTentaMax = 5;
            do
            {
                try
                {
                    data = client.DownloadData(uri.AbsoluteUri);
                    res.Error = false;
                    res.APIResponse = "OK";
                }
                catch (Exception ex)
                {
                    res.Error = true;
                    res.LocalErrorMessage = ex.Message;
                }
                nbTenta++;
            } while (res.Error && nbTenta < nbTentaMax);
            ExecuteInMainThread(() => Callback(res, data));
        });
        t.Start();
    }

    public static void AddWorkingMap(string mapID, int userID, Action<APIResult> Callback)
    {
        DownloadAsyncString(route("AddWorkingMap", param("mapID", mapID), param("userID", userID)), (res) =>
        {
            res.Error |= res.APIResponse != "OK";
            Callback?.Invoke(res);
        });
    }

    public static void DeleteWorkingMap(string mapID, int userID, Action<APIResult> Callback)
    {
        DownloadAsyncString(route("DeleteWorkingMap", param("mapID", mapID), param("userID", userID)), (res) =>
        {
            res.Error |= res.APIResponse != "OK";
            Callback?.Invoke(res);
        });
    }

    public static void UpdateFinishState(string mapID, int state, Action<APIResult> Callback)
    {
        DownloadAsyncString(route("UpdateFinishState", param("ID", mapID), param("finished", state)), (res) =>
        {
            res.Error |= res.APIResponse != "OK";
            Callback?.Invoke(res);
        });
    }

    public static void UpdateState(string mapID, MapState state, Action<APIResult> Callback)
    {
        DownloadAsyncString(route("UpdateState", param("ID", mapID), param("state", (int)state)), (res) =>
        {
            res.Error |= res.APIResponse != "OK";
            Callback?.Invoke(res);
        });
    }
    #endregion

    #region Hub
    public static void AwnserEnterHub(int senderID, bool response)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_AwnserEnterHub).Set(senderID).Set(response));
    }

    public static void TryJoinPlayer(int target, int sender, bool force)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_TryJoinPlayer).Set(target).Set(sender).Set(force));
    }

    /// <summary>
    /// Upload a Hub /!\ Handle Validation
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="Callback"></param>
    public static void UploadHub(Action<APIResult> Callback)
    {
        Thread t = new Thread(() =>
        {
            string gridPath = GridManager.Folder + @"\grid.json";
            WebClient client = new WebClient();
            APIResult res = new APIResult();

            Uri url = route("UploadHub", param("ID", CurrentUser.ID));
            string repGrid = Encoding.UTF8.GetString(client.UploadFile(url.AbsoluteUri, "POST", gridPath));

            if (repGrid != "OK")
            {
                res.Error = true;
                res.APIErrorMessage += repGrid != "OK" ? repGrid : "";
                res.APIResponse = res.APIErrorMessage;
            }
            File.Delete(gridPath);

            ExecuteInMainThread(() => Callback(res));
        });
        t.Start();
    }

    public static void DownloadHub(int hubID, Action<APIResult> Callback)
    {
        Uri uri = route("DownloadHub", param("ID", hubID));
        WebClient client = new WebClient();
        APIResult res = new APIResult();
        GridManager.CleanFolder();
        Thread t = new Thread(() =>
        {
            try
            {
                string gridPath = GridManager.GetGridPath();
                string tmpgridPath = GridManager.GetGridPath() + "_" + Guid.NewGuid().ToStr();
                client.DownloadFile(uri.AbsoluteUri, tmpgridPath);
                if (File.Exists(tmpgridPath))
                {
                    File.Copy(tmpgridPath, gridPath, true);
                    File.Delete(tmpgridPath);
                    res.Error = false;
                    res.APIResponse = "OK";
                }
                else
                {
                    res.Error = true;
                    res.LocalErrorMessage = "Download failed.";
                }
            }
            catch (Exception ex)
            {
                res.Error = true;
                res.LocalErrorMessage = ex.Message;
            }
            ExecuteInMainThread(() => Callback(res));
        });
        t.Start();
    }

    public static void HasHub(Action<bool, bool> Callback)
    {
        DownloadAsyncString(route("HasHub", param("ID", CurrentUser.ID)), (res) =>
        {
            bool hub = false;
            bool success = bool.TryParse(res.APIResponse, out hub);
            Callback?.Invoke(success, hub);
        });
    }
    #endregion

    #region Map Start
    public static void GetDayMapStar(Action<APIResult, List<Map>> Callback)
    {
        DownloadAsyncString(route("GetDayMapStar"), (res) =>
        {
            if (!res.Error)
            {
                try
                {
                    ExecuteInMainThread(() => Callback(res, JsonConvert.DeserializeObject<List<Map>>(res.APIResponse)));
                }
                catch (Exception ex)
                {
                    res.LocalErrorMessage = ex.Message;
                    ExecuteInMainThread(() => Callback(res, null));
                }
            }
            else
                ExecuteInMainThread(() => Callback(res, null));
        });
    }

    public static void GetWeekMapStar(Action<APIResult, List<Map>> Callback)
    {
        DownloadAsyncString(route("GetWeekMapStar"), (res) =>
        {
            if (!res.Error)
            {
                try
                {
                    ExecuteInMainThread(() => Callback(res, JsonConvert.DeserializeObject<List<Map>>(res.APIResponse)));
                }
                catch (Exception ex)
                {
                    res.LocalErrorMessage = ex.Message;
                    ExecuteInMainThread(() => Callback(res, null));
                }
            }
            else
                ExecuteInMainThread(() => Callback(res, null));
        });
    }

    public static void GetMonthMapStar(Action<APIResult, List<Map>> Callback)
    {
        DownloadAsyncString(route("GetMonthMapStar"), (res) =>
        {
            if (!res.Error)
            {
                try
                {
                    ExecuteInMainThread(() => Callback(res, JsonConvert.DeserializeObject<List<Map>>(res.APIResponse)));
                }
                catch (Exception ex)
                {
                    res.LocalErrorMessage = ex.Message;
                    ExecuteInMainThread(() => Callback(res, null));
                }
            }
            else
                ExecuteInMainThread(() => Callback(res, null));
        });
    }
    #endregion

    #region Lobby
    public static void PlayAnnimation(string animationName)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_PlayAnimation).Set(animationName));
    }

    public static void DownloadLobby(int lobbyID, Action<APIResult> Callback)
    {
        Uri uri = route("DownloadLobby", param("ID", lobbyID));
        WebClient client = new WebClient();
        APIResult res = new APIResult();
        GridManager.CleanFolder();
        Thread t = new Thread(() =>
        {
            try
            {
                string gridPath = GridManager.GetGridPath();
                string tmpgridPath = GridManager.GetGridPath() + "_" + Guid.NewGuid().ToStr();
                client.DownloadFile(uri.AbsoluteUri, tmpgridPath);
                if (File.Exists(tmpgridPath))
                {
                    File.Copy(tmpgridPath, gridPath, true);
                    File.Delete(tmpgridPath);
                    res.Error = false;
                    res.APIResponse = "OK";
                }
                else
                {
                    res.Error = true;
                    res.LocalErrorMessage = "Download failed.";
                }
            }
            catch (Exception ex)
            {
                res.Error = true;
                res.LocalErrorMessage = ex.Message;
            }
            ExecuteInMainThread(() => Callback(res));
        });
        t.Start();
    }

    public static void SetOrientation(Orientation dir)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_SetOrientation).Set((byte)dir));
    }

    public static void SpeakLobby(string text)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_Speak).Set(text));
    }

    public static void SpeakEmote(EmoteType type, int emoteID)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_Emote).Set((byte)type).Set((short)emoteID));
    }

    public static void GetClientsOnLobby(int lobbyID, Action<bool, List<LitePlayerSave>> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_SendMeClientsOnLobby).Set(lobbyID), (res) =>
       {
           bool success = false;
           CurrentMessage.Get(ref success);
           List<LitePlayerSave> clients = new List<LitePlayerSave>();
           string rep = string.Empty;
           if (success)
               CurrentMessage.GetObject<List<LitePlayerSave>>(ref clients);
           Callback?.Invoke(success, clients);
       });
    }

    public static void EnterLobby(int lobbyID, CellPos pos, Action<bool, LitePlayerSave> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_EnterLobby).Set(lobbyID).Set((short)pos.X).Set((short)pos.Y), (res) =>
        {
            bool arg1 = false;
            LitePlayerSave arg2 = null;
            CurrentMessage.Get(ref arg1);
            CurrentMessage.GetObject(ref arg2);
            Callback?.Invoke(arg1, arg2);
        });
    }

    public static void ChangeCell(CellPos pos)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_ChangeCell).Set((short)pos.X).Set((short)pos.Y));
    }

    public static void LeaveLobby(Action<bool> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_LeaveLobby), (res) =>
        {
            bool retVal = false;
            CurrentMessage.Get(ref retVal);
            Callback?.Invoke(retVal);
        });
    }

    public static void GiveMeLobbiesList(Action<List<ViewLobby>> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_GiveMeLobbiesList), (res) =>
       {
           List<ViewLobby> lobbies = new List<ViewLobby>();
           CurrentMessage.GetObject<List<ViewLobby>>(ref lobbies);
           Callback?.Invoke(lobbies);
       });
    }
    #endregion

    #region Vote
    public static void HasVote(string mapID, int userID, Action<bool, bool> Callback)
    {
        DownloadAsyncString(route("HasVote", param("mapID", mapID), param("userID", userID)), (res) =>
        {
            bool vote = false;
            bool success = bool.TryParse(res.APIResponse, out vote);
            Callback?.Invoke(success, vote);
        });
    }

    public static void Vote(string mapID, int userID, VoteType type, Action<bool> Callback)
    {
        DownloadAsyncString(route("Vote", param("mapID", mapID), param("userID", userID), param("type", (int)type)), (res) =>
        {
            res.Error = res.Error | res.APIResponse != "OK";
            Callback?.Invoke(!res.Error);
        });
    }
    #endregion

    #region Score
    public static void AddScore(string mapID, long time, int turn, Action<bool, string, bool> Callback)
    {
        DownloadAsyncString(route("AddScore", param("mapID", mapID), param("userID", CurrentUser.ID), param("userName", CurrentUser.Name), param("time", time), param("turns", turn)), (res) =>
        {
            res.Error = res.Error | (res.APIResponse != "OK" && res.APIResponse != "WINXP");
            Callback?.Invoke(!res.Error, res.APIResponse, res.APIResponse == "WINXP");
        });
    }

    public static void GetScores(string mapID, int limite, Action<bool, List<Score>> Callback)
    {
        DownloadAsyncString(route("GetScores", param("mapID", mapID), param("limite", limite)), (res) =>
        {
            List<Score> scores = null;
            if (!res.Error)
            {
                try
                {
                    scores = JsonConvert.DeserializeObject<List<Score>>(res.APIResponse);
                }
                catch (Exception ex)
                {
                    res.Error = true;
                    res.LocalErrorMessage = ex.Message;
                }
            }
            Callback?.Invoke(!res.Error, scores);
        });
    }
    #endregion

    #region Social
    public static void AddFriend(int friendID, Action<bool> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_AddFriend).Set(friendID), (res) =>
        {
            bool retVal = false;
            CurrentMessage.Get(ref retVal);
            Callback?.Invoke(retVal);
        });
    }

    public static void RemoveFriend(int friendID, Action<bool> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_RemoveFriend).Set(friendID), (res) =>
        {
            bool retVal = false;
            CurrentMessage.Get(ref retVal);
            Callback?.Invoke(retVal);
        });
    }

    public static void GetFriends(Action<bool, List<SocialPlayer>> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_GetFriends), (res) =>
        {
            bool success = false;
            CurrentMessage.Get(ref success);
            List<SocialPlayer> friends = new List<SocialPlayer>();
            if (success)
                CurrentMessage.GetObject<List<SocialPlayer>>(ref friends);
            Callback?.Invoke(success, friends);
        });
    }
    #endregion

    #region Player save
    public static void SetState(PlayerState state)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_SetState).Set((byte)state));
    }

    public static void GetLiteHero(int userID, Action<bool, LitePlayerSave, string> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_GetLiteHero).Set(userID), (res) =>
        {
            bool success = false;
            CurrentMessage.Get(ref success);
            LitePlayerSave client = null;
            string rep = string.Empty;
            if (success)
                CurrentMessage.GetObject<LitePlayerSave>(ref client);
            else
                CurrentMessage.Get(ref rep);
            Callback?.Invoke(success, client, rep);
        });
    }

    public static void EquipSkin(SkinType skinType, int SkinID)
    {
        SendMessage(new NetworkMessage(HeadActions.toServer_EquipSkin).Set(ID).Set((byte)skinType).Set((short)SkinID));
    }

    public static void UnlockSkin(SkinType skinType, int SkinID, int OnlineRewardIndex, Action<bool, string> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_UnlockSkin).Set(ID)
            .Set((byte)skinType).Set((short)SkinID).Set((short)OnlineRewardIndex), (res) =>
            {
                bool success = false;
                CurrentMessage.Get(ref success);
                string rep = string.Empty;
                CurrentMessage.Get(ref rep);
                Callback?.Invoke(success, rep);
            });
    }

    public static void AddXP(int xp, Action<bool, string> Callback)
    {
        int lastxp = SaveManager.CurrentSave.XP;
        SaveManager.CurrentSave.XP += xp;

        SetXP(SaveManager.CurrentSave.XP, (success, errorMess) =>
        {
            if (success)
                Events.Fire_WinXP(xp);
            else
                SaveManager.CurrentSave.XP = lastxp;
            Callback?.Invoke(success, errorMess);
        });
    }

    public static void SetXP(int XP, Action<bool, string> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_SetXP).Set(ID).Set(XP), (res) =>
        {
            bool success = false;
            CurrentMessage.Get(ref success);
            string rep = string.Empty;
            CurrentMessage.Get(ref rep);
            Callback?.Invoke(success, rep);
        });
    }

    private static void RefreshPlayerSave()
    {
        PlayerSave save = null;
        CurrentMessage.GetObject<PlayerSave>(ref save);
        SaveManager.SetSave(save);
    }
    #endregion

    #region Accounts
    public static void ConnectAccount(string userName, string pass, Action<NetworkMessage, bool> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_ConnectUser).Set(userName).Set(pass), (res) =>
       {
           bool ok = false;
           CurrentMessage.Get(ref ok);
           if (ok)
           {
               GameClient user = null;
               CurrentMessage.GetObject<GameClient>(ref user);
               ID = user.ID;
               CurrentUser = user;
               LoginState = LoginState.LoggedIn;
               PlayerPrefs.SetString("Username", user.Name);
               Events.Fire_UserLogin();
               SaveManager.SetSave(user.PlayerSave);
           }
           Callback?.Invoke(res, ok);
       });
    }

    public static void CreateAccount(string userName, string pass, string mail, Action<NetworkMessage> Callback)
    {
        AskForReply(new NetworkMessage(HeadActions.toServer_CreateUser).Set(userName).Set(pass).Set(mail).SetObject(SaveManager.GetDefaulSave()), Callback);
    }

    public static void DeconnectUser()
    {
        CurrentUser = null;
        LoginState = LoginState.LoggedOut;
    }
    #endregion

    #region Online
    public static void CheckIfOnline(Action<OnlineState> Callback)
    {
        DodgeFromLight.UI_WorkerNotifier.Show("Conneting to server...");
        Thread thread = new Thread(() =>
        {
            try
            {
                Start(ConfigurationManager.Config.AdresseIP, ConfigurationManager.Config.Port);
                OnlineState = OnlineState.Online;
            }
            catch (Exception ex)
            {
                OnlineState = OnlineState.Offline;
                string err = ex.Message;
                ExecuteInMainThread(() =>
                {
                    DodgeFromLight.UI_Notifications.Notify(err);
                    Debug.Log(err);
                });
            }

            ExecuteInMainThread(() =>
            {
                DodgeFromLight.UI_WorkerNotifier.Hide();
                Callback(OnlineState);
            });
        });
        thread.Start();
    }
    #endregion

    #region Utils
    public static void ExecuteInMainThread(Action Callback)
    {
        Client.ExecuteInMainThread(Callback);
    }

    private static void DownloadAsyncString(Uri uri, Action<APIResult> Callback)
    {
        WebClient client = new WebClient();
        client.DownloadStringCompleted += (s, e) =>
        {
            ExecuteInMainThread(() => Callback?.Invoke(result(e)));
        };
        client.DownloadStringAsync(uri);
    }

    private static Uri route(string routeName, params APIRequestParameter[] param)
    {
        return new APIBuilder(routeName).Add(param).GetRoute(APIConfiguration.APIURL);
    }

    private static APIRequestParameter param(string key, object value)
    {
        return new APIRequestParameter(key, value);
    }

    private static APIResult result(DownloadStringCompletedEventArgs res)
    {
        APIResult result = new APIResult();
        if (res.Error != null)
        {
            result.Error = true;
            result.LocalErrorMessage = res.Error.Message;
            try
            {
                result.APIErrorMessage = res.Result;
            }
            catch (Exception ex)
            {
                result.APIErrorMessage = ex.Message;
            }
        }
        else
            result.APIResponse = res.Result;
        return result;
    }

    private static FormFile filePNG(string name, string key, string path)
    {
        return new FormFile(name, "image/png", path, key);
    }

    private static FormFile fileJSON(string name, string key, string path)
    {
        return new FormFile(name, "application/json", path, key);
    }
    #endregion
}

public enum OnlineState
{
    NotChecked,
    Online,
    Offline
}

public enum LoginState
{
    LoggedIn,
    LoggedOut
}

public class APIConfiguration
{
    public string APIURL { get; set; }
    public string MapsURL { get; set; }
}

public class APIResult
{
    public bool Error;
    public string LocalErrorMessage;
    public string APIErrorMessage;
    public string APIResponse;

    public APIResult()
    {
        Error = false;
    }

    public APIResult(string localError, string APIError)
    {
        Error = true;
        LocalErrorMessage = localError;
        APIErrorMessage = APIError;
    }

    public APIResult(string APIError, bool error = false)
    {
        Error = error;
        APIErrorMessage = APIError;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        if (Error)
        {
            sb.Append("ERROR : ").AppendLine(APIResponse)
                .AppendLine("Local error : " + LocalErrorMessage)
                .AppendLine("API error : " + APIErrorMessage);
        }
        else
        {
            sb.Append("SUCCESS : ").AppendLine(APIResponse);
        }
        return sb.ToString();
    }
}

public class Map
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

    public bool IsFinished()
    {
        return AlreadyFinished == 1;
    }

    public Map(string name, int width, int height)
    {
        ID = Guid.NewGuid().ToStr();
        Name = name;
        Width = width;
        Height = height;
        Likes = 0;
        Dislikes = 0;
        State = MapState.InWork;
        UploadDate = DateTime.Now.ToShortDateString();
        if (DFLClient.CurrentUser != null)
            SetAuthor(DFLClient.CurrentUser);
        else
        {
            Author = "Me";
            AuthorID = -1;
        }
    }

    public void SetState(MapState state)
    {
        State = state;
    }

    public void SetAuthor(GameClient user)
    {
        Author = user.Name;
        AuthorID = user.ID;
    }
}

public class FullMap
{
    public Grid Grid;
    public Map Map;

    public FullMap(Grid grid, Map map)
    {
        Grid = grid;
        Map = map;
    }
}

public enum MapState
{
    Locked = 0, // system and downloaded maps
    InWork = 1, // My maps in progress
}

public class APIBuilder
{
    public List<APIRequestParameter> Params;
    public string APIRoute;

    public APIBuilder(string route)
    {
        APIRoute = route;
        Params = new List<APIRequestParameter>();
    }

    public APIBuilder Add(APIRequestParameter param)
    {
        Params.Add(param);
        return this;
    }

    public APIBuilder Add(APIRequestParameter[] param)
    {
        Params.AddRange(param);
        return this;
    }

    public Uri GetRoute(string APIBaseURL)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(APIBaseURL).Append("?").Append(APIRoute);
        for (int i = 0; i < Params.Count; i++)
            sb.Append("&").Append(Params[i].Key).Append("=").Append(Params[i].Value);
        return new Uri(sb.ToString());
    }
}

public class APIRequestParameter
{
    public string Key;
    public string Value;

    public APIRequestParameter(string key, object value)
    {
        Key = key;
        Value = value.ToString();
    }

    public override string ToString()
    {
        return Key + "=" + Value;
    }
}

public class FormFile
{
    public string Name { get; set; }

    public string ContentType { get; set; }

    public string FilePath { get; set; }

    public string Key { get; set; }

    public FormFile(string name, string content, string path, string key)
    {
        Name = name;
        ContentType = content;
        FilePath = path;
        Key = key;
    }
}