# Osa 1 - Powershell-skripti
Minecraft Dungeonsin Windows Store-versio ei normaalisti anna sinun muokata sen tiedostoja/kansioita. Kiertääksesi tämän ongelman, seuraa näitä ohjeita:

## Ennakkovaatimukset:
- Ota antivirus-ohjelmistosi pois käytöstä väliaikaisesti. Useimmat havaitsevat tuntemattoman skriptin olevan käynnissä ja yrittävät pysäyttää sen.
- Jos käytät Bitdefenderiä, sinun on poistettava se ennen jatkamista, koska se rikkoo skriptin vaikka se olisi kytketty pois päältä.
- Varmista, että sinulla on ainakin 10 GB tallennustilaa vapaana.
- Varmista, että pelisi on ajan tasalla. Tehdäksesi tämän, paina Win + R, ja syötä `ms-windows-store://DownloadsAndUpdates/` ja paina enteriä. Sitten paina "Get updates" avautuneen ikkunan yläoikeassa kulmassa.
- Asenna [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Vaikka luulisitkin, että se olisi sinulla asennettuna, yritä silti asenninta. Sinulla voi olla vanhempi versio joka ei toimi.

## Bedrock Launcherissa:
1. Varmista että pelivarianttisi on asetettu `Microsoft Store`ksi
3. Paina `Asenna Store Patch`

## Powershell-ikkunassa:

3. Sinua tullaan pyytämään valitsemaan kansio. Valitse tyhjä kansio minne haluat pelisi siirrettävän. Älä valitse kansiota Program Filesissä tai OneDrivessä, koska se tulee rikkomaan asioita.
4. Peli tulee avautumaan jossain kohtaa. Älä sulje sitä kun tämä tapahtuu. Jos kohtaat ongelmia, tarkistathan vianmääritysosion alla.
5. `~mods`-kansio tulee ilmestymään. Tämä on paikka minne laitat modisi.
7. Modatun pelin käynnistäminen on samanlaista kuin tavallisen pelin käynnistäminen. Voit tehdä sen käynnistysvalikosta, Windows Storesta, Xbox-sovelluksesta, ja niin edelleen, kuten normaalisti tekisitkin. ÄLÄ yritä käynnistää sitä suorittamalla .exe-tiedostoja pelikansiossa.

## Vianmääritys:
- Jos kohtaat ongelmia kun patchaat peliä/sen jälkeen, jokin näistä voi auttaa sinua.
- Jos peli ei avautunut ollenkaan kun patchasit tai patchaaminen ei toiminut, yritä pelin avaamista manuaalisesti ennen patchaajan suorittamista. Pidä peli avoinna kunnes se joko sulkee itsensä tai kun patchaaja valmistuu.
- Jos saat virheen joka ilmoittaa, että pelin omistajuuttasi ei voida vahvistaa, olet käynnistänyt pelin käyttäen .exe-tiedostoa. Älä tee sitä. Suorita peli käynnistysvalikosta, Windows Storesta, tai Xbox-sovelluksesta. Jos teit niin, mutta saat tämän virheviestin edelleen, asenna normaali peli uudelleen ja kirjaudu sisään ainakin kerran (avaa peli ja valitse hahmo) ennen patchaamista.

# Osa 2 - Bedrock Launcherin määritys
1. Aseta asennussijainti kansioon, joka sisältää `Dungeons.exe`n. Sen pitäisi olla aikaisemmin mainittua `~mods`-kansiota ylemmässä kansiossa
2. Valitse missä haluat symbolisen modikansiosi olevan (sen ei pitäisi olla samassa sijainnissa kuin aiempana mainittu `~mods`-kansio)
3. Paina `Asenna symbolinen linkki`
4. Pelisi modikansion pitäisi olla nyt yhdistetty symbolisen modikansiosi kanssa

# Kuinka päivittää
1. Paina `Poista symbolinen linkki`
2. Paina `Päivitä Store Patch`
3. Toista Osa 1:n vaiheet 3 - 6 jos tarpeellista
4. Toista Osa 2:n vaiheet 1 - 4
5. Valmis



