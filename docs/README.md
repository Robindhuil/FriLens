# FriLens — dokumentácia

**Verzia:** 0.2.0-alpha · **Stav:** fázy 0–2, 3c a 5 hotové; rozsah rozšírený ([ADR 008](decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md))

Prístroj je overený v teréne na Redmi Note 10 Pro: prejdená vzdialenosť sedí na −2,7 %, kotvy
prežijú stratu trackingu a kladenie diskov na navmesh funguje. **Chýba vytlačená a zameraná
značka** (fázy 3a, 3b) — dovtedy sa meria tracker, nie zhoda modelu s budovou.

Čísla z behov pred 0.1.4-alpha sú nadhodnotené: metóda merania dráhy sa vtedy zmenila
([ADR 005](decisions/005-ako-merat-prejdenu-vzdialenost.md)).

Samostatný Unity projekt oddelený od `FriWorld`. Vetva 0.1.x odpovedala na jedinú otázku:
**ako presne sa navmesh premietne do skutočnej fakulty a ako rýchlo to odchádza, keď sa
človek prejde.** Prístroj na to je hotový a overený.

Od 0.2.0-alpha je z FriLens **inžiniersky projekt na tri semestre**: lokalizácia telefónu voči
existujúcemu modelu budovy, navigácia k miestnostiam a hra na deň otvorených dverí. Meranie sa
nezrušilo — zostáva ako trvalý režim a je tým, čím sa práca obhajuje. Prečo a s akými
obmedzeniami: [ADR 008](decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md).

> Staršia veta *„Nie navigačná appka. Nie prekryv miestností."* už neplatí. Dokumenty vetvy
> 0.1.x sa neprepisujú — popisujú, čo sa naozaj robilo, a zostávajú platné ako záznam.

## Aktuálne dokumenty

| Dokument | Čo je v ňom |
|---|---|
| [Plán inžinierskeho projektu](2026-09-05-plan-inzinierskeho-projektu.md) | tri semestre po 125 h: cieľ, pracovné balíky s hodinami, akceptačné kritériá, čo sa vypúšťa pri sklzu |
| [Stav projektu a analýza navmeshu](2026-09-02-stav-projektu-a-analyza-navmeshu.md) | čo v projekte skutočne je, čo obsahuje `navmesh.blend`, kritické diery |
| [Implementačný plán](2026-09-02-implementacny-plan.md) | fázy 0–6, od hygieny projektu po test v teréne |
| [Brief pre návrh UI](2026-09-03-brief-navrh-ui.md) | zadanie pre návrhára HUD-u: podmienky v teréne, mantinely UI Toolkitu, čo sa nesmie meniť |
| [Protokol baseline testu](2026-09-04-protokol-baseline-testu.md) | čo odmerať v teréne, kým neexistuje značka, a čo znamená každý stĺpec CSV |
| [Výsledky baseline testu](2026-09-04-vysledky-baseline.md) | čo namerali behy 0.1.5 až 0.1.7: vzdialenosť sedí na −2,7 %, dlhé zakrytie rozbije mapu, relokalizácie sú aj zvislé |
| [Analýza geometrie a stien](2026-09-04-analyza-geometrie-a-stien.md) | čo `navmesh.blend` naozaj obsahuje a prečo sa steny dajú odvodiť z hraníc navmeshu |
| [ADR 001 — Zdroj navigačnej geometrie](decisions/001-zdroj-navmesh-geometrie.md) | prečo nepiecť navmesh, ale extrahovať existujúce plochy |
| [ADR 002 — Verzovanie modelov](decisions/002-verzovanie-modelov.md) | čo robiť s 300 MB blend súborom |
| [ADR 003 — Póza značky z nav polygónov](decisions/003-poza-znacky-z-nav-polygonov.md) | prečo model budovy netreba a čo to znamená pre výber miesta na značku |
| [ADR 004 — Zariadenia bez ARCore](decisions/004-zariadenia-bez-arcore.md) | AR Optional, runtime detekcia a náhradný Preview režim |
| [ADR 005 — Ako merať prejdenú vzdialenosť](decisions/005-ako-merat-prejdenu-vzdialenost.md) | prečo sčítavanie snímok po snímku nafukuje menovateľ driftu |
| [ADR 006 — Kotvenie a strata trackingu](decisions/006-kotvenie-a-strata-trackingu.md) | čo sa stane pri zakrytí kamery, prečo `ARAnchor` a prečo značiek viac |
| [ADR 007 — Využitie modelu na lokalizáciu](decisions/007-vyuzitie-modelu-na-lokalizaciu.md) | prečo je kamera nutná, aké možnosti dáva znalosť modelu a prečo baseline musí ísť prvý |
| [ADR 008 — Rozšírenie rozsahu na navigáciu a hru](decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md) | prečo sa zadanie mení, čo z merania zostáva a prečo je herné stanovište zameraná značka |

Východiskový návrh testu žije vo FriWorlde:
[`FriWorld/docs/2026-08-29-frilens-ar-test.md`](../../FriWorld/docs/2026-08-29-frilens-ar-test.md).
Tam, kde sa s ním tunajšie dokumenty rozchádzajú, je rozdiel výslovne vysvetlený.

## Verzie a buildy

Verzia je konštanta `Version` v
[`Assets/_Game/Editor/AndroidBuilder.cs`](../Assets/_Game/Editor/AndroidBuilder.cs). Je to
jediné miesto, kde sa mení — odtiaľ sa stampuje do `PlayerSettings.bundleVersion` aj do
názvu priečinka s buildom. Player Settings sa preto ručne needitujú; editor ich vie prepísať
pri každom uložení a build, ktorého manifest nesedí s názvom priečinka, je horší než žiadna
verzia.

Build sa spúšťa cez menu **`FriLens > Build Android <verzia>`** a skončí v:

```
Documents/Robin/unity/frilens/<verzia>/
    FriLens-<verzia>.apk
    build-info.txt      ← verzia, dátum, Unity, bundle id, min SDK, backend, ABI, scény
```

Nový build:

1. zdvihnúť `Version` **aj** `VersionCode` v `AndroidBuilder.cs`,
2. dopísať, čo sa zmenilo, do [`CHANGELOG.md`](../CHANGELOG.md),
3. spustiť **`FriLens > Wire Scene`**, ak pribudol komponent alebo referencia — default
   v zdroji sa na komponent, ktorý už v scéne je, nevzťahuje,
4. spustiť menu položku buildu,
5. **vydať na GitHub Releases** (nižšie).

Prvý IL2CPP build trvá aj desiatky minút a editor je počas neho nereagujúci. Ďalšie sú
podstatne rýchlejšie.

### Vydanie na GitHub Releases

Každý build ide na releases, lebo odtiaľ ho sťahuje tlačidlo na
[FriWorld-Hub](https://github.com/Robindhuil/FriWorld-Hub).

```bash
gh release create v<verzia> "$USERPROFILE/Documents/Robin/unity/frilens/<verzia>/FriLens-<verzia>.apk" --repo Robindhuil/FriLens --title "FriLens <verzia>" --notes-file <poznámky.md> --prerelease
```

Výsledná adresa je predvídateľná zo samotnej verzie:

```
https://github.com/Robindhuil/FriLens/releases/download/v<verzia>/FriLens-<verzia>.apk
```

Preto má APK v názve pomlčku, nie medzeru — GitHub by medzeru v názve assetu premenil na
bodku a adresa by sa už z verzie odvodiť nedala.

Potom stačí prepísať `version`, `apk` a `apkMb` v `src/content/frilens.ts` vo webovom
repozitári a stránka ukazuje nový build.

> **Repozitár musí zostať verejný.** Release assety z privátneho repozitára vracajú každému
> okrem majiteľa chybu 404, takže tlačidlo na webe by prestalo fungovať.

> **História bola 2026-09-03 prepísaná**, aby sa v commitoch neobjavoval Claude ako
> spoluautor. Všetky SHA sa zmenili a značky `v0.1.2-alpha` aj `v0.1.3-alpha` ukazujú na nové
> commity; samotné releasy aj ich APK zostali. Existujúci klon repozitára sa s remote
> rozchádza a treba ho preklonovať alebo resetnúť na `origin/main`.

**Každý dokument nesie v hlavičke verziu**, ku ktorej sa vzťahuje. Keď sa verzia zdvihne
a dokument sa vecne zmení, prepíše sa mu aj verzia v hlavičke.

## Ako túto dokumentáciu udržiavať

- Nové zistenie o projekte ide do **analýzy**, nie do plánu.
- Zmena postupu ide do **plánu**.
- Voľba medzi dvoma cestami, ktorú by sa niekto o mesiac spýtal „prečo takto?",
  ide do **`decisions/`** ako nové ADR. Staré ADR sa neprepisuje — dostane stav
  `nahradené ADR NNN`.
- Dátum v názve súboru je dátum vzniku, nie poslednej úpravy.
