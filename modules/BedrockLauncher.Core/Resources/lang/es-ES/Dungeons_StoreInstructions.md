# Parte 1 - El script Powershell
La versión de Windows Store de Minecraft Dungeons normalmente no le permitirá modificar sus archivos/carpetas. Para evitar ese problema, siga estos pasos:

## Prerrequisitos:
- Deshabilita temporalmente o elimina tu software de antivirus. La mayoría detectará que se está ejecutando un script desconocido e intenta detenerlo.
- Si utiliza Bitdefender, necesita desinstalarlo antes de continuar, ya que rompe el script incluso cuando está apagado.
- Asegúrese de tener al menos 10 GB de espacio libre.
- Asegúrate de que tu juego está actualizado. Para ello, presiona Win + R, ingresa `ms-windows-store://DownloadsAndUpdates/` y pulsa Enter. Luego, presione "Obtener actualizaciones" en la esquina superior derecha de la ventana que se abre.
- Instala [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Incluso si cree que lo tiene instalado, pruebe con el instalador. Quizás tenga una versión anterior que no funcione.

## En el Bedrock Launcher:
1. Asegúrese de que su variante de juego está establecida en `Microsoft Store`
3. Haga clic en `Instalar Parche de Tienda`

## En la ventana de Powershell:

3. Se le pedirá que seleccione una carpeta. Elige una carpeta vacía a la que quieres mover el juego. No elijas una carpeta en los Archivos del Programa o en OneDrive, va a romper las cosas.
4. El juego se abrirá en un punto. No lo cierres cuando esto suceda. Si encuentras algún problema, asegúrate de revisar la sección de Solución de Problemas a continuación.
5. Una carpeta llamada `~mods` aparecerá. Aquí es donde usted coloca sus mods.
7. Lanzar el juego con mods es igual que lanzar el juego normal. Puede hacerlo desde el menú de inicio, Windows Store, la aplicación Xbox, etc., como lo hace normalmente. NO intente lanzarlo ejecutando los archivos .exe en la carpeta del juego.

## Solución de Problemas:
- Si te encuentras con algún problema durante o después de parchear el juego, algunas de estas cosas podrían ayudarte.
- Si el juego no se abrió mientras el parche no funcionaba y el parche no funcionó, intente abrir el juego manualmente antes de ejecutar el parche. Mantenga el juego abierto hasta que se cierre por sí mismo o el parche termine.
- Si recibes un error diciendo que no puede verificar tu propiedad del juego, debes haber iniciado el juego usando el archivo .exe. No lo hagas. Ejecuta el juego desde el menú de inicio, Windows Store o la aplicación Xbox. Si lo hiciste, pero todavía obtienes este error, reinstala el juego regular e inicia sesión al menos una vez (abre el juego y selecciona un personaje) antes de parchearlo.

# Parte 2 - Configuración del Bedrock Launcher
1. Establezca la ubicación de instalación en la carpeta que contiene `Dungeons.exe`. Debería estar en una carpeta padre de la carpeta anterior a `~mods`
2. Seleccione dónde desea que esté su carpeta de mods simbólicos (no debe colocarse dentro de su carpeta `Dungeons\Content\Techns\~mods` de antes)
3. Haz clic en `Instalar Enlace Simbólico`
4. La carpeta de mods del juego ahora debe emparejarse con la carpeta de mods simbólicos

# Cómo actualizar
1. Haz clic en `Instalar Enlace Simbólico`
2. Haga clic en `Actualizar Parche de Tienda`
3. Repetir pasos del 3 al 6 de la parte 1 si es necesario
4. Repetir pasos de 1 a 4 de la Parte 2
5. Listo



