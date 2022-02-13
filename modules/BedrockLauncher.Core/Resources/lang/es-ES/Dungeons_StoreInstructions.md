# Parte 1 - El script de Powershell
La versión de Windows Store de Minecraft Dungeons normalmente no te permite modificar sus archivos/carpetas. Para evitar ese problema, sigue estos pasos:

## Prerrequisitos:
- Deshabilita temporalmente o elimina tu antivirus. La mayoría detectará que se está ejecutando un script desconocido e intentará detenerlo.
- Si usas Bitdefender, necesitas desinstalarlo antes de continuar, ya que rompe el script incluso cuando está apagado.
- Asegúrate de tener al menos 10 GB de espacio libre.
- Asegúrate de que tu juego está actualizado. Para ello, haz Win + R, pon`ms-windows-store://DownloadsAndUpdates/` y pulsa Enter. Después, dale a "Obtener actualizaciones" en la esquina superior derecha de la ventana que se abrirá.
- Instala [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Incluso si crees que lo tienes instalado, pruebe a usar el instalador. Podrías tener una versión anterior que no funcione.

## En el lanzador de Bedrock:
1. Make Sure your game variant is set to `Microsoft Store`
3. Click on `Install Store Patch`

## In the Powershell Window:

3. You will be asked to select a folder. Choose an empty folder where you want the game to be moved to. Do not choose a folder in Program Files or One Drive, it will break things.
4. The game will open at one point. Do not close it when this happens. If you run into any issues, make sure to check the Troubleshooting section below.
5. A `~mods` folder will appear. This is where you place your mods.
7. Launching the modded game is just like launching the regular game. You can do it from the start menu, Windows Store, Xbox app, and so on, just like you normally do. Do NOT try to launch it by running the .exe files in the game folder.

## Troubleshooting:
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



