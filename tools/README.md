# tools

Skripty, ktoré nie sú súčasťou aplikácie. Bežia na počítači nad tým, čo appka vyprodukovala.

## `frilens_eval.py` — vyhodnotenie session logov

Prečíta CSV, ktoré appka zapisuje do `Application.persistentDataPath`, a vypočíta z neho
čísla, kvôli ktorým sa beh robil. Metriky potrebujú iba štandardnú knižnicu Pythonu; grafy
potrebujú `matplotlib` a sú voliteľné.

```bash
python3 tools/frilens_eval.py frilens-20260904-145117.csv --tape 8.00
python3 tools/frilens_eval.py run.csv --plot run.png
python3 tools/frilens_eval.py logy/*.csv --table
python3 tools/frilens_eval.py --selftest
```

Výstup je markdown, takže sa dá vložiť rovno do `docs/`.

### Čo počíta

| sekcia | čo v nej je |
|---|---|
| **Súhrn** | trvanie, `walked_m`, `path_raw_m` a ich pomer, skoky, straty, podiel `verified`, rozsah `cam_y` |
| **Úseky medzi značkami** | vzdialenosť medzi stlačeniami `Mark`; s `--tape` aj chyba proti pásmu |
| **Skoky** | čas, veľkosť a **zvislá zložka** každej relokalizácie |
| **Straty trackingu** | epizódy strát a **skoky, ktoré po nich nasledovali v okne 60 s** |
| **Disky na navmeshi** | `model floor … below height` — jediné číslo v logu, ktoré hovorí o modeli |

### Prepínače

| prepínač | čo robí |
|---|---|
| `--tape M` | známa dĺžka meraného úseku; zapne stĺpce s chybou |
| `--segments 1-2,3-4` | ktoré dvojice značiek sú merané prechody; bez toho každý druhý úsek |
| `--after S` | okno po obnove trackingu, v ktorom sa skoky pripisujú strate (predvolene 60 s) |
| `--plot CESTA` | uloží graf; pri viacerých behoch je to priečinok |
| `--table` | jeden riadok na beh — tvar, ktorý potrebuje ablačná štúdia |
| `--json` | strojovo čitateľný výstup |
| `--selftest` | overí metriky na logu so známymi odpoveďami |

### Tri veci, ktoré robí zámerne inak, než by sa čakalo

**Prechody a státie sa priemerujú zvlášť.** Protokol necháva chodca stlačiť `Mark` na oboch
koncoch každého prechodu, takže prechody sú každý druhý úsek a medzi nimi je otočka. Priemer
cez oboje neopisuje ani jedno — na testovacích dátach vyšiel `−44,6 %` namiesto `−3,0 %`.
Keď sa značky stláčali inak, treba to povedať cez `--segments`.

**Kumulatívne počítadlá sa sčítavajú po riadkoch, nie odčítaním koncov.** Zosúladenie
reštartuje `walked_m` aj počítadlá skokov. Odčítanie krajných hodnôt úseku, v ktorom
zosúladenie prebehlo, dá nezmysel; sčítanie prírastkov dá správne číslo. Takýto úsek je
v tabuľke označený `⚠`, lebo už nejde o jednu neprerušenú chôdzu.

**Skoky sa po strate trackingu zbierajú celú minútu.** Pätnásťsekundové zakrytie v behu
`001103` nevyvolalo jeden skok, ale tri — 13 m hneď, 22 m po osemnástich sekundách a 36 m
po minúte. Pripísať strate len prvý z nich znamená podhodnotiť ju trojnásobne.

### Staršie logy

Čítanie je tolerantné k dvom veciam, ktoré sa v starých súboroch naozaj vyskytujú:

- **Kratšia hlavička.** Logy spred 0.1.5 nemajú `path_raw_m` ani počítadlá skokov. Stĺpce sa
  hľadajú podľa mena a chýbajúce metriky sa vypíšu ako `—`, súbor sa neodmietne.
- **Desatinná čiarka v menovke udalosti.** Do 0.1.7 sa `probe-1 eye 1,70 m` rozsekol na dva
  stĺpce. Prebytočné polia sa zlepia späť do menovky, lebo tam patria.
