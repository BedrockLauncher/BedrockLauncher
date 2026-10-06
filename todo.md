Non voglio più una soluzione che richieda che il GDK sia già installato dal Microsoft Store.

Il launcher ha già un Microsoft Account login nel Welcome flow. Questo login deve essere parte integrante del percorso GDK: autenticazione → entitlement Minecraft → download → installazione/gestione GDK → launch.

Studia il flusso GDK attuale di LeviLauncher nel repository LiteLDev/LeviLauncher e confrontalo con il nostro BedrockLauncher.

In particolare cerca e analizza il loro percorso:
- Microsoft/Xbox authentication
- entitlement/licensing
- download del MSIXVC
- InstallExtractMsixvc / equivalente
- gestione del payload MSIXVC
- registrazione dell'istanza
- launch

Poi confrontalo con:
- BedrockLauncher/Handlers/PackageHandler.cs
- BedrockLauncher/ViewModels/MainDataModel.cs
- BedrockLauncher/Classes/MCVersion.cs
- il codice Microsoft Account già presente nel nostro progetto
- todo.md

IMPORTANTE:

Non voglio bypassare licenze, DRM, firma dei pacchetti, entitlement o autenticazione Microsoft.

Al contrario, il Microsoft Account login già implementato deve essere utilizzato per verificare che l'utente abbia effettivamente l'entitlement necessario.

Non voglio neanche che il GDK venga trattato come UWP.

Voglio due pipeline separate:

UWP:
download → extract → register → verify → launch

GDK:
Microsoft authentication/entitlement → download MSIXVC → GDK-specific installation/extraction flow → register/manage instance → launch

Non aggiungere workaround basati sul Microsoft Store come prerequisito per ogni versione GDK.

Non continuare a rattoppare MainDataModel.Play() semplicemente per evitare InstallPackage.

Prima di modificare il codice, fammi un'analisi concreta del codice di LeviLauncher e del nostro codice e indicami:
1. quali componenti GDK di LeviLauncher dobbiamo replicare;
2. quali possiamo riutilizzare/adattare dalle classi esistenti;
3. quale parte del nostro attuale PackageHandler è diventata inutile;
4. come deve diventare il call chain completo del pulsante Play;
5. come deve essere usato il Microsoft Account login già presente.

Dopo l'analisi, implementa la soluzione nel nostro progetto senza creare codice duplicato inutilmente.

Mantieni UWP invariato.

Compila il progetto e correggi gli errori di compilazione.

Alla fine mostrami:
- file modificati;
- call chain finale GDK;
- call chain finale UWP;
- come viene verificato l'entitlement Microsoft;
- come viene installato/gestito il GDK;
- come viene lanciato.