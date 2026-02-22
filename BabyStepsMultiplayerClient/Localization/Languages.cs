namespace BabyStepsMultiplayerClient.Localization
{
    public static class LanguageNativeNames
    {
        public static readonly Dictionary<string, string> NativeNames = new()
        {
            { "English", "English" },
            { "Spanish", "Español" },
            { "French", "Français" },
            { "German", "Deutsch" }
        };

        public static string GetNativeName(string languageCode)
        {
            if (NativeNames.TryGetValue(languageCode, out var name))
                return name;
            return languageCode;
        }
    }

    public interface ILanguage
    {
        string Disconnect { get; }
        string Connect { get; }
        string ServerInformation { get; }
        string AudioSettings { get; }
        string MicrophoneDevices { get; }
        string GeneralSettings { get; }
        string PlayerCustomization { get; }
        string ServerIP { get; }
        string ServerPort { get; }
        string PasswordOptional { get; }
        string DisableMicrophone { get; }
        string EnableMicrophone { get; }
        string Deafen { get; }
        string Undeafen { get; }
        string EnablePushToTalk { get; }
        string DisablePushToTalk { get; }
        string PushToTalkKey { get; }
        string PressAnyKey { get; }
        string MicrophoneGain { get; }
        string Level { get; }
        string EnableCollisions { get; }
        string DisableCollisions { get; }
        string EnablePlayerCutsceneVisibility { get; }
        string DisablePlayerCutsceneVisibility { get; }
        string ConnectedPlayers { get; }
        string NoPlayersConnected { get; }
        string You { get; }
        string UpdateNameAndAppearance { get; }
        string Nickname { get; }
        string SuitTint { get; }
        string Red { get; }
        string Green { get; }
        string Blue { get; }
        string EnableNametags { get; }
        string DisableNametags { get; }
        string AppearanceUpdated { get; }
        string HasConnected { get; }
        string HasDisconnected { get; }
        string HasChangedNicknameTo { get; }
        string HasUpdatedTheirColor { get; }
        string ConnectedToServer { get; }
        string Language { get; }
    }

    public class EnglishLanguage : ILanguage
    {
        public string Disconnect => "Disconnect";
        public string Connect => "Connect";
        public string ServerInformation => "Server Information";
        public string AudioSettings => "Audio Settings";
        public string MicrophoneDevices => "Microphone Devices";
        public string GeneralSettings => "General Settings";
        public string PlayerCustomization => "Player Customization";
        public string ServerIP => "Server IP:";
        public string ServerPort => "Server Port:";
        public string PasswordOptional => "Password (Optional):";
        public string DisableMicrophone => "Disable Microphone";
        public string EnableMicrophone => "Enable Microphone";
        public string Deafen => "Deafen";
        public string Undeafen => "Undeafen";
        public string EnablePushToTalk => "Enable Push to Talk";
        public string DisablePushToTalk => "Disable Push to Talk";
        public string PushToTalkKey => "Push to Talk Key:";
        public string PressAnyKey => "Press any key...";
        public string MicrophoneGain => "Microphone Gain:";
        public string Level => "Level:";
        public string EnableCollisions => "Enable Collisions";
        public string DisableCollisions => "Disable Collisions";
        public string EnablePlayerCutsceneVisibility => "Enable Player Cutscene Visibility";
        public string DisablePlayerCutsceneVisibility => "Disable Player Cutscene Visibility";
        public string ConnectedPlayers => "Connected Players";
        public string NoPlayersConnected => "No players connected.";
        public string You => "You";
        public string UpdateNameAndAppearance => "Update Name & Appearance";
        public string Nickname => "Nickname:";
        public string SuitTint => "Suit Tint:";
        public string Red => "Red:";
        public string Green => "Green:";
        public string Blue => "Blue:";
        public string EnableNametags => "Enable Nametag Visibility";
        public string DisableNametags => "Disable Nametag Visibility";
        public string AppearanceUpdated => "Your appearance has been updated";
        public string HasConnected => "{0} has connected";
        public string HasDisconnected => "{0} has disconnected";
        public string HasChangedNicknameTo => "{0} has changed their nickname to {1}";
        public string HasUpdatedTheirColor => "{0} has updated their color";
        public string ConnectedToServer => "Connected to server";
        public string Language => "Language:";
    }

    public class SpanishLanguage : ILanguage
    {
        public string Disconnect => "Desconectar";
        public string Connect => "Conectar";
        public string ServerInformation => "Información del Servidor";
        public string AudioSettings => "Configuración de Audio";
        public string MicrophoneDevices => "Dispositivos de Micrófono";
        public string GeneralSettings => "Configuración General";
        public string PlayerCustomization => "Personalización del Jugador";
        public string ServerIP => "IP del Servidor:";
        public string ServerPort => "Puerto del Servidor:";
        public string PasswordOptional => "Contraseña (Opcional):";
        public string DisableMicrophone => "Deshabilitar Micrófono";
        public string EnableMicrophone => "Habilitar Micrófono";
        public string Deafen => "Silenciar";
        public string Undeafen => "Activar Sonido";
        public string EnablePushToTalk => "Habilitar Pulsar para Hablar";
        public string DisablePushToTalk => "Deshabilitar Pulsar para Hablar";
        public string PushToTalkKey => "Tecla Pulsar para Hablar:";
        public string PressAnyKey => "Presiona cualquier tecla...";
        public string MicrophoneGain => "Ganancia del Micrófono:";
        public string Level => "Nivel:";
        public string EnableCollisions => "Habilitar Colisiones";
        public string DisableCollisions => "Deshabilitar Colisiones";
        public string EnablePlayerCutsceneVisibility => "Habilitar Visibilidad del Jugador en Cinemáticas";
        public string DisablePlayerCutsceneVisibility => "Deshabilitar Visibilidad del Jugador en Cinemáticas";
        public string ConnectedPlayers => "Jugadores Conectados";
        public string NoPlayersConnected => "No hay jugadores conectados.";
        public string You => "Tú";
        public string UpdateNameAndAppearance => "Actualizar Nombre y Apariencia";
        public string Nickname => "Apodo:";
        public string SuitTint => "Tono del Traje:";
        public string Red => "Rojo:";
        public string Green => "Verde:";
        public string Blue => "Azul:";
        public string EnableNametags => "Habilitar Visibilidad de Nombre";
        public string DisableNametags => "Deshabilitar Visibilidad de Nombre";
        public string AppearanceUpdated => "Tu apariencia ha sido actualizada";
        public string HasConnected => "{0} se ha conectado";
        public string HasDisconnected => "{0} se ha desconectado";
        public string HasChangedNicknameTo => "{0} ha cambiado su apodo a {1}";
        public string HasUpdatedTheirColor => "{0} ha actualizado su color";
        public string ConnectedToServer => "Conectado al servidor";
        public string Language => "Idioma:";
    }

    public class FrenchLanguage : ILanguage
    {
        public string Disconnect => "Déconnecter";
        public string Connect => "Connecter";
        public string ServerInformation => "Informations du Serveur";
        public string AudioSettings => "Paramètres Audio";
        public string MicrophoneDevices => "Appareils Microphone";
        public string GeneralSettings => "Paramètres Généraux";
        public string PlayerCustomization => "Personnalisation du Joueur";
        public string ServerIP => "IP du Serveur:";
        public string ServerPort => "Port du Serveur:";
        public string PasswordOptional => "Mot de passe (Optionnel):";
        public string DisableMicrophone => "Désactiver le Microphone";
        public string EnableMicrophone => "Activer le Microphone";
        public string Deafen => "Couper le Son";
        public string Undeafen => "Activer le Son";
        public string EnablePushToTalk => "Activer l'Appui pour Parler";
        public string DisablePushToTalk => "Désactiver l'Appui pour Parler";
        public string PushToTalkKey => "Touche Appui pour Parler:";
        public string PressAnyKey => "Appuyez sur une touche...";
        public string MicrophoneGain => "Gain du Microphone:";
        public string Level => "Niveau:";
        public string EnableCollisions => "Activer les Collisions";
        public string DisableCollisions => "Désactiver les Collisions";
        public string EnablePlayerCutsceneVisibility => "Activer la Visibilité du Joueur en Cinématique";
        public string DisablePlayerCutsceneVisibility => "Désactiver la Visibilité du Joueur en Cinématique";
        public string ConnectedPlayers => "Joueurs Connectés";
        public string NoPlayersConnected => "Aucun joueur connecté.";
        public string You => "Vous";
        public string UpdateNameAndAppearance => "Mettre à jour le Nom et l'Apparence";
        public string Nickname => "Surnom:";
        public string SuitTint => "Teinte du Costume:";
        public string Red => "Rouge:";
        public string Green => "Vert:";
        public string Blue => "Bleu:";
        public string EnableNametags => "Activer la Visibilité du Nom";
        public string DisableNametags => "Désactiver la Visibilité du Nom";
        public string AppearanceUpdated => "Votre apparence a été mise à jour";
        public string HasConnected => "{0} s'est connecté";
        public string HasDisconnected => "{0} s'est déconnecté";
        public string HasChangedNicknameTo => "{0} a changé son surnom en {1}";
        public string HasUpdatedTheirColor => "{0} a mis à jour sa couleur";
        public string ConnectedToServer => "Connecté au serveur";
        public string Language => "Langue:";
    }

    public class GermanLanguage : ILanguage
    {
        public string Disconnect => "Trennen";
        public string Connect => "Verbinden";
        public string ServerInformation => "Serverinformationen";
        public string AudioSettings => "Audioeinstellungen";
        public string MicrophoneDevices => "Mikrofongeräte";
        public string GeneralSettings => "Allgemeine Einstellungen";
        public string PlayerCustomization => "Spieleranpassung";
        public string ServerIP => "Server-IP:";
        public string ServerPort => "Serverport:";
        public string PasswordOptional => "Passwort (Optional):";
        public string DisableMicrophone => "Mikrofon deaktivieren";
        public string EnableMicrophone => "Mikrofon aktivieren";
        public string Deafen => "Stumm";
        public string Undeafen => "Laut";
        public string EnablePushToTalk => "Drücken zum Sprechen aktivieren";
        public string DisablePushToTalk => "Drücken zum Sprechen deaktivieren";
        public string PushToTalkKey => "Taste für Drücken zum Sprechen:";
        public string PressAnyKey => "Beliebige Taste drücken...";
        public string MicrophoneGain => "Mikrofonverstärkung:";
        public string Level => "Pegel:";
        public string EnableCollisions => "Kollisionen aktivieren";
        public string DisableCollisions => "Kollisionen deaktivieren";
        public string EnablePlayerCutsceneVisibility => "Sichtbarkeit des Spielers in Zwischenszenen aktivieren";
        public string DisablePlayerCutsceneVisibility => "Sichtbarkeit des Spielers in Zwischenszenen deaktivieren";
        public string ConnectedPlayers => "Verbundene Spieler";
        public string NoPlayersConnected => "Keine Spieler verbunden.";
        public string You => "Sie";
        public string UpdateNameAndAppearance => "Namen und Erscheinungsbild aktualisieren";
        public string Nickname => "Spitzname:";
        public string SuitTint => "Anzugton:";
        public string Red => "Rot:";
        public string Green => "Grün:";
        public string Blue => "Blau:";
        public string EnableNametags => "Nametag-Sichtbarkeit aktivieren";
        public string DisableNametags => "Nametag-Sichtbarkeit deaktivieren";
        public string AppearanceUpdated => "Ihr Erscheinungsbild wurde aktualisiert";
        public string HasConnected => "{0} hat sich verbunden";
        public string HasDisconnected => "{0} hat die Verbindung getrennt";
        public string HasChangedNicknameTo => "{0} hat seinen Spitznamen zu {1} geändert";
        public string HasUpdatedTheirColor => "{0} hat seine Farbe aktualisiert";
        public string ConnectedToServer => "Mit dem Server verbunden";
        public string Language => "Sprache:";
    }
}
