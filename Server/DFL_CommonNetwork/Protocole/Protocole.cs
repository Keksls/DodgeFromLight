using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DFLNetwork.Protocole
{
    public class NetworkMessage
    {
        public int ClientID { get; set; }
        public int ReplyID { get; set; }
        public HeadActions Head { get; set; }
        public List<MessageBlockData> Blocks { get; set; }
        public TcpClient TcpClient { get; set; }
        public byte[] Data { get; set; }
        public int Length { get; set; }
        private int currentReadingIndex = 0;

        public void ReplyTo(int id)
        {
            ReplyID = id;
        }

        public NetworkMessage(HeadActions head, int clientID)
        {
            Head = head;
            ClientID = clientID;
            Length = 0;
            Blocks = new List<MessageBlockData>();
        }

        public NetworkMessage(int clientID)
        {
            Head = HeadActions.None;
            ClientID = clientID;
            Length = 0;
            Blocks = new List<MessageBlockData>();
        }

        public NetworkMessage()
        {
            Head = HeadActions.None;
            Length = 0;
            Blocks = new List<MessageBlockData>();
        }

        public NetworkMessage(HeadActions head)
        {
            Head = head;
            Length = 0;
            Blocks = new List<MessageBlockData>();
        }

        /// <summary>
        /// Get ClientID, HeadAction and Data from msg byte[]
        /// </summary>
        /// <param name="msg"></param>
        public NetworkMessage(byte[] msg, TcpClient client)
        {
            TcpClient = client;
            ClientID = BitConverter.ToInt32(msg, 0);
            Head = (HeadActions)BitConverter.ToInt16(msg, 4);
            ReplyID = BitConverter.ToInt32(msg, 6);
            Data = msg;
            RestartRead();
        }

        /// <summary>
        /// Get ClientID, HeadAction and Data from msg byte[]
        /// </summary>
        /// <param name="msg"></param>
        public NetworkMessage(byte[] msg)
        {
            TcpClient = null;
            ClientID = BitConverter.ToInt32(msg, 0);
            Head = (HeadActions)BitConverter.ToInt16(msg, 4);
            ReplyID = BitConverter.ToInt32(msg, 6);
            Data = msg;
            RestartRead();
        }

        #region Reading
        public void RestartRead()
        {
            currentReadingIndex = 10; // start + ClientID + Head + ReplyID
        }

        public void Get(ref byte val)
        {
            val = Data[currentReadingIndex];
            currentReadingIndex++;
        }

        public void Get(ref short val)
        {
            val = BitConverter.ToInt16(Data, currentReadingIndex);
            currentReadingIndex += 2;
        }

        public void Get(ref int val)
        {
            val = BitConverter.ToInt32(Data, currentReadingIndex);
            currentReadingIndex += 4;
        }

        public void Get(ref long val)
        {
            val = BitConverter.ToInt64(Data, currentReadingIndex);
            currentReadingIndex += 8;
        }

        public void Get(ref ushort val)
        {
            val = BitConverter.ToUInt16(Data, currentReadingIndex);
            currentReadingIndex += 2;
        }

        public void Get(ref uint val)
        {
            val = BitConverter.ToUInt32(Data, currentReadingIndex);
            currentReadingIndex += 4;
        }

        public void Get(ref ulong val)
        {
            val = BitConverter.ToUInt64(Data, currentReadingIndex);
            currentReadingIndex += 8;
        }

        public void Get(ref float val)
        {
            val = BitConverter.ToSingle(Data, currentReadingIndex);
            currentReadingIndex += 4;
        }

        public void Get(ref bool val)
        {
            val = BitConverter.ToBoolean(Data, currentReadingIndex);
            currentReadingIndex += 1;
        }

        public void Get(ref char val)
        {
            val = BitConverter.ToChar(Data, currentReadingIndex);
            currentReadingIndex += 2;
        }

        public void Get(ref string val)
        {
            int size = 0;
            Get(ref size);
            val = System.Text.Encoding.Default.GetString(Data, currentReadingIndex, size);
            currentReadingIndex += size;
        }

        /// <summary>
        /// Must be called only once by message and alwayse at the end
        /// </summary>
        /// <param name="val">serializable object (will be json and byte[] by Default))</param>
        public void GetObject<T>(ref T val)
        {
            string json = "";
            Get(ref json);
            val = JsonConvert.DeserializeObject<T>(json);
        }
        #endregion

        #region Set Data
        public NetworkMessage Set(byte val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetByte(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(short val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetShort(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(int val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetInt(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(long val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetLong(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(ushort val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetUShort(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(uint val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetUInt(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(ulong val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetULong(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(float val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetFloat(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(bool val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetBool(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(char val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetChar(val);
            Blocks.Add(block);
            return this;
        }

        public NetworkMessage Set(string val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetString(val);
            Blocks.Add(block);
            return this;
        }

        /// <summary>
        /// Must be called only once by message and alwayse at the end
        /// </summary>
        /// <param name="val">serializable object (will be json and byte[] by Default))</param>
        public NetworkMessage SetObject(object val)
        {
            MessageBlockData block = new MessageBlockData();
            block.SetObject(val);
            Blocks.Add(block);
            return this;
        }

        /// <summary>
        /// Tram Definition :
        ///     - FullMessageSize : Int32   4 bytes
        ///     - ClientID :        Int32   4 bytes
        ///     - HeadAction :      Int16   2 bytes
        ///     - ReplyID :         Int32   4 bytes
        ///     - Data :            var     var
        ///     
        /// Data Definition :
        ///     - Primitive type, size by type => Function Enum to Size
        ///     - Custom or String : HEAD : size Int32 4 bytes  |   BODY : deserialize by type
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            // process Size
            Length = 14; // head size : Full Message Size + ClientID + HeadAction + ReplyID (4 + 4 + 2 + 4)
            foreach (MessageBlockData block in Blocks)
                Length += block.Size;

            // create full empty array
            Data = new byte[Length];
            // write size
            Array.Copy(BitConverter.GetBytes(Length - 4), 0, Data, 0, 4);
            // write Client ID
            Array.Copy(BitConverter.GetBytes(ClientID), 0, Data, 4, 4);
            // write Head Action
            Array.Copy(BitConverter.GetBytes((short)Head), 0, Data, 8, 2);
            // write Client ID
            Array.Copy(BitConverter.GetBytes(ReplyID), 0, Data, 10, 4);

            // Write Blocks
            Length = 14;
            foreach (MessageBlockData block in Blocks)
            {
                // using switch for better perfs
                switch (block.Type)
                {
                    // primitiv types, simply copy data block to message buffer
                    case MessageBlockType.Byte:
                    case MessageBlockType.Short:
                    case MessageBlockType.Int:
                    case MessageBlockType.Long:
                    case MessageBlockType.Float:
                    case MessageBlockType.Bool:
                    case MessageBlockType.Char:
                    case MessageBlockType.UShort:
                    case MessageBlockType.UInt:
                    case MessageBlockType.ULong:
                        Array.Copy(block.data, 0, Data, Length, block.Size);
                        Length += block.Size;
                        break;

                    // primitiv types, simply copy data block to message buffer
                    default:
                    case MessageBlockType.String:
                    case MessageBlockType.Custom:
                        // Write data size
                        byte[] sizeData = BitConverter.GetBytes(block.Size - 4);
                        Array.Copy(sizeData, 0, Data, Length, 4);
                        Array.Copy(block.data, 0, Data, Length + 4, block.Size - 4);
                        Length += block.Size;
                        break;
                }
            }

            return Data;
        }
        #endregion
    }

    public class MessageBlockData
    {
        public MessageBlockType Type;
        public byte[] data;
        public int Size;

        public void SetByte(byte val)
        {
            data = new byte[1] { val };
            Type = MessageBlockType.Byte;
            Size = 1;
        }

        public void SetShort(short val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.Short;
            Size = 2;
        }

        public void SetInt(int val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.Int;
            Size = 4;
        }

        public void SetLong(long val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.Long;
            Size = 8;
        }

        public void SetUShort(ushort val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.UShort;
            Size = 2;
        }

        public void SetUInt(uint val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.UInt;
            Size = 4;
        }

        public void SetULong(ulong val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.ULong;
            Size = 8;
        }

        public void SetFloat(float val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.Float;
            Size = 4;
        }

        public void SetBool(bool val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.Bool;
            Size = 1;
        }

        public void SetChar(char val)
        {
            data = BitConverter.GetBytes(val);
            Type = MessageBlockType.Char;
            Size = 2;
        }

        public void SetString(string val)
        {
            data = System.Text.Encoding.Default.GetBytes(val);
            Type = MessageBlockType.String;
            Size = data.Length + 4;
        }

        /// <summary>
        /// Must be called only once by message and alwayse at the end
        /// </summary>
        /// <param name="val">serializable object (will be json and byte[] by Default))</param>
        public void SetObject(object val)
        {
            data = System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(val));
            Type = MessageBlockType.Custom;
            Size = data.Length + 4;
        }
    }

    public enum MessageBlockType
    {
        Byte = 0,
        Short = 1,
        Int = 2,
        Long = 3,
        Float = 4,
        Bool = 5,
        Char = 6,
        String = 7,
        Custom = 8,
        UShort = 9,
        UInt = 10,
        ULong = 11
    }

    public enum HeadActions
    {
        None,
        DebugText,
        Recieved,
        toClient_Notify,
        toClient_Modal,

        // Users
        toServer_CreateUser,
        toServer_ConnectUser,
        toServer_GetLiteHero,
        toServer_SetXP,
        toServer_EquipSkin,
        toClient_EquipSkin,
        toServer_UnlockSkin,
        toClient_UnlockSkin,
        toClient_RefreshPlayerSave,
        toServer_SetState,

        // Social
        toServer_AddFriend,
        toServer_RemoveFriend,
        toServer_GetFriends,

        // GameClient
        toServer_GetGameClient,
        toServer_SetGameClient,

        // Lobby
        toServer_LeaveLobby,
        toClient_LeaveLobby,
        toServer_EnterLobby,
        toClient_EnterLobby,
        toServer_SendMeClientsOnLobby,
        toServer_ChangeCell,
        toClient_ChangeCell,
        toServer_GiveMeLobbiesList,
        toServer_SetOrientation,
        toClient_SetOrientation,
        toServer_TryJoinPlayer,
        toClient_ForceEnterLobby,
        toClient_AskEnterHub,
        toServer_AwnserEnterHub,
        toClient_AwnserEnterHub,
        toServer_PlayAnimation,
        toClient_PlayAnimation,

        // Console
        toServer_Speak,
        toClient_Speak,
        toServer_Emote,
        toClient_Emote,

        ConnectionLost
    }
}