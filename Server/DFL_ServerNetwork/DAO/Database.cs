using LiteDB;
using DFL.Utils;
using System;
using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;
using System.Linq;
using DFLServerNetwork.DAO.DAOClasses;
using System.Threading;
using System.IO;

namespace DFLServer
{
    public static class Database
    {
        public static ILiteCollection<GameClient> Users;
        public static ILiteCollection<Lobby> Lobbies;
        public static ILiteCollection<SocialRelation> SocialRelations;
        private static LiteDatabase db;

        public static void OpenDatabase()
        {
            Writer.Write_Database("Opening Database...", ConsoleColor.DarkYellow, false);
            db = new LiteDatabase(@"filename=" + Server.config.CurrentDatabaseFile + "; journal=false");
            Writer.Write("OK", ConsoleColor.Green);

            Writer.Write_Database("Loading Users... ", ConsoleColor.DarkYellow, false);
            Users = db.GetCollection<GameClient>("Users");
            Writer.Write(Users.Count().ToString(), ConsoleColor.Green);
            //Users.Delete(Query.All());

            Writer.Write_Database("Loading Social Relations... ", ConsoleColor.DarkYellow, false);
            SocialRelations = db.GetCollection<SocialRelation>("SocialRelations");
            Writer.Write(SocialRelations.Count().ToString(), ConsoleColor.Green);
            //Users.Delete(Query.All());

            Writer.Write_Database("Loading Lobbies... ", ConsoleColor.DarkYellow, false);
            Lobbies = db.GetCollection<Lobby>("Lobbies");
            //Lobbies.Delete(Query.All());
            //CreateLobby("Main Lobby", "", 100);
            Lobbies_Manager.LoadLobbies(Lobbies.FindAll().ToList());
            Lobbies_Manager.LoadHubs(Users.FindAll().ToList());
            Writer.Write(Lobbies_Manager.Lobbies.Count().ToString(), ConsoleColor.Green);
        }

        public static void AutoSaveDatabase()
        {
            int NbSaveToKeep = Server.config.DatabaseNbSaveToKeep;
            int SaveInterval = Server.config.DatabaseSaveInterval;
            SaveInterval = SaveInterval * 60 * 1000; // minutes

            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    string DatabaseFolder = Server.config.DatabaseFolder;
                    string saveFileName = DateTime.Now.Ticks + "_Database_save_" + DateTime.Now.ToString().Replace(":", "_").Replace("/", "_") + ".db";
                    string saveDbFile = DatabaseFolder + @"\" + saveFileName;
                    File.Copy(Server.config.CurrentDatabaseFile, saveDbFile, true);
                    Writer.Write_Database(saveFileName + " Saved.", ConsoleColor.Magenta);

                    string[] files = Directory.GetFiles(DatabaseFolder);
                    if (files.Length > NbSaveToKeep)
                    {
                        long minTicks = long.MaxValue;
                        string latestSaveFile = string.Empty;
                        foreach (string file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            long ticks = -1;
                            string[] spl = fileName.Split('_');
                            if (spl.Length > 0)
                            {
                                if (long.TryParse(spl[0], out ticks))
                                {
                                    // this is a save
                                    if (ticks < minTicks && ticks > -1)
                                    {
                                        minTicks = ticks;
                                        latestSaveFile = file;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(latestSaveFile))
                        {
                            File.Delete(latestSaveFile);
                            Writer.Write_Database(Path.GetFileName(latestSaveFile) + " deleted.", ConsoleColor.DarkMagenta);
                        }
                    }
                    Thread.Sleep(SaveInterval);
                }
            });
            thread.Start();
        }

        #region SocialRelations
        private static bool HasSocialRelations(int clientID)
        {
            return SocialRelations.Count(s => s.ClientID == clientID) > 0;
        }

        private static void AddSocialRelation(int clientID)
        {
            if (HasSocialRelations(clientID))
                return;
            SocialRelations.Insert(new SocialRelation() { ClientID = clientID });
        }

        public static SocialRelation GetSocialRelation(int clientID)
        {
            AddSocialRelation(clientID);
            return SocialRelations.FindOne(s => s.ClientID == clientID);
        }

        public static bool AddFriend(int clientID, int friendID)
        {
            var sr = GetSocialRelation(clientID);
            bool retVal = sr.AddFriend(friendID);
            SocialRelations.Update(sr);
            return retVal;
        }

        public static bool RemoveFriend(int clientID, int friendID)
        {
            var sr = GetSocialRelation(clientID);
            bool retVal = sr.RemoveFriend(friendID);
            SocialRelations.Update(sr);
            return retVal;
        }

        public static List<SocialPlayer> GetFriends(int clientID)
        {
            var sr = GetSocialRelation(clientID);
            return sr.GetFriends();
        }
        #endregion

        #region Player
        public static bool AddUser(string name, string pass, string mail, PlayerSave playerSave)
        {
            if (Users.Count(u => u.Name == name) == 0)
            {
                GameClient User = new GameClient
                {
                    Mail = mail,
                    Name = name,
                    Pass = pass,
                    PlayerSave = playerSave,
                    State = PlayerState.NotConnected
                };
                Users.Insert(User);
                return true;
            }
            else
                return false;
        }

        public static void UpdateUser(GameClient user)
        {
            Users.Update(user);
        }

        public static GameClient getUser(string name, string pass)
        {
            return Users.FindOne(u => u.Name == name && u.Pass == pass);
        }

        public static GameClient getUserByName(string name)
        {
            return Users.FindOne(u => u.Name == name);
        }

        public static GameClient getUser(int ID)
        {
            return Users.FindById(ID);
        }

        public static void SetState(int ID, PlayerState state)
        {
            SetState(getUser(ID), state);
        }

        public static void SetState(GameClient user, PlayerState state)
        {
            user.State = state;
            UpdateUser(user);
        }
        #endregion

        #region Lobbies
        public static bool CreateLobby(string name, string desc, int maxClients)
        {
            if (Lobbies.Count(l => l.Name == name) == 0)
            {
                Lobby lobby = new Lobby()
                {
                    Clients = new Dictionary<int, CellPos>(),
                    Description = desc,
                    MaxClients = maxClients,
                    Name = name
                };
                Lobbies.Insert(lobby);
                return true;
            }
            else
                return false;
        }

        public static bool RemoveLobby(string name)
        {
            if (Lobbies.Count(l => l.Name == name) > 0)
                return Lobbies.Delete(name);
            else
                return false;
        }
        #endregion
    }
}