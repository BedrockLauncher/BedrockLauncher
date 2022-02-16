# Parte 1 - El script de Powershell
La versión de Windows Store de Minecraft Dungeons normalmente no te permite modificar sus archivos/carpetas. Para evitar ese problema, sigue estos pasos:

## Prerrequisitos:
- Deshabilita temporalmente o elimina tu antivirus. La mayoría detectará que se está ejecutando un script desconocido e intentará detenerlo.
- Si usas Bitdefender, necesitas desinstalarlo antes de continuar, ya que rompe el script incluso cuando está apagado.
- Asegúrate de tener al menos 10 GB de espacio libre.
- Asegúrate de que tu juego está actualizado. Para ello, haz Win + R, pon`ms-windows-store://DownloadsAndUpdates/` y pulsa Enter. Después, dale a "Obtener actualizaciones" en la esquina superior derecha de la ventana que se abrirá.
- Instala [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Incluso si crees que lo tienes instalado, pruebe a usar el instalador. Podrías tener una versión anterior que no funcione.

## En el lanzador de Bedrock:
1. Asegúrate de que la variante de tu juego está establecida como `Microsoft Store`
3. Haz clic en `Instalar Parche de Tienda`

## En la ventana de Powershell:

3. Se te pedirá que elijas una carpeta. Elige una carpeta vacía a la que quieras mover el juego. No elijas una carpeta en los Archivos del Programa o en OneDrive, eso rompe cosas.
4. El juego se abrirá en algún momento. No lo cierres cuando ocurra esto. Si te encuentras algún problema, asegúrate de revisar la sección de Solución de Problemas a continuación.
5. Una carpeta llamada `~mods` aparecerá. Aquí es donde pones tus mods.
7. Lanzar el juego con mods es igual que lanzar el juego normal. Puedes hacerlo desde el menú de inicio, Windows Store, la aplicación de Xbox, etc., como haces normalmente. NO intente lanzarlo ejecutando los archivos .exe en la carpeta del juego.

## Solución de Problemas:
- Si te encuentras con algún problema durante o después de parchear el juego, algunas de estas cosas podrían ayudarte.
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



