# Teil 1 - Das Powershell-Skript
Die Windows Store Version von Minecraft Dungeons lässt dich normalerweise nicht die Dateien/Ordner modifizieren. Um dieses Problem zu umgehen, folge diesen Schritten:

## Voraussetzungen:
- Deaktiviere Temporär jegliche Antivirus-Software. Die meisten werden erkennen, dass ein Unbekannter Skript ausgeführt wird und werden diesen versuchen zu stoppen.
- Wenn du Bitdefender benutzt, musst du es Deinstallieren bevor du Fortfährst, weil es das Skript zerstören wird, auch wenn es Deaktiviert ist.
- Stelle sicher, dass du mindestens 10 GB freien Speicher zur Verfügung hast.
- Stelle sicher, dass dein Spiel auf dem neusten Stand ist. Um das zu tun, drücke Win + R, gebe `ms-windows-store://DownloadsAndUpdates/` ein und drücke Eingabe. Dann drücke "Get Updates" in der oberen Rechten Ecke des Fensters, welches sich öffnet.
- Installiere [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Auch wenn du denkst, du hast es installiert, probiere den Installer aus. Du hast vielleicht eine ältere Version, welche nicht funktioniert.

## Im Bedrock Launcher:
1. Stelle sicher, dass deine Spielvariante zu `Microsoft Store` gesetzt ist
3. Klicke auf `Install Store Patch`

## Im Powershell Fenster:

3. Du wirst gefragt, einen Ordner auszuwählen. Wähle einen leeren Ordner aus, in den das Spiel verschoben werden soll. Wähle keinen Ordner aus, der in Program Files oder in der OneDrive gespeichert ist, das wird es zerstören.
4. Das Spiel wird an einem Punkt geöffnet. Schließe es nicht, während es passiert. Wenn du auf irgendwelche Probleme stößt, überprüfe den Abschnitt Fehlerbehebung.
5. Ein `~mods` Ordner wird erscheinen. Hier platzierst du deine Mods.
7. Das Starten des modifizierten Spiels gleicht dem Start des regulären Spiels. Du kannst es über das Startmenü, über den Windows Store, über die Xbox App usw. machen, so wie du es normalerweise machst. Versuche es NICHT, es zu starten, indem du die .exe-Datei im Spielordner öffnest.

## Fehlerbehebung:
- Wenn du auf irgendwelche Probleme stößt während/nach dem Patchen des Spiels, könnten dir manche dinge helfen.
- Wenn das Spiel während des Patchens nicht geöffnet wurde und das Patchen nicht funktioniert hat, versuche das Spiel manuell zu öffnen bevor du den Patcher ausführst. Lasse das Spiel offen bis es sich entweder von allein schließt oder der Patcher beendet ist.
- Wenn du einen Fehler bekommst, welcher sagt, dass es nicht den Besitzer des Spiels verifizieren kann, musst du das Spiel mit der .exe-Datei gestartet haben. Mach dies nicht. Führe das Spiel aus dem Startmenü, aus dem Windows Store oder der Xbox App aus. Wenn du das gemacht hast, aber trotzdem den Fehler bekommst, installiere das reguläre Spiel neu und melde dicht mindestens ein mal an (öffne das Spiel und wähle einen Charakter aus), bevor du es Patchst.

# Teil 2 - Das Bedrock Launcher Setup
1. Lege den Installationsort in den Ordner mit `Dungeons.exe` fest. Es sollte in einem übergeordneten Ordner des `~mods` Ordners von früher sein
2. Wähle aus, wo der Symbolische Modordner erstellt werden soll (dieser sollte nicht in deinem `~mods` Ordner von früher sein)
3. Klicke auf `Install Symbolic Link`
4. Der Modordner deines Spiels sollte jetzt mit dem symbolischen Modordner gekoppelt sein

# Wie man Updatet
1. Klicke auf `Uninstall Symbolic Link`
2. Klicke auf `Update Store Patch`
3. Wiederhole die Schritte von 3 bis 6 von Teil 1, wenn erforderlich
4. Wiederhole die Schritte 1 bis 4 von Teil 2
5. Fertig



