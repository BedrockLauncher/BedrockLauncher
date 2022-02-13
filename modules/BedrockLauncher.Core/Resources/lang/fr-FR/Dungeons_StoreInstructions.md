# Partie 1 - Le script Powershell
La version du Windows Store de Minecraft Dungeons ne vous laisse normalement pas modifier ses fichiers/dossiers. Pour contourner ce problème, suivez les étapes suivantes :

## Prérequis :
- Désactivez temporairement tout logiciel antivirus. La plupart détecteront qu'un script inconnu est en cours d'exécution et essaieront de l'arrêter.
- Si vous utilisez Bitdefender, vous devez le désinstaller avant de continuer, car il casse le script même lorsqu'il est désactivé.
- Assurez-vous d'avoir au moins 10 Go d'espace libre.
- Assurez-vous que votre jeu est à jour. Pour cela, appuyez sur Win + R, tapez `ms-windows-store://DownloadsAndUpdates/` et appuyez sur Entrée. Ensuite, appuyez sur "Obtenir des mises à jour" dans le coin supérieur droit de la fenêtre qui s'ouvre.
- Installez [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Même si vous pensez l'avoir installé, essayez l'installateur. Il se peut que vous ayez une ancienne version qui ne fonctionnera pas.

## Dans le Bedrock Launcher :
1. Assurez-vous que votre variante du jeu est configurée à `Microsoft Store`
3. Cliquez sur `Install Store Patch`

## Dans la fenêtre Powershell :

3. Il vous sera demandé de sélectionner un dossier. Choisissez un dossier vide auquel vous voulez que le jeu soit déplacé. Ne choisissez pas un dossier dans Program Files ou One Drive, cela ne fonctionnera pas correctement.
4. Le jeu s'ouvrira au bout d'un moment. Ne le fermez pas lorsque cela se produit. Si vous rencontrez des problèmes, vérifiez la section Dépannage ci-dessous.
5. Un dossier `~mods` va apparaître. C'est ici que vous placez vos mods.
7. Lancer le jeu moddé est comme lancer le jeu habituel. Vous pouvez le faire à partir du menu de démarrage, du Windows Store, de l'application Xbox et ainsi de suite, comme vous le faites normalement. N'essayez PAS de le lancer en exécutant les fichiers .exe dans le dossier du jeu.

## Dépannage :
- Si vous rencontrez des problèmes pendant/après la mise à jour du jeu, certaines de ces choses peuvent vous aider.
- Si le jeu n'a pas du tout été ouvert pendant la mise à jour et que la mie à jour n'a pas fonctionné, essayez d'ouvrir le jeu manuellement avant d'exécuter le lanceur. Gardez le jeu ouvert jusqu'à ce qu'il se ferme par lui-même ou que la mise à jour se termine.
- Si vous obtenez une erreur disant qu'il ne peut pas vérifier votre propriété du jeu, vous devez avoir lancé le jeu en utilisant le fichier .exe. Ne faites pas cela. Exécutez le jeu à partir du menu de démarrage, du Windows Store ou de l'application Xbox. Si vous l'avez fait, mais toujours obtenez cette erreur, réinstallez le jeu normal et connectez-vous au moins une fois (ouvrez le jeu et sélectionnez un personnage) avant de le mettre à jour.

# Partie 2 - Le paramétrage du Bedrock Launcher
1. Définissez l'emplacement d'installation au dossier contenant `Dungeons.exe`. Il devrait être dans un dossier parent du dossier `~mods` de tout à l'heure
2. Sélectionnez l'emplacement souhaité pour votre dossier de mods symboliques (il ne devrait pas être placé dans votre dossier `~mods` de tout à l'heure)
3. Cliquez sur `Installer un lien symbolique`
4. Le dossier des mods de votre jeu devrait maintenant être jumelé à votre dossier de mods symboliques

# Comment mettre à jour
1. Cliquez sur `Désinstaller un lien symbolique`
2. Cliquez sur `Update Store Patch`
3. Répétez les étapes 3 à 6 de la partie 1 si nessisaire
4. Répétez les étapes 1 à 4 de la partie 2
5. Terminé



