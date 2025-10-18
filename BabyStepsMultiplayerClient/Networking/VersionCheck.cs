using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    public class VersionCheck
    {
        public static Version GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
        public static async Task<string> GetLatestTagAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"BBSMMClient/{GetAssemblyVersion().ToString()}");

            var response = await client.GetStringAsync("https://api.github.com/repos/caleborchard/Baby-Steps-Multiplayer-Mod-Client/tags");

            using var doc = JsonDocument.Parse(response);
            var latestTag = doc.RootElement.EnumerateArray()
                .Select(tag => tag.GetProperty("name").GetString())
                .FirstOrDefault();
            return latestTag;
        }
        public static async void CheckForUpdateAsync()
        {
            try
            {
                var latestTag = await GetLatestTagAsync();
                var assemblyVersion = GetAssemblyVersion();

                if (Version.TryParse(latestTag, out var latestVersion))
                {
                    if (assemblyVersion < latestVersion)
                    {
                        Core.uiManager.notificationsUI
                            .AddMessage("You are on an outdated client version! Please update to connect to the official server!", 10f, Color.red);
                    }
                    else
                    {
                        Core.uiManager.notificationsUI
                            .AddMessage("Your client is up to date!", 3f, Color.green);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking for update: {ex.Message}");
            }
        }
    }
}
