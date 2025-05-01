using System;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace AccountManagementPlugin
{
    [HarmonyPatch]
    public class MyDatabasePatch
    {
        [HarmonyPatch(typeof(RedGlobal), "OnNetworkSpawn")]
        class OnNetworkSpawnPatch
        {
            static void Postfix()
            {
                new MyDatabase();
            }
        }
    }

    public class MyDatabase
    {
        public MyDatabase()
        {
            this.idDatabase = MyDatabase.IdDatabase.SQLITE;
            MyDatabase.dbName = "URI=file:" + Application.dataPath + "/GreyHackDB.db;";
            MyDatabase.Singleton = this;
        }

        private MyDatabase.IGreyDB Config(string query)
        {
            return new MyDatabase.ConfigSqlite(query, MyDatabase.dbName);
        }

        public void RemoveBankFromPlayer(PlayerComputer pc)
        {
            BankSubs.Singleton.RemovePayments(pc.GetPlayerID());
            FieldInfo fieldInfo = AccessTools.Field(typeof(PlayerConfigOS), "userBank");
            fieldInfo.SetValue(pc.GetPlayerConfigOS(), null);
            Database.Singleton.SyncConfigOS(pc);
            string text = "UPDATE Players SET BankUser = @bankUser Where PlayerID = @playerID;";
            using (MyDatabase.IGreyDB greyDB = this.Config(text))
            {
                greyDB.AddParameter("@playerID", pc.GetPlayerID());
                greyDB.AddParameter("bankUser", "");
                greyDB.ExecuteNonQuery();
            }
        }

        public void RemoveMailFromPlayer(PlayerComputer pc)
        {
            FieldInfo fieldInfo = AccessTools.Field(typeof(PlayerConfigOS), "userMail");
            fieldInfo.SetValue(pc.GetPlayerConfigOS(), null);
            Database.Singleton.SyncConfigOS(pc);
        }

        public static MyDatabase Singleton;
        public static string dbName;
        private MyDatabase.IdDatabase idDatabase;

        public enum IdDatabase
        {
            MYSQL,
            SQLITE
        }

        private interface IGreyReader
        {
            bool Read();
            string GetString(int i);
            int GetInt32(int i);
            float GetFloat(int i);
            void Close();
        }

        private class WrapperSqliteReader : MyDatabase.IGreyReader
        {
            public WrapperSqliteReader(IDataReader reader)
            {
                this.reader = reader;
            }

            public bool Read()
            {
                return this.reader.Read();
            }

            public string GetString(int i)
            {
                return this.reader.GetString(i);
            }

            public int GetInt32(int i)
            {
                return this.reader.GetInt32(i);
            }

            public float GetFloat(int i)
            {
                return this.reader.GetFloat(i);
            }

            public void Close()
            {
                this.reader.Close();
            }

            private IDataReader reader;
        }

        private interface IGreyDB : IDisposable
        {
            MyDatabase.IGreyReader ExecuteReader();
            MyDatabase.IGreyReader ExecuteReader(string query);
            object ExecuteScalar();
            object ExecuteScalar(string query);
            void ExecuteNonQuery();
            void ExecuteNonQuery(string nonQuery);
            void AddParameter(string name, string value);
            void AddParameter(string name, int value);
            void AddParameter(string name, float value);
            bool ExistValue();
            void ClearParameters();
        }

        private class ConfigSqlite : MyDatabase.IGreyDB, IDisposable
        {
            public ConfigSqlite(string query, string dbName)
            {
                this.connection = new SqliteConnection(dbName);
                this.connection.Open();
                this.command = this.connection.CreateCommand();
                this.command.CommandText = query;
            }

            public MyDatabase.IGreyReader ExecuteReader()
            {
                if (this.reader != null)
                {
                    this.reader.Close();
                }
                this.reader = this.command.ExecuteReader();
                return new MyDatabase.WrapperSqliteReader(this.reader);
            }

            public MyDatabase.IGreyReader ExecuteReader(string query)
            {
                if (this.reader != null)
                {
                    this.reader.Close();
                }
                this.command.CommandText = query;
                return this.ExecuteReader();
            }

            public object ExecuteScalar()
            {
                return this.command.ExecuteScalar();
            }

            public object ExecuteScalar(string query)
            {
                if (this.reader != null)
                {
                    this.reader.Close();
                }
                this.command.CommandText = query;
                return this.ExecuteScalar();
            }

            public void ExecuteNonQuery()
            {
                this.command.ExecuteNonQuery();
            }

            public void ExecuteNonQuery(string nonQuery)
            {
                this.command.CommandText = nonQuery;
                this.command.ExecuteNonQuery();
            }

            public void AddParameter(string name, string value)
            {
                this.command.Parameters.Add(new SqliteParameter(name, value));
            }

            public void AddParameter(string name, int value)
            {
                this.command.Parameters.Add(new SqliteParameter(name, value));
            }

            public void AddParameter(string name, float value)
            {
                this.command.Parameters.Add(new SqliteParameter(name, value));
            }

            public bool ExistValue()
            {
                return Convert.ToInt32(this.command.ExecuteScalar()) >= 1;
            }

            public void ClearParameters()
            {
                this.command.Parameters.Clear();
            }

            public void Dispose()
            {
                if (this.reader != null)
                {
                    this.reader.Dispose();
                    this.reader = null;
                }
                if (this.command != null)
                {
                    this.command.Dispose();
                    this.command = null;
                }
                if (this.connection != null)
                {
                    this.connection.Dispose();
                    this.connection = null;
                }
                GC.SuppressFinalize(this);
            }

            private IDbConnection connection;
            private IDbCommand command;
            private IDataReader reader;
        }
    }
}