using MelonLoader;

namespace BabyStepsMultiplayerClient.Config
{
    public class ConnectionConfig : ConfigCategory
    {
        internal MelonPreferences_Entry<string> Address;
        internal MelonPreferences_Entry<int> Port;
        internal MelonPreferences_Entry<string> Password;
        internal MelonPreferences_Entry<string> HostPassword;
        internal MelonPreferences_Entry<string> PublicMainIpOverride;

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

            HostPassword = CreatePref("host_password",
                "Host Password",
                "Host Password",
                string.Empty);

            PublicMainIpOverride = CreatePref("public_main_ip",
                "Public Main IP Override",
                "Override the Public Main server IP shown in the lobby list. Leave blank to use the default (bbsmm.mooo.com).",
                string.Empty);
        }
    }
}
