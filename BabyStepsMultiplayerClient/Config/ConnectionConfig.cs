using MelonLoader;

namespace BabyStepsMultiplayerClient.Config
{
    public class ConnectionConfig : ConfigCategory
    {
        internal MelonPreferences_Entry<string> Address;
        internal MelonPreferences_Entry<int> Port;
        internal MelonPreferences_Entry<string> Password;

        public override eConfigType ConfigType
            => eConfigType.Connection;
        public override string ID
            => "Connection";
        public override string DisplayName
            => "Connection";

        public override void CreatePreferences()
        {
            Address = CreatePref("address",
                "IP Address",
                "IP Address",
                "127.0.0.1");

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
