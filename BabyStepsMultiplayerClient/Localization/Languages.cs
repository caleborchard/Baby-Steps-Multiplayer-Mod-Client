namespace BabyStepsMultiplayerClient.Localization
{
    // These are crappy AI translations, please send a PR with better ones if you have the time to do so. -Caleb
    public interface ILanguage
    {
        // Menu title & popup actions
        string MultiplayerTitle { get; }
        string Enter { get; }
        string PasswordForSession { get; }  // format string: {0} = lobby name

        // Tabs & navigation
        string TabLobby { get; }
        string TabPlayer { get; }
        string TabGeneral { get; }
        string TabAudio { get; }
        string TabLAN { get; }
        string ButtonBack { get; }

        // Lobby tab
        string HostLobby { get; }
        string StopHosting { get; }
        string LobbyBrowser { get; }
        string Refresh { get; }
        string Refreshing { get; }
        string LobbyPassword { get; }

        // LAN tab
        string HostLAN { get; }
        string ServerIP { get; }
        string ServerPort { get; }
        string PasswordOptional { get; }

        // Connection
        string Disconnect { get; }
        string Connect { get; }

        // Audio tab
        string DisableMicrophone { get; }
        string EnableMicrophone { get; }
        string Deafen { get; }
        string Undeafen { get; }
        string EnablePushToTalk { get; }
        string DisablePushToTalk { get; }
        string PushToTalkKey { get; }
        string MicrophoneGain { get; }
        string AudioDevice { get; }
        string NoDevicesFound { get; }

        // General tab
        string EnableCollisions { get; }
        string DisableCollisions { get; }
        string EnablePlayerCutsceneVisibility { get; }
        string DisablePlayerCutsceneVisibility { get; }
        string EnableNametags { get; }
        string DisableNametags { get; }
        string PlayerListKeyLabel { get; }
        string ChatMenuKeyLabel { get; }

        // Player tab
        string UpdateNameAndAppearance { get; }
        string Nickname { get; }

        // Common
        string PressAnyKey { get; }
        string Language { get; }
        string Level { get; }

        // Players window
        string ConnectedPlayers { get; }
        string NoPlayersConnected { get; }
        string You { get; }

        // Notifications
        string ConnectedToServer { get; }
        string AppearanceUpdated { get; }
        string JoiningServer { get; }       // "Joining {0}…"
        string CreatingLobby { get; }
        string StoppedHosting { get; }
        string HostingOnPort { get; }       // "Hosting on port {0}"
        string PlayerJoinedSession { get; } // "{0} joined your session"
        string PlayerLeftSession { get; }   // "{0} left your session"
        string HasConnected { get; }        // "{0} has connected"
        string HasDisconnected { get; }     // "{0} has disconnected"
        string HasChangedNicknameTo { get; }// "{0} has changed their nickname to {1}"
        string HasUpdatedTheirColor { get; }// "{0} has updated their color"

        // Legacy (ServerConnectUI)
        string ServerInformation { get; }
        string AudioSettings { get; }
        string MicrophoneDevices { get; }
        string GeneralSettings { get; }
        string PlayerCustomization { get; }
        string SuitTint { get; }
        string Red { get; }
        string Green { get; }
        string Blue { get; }
    }

    public class EnglishLanguage : ILanguage
    {
        public string MultiplayerTitle => "Multiplayer";
        public string Enter => "Enter";
        public string PasswordForSession => "Enter password for:\n{0}";
        public string TabLobby => "LOBBY";
        public string TabPlayer => "Player";
        public string TabGeneral => "General";
        public string TabAudio => "Audio";
        public string TabLAN => "LAN";
        public string ButtonBack => "Back";
        public string HostLobby => "Host Lobby";
        public string StopHosting => "Stop Hosting";
        public string LobbyBrowser => "Lobby Browser";
        public string Refresh => "Refresh";
        public string Refreshing => "Refreshing…";
        public string LobbyPassword => "Lobby password (blank = public)";
        public string HostLAN => "Host (LAN)";
        public string ServerIP => "Server IP:";
        public string ServerPort => "Server Port:";
        public string PasswordOptional => "Password (Optional):";
        public string Disconnect => "Disconnect";
        public string Connect => "Connect";
        public string DisableMicrophone => "Disable Microphone";
        public string EnableMicrophone => "Enable Microphone";
        public string Deafen => "Deafen";
        public string Undeafen => "Undeafen";
        public string EnablePushToTalk => "Enable Push to Talk";
        public string DisablePushToTalk => "Disable Push to Talk";
        public string PushToTalkKey => "Push to Talk Key:";
        public string MicrophoneGain => "Microphone Gain:";
        public string AudioDevice => "Audio Device:";
        public string NoDevicesFound => "No devices found";
        public string EnableCollisions => "Enable Collisions";
        public string DisableCollisions => "Disable Collisions";
        public string EnablePlayerCutsceneVisibility => "Enable Player Cutscene Visibility";
        public string DisablePlayerCutsceneVisibility => "Disable Player Cutscene Visibility";
        public string EnableNametags => "Enable Nametag Visibility";
        public string DisableNametags => "Disable Nametag Visibility";
        public string PlayerListKeyLabel => "Player List Key:";
        public string ChatMenuKeyLabel => "Chat Menu Key:";
        public string UpdateNameAndAppearance => "Update Name & Appearance";
        public string Nickname => "Nickname:";
        public string PressAnyKey => "Press any key…";
        public string Language => "Language:";
        public string Level => "Level:";
        public string ConnectedPlayers => "Connected Players";
        public string NoPlayersConnected => "No players connected.";
        public string You => "You";
        public string ConnectedToServer => "Connected to server";
        public string AppearanceUpdated => "Your appearance has been updated";
        public string JoiningServer => "Joining {0}…";
        public string CreatingLobby => "Creating Steam lobby…";
        public string StoppedHosting => "Stopped hosting";
        public string HostingOnPort => "Hosting on port {0}";
        public string PlayerJoinedSession => "{0} joined your session";
        public string PlayerLeftSession => "{0} left your session";
        public string HasConnected => "{0} has connected";
        public string HasDisconnected => "{0} has disconnected";
        public string HasChangedNicknameTo => "{0} has changed their nickname to {1}";
        public string HasUpdatedTheirColor => "{0} has updated their color";
        public string ServerInformation => "Server Information";
        public string AudioSettings => "Audio Settings";
        public string MicrophoneDevices => "Microphone Devices";
        public string GeneralSettings => "General Settings";
        public string PlayerCustomization => "Player Customization";
        public string SuitTint => "Suit Tint:";
        public string Red => "Red:";
        public string Green => "Green:";
        public string Blue => "Blue:";
    }

    public class SpanishLanguage : ILanguage
    {
        public string MultiplayerTitle => "Multijugador";
        public string Enter => "Aceptar";
        public string PasswordForSession => "Contraseña para:\n{0}";
        public string TabLobby => "SALA";
        public string TabPlayer => "Jugador";
        public string TabGeneral => "General";
        public string TabAudio => "Audio";
        public string TabLAN => "LAN";
        public string ButtonBack => "Atrás";
        public string HostLobby => "Crear sala";
        public string StopHosting => "Detener";
        public string LobbyBrowser => "Explorador";
        public string Refresh => "Actualizar";
        public string Refreshing => "Actualizando…";
        public string LobbyPassword => "Contraseña de sala (vacío = pública)";
        public string HostLAN => "Alojar (LAN)";
        public string ServerIP => "IP del servidor:";
        public string ServerPort => "Puerto:";
        public string PasswordOptional => "Contraseña (opcional):";
        public string Disconnect => "Desconectar";
        public string Connect => "Conectar";
        public string DisableMicrophone => "Desactivar micrófono";
        public string EnableMicrophone => "Activar micrófono";
        public string Deafen => "Ensordecer";
        public string Undeafen => "Quitar ensordecer";
        public string EnablePushToTalk => "Activar pulsar para hablar";
        public string DisablePushToTalk => "Desactivar pulsar para hablar";
        public string PushToTalkKey => "Tecla para hablar:";
        public string MicrophoneGain => "Ganancia:";
        public string AudioDevice => "Dispositivo de audio:";
        public string NoDevicesFound => "No se encontraron dispositivos";
        public string EnableCollisions => "Activar colisiones";
        public string DisableCollisions => "Desactivar colisiones";
        public string EnablePlayerCutsceneVisibility => "Mostrar jugador en cinemáticas";
        public string DisablePlayerCutsceneVisibility => "Ocultar jugador en cinemáticas";
        public string EnableNametags => "Mostrar nombres";
        public string DisableNametags => "Ocultar nombres";
        public string PlayerListKeyLabel => "Tecla lista:";
        public string ChatMenuKeyLabel => "Tecla chat:";
        public string UpdateNameAndAppearance => "Actualizar nombre y apariencia";
        public string Nickname => "Apodo:";
        public string PressAnyKey => "Pulsa una tecla…";
        public string Language => "Idioma:";
        public string Level => "Nivel:";
        public string ConnectedPlayers => "Jugadores conectados";
        public string NoPlayersConnected => "No hay jugadores conectados.";
        public string You => "Tú";
        public string ConnectedToServer => "Conectado al servidor";
        public string AppearanceUpdated => "Tu apariencia se ha actualizado";
        public string JoiningServer => "Uniéndose a {0}…";
        public string CreatingLobby => "Creando sala de Steam…";
        public string StoppedHosting => "Se dejó de alojar";
        public string HostingOnPort => "Alojando en puerto {0}";
        public string PlayerJoinedSession => "{0} se unió a tu sesión";
        public string PlayerLeftSession => "{0} abandonó tu sesión";
        public string HasConnected => "{0} se ha conectado";
        public string HasDisconnected => "{0} se ha desconectado";
        public string HasChangedNicknameTo => "{0} cambió su apodo a {1}";
        public string HasUpdatedTheirColor => "{0} actualizó su color";
        public string ServerInformation => "Información del servidor";
        public string AudioSettings => "Ajustes de audio";
        public string MicrophoneDevices => "Dispositivos de micrófono";
        public string GeneralSettings => "Ajustes generales";
        public string PlayerCustomization => "Personalización";
        public string SuitTint => "Color del traje:";
        public string Red => "Rojo:";
        public string Green => "Verde:";
        public string Blue => "Azul:";
    }

    public class FrenchLanguage : ILanguage
    {
        public string MultiplayerTitle => "Multijoueur";
        public string Enter => "Valider";
        public string PasswordForSession => "Mot de passe pour :\n{0}";
        public string TabLobby => "SALLE";
        public string TabPlayer => "Joueur";
        public string TabGeneral => "Général";
        public string TabAudio => "Audio";
        public string TabLAN => "LAN";
        public string ButtonBack => "Retour";
        public string HostLobby => "Créer une salle";
        public string StopHosting => "Arrêter";
        public string LobbyBrowser => "Navigateur";
        public string Refresh => "Actualiser";
        public string Refreshing => "Actualisation…";
        public string LobbyPassword => "Mot de passe (vide = public)";
        public string HostLAN => "Héberger (LAN)";
        public string ServerIP => "IP du serveur :";
        public string ServerPort => "Port :";
        public string PasswordOptional => "Mot de passe (optionnel) :";
        public string Disconnect => "Se déconnecter";
        public string Connect => "Se connecter";
        public string DisableMicrophone => "Désactiver le micro";
        public string EnableMicrophone => "Activer le micro";
        public string Deafen => "Assourdir";
        public string Undeafen => "Rétablir le son";
        public string EnablePushToTalk => "Activer pousser pour parler";
        public string DisablePushToTalk => "Désactiver pousser pour parler";
        public string PushToTalkKey => "Touche parler :";
        public string MicrophoneGain => "Gain micro :";
        public string AudioDevice => "Périphérique audio :";
        public string NoDevicesFound => "Aucun périphérique";
        public string EnableCollisions => "Activer collisions";
        public string DisableCollisions => "Désactiver collisions";
        public string EnablePlayerCutsceneVisibility => "Afficher joueur en cinématique";
        public string DisablePlayerCutsceneVisibility => "Masquer joueur en cinématique";
        public string EnableNametags => "Afficher noms";
        public string DisableNametags => "Masquer noms";
        public string PlayerListKeyLabel => "Touche joueurs :";
        public string ChatMenuKeyLabel => "Touche chat :";
        public string UpdateNameAndAppearance => "Mettre à jour nom et apparence";
        public string Nickname => "Pseudo :";
        public string PressAnyKey => "Appuyez sur une touche…";
        public string Language => "Langue :";
        public string Level => "Niveau :";
        public string ConnectedPlayers => "Joueurs connectés";
        public string NoPlayersConnected => "Aucun joueur connecté.";
        public string You => "Vous";
        public string ConnectedToServer => "Connecté au serveur";
        public string AppearanceUpdated => "Apparence mise à jour";
        public string JoiningServer => "Connexion à {0}…";
        public string CreatingLobby => "Création du salon Steam…";
        public string StoppedHosting => "Hébergement arrêté";
        public string HostingOnPort => "Port {0}";
        public string PlayerJoinedSession => "{0} a rejoint votre session";
        public string PlayerLeftSession => "{0} a quitté votre session";
        public string HasConnected => "{0} s'est connecté";
        public string HasDisconnected => "{0} s'est déconnecté";
        public string HasChangedNicknameTo => "{0} a changé son pseudo en {1}";
        public string HasUpdatedTheirColor => "{0} a modifié sa couleur";
        public string ServerInformation => "Infos serveur";
        public string AudioSettings => "Paramètres audio";
        public string MicrophoneDevices => "Périphériques micro";
        public string GeneralSettings => "Paramètres généraux";
        public string PlayerCustomization => "Personnalisation";
        public string SuitTint => "Couleur tenue :";
        public string Red => "Rouge :";
        public string Green => "Vert :";
        public string Blue => "Bleu :";
    }

    public class GermanLanguage : ILanguage
    {
        public string MultiplayerTitle => "Mehrspieler";
        public string Enter => "Bestätigen";
        public string PasswordForSession => "Passwort für:\n{0}";
        public string TabLobby => "LOBBY";
        public string TabPlayer => "Spieler";
        public string TabGeneral => "Allgemein";
        public string TabAudio => "Audio";
        public string TabLAN => "LAN";
        public string ButtonBack => "Zurück";
        public string HostLobby => "Lobby erstellen";
        public string StopHosting => "Beenden";
        public string LobbyBrowser => "Browser";
        public string Refresh => "Aktualisieren";
        public string Refreshing => "Wird geladen…";
        public string LobbyPassword => "Lobby-Passwort (leer = öffentlich)";
        public string HostLAN => "Hosten (LAN)";
        public string ServerIP => "Server-IP:";
        public string ServerPort => "Port:";
        public string PasswordOptional => "Passwort (optional):";
        public string Disconnect => "Verbindung trennen";
        public string Connect => "Verbinden";
        public string DisableMicrophone => "Mikrofon deaktivieren";
        public string EnableMicrophone => "Mikrofon aktivieren";
        public string Deafen => "Stummschalten";
        public string Undeafen => "Stummschaltung aufheben";
        public string EnablePushToTalk => "Push-to-Talk aktivieren";
        public string DisablePushToTalk => "Push-to-Talk deaktivieren";
        public string PushToTalkKey => "Push-to-Talk-Taste:";
        public string MicrophoneGain => "Mikrofonpegel:";
        public string AudioDevice => "Audiogerät:";
        public string NoDevicesFound => "Keine Geräte";
        public string EnableCollisions => "Kollisionen aktivieren";
        public string DisableCollisions => "Kollisionen deaktivieren";
        public string EnablePlayerCutsceneVisibility => "Spieler in Zwischensequenzen anzeigen";
        public string DisablePlayerCutsceneVisibility => "Spieler in Zwischensequenzen ausblenden";
        public string EnableNametags => "Namen anzeigen";
        public string DisableNametags => "Namen ausblenden";
        public string PlayerListKeyLabel => "Spielerliste-Taste:";
        public string ChatMenuKeyLabel => "Chat-Taste:";
        public string UpdateNameAndAppearance => "Name und Aussehen aktualisieren";
        public string Nickname => "Spitzname:";
        public string PressAnyKey => "Beliebige Taste drücken…";
        public string Language => "Sprache:";
        public string Level => "Pegel:";
        public string ConnectedPlayers => "Verbundene Spieler";
        public string NoPlayersConnected => "Keine Spieler verbunden.";
        public string You => "Du";
        public string ConnectedToServer => "Mit Server verbunden";
        public string AppearanceUpdated => "Aussehen aktualisiert";
        public string JoiningServer => "{0} wird beigetreten…";
        public string CreatingLobby => "Steam-Lobby wird erstellt…";
        public string StoppedHosting => "Hosting beendet";
        public string HostingOnPort => "Hosting auf Port {0}";
        public string PlayerJoinedSession => "{0} ist deiner Sitzung beigetreten";
        public string PlayerLeftSession => "{0} hat deine Sitzung verlassen";
        public string HasConnected => "{0} ist beigetreten";
        public string HasDisconnected => "{0} hat verlassen";
        public string HasChangedNicknameTo => "{0} heißt jetzt {1}";
        public string HasUpdatedTheirColor => "{0} hat die Farbe geändert";
        public string ServerInformation => "Serverinformationen";
        public string AudioSettings => "Audioeinstellungen";
        public string MicrophoneDevices => "Mikrofone";
        public string GeneralSettings => "Allgemein";
        public string PlayerCustomization => "Anpassung";
        public string SuitTint => "Anzugfarbe:";
        public string Red => "Rot:";
        public string Green => "Grün:";
        public string Blue => "Blau:";
    }

    public class JapaneseLanguage : ILanguage
    {
        public string MultiplayerTitle => "マルチプレイヤー";
        public string Enter => "決定";
        public string PasswordForSession => "パスワードを入力:\n{0}";
        public string TabLobby => "ロビー";
        public string TabPlayer => "プレイヤー";
        public string TabGeneral => "一般";
        public string TabAudio => "音声";
        public string TabLAN => "LAN";
        public string ButtonBack => "戻る";
        public string HostLobby => "ロビー作成";
        public string StopHosting => "終了";
        public string LobbyBrowser => "ロビー一覧";
        public string Refresh => "更新";
        public string Refreshing => "更新中…";
        public string LobbyPassword => "パスワード（空欄=公開）";
        public string HostLAN => "ホスト（LAN）";
        public string ServerIP => "サーバーIP：";
        public string ServerPort => "ポート：";
        public string PasswordOptional => "パスワード（任意）：";
        public string Disconnect => "切断";
        public string Connect => "接続";
        public string DisableMicrophone => "マイクをオフ";
        public string EnableMicrophone => "マイクをオン";
        public string Deafen => "ミュート";
        public string Undeafen => "ミュート解除";
        public string EnablePushToTalk => "プッシュトゥトークを有効化";
        public string DisablePushToTalk => "プッシュトゥトークを無効化";
        public string PushToTalkKey => "PTTキー：";
        public string MicrophoneGain => "マイク音量：";
        public string AudioDevice => "デバイス：";
        public string NoDevicesFound => "デバイスなし";
        public string EnableCollisions => "衝突を有効化";
        public string DisableCollisions => "衝突を無効化";
        public string EnablePlayerCutsceneVisibility => "カットシーンでプレイヤーを表示";
        public string DisablePlayerCutsceneVisibility => "カットシーンでプレイヤーを非表示";
        public string EnableNametags => "ネームタグを表示";
        public string DisableNametags => "ネームタグを非表示";
        public string PlayerListKeyLabel => "リストキー：";
        public string ChatMenuKeyLabel => "チャットキー：";
        public string UpdateNameAndAppearance => "名前と外見を更新";
        public string Nickname => "ニックネーム：";
        public string PressAnyKey => "いずれかのキーを押してください…";
        public string Language => "言語：";
        public string Level => "レベル：";
        public string ConnectedPlayers => "接続中のプレイヤー";
        public string NoPlayersConnected => "接続中のプレイヤーはいません。";
        public string You => "あなた";
        public string ConnectedToServer => "サーバーに接続しました";
        public string AppearanceUpdated => "外見が更新されました";
        public string JoiningServer => "{0} に参加中…";
        public string CreatingLobby => "Steamロビーを作成中…";
        public string StoppedHosting => "ホスト終了";
        public string HostingOnPort => "ポート {0}";
        public string PlayerJoinedSession => "{0} があなたのセッションに参加しました";
        public string PlayerLeftSession => "{0} があなたのセッションから退出しました";
        public string HasConnected => "{0} が接続しました";
        public string HasDisconnected => "{0} が切断しました";
        public string HasChangedNicknameTo => "{0} がニックネームを {1} に変更しました";
        public string HasUpdatedTheirColor => "{0} が色を変更しました";
        public string ServerInformation => "サーバー情報";
        public string AudioSettings => "オーディオ設定";
        public string MicrophoneDevices => "マイクデバイス";
        public string GeneralSettings => "一般設定";
        public string PlayerCustomization => "プレイヤー設定";
        public string SuitTint => "スーツの色：";
        public string Red => "赤：";
        public string Green => "緑：";
        public string Blue => "青：";
    }

    public class KoreanLanguage : ILanguage
    {
        public string MultiplayerTitle => "멀티플레이어";
        public string Enter => "확인";
        public string PasswordForSession => "비밀번호 입력:\n{0}";
        public string TabLobby => "로비";
        public string TabPlayer => "플레이어";
        public string TabGeneral => "일반";
        public string TabAudio => "오디오";
        public string TabLAN => "LAN";
        public string ButtonBack => "뒤로";
        public string HostLobby => "로비 만들기";
        public string StopHosting => "종료";
        public string LobbyBrowser => "로비 목록";
        public string Refresh => "새로 고침";
        public string Refreshing => "로딩 중…";
        public string LobbyPassword => "비밀번호 (빈 칸=공개)";
        public string HostLAN => "호스트 (LAN)";
        public string ServerIP => "서버 IP:";
        public string ServerPort => "포트:";
        public string PasswordOptional => "비밀번호(선택):";
        public string Disconnect => "연결 끊기";
        public string Connect => "연결";
        public string DisableMicrophone => "마이크 끄기";
        public string EnableMicrophone => "마이크 켜기";
        public string Deafen => "소리 끄기";
        public string Undeafen => "소리 켜기";
        public string EnablePushToTalk => "눌러서 말하기 사용";
        public string DisablePushToTalk => "눌러서 말하기 해제";
        public string PushToTalkKey => "말하기 키:";
        public string MicrophoneGain => "마이크 게인:";
        public string AudioDevice => "오디오 장치:";
        public string NoDevicesFound => "장치 없음";
        public string EnableCollisions => "충돌 사용";
        public string DisableCollisions => "충돌 해제";
        public string EnablePlayerCutsceneVisibility => "컷신에서 플레이어 표시";
        public string DisablePlayerCutsceneVisibility => "컷신에서 플레이어 숨김";
        public string EnableNametags => "이름표 표시";
        public string DisableNametags => "이름표 숨김";
        public string PlayerListKeyLabel => "목록 키:";
        public string ChatMenuKeyLabel => "채팅 키:";
        public string UpdateNameAndAppearance => "이름 및 외형 변경";
        public string Nickname => "닉네임:";
        public string PressAnyKey => "아무 키나 누르세요…";
        public string Language => "언어:";
        public string Level => "레벨:";
        public string ConnectedPlayers => "접속 중인 플레이어";
        public string NoPlayersConnected => "접속한 플레이어가 없습니다.";
        public string You => "나";
        public string ConnectedToServer => "서버에 연결됨";
        public string AppearanceUpdated => "외형이 변경되었습니다";
        public string JoiningServer => "{0} 참가 중…";
        public string CreatingLobby => "Steam 로비 생성 중…";
        public string StoppedHosting => "호스팅 중단";
        public string HostingOnPort => "포트 {0}";
        public string PlayerJoinedSession => "{0} 님이 세션에 참가했습니다";
        public string PlayerLeftSession => "{0} 님이 세션을 떠났습니다";
        public string HasConnected => "{0} 님이 접속했습니다";
        public string HasDisconnected => "{0} 님이 퇴장했습니다";
        public string HasChangedNicknameTo => "{0} 님이 이름을 {1}(으)로 변경했습니다";
        public string HasUpdatedTheirColor => "{0} 님이 색상을 변경했습니다";
        public string ServerInformation => "서버 정보";
        public string AudioSettings => "오디오 설정";
        public string MicrophoneDevices => "마이크 장치";
        public string GeneralSettings => "일반 설정";
        public string PlayerCustomization => "플레이어 설정";
        public string SuitTint => "슈트 색상:";
        public string Red => "빨강:";
        public string Green => "초록:";
        public string Blue => "파랑:";
    }

    public class ChineseSimplifiedLanguage : ILanguage
    {
        public string MultiplayerTitle => "多人游戏";
        public string Enter => "确认";
        public string PasswordForSession => "输入密码：\n{0}";
        public string TabLobby => "大厅";
        public string TabPlayer => "玩家";
        public string TabGeneral => "通用";
        public string TabAudio => "音频";
        public string TabLAN => "LAN";
        public string ButtonBack => "返回";
        public string HostLobby => "创建大厅";
        public string StopHosting => "停止托管";
        public string LobbyBrowser => "大厅列表";
        public string Refresh => "刷新";
        public string Refreshing => "刷新中…";
        public string LobbyPassword => "大厅密码（空=公开）";
        public string HostLAN => "托管 (LAN)";
        public string ServerIP => "服务器IP：";
        public string ServerPort => "端口：";
        public string PasswordOptional => "密码（可选）：";
        public string Disconnect => "断开连接";
        public string Connect => "连接";
        public string DisableMicrophone => "关闭麦克风";
        public string EnableMicrophone => "开启麦克风";
        public string Deafen => "关闭声音";
        public string Undeafen => "恢复声音";
        public string EnablePushToTalk => "启用按键说话";
        public string DisablePushToTalk => "禁用按键说话";
        public string PushToTalkKey => "说话按键：";
        public string MicrophoneGain => "麦克风音量：";
        public string AudioDevice => "音频设备：";
        public string NoDevicesFound => "未找到设备";
        public string EnableCollisions => "启用碰撞";
        public string DisableCollisions => "禁用碰撞";
        public string EnablePlayerCutsceneVisibility => "过场动画中显示玩家";
        public string DisablePlayerCutsceneVisibility => "过场动画中隐藏玩家";
        public string EnableNametags => "显示名称";
        public string DisableNametags => "隐藏名称";
        public string PlayerListKeyLabel => "玩家列表键：";
        public string ChatMenuKeyLabel => "聊天键：";
        public string UpdateNameAndAppearance => "更新名称和外观";
        public string Nickname => "昵称：";
        public string PressAnyKey => "按任意键…";
        public string Language => "语言：";
        public string Level => "级别：";
        public string ConnectedPlayers => "已连接玩家";
        public string NoPlayersConnected => "没有玩家连接。";
        public string You => "你";
        public string ConnectedToServer => "已连接到服务器";
        public string AppearanceUpdated => "外观已更新";
        public string JoiningServer => "正在加入 {0}…";
        public string CreatingLobby => "正在创建Steam大厅…";
        public string StoppedHosting => "已停止托管";
        public string HostingOnPort => "端口 {0} 托管";
        public string PlayerJoinedSession => "{0} 加入了你的会话";
        public string PlayerLeftSession => "{0} 离开了你的会话";
        public string HasConnected => "{0} 已连接";
        public string HasDisconnected => "{0} 已断开";
        public string HasChangedNicknameTo => "{0} 将昵称改为 {1}";
        public string HasUpdatedTheirColor => "{0} 更新了颜色";
        public string ServerInformation => "服务器信息";
        public string AudioSettings => "音频设置";
        public string MicrophoneDevices => "麦克风设备";
        public string GeneralSettings => "常规设置";
        public string PlayerCustomization => "玩家自定义";
        public string SuitTint => "服装颜色：";
        public string Red => "红：";
        public string Green => "绿：";
        public string Blue => "蓝：";
    }

    public static class LanguageNativeNames
    {
        private static readonly System.Collections.Generic.Dictionary<string, string> _names = new()
        {
            { "English",             "English" },
            { "Spanish",             "Español" },
            { "French",              "Français" },
            { "German",              "Deutsch" },
            { "Japanese",            "日本語" },
            { "Korean",              "한국어" },
            { "Chinese Simplified",  "中文(简体)" },
            { "Chinese Traditional", "中文(繁體)" },
            { "ChineseSimplified",   "中文(简体)" },
            { "ChineseTraditional",  "中文(繁體)" },
            { "Russian",             "Русский" },
            { "Portuguese",          "Português (BR)" },
        };

        public static string GetNativeName(string key)
            => _names.TryGetValue(key, out var n) ? n : key;
    }

    public class RussianLanguage : ILanguage
    {
        public string MultiplayerTitle => "Мультиплеер";
        public string Enter => "Подтвердить";
        public string PasswordForSession => "Введите пароль для:\n{0}";
        public string TabLobby => "ЛОББИ";
        public string TabPlayer => "Игрок";
        public string TabGeneral => "Общее";
        public string TabAudio => "Звук";
        public string TabLAN => "LAN";
        public string ButtonBack => "Назад";
        public string HostLobby => "Создать лобби";
        public string StopHosting => "Остановить";
        public string LobbyBrowser => "Список лобби";
        public string Refresh => "Обновить";
        public string Refreshing => "Обновление…";
        public string LobbyPassword => "Пароль лобби (пусто = публичное)";
        public string HostLAN => "Хост (LAN)";
        public string ServerIP => "IP сервера:";
        public string ServerPort => "Порт:";
        public string PasswordOptional => "Пароль (необязательно):";
        public string Disconnect => "Отключиться";
        public string Connect => "Подключиться";
        public string DisableMicrophone => "Выключить микрофон";
        public string EnableMicrophone => "Включить микрофон";
        public string Deafen => "Заглушить";
        public string Undeafen => "Включить звук";
        public string EnablePushToTalk => "Включить Push-to-Talk";
        public string DisablePushToTalk => "Выключить Push-to-Talk";
        public string PushToTalkKey => "Кнопка PTT:";
        public string MicrophoneGain => "Усиление микрофона:";
        public string AudioDevice => "Аудиоустройство:";
        public string NoDevicesFound => "Устройства не найдены";
        public string EnableCollisions => "Включить столкновения";
        public string DisableCollisions => "Выключить столкновения";
        public string EnablePlayerCutsceneVisibility => "Показывать игроков в катсценах";
        public string DisablePlayerCutsceneVisibility => "Скрывать игроков в катсценах";
        public string EnableNametags => "Показывать имена";
        public string DisableNametags => "Скрывать имена";
        public string PlayerListKeyLabel => "Клавиша списка:";
        public string ChatMenuKeyLabel => "Клавиша чата:";
        public string UpdateNameAndAppearance => "Обновить имя и внешность";
        public string Nickname => "Никнейм:";
        public string PressAnyKey => "Нажмите любую клавишу…";
        public string Language => "Язык:";
        public string Level => "Уровень:";
        public string ConnectedPlayers => "Подключённые игроки";
        public string NoPlayersConnected => "Нет подключённых игроков.";
        public string You => "Вы";
        public string ConnectedToServer => "Подключено к серверу";
        public string AppearanceUpdated => "Внешность обновлена";
        public string JoiningServer => "Подключение к {0}…";
        public string CreatingLobby => "Создание лобби Steam…";
        public string StoppedHosting => "Хостинг остановлен";
        public string HostingOnPort => "Хостинг на порту {0}";
        public string PlayerJoinedSession => "{0} присоединился к сессии";
        public string PlayerLeftSession => "{0} покинул сессию";
        public string HasConnected => "{0} подключился";
        public string HasDisconnected => "{0} отключился";
        public string HasChangedNicknameTo => "{0} изменил ник на {1}";
        public string HasUpdatedTheirColor => "{0} изменил цвет";
        public string ServerInformation => "Информация о сервере";
        public string AudioSettings => "Настройки звука";
        public string MicrophoneDevices => "Устройства микрофона";
        public string GeneralSettings => "Общие настройки";
        public string PlayerCustomization => "Настройка персонажа";
        public string SuitTint => "Цвет костюма:";
        public string Red => "Красный:";
        public string Green => "Зелёный:";
        public string Blue => "Синий:";
    }

    public class PortugueseLanguage : ILanguage
    {
        public string MultiplayerTitle => "Multijogador";
        public string Enter => "Confirmar";
        public string PasswordForSession => "Senha para:\n{0}";
        public string TabLobby => "LOBBY";
        public string TabPlayer => "Jogador";
        public string TabGeneral => "Geral";
        public string TabAudio => "Áudio";
        public string TabLAN => "LAN";
        public string ButtonBack => "Voltar";
        public string HostLobby => "Criar Sala";
        public string StopHosting => "Parar";
        public string LobbyBrowser => "Salas";
        public string Refresh => "Atualizar";
        public string Refreshing => "Atualizando…";
        public string LobbyPassword => "Senha da sala (vazio = pública)";
        public string HostLAN => "Hospedar (LAN)";
        public string ServerIP => "IP do servidor:";
        public string ServerPort => "Porta:";
        public string PasswordOptional => "Senha (opcional):";
        public string Disconnect => "Desconectar";
        public string Connect => "Conectar";
        public string DisableMicrophone => "Desativar microfone";
        public string EnableMicrophone => "Ativar microfone";
        public string Deafen => "Silenciar";
        public string Undeafen => "Dessilenciar";
        public string EnablePushToTalk => "Ativar Push to Talk";
        public string DisablePushToTalk => "Desativar Push to Talk";
        public string PushToTalkKey => "Tecla PTT:";
        public string MicrophoneGain => "Ganho do microfone:";
        public string AudioDevice => "Dispositivo de áudio:";
        public string NoDevicesFound => "Nenhum dispositivo";
        public string EnableCollisions => "Ativar colisões";
        public string DisableCollisions => "Desativar colisões";
        public string EnablePlayerCutsceneVisibility => "Mostrar jogador nas cutscenes";
        public string DisablePlayerCutsceneVisibility => "Ocultar jogador nas cutscenes";
        public string EnableNametags => "Mostrar nomes";
        public string DisableNametags => "Ocultar nomes";
        public string PlayerListKeyLabel => "Tecla da lista:";
        public string ChatMenuKeyLabel => "Tecla do chat:";
        public string UpdateNameAndAppearance => "Atualizar nome e aparência";
        public string Nickname => "Apelido:";
        public string PressAnyKey => "Pressione qualquer tecla…";
        public string Language => "Idioma:";
        public string Level => "Nível:";
        public string ConnectedPlayers => "Jogadores conectados";
        public string NoPlayersConnected => "Nenhum jogador conectado.";
        public string You => "Você";
        public string ConnectedToServer => "Conectado ao servidor";
        public string AppearanceUpdated => "Aparência atualizada";
        public string JoiningServer => "Entrando em {0}…";
        public string CreatingLobby => "Criando sala Steam…";
        public string StoppedHosting => "Hospedagem encerrada";
        public string HostingOnPort => "Hospedando na porta {0}";
        public string PlayerJoinedSession => "{0} entrou na sua sessão";
        public string PlayerLeftSession => "{0} saiu da sua sessão";
        public string HasConnected => "{0} conectou-se";
        public string HasDisconnected => "{0} desconectou-se";
        public string HasChangedNicknameTo => "{0} mudou o apelido para {1}";
        public string HasUpdatedTheirColor => "{0} atualizou a cor";
        public string ServerInformation => "Informações do servidor";
        public string AudioSettings => "Configurações de áudio";
        public string MicrophoneDevices => "Dispositivos de microfone";
        public string GeneralSettings => "Configurações gerais";
        public string PlayerCustomization => "Personalização";
        public string SuitTint => "Cor do traje:";
        public string Red => "Vermelho:";
        public string Green => "Verde:";
        public string Blue => "Azul:";
    }

    public class ChineseTraditionalLanguage : ILanguage
    {
        public string MultiplayerTitle => "多人遊戲";
        public string Enter => "確認";
        public string PasswordForSession => "輸入密碼：\n{0}";
        public string TabLobby => "大廳";
        public string TabPlayer => "玩家";
        public string TabGeneral => "通用";
        public string TabAudio => "音訊";
        public string TabLAN => "LAN";
        public string ButtonBack => "返回";
        public string HostLobby => "建立大廳";
        public string StopHosting => "停止托管";
        public string LobbyBrowser => "大廳列表";
        public string Refresh => "重新整理";
        public string Refreshing => "重新整理中…";
        public string LobbyPassword => "大廳密碼（空=公開）";
        public string HostLAN => "托管 (LAN)";
        public string ServerIP => "伺服器IP：";
        public string ServerPort => "連接埠：";
        public string PasswordOptional => "密碼（選填）：";
        public string Disconnect => "中斷連線";
        public string Connect => "連線";
        public string DisableMicrophone => "關閉麥克風";
        public string EnableMicrophone => "開啟麥克風";
        public string Deafen => "關閉聲音";
        public string Undeafen => "恢復聲音";
        public string EnablePushToTalk => "啟用按鍵說話";
        public string DisablePushToTalk => "停用按鍵說話";
        public string PushToTalkKey => "說話按鍵：";
        public string MicrophoneGain => "麥克風音量：";
        public string AudioDevice => "音訊裝置：";
        public string NoDevicesFound => "找不到裝置";
        public string EnableCollisions => "啟用碰撞";
        public string DisableCollisions => "停用碰撞";
        public string EnablePlayerCutsceneVisibility => "過場動畫中顯示玩家";
        public string DisablePlayerCutsceneVisibility => "過場動畫中隱藏玩家";
        public string EnableNametags => "顯示名稱";
        public string DisableNametags => "隱藏名稱";
        public string PlayerListKeyLabel => "玩家清單鍵：";
        public string ChatMenuKeyLabel => "聊天鍵：";
        public string UpdateNameAndAppearance => "更新名稱與外觀";
        public string Nickname => "暱稱：";
        public string PressAnyKey => "按任意鍵…";
        public string Language => "語言：";
        public string Level => "等級：";
        public string ConnectedPlayers => "已連線玩家";
        public string NoPlayersConnected => "沒有玩家連線。";
        public string You => "你";
        public string ConnectedToServer => "已連線至伺服器";
        public string AppearanceUpdated => "外觀已更新";
        public string JoiningServer => "正在加入 {0}…";
        public string CreatingLobby => "正在建立Steam大廳…";
        public string StoppedHosting => "已停止托管";
        public string HostingOnPort => "連接埠 {0} 托管";
        public string PlayerJoinedSession => "{0} 加入了你的工作階段";
        public string PlayerLeftSession => "{0} 離開了你的工作階段";
        public string HasConnected => "{0} 已連線";
        public string HasDisconnected => "{0} 已中斷連線";
        public string HasChangedNicknameTo => "{0} 將暱稱改為 {1}";
        public string HasUpdatedTheirColor => "{0} 更新了顏色";
        public string ServerInformation => "伺服器資訊";
        public string AudioSettings => "音訊設定";
        public string MicrophoneDevices => "麥克風裝置";
        public string GeneralSettings => "一般設定";
        public string PlayerCustomization => "玩家自訂";
        public string SuitTint => "服裝顏色：";
        public string Red => "紅：";
        public string Green => "綠：";
        public string Blue => "藍：";
    }
}
