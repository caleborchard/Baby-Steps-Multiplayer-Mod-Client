namespace BabyStepsMultiplayerClient.Localization
{
    public static class LanguageNativeNames
    {
        // All AI translations must be checked with native speakers before the full release of the update.
        public static readonly Dictionary<string, string> NativeNames = new()
        {
            { "English", "English" },
            { "Spanish", "Español" }, //Human reviewed
            { "French", "Français" }, //AI
            { "German", "Deutsch" }, //AI
            { "Korean", "한국어" }, //AI
            { "ChineseSimplified", "简体中文" }, //AI
            { "ChineseTraditional", "繁體中文" }, //AI
            { "Japanese", "日本語" } //AI
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
        public string ServerInformation => "Información del servidor";
        public string AudioSettings => "Ajustes de audio";
        public string MicrophoneDevices => "Dispositivos de micrófono";
        public string GeneralSettings => "Ajustes generales";
        public string PlayerCustomization => "Personalización";
        public string ServerIP => "IP del servidor:";
        public string ServerPort => "Puerto:";
        public string PasswordOptional => "Contraseña (opcional):";
        public string DisableMicrophone => "Desactivar micrófono";
        public string EnableMicrophone => "Activar micrófono";
        public string Deafen => "Ensordecer";
        public string Undeafen => "Quitar ensordecer";
        public string EnablePushToTalk => "Activar pulsar para hablar";
        public string DisablePushToTalk => "Desactivar pulsar para hablar";
        public string PushToTalkKey => "Tecla para hablar:";
        public string PressAnyKey => "Pulsa una tecla...";
        public string MicrophoneGain => "Ganancia:";
        public string Level => "Nivel:";
        public string EnableCollisions => "Activar colisiones";
        public string DisableCollisions => "Desactivar colisiones";
        public string EnablePlayerCutsceneVisibility => "Mostrar jugador en cinemáticas";
        public string DisablePlayerCutsceneVisibility => "Ocultar jugador en cinemáticas";
        public string ConnectedPlayers => "Jugadores conectados";
        public string NoPlayersConnected => "No hay jugadores conectados.";
        public string You => "Tú";
        public string UpdateNameAndAppearance => "Actualizar nombre y apariencia";
        public string Nickname => "Apodo:";
        public string SuitTint => "Color del traje:";
        public string Red => "Rojo:";
        public string Green => "Verde:";
        public string Blue => "Azul:";
        public string EnableNametags => "Mostrar nombres";
        public string DisableNametags => "Ocultar nombres";
        public string AppearanceUpdated => "Tu apariencia se ha actualizada";
        public string HasConnected => "{0} se ha conectado";
        public string HasDisconnected => "{0} se ha desconectado";
        public string HasChangedNicknameTo => "{0} cambió su apodo a {1}";
        public string HasUpdatedTheirColor => "{0} actualizó su color";
        public string ConnectedToServer => "Conectado al servidor";
        public string Language => "Idioma:";
    }

    public class FrenchLanguage : ILanguage
    {
        public string Disconnect => "Se déconnecter";
        public string Connect => "Se connecter";
        public string ServerInformation => "Infos serveur";
        public string AudioSettings => "Paramètres audio";
        public string MicrophoneDevices => "Périphériques micro";
        public string GeneralSettings => "Paramètres généraux";
        public string PlayerCustomization => "Personnalisation";
        public string ServerIP => "IP du serveur :";
        public string ServerPort => "Port :";
        public string PasswordOptional => "Mot de passe (optionnel) :";
        public string DisableMicrophone => "Désactiver le micro";
        public string EnableMicrophone => "Activer le micro";
        public string Deafen => "Assourdir";
        public string Undeafen => "Rétablir le son";
        public string EnablePushToTalk => "Activer parler pour parler";
        public string DisablePushToTalk => "Désactiver parler pour parler";
        public string PushToTalkKey => "Touche parler :";
        public string PressAnyKey => "Appuyez sur une touche...";
        public string MicrophoneGain => "Gain micro :";
        public string Level => "Niveau :";
        public string EnableCollisions => "Activer collisions";
        public string DisableCollisions => "Désactiver collisions";
        public string EnablePlayerCutsceneVisibility => "Afficher joueur en cinématique";
        public string DisablePlayerCutsceneVisibility => "Masquer joueur en cinématique";
        public string ConnectedPlayers => "Joueurs connectés";
        public string NoPlayersConnected => "Aucun joueur connecté.";
        public string You => "Vous";
        public string UpdateNameAndAppearance => "Mettre à jour nom et apparence";
        public string Nickname => "Pseudo :";
        public string SuitTint => "Couleur tenue :";
        public string Red => "Rouge :";
        public string Green => "Vert :";
        public string Blue => "Bleu :";
        public string EnableNametags => "Afficher noms";
        public string DisableNametags => "Masquer noms";
        public string AppearanceUpdated => "Apparence mise à jour";
        public string HasConnected => "{0} s'est connecté";
        public string HasDisconnected => "{0} s'est déconnecté";
        public string HasChangedNicknameTo => "{0} a changé son pseudo en {1}";
        public string HasUpdatedTheirColor => "{0} a modifié sa couleur";
        public string ConnectedToServer => "Connecté au serveur";
        public string Language => "Langue :";
    }

    public class GermanLanguage : ILanguage
    {
        public string Disconnect => "Verbindung trennen";
        public string Connect => "Verbinden";
        public string ServerInformation => "Serverinformationen";
        public string AudioSettings => "Audioeinstellungen";
        public string MicrophoneDevices => "Mikrofone";
        public string GeneralSettings => "Allgemein";
        public string PlayerCustomization => "Anpassung";
        public string ServerIP => "Server-IP:";
        public string ServerPort => "Port:";
        public string PasswordOptional => "Passwort (optional):";
        public string DisableMicrophone => "Mikrofon deaktivieren";
        public string EnableMicrophone => "Mikrofon aktivieren";
        public string Deafen => "Stummschalten";
        public string Undeafen => "Stummschaltung aufheben";
        public string EnablePushToTalk => "Push-to-Talk aktivieren";
        public string DisablePushToTalk => "Push-to-Talk deaktivieren";
        public string PushToTalkKey => "Push-to-Talk-Taste:";
        public string PressAnyKey => "Beliebige Taste drücken...";
        public string MicrophoneGain => "Mikrofonpegel:";
        public string Level => "Pegel:";
        public string EnableCollisions => "Kollisionen aktivieren";
        public string DisableCollisions => "Kollisionen deaktivieren";
        public string EnablePlayerCutsceneVisibility => "Spieler in Zwischensequenzen anzeigen";
        public string DisablePlayerCutsceneVisibility => "Spieler in Zwischensequenzen ausblenden";
        public string ConnectedPlayers => "Verbundene Spieler";
        public string NoPlayersConnected => "Keine Spieler verbunden.";
        public string You => "Du";
        public string UpdateNameAndAppearance => "Name und Aussehen aktualisieren";
        public string Nickname => "Spitzname:";
        public string SuitTint => "Anzugfarbe:";
        public string Red => "Rot:";
        public string Green => "Grün:";
        public string Blue => "Blau:";
        public string EnableNametags => "Namen anzeigen";
        public string DisableNametags => "Namen ausblenden";
        public string AppearanceUpdated => "Aussehen aktualisiert";
        public string HasConnected => "{0} ist beigetreten";
        public string HasDisconnected => "{0} hat verlassen";
        public string HasChangedNicknameTo => "{0} heißt jetzt {1}";
        public string HasUpdatedTheirColor => "{0} hat die Farbe geändert";
        public string ConnectedToServer => "Mit Server verbunden";
        public string Language => "Sprache:";
    }

    public class KoreanLanguage : ILanguage
    {
        public string Disconnect => "연결 끊기";
        public string Connect => "연결";
        public string ServerInformation => "서버 정보";
        public string AudioSettings => "오디오 설정";
        public string MicrophoneDevices => "마이크 장치";
        public string GeneralSettings => "일반 설정";
        public string PlayerCustomization => "플레이어 설정";
        public string ServerIP => "서버 IP:";
        public string ServerPort => "포트:";
        public string PasswordOptional => "비밀번호(선택):";
        public string DisableMicrophone => "마이크 끄기";
        public string EnableMicrophone => "마이크 켜기";
        public string Deafen => "소리 끄기";
        public string Undeafen => "소리 켜기";
        public string EnablePushToTalk => "눌러서 말하기 사용";
        public string DisablePushToTalk => "눌러서 말하기 해제";
        public string PushToTalkKey => "말하기 키:";
        public string PressAnyKey => "아무 키나 누르세요...";
        public string MicrophoneGain => "마이크 게인:";
        public string Level => "레벨:";
        public string EnableCollisions => "충돌 사용";
        public string DisableCollisions => "충돌 해제";
        public string EnablePlayerCutsceneVisibility => "컷신에서 플레이어 표시";
        public string DisablePlayerCutsceneVisibility => "컷신에서 플레이어 숨김";
        public string ConnectedPlayers => "접속 중인 플레이어";
        public string NoPlayersConnected => "접속한 플레이어가 없습니다.";
        public string You => "나";
        public string UpdateNameAndAppearance => "이름 및 외형 변경";
        public string Nickname => "닉네임:";
        public string SuitTint => "슈트 색상:";
        public string Red => "빨강:";
        public string Green => "초록:";
        public string Blue => "파랑:";
        public string EnableNametags => "이름표 표시";
        public string DisableNametags => "이름표 숨김";
        public string AppearanceUpdated => "외형이 변경되었습니다";
        public string HasConnected => "{0} 님이 접속했습니다";
        public string HasDisconnected => "{0} 님이 퇴장했습니다";
        public string HasChangedNicknameTo => "{0} 님이 이름을 {1}(으)로 변경했습니다";
        public string HasUpdatedTheirColor => "{0} 님이 색상을 변경했습니다";
        public string ConnectedToServer => "서버에 연결됨";
        public string Language => "언어:";
    }

    public class ChineseSimplifiedLanguage : ILanguage
    {
        public string Disconnect => "断开连接";
        public string Connect => "连接";
        public string ServerInformation => "服务器信息";
        public string AudioSettings => "音频设置";
        public string MicrophoneDevices => "麦克风设备";
        public string GeneralSettings => "常规设置";
        public string PlayerCustomization => "玩家自定义";
        public string ServerIP => "服务器IP：";
        public string ServerPort => "端口：";
        public string PasswordOptional => "密码（可选）：";
        public string DisableMicrophone => "关闭麦克风";
        public string EnableMicrophone => "开启麦克风";
        public string Deafen => "关闭声音";
        public string Undeafen => "恢复声音";
        public string EnablePushToTalk => "启用按键说话";
        public string DisablePushToTalk => "禁用按键说话";
        public string PushToTalkKey => "说话按键：";
        public string PressAnyKey => "按任意键...";
        public string MicrophoneGain => "麦克风增益：";
        public string Level => "级别：";
        public string EnableCollisions => "启用碰撞";
        public string DisableCollisions => "禁用碰撞";
        public string EnablePlayerCutsceneVisibility => "过场动画中显示玩家";
        public string DisablePlayerCutsceneVisibility => "过场动画中隐藏玩家";
        public string ConnectedPlayers => "已连接玩家";
        public string NoPlayersConnected => "没有玩家连接。";
        public string You => "你";
        public string UpdateNameAndAppearance => "更新名称和外观";
        public string Nickname => "昵称：";
        public string SuitTint => "服装颜色：";
        public string Red => "红：";
        public string Green => "绿：";
        public string Blue => "蓝：";
        public string EnableNametags => "显示名称";
        public string DisableNametags => "隐藏名称";
        public string AppearanceUpdated => "外观已更新";
        public string HasConnected => "{0} 已连接";
        public string HasDisconnected => "{0} 已断开";
        public string HasChangedNicknameTo => "{0} 将昵称改为 {1}";
        public string HasUpdatedTheirColor => "{0} 更新了颜色";
        public string ConnectedToServer => "已连接到服务器";
        public string Language => "语言：";
    }

    public class ChineseTraditionalLanguage : ILanguage
    {
        public string Disconnect => "中斷連線";
        public string Connect => "連線";
        public string ServerInformation => "伺服器資訊";
        public string AudioSettings => "音訊設定";
        public string MicrophoneDevices => "麥克風裝置";
        public string GeneralSettings => "一般設定";
        public string PlayerCustomization => "玩家自訂";
        public string ServerIP => "伺服器IP：";
        public string ServerPort => "連接埠：";
        public string PasswordOptional => "密碼（選填）：";
        public string DisableMicrophone => "關閉麥克風";
        public string EnableMicrophone => "開啟麥克風";
        public string Deafen => "關閉聲音";
        public string Undeafen => "恢復聲音";
        public string EnablePushToTalk => "啟用按鍵說話";
        public string DisablePushToTalk => "停用按鍵說話";
        public string PushToTalkKey => "說話按鍵：";
        public string PressAnyKey => "按任意鍵...";
        public string MicrophoneGain => "麥克風增益：";
        public string Level => "等級：";
        public string EnableCollisions => "啟用碰撞";
        public string DisableCollisions => "停用碰撞";
        public string EnablePlayerCutsceneVisibility => "過場動畫中顯示玩家";
        public string DisablePlayerCutsceneVisibility => "過場動畫中隱藏玩家";
        public string ConnectedPlayers => "已連線玩家";
        public string NoPlayersConnected => "沒有玩家連線。";
        public string You => "你";
        public string UpdateNameAndAppearance => "更新名稱與外觀";
        public string Nickname => "暱稱：";
        public string SuitTint => "服裝顏色：";
        public string Red => "紅：";
        public string Green => "綠：";
        public string Blue => "藍：";
        public string EnableNametags => "顯示名稱";
        public string DisableNametags => "隱藏名稱";
        public string AppearanceUpdated => "外觀已更新";
        public string HasConnected => "{0} 已連線";
        public string HasDisconnected => "{0} 已中斷連線";
        public string HasChangedNicknameTo => "{0} 將暱稱改為 {1}";
        public string HasUpdatedTheirColor => "{0} 更新了顏色";
        public string ConnectedToServer => "已連線至伺服器";
        public string Language => "語言：";
    }

    public class JapaneseLanguage : ILanguage
    {
        public string Disconnect => "切断";
        public string Connect => "接続";
        public string ServerInformation => "サーバー情報";
        public string AudioSettings => "オーディオ設定";
        public string MicrophoneDevices => "マイクデバイス";
        public string GeneralSettings => "一般設定";
        public string PlayerCustomization => "プレイヤー設定";
        public string ServerIP => "サーバーIP：";
        public string ServerPort => "ポート：";
        public string PasswordOptional => "パスワード（任意）：";
        public string DisableMicrophone => "マイクをオフ";
        public string EnableMicrophone => "マイクをオン";
        public string Deafen => "ミュート（受信）";
        public string Undeafen => "ミュート解除（受信）";
        public string EnablePushToTalk => "プッシュトゥトークを有効化";
        public string DisablePushToTalk => "プッシュトゥトークを無効化";
        public string PushToTalkKey => "プッシュトゥトークキー：";
        public string PressAnyKey => "いずれかのキーを押してください...";
        public string MicrophoneGain => "マイク音量：";
        public string Level => "レベル：";
        public string EnableCollisions => "衝突を有効化";
        public string DisableCollisions => "衝突を無効化";
        public string EnablePlayerCutsceneVisibility => "カットシーンでプレイヤーを表示";
        public string DisablePlayerCutsceneVisibility => "カットシーンでプレイヤーを非表示";
        public string ConnectedPlayers => "接続中のプレイヤー";
        public string NoPlayersConnected => "接続中のプレイヤーはいません。";
        public string You => "あなた";
        public string UpdateNameAndAppearance => "名前と外見を更新";
        public string Nickname => "ニックネーム：";
        public string SuitTint => "スーツの色：";
        public string Red => "赤：";
        public string Green => "緑：";
        public string Blue => "青：";
        public string EnableNametags => "ネームタグを表示";
        public string DisableNametags => "ネームタグを非表示";
        public string AppearanceUpdated => "外見が更新されました";
        public string HasConnected => "{0} が接続しました";
        public string HasDisconnected => "{0} が切断しました";
        public string HasChangedNicknameTo => "{0} がニックネームを {1} に変更しました";
        public string HasUpdatedTheirColor => "{0} が色を変更しました";
        public string ConnectedToServer => "サーバーに接続しました";
        public string Language => "言語：";
    }
}
