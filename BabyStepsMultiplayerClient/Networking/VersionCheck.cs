using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    public class VersionCheck
    {
        public static Version GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
        public static string GetLatestTag()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"BBSMMClient/{GetAssemblyVersion().ToString()}");

            var response = client.GetStringAsync("https://api.github.com/repos/caleborchard/Baby-Steps-Multiplayer-Mod-Client/tags").Result;

            using var doc = JsonDocument.Parse(response);
            var latestTag = doc.RootElement.EnumerateArray()
                .Select(tag => tag.GetProperty("name").GetString())
                .FirstOrDefault();

            return latestTag;
        }
        public static void CheckForUpdate()
        {
            try
            {
                var latestTag = GetLatestTag();
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
