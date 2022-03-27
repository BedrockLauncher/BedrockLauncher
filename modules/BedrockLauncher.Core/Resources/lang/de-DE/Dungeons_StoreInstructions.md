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
- If you get an error saying it can't verify your ownership of the game, you must have launched the game using the .exe file. Don't do that. Run the game from the start menu, Windows Store, or Xbox app. If you did, but still get this error, reinstall the regular game and log in at least once (open the game and select a character) before patching it.

# Part 2 - The Bedrock Launcher Setup
1. Set the install location to the folder containing `Dungeons.exe`. It should be in a parent folder of the `~mods` folder from earlier
2. Select where you want your symbolic mods folder to be (it should not be in the same location as `~mods` folder from earlier)
3. Click on `Install Symbolic Link`
4. Your game's mod folder should now be paired with your symbolic mods folder

# How to Update
1. Click on `Uninstall Symbolic Link`
2. Click on `Update Store Patch`
3. Repeat Steps 3 through 6 of Part 1 if nessisary
4. Repeat Steps 1 through 4 of Part 2
5. Done



