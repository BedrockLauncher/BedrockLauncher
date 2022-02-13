# Part 1 - The Powershell Script
The Windows Store version of Minecraft Dungeons normally won't let you modify its files/folders. To get around this issue, follow these steps:

## Системні вимоги:
- Тимчасово вимкніть будь-яку антивірусну програму. Most will detect that an unknown script is being run and try to stop it.
- If you use Bitdefender, you need to uninstall it before continuing, as it breaks the script even when turned off.
- Make sure you have at least 10 GBs of space free.
- Make sure your game is up to date. To do this, press Win + R, enter `ms-windows-store://DownloadsAndUpdates/` and press enter. Then, press "Get updates" in the top right corner of the window that opens.
- Install [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Even if you think you have it installed, try the installer. You may have an older version that won't work.

## In the Bedrock Launcher:
1. Make Sure your game variant is set to `Microsoft Store`
3. Натисніть `Встановити Store Patch`

## У вікні Powershell:

3. Вам буде запропоновано вибрати теку. Choose an empty folder where you want the game to be moved to. Do not choose a folder in Program Files or One Drive, it will break things.
4. The game will open at one point. Do not close it when this happens. If you run into any issues, make sure to check the Troubleshooting section below.
5. A `~mods` folder will appear. This is where you place your mods.
7. Launching the modded game is just like launching the regular game. You can do it from the start menu, Windows Store, Xbox app, and so on, just like you normally do. Do NOT try to launch it by running the .exe files in the game folder.

## Troubleshooting:
- If you run into any issues while/after patching the game, some of these things might help you.
- If the game didn't open at all while patching and the patching didn't work, try opening the game manually before running the patcher. Keep the game open until it either closes by itself or the patcher finishes.
- Якщо ви отримуєте помилку в тому, що не вдається перевірити право власності на гру, то ви повинні були запустити гру з використанням .exe файлу. Не робіть цього. Запустіть гру з меню "Пуск", Windows Store, або Xbox app. Якщо ви це зробили, але все одно отримуєте цю помилку, перевстановите звичайну гру й увійдіть хоча б один раз (відкрити гру й вибрати символ) перед тим, як виправити її.

# Частина 2 - Налаштування Bedrock Launcher
1. Як місце встановлення вкажіть папку, що містить `Dungeons.exe`. Це має бути в батьківській папці `~моди` з більш ранньої версії
2. Select where you want your symbolic mods folder to be (it should not be in the same location as `~mods` folder from earlier)
3. Click on `Install Symbolic Link`
4. Your game's mod folder should now be paired with your symbolic mods folder

# Як оновити
1. Click on `Uninstall Symbolic Link`
2. Click on `Update Store Patch`
3. Repeat Steps 3 through 6 of Part 1 if nessisary
4. Повторіть кроки 1-4 частини 2
5. Готово



