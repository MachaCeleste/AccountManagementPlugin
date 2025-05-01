using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

[HarmonyPatch]
public class DatabasePatch
{
    [HarmonyPatch(typeof(Database), "UserMailUpdate")]
    class UserMailUpdatePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string str && str.Contains("UPDATE MailAccounts SET Mails=@serializedMails WHERE User=@mailAdress;"))
                {
                    codes[i].operand = "UPDATE MailAccounts SET Mails=@serializedMails, Password=@password WHERE User=@mailAdress;";
                }
            }
            return codes;
        }
    }
}