# ADR 002 — Verzovanie veľkých modelov

**Dátum:** 2026-09-02 · **Stav:** navrhnuté, čaká na potvrdenie

## Kontext

`Assets/Models/navmesh.blend` má **300 MB**. Git LFS je v projekte nakonfigurovaný správne
(`.gitattributes` pokrýva `*.blend`), takže commit technicky prejde. Problémy sú dva:

1. **Kvóta.** GitHub dáva na free účte 1 GB LFS priestoru a 1 GB prenosu mesačne. Jeden
   commit tohto súboru zožerie 30 % priestoru. Každý ďalší export ďalších 300 MB.
2. **Závislosť na Blenderi.** Unity číta `.blend` tak, že na pozadí spustí Blender. Na
   stroji bez neho sa projekt otvorí s prázdnymi mesh assetmi. Tichá chyba, ťažko
   diagnostikovateľná.

K tomu: 92 % trojuholníkov v súbore je terén, cesty, tráva a obrubníky, ktoré tento projekt
nepoužije na nič. Navigačné plochy tvoria 8 %.

## Zvažované možnosti

**A. Commitnúť blend cez LFS tak, ako je.** Nulová práca teraz, kvóta a krehkosť zostávajú.

**B. Vyexportovať z Blendera štíhly FBX (len `*_nav_*` objekty) a commitnúť ten.**
Odhadom jednotky MB. Blend zostáva mimo repozitára ako zdroj pravdy na disku autora.

**C. Nechať blend mimo repozitára a commitnúť len vygenerované `.asset` meshe.**
Najmenšie (stovky KB), ale repozitár potom neobsahuje nič, z čoho by sa dali meshe znovu
vyrobiť.

## Návrh

**B ako cieľ, A ako dočasné riešenie, kým FBX nie je.**

Konkrétne:

1. Teraz: `Assets/Models/*.blend` a `*.blend1` pridať do `.gitignore`. Zdroj zostáva
   na disku, do repozitára nejde.
2. Z Blendera vyexportovať `navmesh_nav.fbx` — len objekty s `_nav_` v mene, bez terénu.
   Ten commitnúť cez LFS.
3. Vygenerované `.asset` meshe (výstup extrakcie podľa [ADR 001](001-zdroj-navmesh-geometrie.md))
   commitovať tiež — sú malé a robia projekt otvárateľný bez Blendera.

## Dôsledky

**Dobré.** Repozitár zostane v jednotkách MB, otvorí sa na stroji bez Blendera, LFS kvóta
sa nemíňa.

**Zlé.** Export z Blendera je manuálny krok, ktorý treba zopakovať pri každej zmene nav
geometrie. Treba ho zapísať do README, inak sa naň zabudne.

**Otvorené.** Ak sa FriWorld a FriLens majú deliť o rovnakú nav geometriu, dlhodobo to
patrí do zdieľaného balíčka alebo submodulu, nie do dvoch kópií. Mimo rozsah tohto testu.
