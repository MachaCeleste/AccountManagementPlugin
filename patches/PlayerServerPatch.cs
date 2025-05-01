using AccountManagementPlugin;
using CompressionTools;
using HarmonyLib;
using NetworkMessages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

[HarmonyPatch]
public class PlayerServerPatch
{
    [HarmonyPatch(typeof(PlayerServer), "ProcessReceivedData")]
    class ProcessReceivedDataPatch
    {
        static bool Prefix(PlayerServer __instance, ref MessageServer m)
        {
            try
            {
                switch ((CustomIdServer)m.ID)
                {
                    case CustomIdServer.ResetAccountPasswordServerRpc:
                        ResetAccountPasswordServerRpc(__instance, m.GetInt(), m.GetByte());
                        return false;
                    case CustomIdServer.DeleteAccountServerRpc:
                        DeleteAccountServerRpc(__instance, m.GetString(), m.GetInt());
                        return false;
                    default:
                        break;
                }
            }
            catch (Exception message)
            {
                Plugin.Logger.LogError(message);
            }
            return true;
        }
    }

    private static void ResetAccountPasswordServerRpc(PlayerServer player, int windowPID, byte[] zipInfo)
    {
        var _kvp = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(GCompressor.Unzip(zipInfo));
        var pc = player.GetComputer();
        string account = _kvp.Key;
        string newPassword = _kvp.Value;
        string encPassword = PasswordsEncrypter.Encrypt(newPassword);
        if (account.Contains("@"))
        {
            if (account != pc.GetMailAddress() || !pc.isPlayer)
            {
                CloseConnection(player, windowPID, "Error: You may only change the email password of your owned account.");
                return;
            }
            if (!Util.OS.IsAlphaNumeric(newPassword))
            {
                CloseConnection(player, windowPID, "Error: Password must be alphanumeric.");
                return;
            }
            if (newPassword.Length < 3)
            {
                CloseConnection(player, windowPID, "Error: Password must be at least 3 characters.");
                return;
            }
            if (newPassword.Length > 32)
            {
                CloseConnection(player, windowPID, "Error: Password limited to 32 characters.");
                return;
            }
            var mail = Database.Singleton.GetMailAccount(account, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                CloseConnection(player, windowPID, $"Error: {error}");
                return;
            }
            if (mail.plainPassword == newPassword)
            {
                CloseConnection(player, windowPID, "Error: New password cannot be the same as old password.");
                return;
            }
            mail.plainPassword = newPassword;
            mail.encPassword = encPassword;
            Database.Singleton.UserMailUpdate(mail);
            pc.GetPlayerConfigOS().SetMailAccount(account, newPassword, encPassword);
        }
        else
        {
            var bank = Database.Singleton.GetBankAccount(account, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                CloseConnection(player, windowPID, $"Error: {error}");
                return;
            }
            if (account != pc.GetUserBank().userName || !bank.isPlayer)
            {
                CloseConnection(player, windowPID, "Error: You may only change the bank password of your owned account.");
                return;
            }
            if (!Util.OS.IsAlphaNumeric(newPassword))
            {
                CloseConnection(player, windowPID, "Error: Password must be alphanumeric.");
                return;
            }
            if (newPassword.Length < 3)
            {
                CloseConnection(player, windowPID, "Error: Password must be at least 3 characters.");
                return;
            }
            if (newPassword.Length > 65535)
            {
                CloseConnection(player, windowPID, "Error: Password limited to 65535 alphanumeric characters.");
                return;
            }
            bank.password = newPassword;
            Database.Singleton.SyncBankAccount(bank);
            pc.GetPlayerConfigOS().SetBankAccount(account, newPassword, encPassword);
        }
        Database.Singleton.AddPassword(encPassword, newPassword);
        Database.Singleton.SyncConfigOS(pc);
        CloseConnection(player, windowPID, $"{account}\nPassword changed successfully!");
    }

    private static void DeleteAccountServerRpc(PlayerServer player, string account, int windowPID)
    {
        var pc = player.GetComputer();
        if (account.Contains("@"))
        {
            if (account != pc.GetMailAddress() || !pc.isPlayer)
            {
                CloseConnection(player, windowPID, "Error: You may only delete your owned email account.");
                return;
            }
            var mail = Database.Singleton.GetMailAccount(account, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                CloseConnection(player, windowPID, $"Error: {error}");
                return;
            }
            mail.playerPcID = "";
            Database.Singleton.UserMailUpdate(mail);
            MyDatabase.Singleton.RemoveMailFromPlayer(player.GetComputer());
        }
        else
        {
            var bank = Database.Singleton.GetBankAccount(account, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                CloseConnection(player, windowPID, $"Error: {error}");
                return;
            }
            if (account != pc.GetUserBank().userName || !bank.isPlayer)
            {
                CloseConnection(player, windowPID, "Error: You may only delete your owned bank account.");
                return;
            }
            MyDatabase.Singleton.RemoveBankFromPlayer(player.GetComputer());
        }
        CloseConnection(player, windowPID, "Account removed from player.");
    }

    private static void CloseConnection(PlayerServer player, int terminalPID, string message, bool ignoreWarning = false)
    {
        MessageClient messageClient = new MessageClient(IdClient.CloseConnectionClientRpc);
        messageClient.AddInt(terminalPID);
        messageClient.AddString(message);
        messageClient.AddBool(ignoreWarning);
        player.SendData(messageClient);
    }
}