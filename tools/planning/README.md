# tools/planning

Prevod [plánu inžinierskeho projektu](../../docs/2026-09-05-plan-inzinierskeho-projektu.md)
na GitHub issues a Project board. Ako sa v tom pracuje, je v
[`docs/2026-09-08-planning-cez-github-projects.md`](../../docs/2026-09-08-planning-cez-github-projects.md).

| súbor | čo je v ňom |
|---|---|
| `plan_frilens.py` | dáta: board, labely, míľniky, 29 issues s telami a hodinami |
| `gh_plan.py` | skript, ktorý to založí a udrží v súlade |

```bash
gh auth refresh -s project,read:project      # raz — Projects v2 sú len v GraphQL
python3 tools/planning/gh_plan.py --all --dry-run
python3 tools/planning/gh_plan.py --all
```

Prepínače: `--issues` (labely, míľniky, issues), `--board` (board, polia, hodnoty polí),
`--all`, `--dry-run`, `--plan <súbor>`.

**Všetko je idempotentné.** Issues sa párujú podľa nadpisu, board podľa názvu, polia podľa
mena. Opakovaný beh dorobí len to, čo chýba — vrátane míľnikov a labelov na issues, ktoré
vznikli inak než týmto skriptom.

## Čo skript neurobí

- **Pohľady** (Board / Table / Roadmap, filtre, zoskupenia) — cez API sa vytvoriť nedajú,
  klikajú sa v boarde. Ktoré štyri sa oplatia, je v dokumente vyššie.
- **Premenovanie možností poľa Status** — tiež iba ručne.

## Ďalší projekt

Skopíruj `plan_frilens.py`, prepíš `REPO`, `MILESTONES` a `ISSUES`, `BOARD` nechaj tak
(board je jeden pre všetky projekty), a pusti s `--plan`.
