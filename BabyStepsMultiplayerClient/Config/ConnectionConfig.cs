using MelonLoader;

namespace BabyStepsMultiplayerClient.Config
{
    public class ConnectionConfig : ConfigCategory
    {
        internal MelonPreferences_Entry<string> Address;
        internal MelonPreferences_Entry<int> Port;
        internal MelonPreferences_Entry<string> Password;

        public override string ID
            => "Connection";

        public override void CreatePreferences()
        {
            Address = CreatePref("address",
                "IP Address",
                "IP Address",
                "bbsmm.mooo.com");

            Port = CreatePref("port",
                "Port",
                "Port",
                7777);

            Password = CreatePref("password",
                "Password",
                "Password",
                string.Empty);
        }
    }
}
