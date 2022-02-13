# Part 1 - The Powershell Script
Normalmente la versione di Minecraft: Dungeons del Windows Store non ti permetterà di modificare i suoi file e le sue cartelle. Per aggirare questo problema, segui questi passaggi:

## Prerequisiti:
- Disattivare temporaneamente qualsiasi software antivirus. La maggior parte rileverà che uno script sconosciuto è in esecuzione e tenterà di fermarlo.
- Se utilizzi Bitdefender, è necessario disinstallarlo prima di continuare, in quanto rompe lo script anche quando disattivato.
- Assicurati di avere almeno 10 GB di spazio libero.
- Assicurati che il tuo gioco sia aggiornato. Per fare questo, premi Win + R, inserisci `ms-windows-store://DownloadsAndUpdates/` e premi invio. Poi, premi "Ricevi aggiornamenti" nell'angolo in alto a destra della finestra che si apre.
- Installa [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Anche se pensi di averlo installato, prova il programma di installazione. Potresti avere una versione precedente che non funzionerà.

## Nel Launcher di Bedrock:
1. Assicurati che la tua variante di gioco sia impostata su `Microsoft Store`
3. Click on `Install Store Patch`

## In the Powershell Window:

3. Ti verrà chiesto di selezionare una cartella. Scegli una cartella vuota in cui vuoi che il gioco venga spostato. Non scegliere una cartella in File di programma o One Drive, potrebbe rompere qualcosa.
4. Il gioco si aprirà ad un certo punto. Non chiuderla quando ciò accade. In caso di problemi, assicurati di controllare la sezione Risoluzione dei problemi qui sotto.
5. Apparirà una cartella `~mods`. Qui è dove inserirai le tue mod.
7. Avviare il gioco moddato è come avviare il gioco regolare. Puoi farlo dal menu di avvio, dal Windows Store, dall'app Xbox e così via, proprio come faresti normalmente. Non provare a avviarlo eseguendo i file .exe nella cartella di gioco.

## Risoluzione dei problemi:
- If you run into any issues while/after patching the game, some of these things might help you.
- If the game didn't open at all while patching and the patching didn't work, try opening the game manually before running the patcher. Keep the game open until it either closes by itself or the patcher finishes.
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



