using AccountManagementPlugin;
using CompressionTools;
using HarmonyLib;
using NetworkMessages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UI.Dialogs;
using UnityEngine;
using UnityEngine.UI;
using Util;
using static AccountsInfoPanel;

[HarmonyPatch]
public class AccountsInfoPanelPatch
{

    [HarmonyPatch(typeof(AccountsInfoPanel), "ResumeConnectionWindow")]
    class ResumeConnectionWindowPatch
    {
        static bool Prefix(AccountsInfoPanel __instance, ref string info)
        {
            foreach (KeyValuePair<AccountsInfoPanel.TypeAccount, HelperAccount> keyValuePair in JsonConvert.DeserializeObject<Dictionary<AccountsInfoPanel.TypeAccount, HelperAccount>>(info))
            {
                GameObject gameObject = Object.Instantiate<GameObject>(__instance.prefabItemAccount, __instance.parentAccounts, false);
                var ui = gameObject.GetComponent<ItemAccountUI>();
                ui.Configure(keyValuePair.Value.userName, keyValuePair.Value.password, keyValuePair.Key);
                if (keyValuePair.Key == TypeAccount.Bank || keyValuePair.Key == TypeAccount.Mail)
                {
                    var reset = MakeButton(gameObject, "Reset", "Password", new Vector2(400, -16));
                    reset.onClick.AddListener(() =>
                    {
                        PasswordResetUI(ui);
                    });
                    var delete = MakeButton(gameObject, "Delete", "Close", new Vector2(400, 18));
                    delete.onClick.AddListener(() =>
                    {
                        FieldInfo fieldInfo = AccessTools.Field(typeof(PlayerConfigOS), "userMail");
                        fieldInfo.SetValue(PlayerClient.Singleton.player.pc.GetPlayerConfigOS(), null);
                        MessageServer messageServer = new MessageServer((IdServer)CustomIdServer.DeleteAccountServerRpc);
                        messageServer.AddString(ui.account.text);
                        messageServer.AddInt(__instance.GetPID());
                        PlayerClient.Singleton.SendData(messageServer);
                    });
                }
                gameObject.SetActive(true);
            }
            return false;
        }

        private static Button MakeButton(GameObject parent, string name, string sprite, Vector2 offset)
        {
            var original = parent.transform.Find("Copy").gameObject;
            var resetPassword = Object.Instantiate<GameObject>(original, parent.transform, false);
            resetPassword.name = name;
            var rect = resetPassword.GetComponent<RectTransform>();
            rect.anchoredPosition = offset;
            var imageObj = resetPassword.transform.Find("Image");
            var oldImg = imageObj.GetComponent<Image>();
            var color = oldImg.color;
            Object.DestroyImmediate(oldImg);
            var img = imageObj.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = DataUtils.LoadSprite(sprite);
            var button = resetPassword.GetComponent<Button>();
            var oldColors = button.colors;
            Object.DestroyImmediate(button);
            var newButton = resetPassword.AddComponent<Button>();
            newButton.colors = oldColors;
            return newButton;
        }

        private static void PasswordResetUI(ItemAccountUI ui)
        {
            var account = ui.account.text;
            uDialog_TaskBar taskBar = Object.FindObjectOfType<uDialog_TaskBar>();
            var parent = GameObject.Find("ComputerCanvas/Desktop").GetComponent<RectTransform>();
            uDialog uDialog = uDialog.NewDialog("WifiConnection", parent);
            Object.DestroyImmediate(uDialog.GetComponent<NetConnDialog>());
            var dia = uDialog.gameObject.AddComponent<AccountManagerDialog>();
            dia.SetInfoAccount(account);
            uDialog.SetTitleText($"Change Password - {account}");
            uDialog.transform.Find("Dialog/Container/Viewport/Content/ConnectWifi").name = "ChangePassword";
            var labelPass = uDialog.transform.Find("Dialog/Container/Viewport/Content/ChangePassword/LabelName");
            labelPass.name = "PasswordLabel";
            labelPass.GetComponent<TextMeshProUGUI>().text = "Password:";
            var labelRetype = uDialog.transform.Find("Dialog/Container/Viewport/Content/ChangePassword/LabelPassword");
            labelRetype.gameObject.SetActive(false);
            var retype = uDialog.transform.Find("Dialog/Container/Viewport/Content/ChangePassword/Password");
            retype.gameObject.SetActive(false);
            var retypeInput = retype.GetComponent<TMP_InputField>();
            var pass = uDialog.transform.Find("Dialog/Container/Viewport/Content/ChangePassword/EssidName");
            pass.name = "Password";
            var passInput = pass.GetComponent<TMP_InputField>();
            passInput.inputType = TMP_InputField.InputType.Password;
            passInput.contentType = TMP_InputField.ContentType.Password;
            retypeInput.fontAsset = passInput.fontAsset;
            var buttonObj = uDialog.transform.Find("Dialog/Container/Viewport/Content/ChangePassword/Button");
            var button = buttonObj.GetComponent<Button>();
            button.transform.Find("Image").gameObject.SetActive(false);
            var buttonTextRect = button.transform.Find("TextMeshPro Text").GetComponent<RectTransform>();
            buttonTextRect.anchoredPosition = new Vector2(15, 0);
            button.GetComponentInChildren<TextMeshProUGUI>().text = "Submit";
            uDialog.AddToTaskBar(taskBar, true);
        }
    }

    public class AccountManagerDialog : Ventana
    {
        private string _account;
        public void SetInfoAccount(string account)
        {
            _account = account;
            SetupConfirm();
        }

        public void SetupConfirm()
        {
            var obj = base.dialogo.transform.Find("Dialog/Container/Viewport/Content/ConnectWifi/Button");
            var button = obj.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                CommitChange();
            });
        }

        public void CommitChange()
        {
            var passInput = base.dialogo.transform.Find("Dialog/Container/Viewport/Content/ChangePassword/Password").GetComponent<TMP_InputField>();
            if (string.IsNullOrEmpty(passInput.text))
            {
                OS.ShowError("Password cannot be blank!");
                return;
            }
            var zipInfo = GCompressor.Zip(JsonConvert.SerializeObject(new KeyValuePair<string, string>(this._account, passInput.text)));
            MessageServer messageServer = new MessageServer((IdServer)CustomIdServer.ResetAccountPasswordServerRpc);
            messageServer.AddByte(zipInfo);
            messageServer.AddInt(this.GetPID());
            PlayerClient.Singleton.SendData(messageServer);
        }
    }
}