#!/usr/bin/env python3
"""Založí z plánu GitHub issues a naplní nimi Project (board).

Beží nad `gh`, lebo Projects v2 sú iba v GraphQL a `gh` to vie autentifikovať.
Všetko je idempotentné — skript sa dá pustiť znova a dorobí len to, čo chýba.

    gh auth refresh -s project,read:project      # raz, board bez toho nejde
    python3 tools/planning/gh_plan.py --issues   # labely, míľniky, issues
    python3 tools/planning/gh_plan.py --board    # board, polia, hodnoty
    python3 tools/planning/gh_plan.py --all --dry-run

Iný projekt: skopíruj `plan_frilens.py`, zmeň `REPO` a `ISSUES`, a pusti
`--plan tools/planning/plan_inyprojekt.py`. `BOARD` nechaj — board je jeden.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import subprocess
import sys
from pathlib import Path

DRY_RUN = False


# --------------------------------------------------------------------- gh ---


def gh(*args: str, stdin: str | None = None, check: bool = True) -> str:
    """Spustí `gh` a vráti stdout. Pri `--dry-run` len vypíše, čo by spustil."""
    mutating = any(
        a in ("create", "close", "edit", "item-add", "field-create")
        or (a.startswith("-X") and a != "-XGET")
        for a in args
    )
    if DRY_RUN and mutating:
        print(f"  [dry-run] gh {' '.join(args)}")
        return ""
    proc = subprocess.run(
        ["gh", *args],
        input=stdin,
        capture_output=True,
        text=True,
    )
    if check and proc.returncode != 0:
        raise RuntimeError(f"gh {' '.join(args)}\n{proc.stderr.strip()}")
    return proc.stdout


def gh_json(*args: str, default=None):
    out = gh(*args).strip()
    if not out or out == "{}":
        return default if default is not None else {}
    return json.loads(out)


def api(path: str, method: str = "GET", **fields) -> object:
    args = ["api", f"-X{method}", path]
    for key, value in fields.items():
        args += ["-f", f"{key}={value}"]
    return gh_json(*args, default={})


# ------------------------------------------------------------ issues časť ---


def sync_labels(plan) -> None:
    owner, repo = plan.REPO["owner"], plan.REPO["name"]
    print(f"Labely v {owner}/{repo}:")
    for name, color, description in plan.LABELS:
        gh(
            "label", "create", name,
            "--color", color,
            "--description", description,
            "--repo", f"{owner}/{repo}",
            "--force",
        )
        print(f"  ✓ {name}")


def sync_milestones(plan) -> dict[str, int]:
    owner, repo = plan.REPO["owner"], plan.REPO["name"]
    existing = {
        m["title"]: m["number"]
        for m in gh_json("api", f"repos/{owner}/{repo}/milestones?state=all", default=[])
    }
    print("Míľniky:")
    for milestone in plan.MILESTONES:
        title = milestone["title"]
        if title in existing:
            print(f"  = {title} (#{existing[title]})")
            continue
        created = api(
            f"repos/{owner}/{repo}/milestones", "POST",
            title=title, due_on=milestone["due_on"], description=milestone["description"],
        )
        existing[title] = created.get("number", 0)
        print(f"  + {title}")
    return existing


def _patch_existing(slug: str, spec: dict, issue: dict, milestones: dict[str, int]) -> str:
    """Doplní míľnik a labely na issue, ktorý už v repozitári je.

    Issues sa dajú založiť aj inak než týmto skriptom (napr. cez MCP), a vtedy
    im míľnik chýba — bez tohto by ho `--issues` nikdy nedorobil.
    """
    notes = []
    want_milestone = spec.get("milestone")
    has_milestone = (issue.get("milestone") or {}).get("title")
    if want_milestone in milestones and has_milestone != want_milestone:
        gh("issue", "edit", str(issue["number"]), "--repo", slug,
           "--milestone", want_milestone)
        notes.append("+míľnik")

    have = {label["name"] for label in issue.get("labels") or []}
    missing = [label for label in spec["labels"] if label not in have]
    if missing:
        args = ["issue", "edit", str(issue["number"]), "--repo", slug]
        for label in missing:
            args += ["--add-label", label]
        gh(*args)
        notes.append(f"+{len(missing)} labelov")
    return f"  ({', '.join(notes)})" if notes else ""


def sync_issues(plan, milestones: dict[str, int]) -> dict[str, dict]:
    """Založí chýbajúce issues. Páruje ich podľa `key` na začiatku nadpisu."""
    owner, repo = plan.REPO["owner"], plan.REPO["name"]
    slug = f"{owner}/{repo}"
    found = gh_json(
        "issue", "list", "--repo", slug, "--state", "all", "--limit", "300",
        "--json", "number,title,url,state,labels,milestone",
        default=[],
    )
    by_title = {issue["title"]: issue for issue in found}

    print("Issues:")
    created: dict[str, dict] = {}
    for spec in plan.ISSUES:
        title = spec["title"]
        if title in by_title:
            issue = by_title[title]
            created[spec["key"]] = issue
            print(f"  = {title}{_patch_existing(slug, spec, issue, milestones)}")
            continue

        args = ["issue", "create", "--repo", slug, "--title", title, "--body-file", "-"]
        for label in spec["labels"]:
            args += ["--label", label]
        if spec.get("milestone") in milestones:
            args += ["--milestone", spec["milestone"]]
        url = gh(*args, stdin=spec["body"]).strip()
        number = int(url.rsplit("/", 1)[-1]) if url.rsplit("/", 1)[-1].isdigit() else 0
        created[spec["key"]] = {"number": number, "title": title, "url": url, "state": "OPEN"}
        print(f"  + {title}  {url}")
    return created


def link_sub_issues(plan, issues: dict[str, dict]) -> None:
    """Zavesí balíky pod epic semestra. Sub-issues chcú id, nie číslo."""
    owner, repo = plan.REPO["owner"], plan.REPO["name"]
    print("Hierarchia (balík → epic):")
    ids: dict[int, int] = {}

    def node_id(number: int) -> int:
        if number not in ids:
            ids[number] = gh_json("api", f"repos/{owner}/{repo}/issues/{number}").get("id", 0)
        return ids[number]

    for spec in plan.ISSUES:
        parent_key = spec.get("parent")
        if not parent_key:
            continue
        child = issues.get(spec["key"])
        parent = issues.get(parent_key)
        if DRY_RUN:
            print(f"  [dry-run] {spec['key']} → {parent_key}")
            continue
        if not child or not parent or not child["number"] or not parent["number"]:
            continue
        args = [
            "api", "-XPOST",
            f"repos/{owner}/{repo}/issues/{parent['number']}/sub_issues",
            "-F", f"sub_issue_id={node_id(child['number'])}",
        ]
        proc = subprocess.run(["gh", *args], capture_output=True, text=True)
        state = "✓" if proc.returncode == 0 else "=  (už visí alebo sa nedá)"
        print(f"  {state} {spec['key']} → {parent_key}")


def close_done(plan, issues: dict[str, dict]) -> None:
    owner, repo = plan.REPO["owner"], plan.REPO["name"]
    for spec in plan.ISSUES:
        if spec.get("state") != "closed":
            continue
        issue = issues.get(spec["key"])
        if not issue or issue.get("state") == "CLOSED" or not issue.get("number"):
            continue
        gh("issue", "close", str(issue["number"]),
           "--repo", f"{owner}/{repo}", "--reason", "completed")
        print(f"  ✓ zatvorené ako hotové: {spec['title']}")


# ------------------------------------------------------------- board časť ---


def ensure_board(plan) -> dict:
    owner = plan.BOARD["owner"]
    projects = gh_json(
        "project", "list", "--owner", owner, "--limit", "100", "--format", "json",
        default={},
    ).get("projects", [])
    for project in projects:
        if project["title"] == plan.BOARD["title"]:
            print(f"Board: = {project['title']} (#{project['number']}) {project['url']}")
            return project
    created = gh_json(
        "project", "create", "--owner", owner,
        "--title", plan.BOARD["title"], "--format", "json",
        default={},
    )
    if DRY_RUN:
        print(f"Board: [dry-run] vytvoril by sa {plan.BOARD['title']}")
        return {"number": 0, "id": "", "url": ""}
    print(f"Board: + {plan.BOARD['title']} {created.get('url', '')}")
    return created


def ensure_fields(plan, project: dict) -> dict[str, dict]:
    owner, number = plan.BOARD["owner"], str(project["number"])

    def current() -> dict[str, dict]:
        listed = gh_json(
            "project", "field-list", number, "--owner", owner, "--limit", "60",
            "--format", "json", default={},
        ).get("fields", [])
        return {field["name"]: field for field in listed}

    fields = current()
    print("Polia boardu:")
    changed = False
    for spec in plan.BOARD["fields"]:
        if spec["name"] in fields:
            print(f"  = {spec['name']}")
            continue
        args = [
            "project", "field-create", number, "--owner", owner,
            "--name", spec["name"], "--data-type", spec["type"],
        ]
        if spec["type"] == "SINGLE_SELECT":
            args += ["--single-select-options", ",".join(spec["options"])]
        gh(*args)
        changed = True
        print(f"  + {spec['name']} ({spec['type']})")
    return current() if changed and not DRY_RUN else fields


def fill_board(plan, project: dict, fields: dict[str, dict], issues: dict[str, dict]) -> None:
    owner, number = plan.BOARD["owner"], str(project["number"])
    project_id = project.get("id", "")

    for spec in plan.ISSUES:
        issue = issues.get(spec["key"])
        if issue and issue.get("url"):
            gh("project", "item-add", number, "--owner", owner, "--url", issue["url"])
    if DRY_RUN:
        print("Položky: [dry-run] pridané by boli všetky issues")
        return

    items = gh_json(
        "project", "item-list", number, "--owner", owner, "--limit", "300",
        "--format", "json", default={},
    ).get("items", [])
    by_url = {item.get("content", {}).get("url"): item["id"] for item in items}

    print("Hodnoty polí:")
    for spec in plan.ISSUES:
        issue = issues.get(spec["key"])
        item_id = by_url.get(issue and issue.get("url"))
        if not item_id:
            print(f"  ! {spec['key']} — položka na boarde sa nenašla")
            continue
        values = [
            ("Semester", "single", spec.get("semester")),
            ("Balík", "text", spec["key"]),
            ("Odhad h", "number", spec.get("hours")),
            ("Termín", "date", spec.get("due")),
        ]
        for field_name, kind, value in values:
            if value is None or field_name not in fields:
                continue
            field = fields[field_name]
            args = ["project", "item-edit", "--id", item_id,
                    "--project-id", project_id, "--field-id", field["id"]]
            if kind == "single":
                option = next(
                    (o["id"] for o in field.get("options", []) if o["name"] == value), None
                )
                if not option:
                    continue
                args += ["--single-select-option-id", option]
            elif kind == "number":
                args += ["--number", str(value)]
            elif kind == "date":
                args += ["--date", value]
            else:
                args += ["--text", str(value)]
            gh(*args)
        print(f"  ✓ {spec['key']}")


# -------------------------------------------------------------------- cli ---


def load_plan(path: Path):
    spec = importlib.util.spec_from_file_location("plan", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    global DRY_RUN
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--plan", default=str(Path(__file__).with_name("plan_frilens.py")))
    parser.add_argument("--issues", action="store_true", help="labely, míľniky, issues")
    parser.add_argument("--board", action="store_true", help="board, polia, položky")
    parser.add_argument("--all", action="store_true", help="oboje")
    parser.add_argument("--dry-run", action="store_true", help="len vypíš, čo by sa stalo")
    args = parser.parse_args()

    DRY_RUN = args.dry_run
    if not (args.issues or args.board or args.all):
        parser.error("vyber aspoň --issues, --board alebo --all")

    if subprocess.run(["which", "gh"], capture_output=True).returncode != 0:
        print("gh nie je nainštalované: https://cli.github.com", file=sys.stderr)
        return 1

    plan = load_plan(Path(args.plan))
    hours = sum(i["hours"] for i in plan.ISSUES if i.get("parent"))
    print(f"Plán: {len(plan.ISSUES)} issues, {hours} h v balíkoch\n")

    issues: dict[str, dict] = {}
    if args.issues or args.all:
        sync_labels(plan)
        milestones = sync_milestones(plan)
        issues = sync_issues(plan, milestones)
        link_sub_issues(plan, issues)
        close_done(plan, issues)
        print()

    if args.board or args.all:
        if not issues:
            owner, repo = plan.REPO["owner"], plan.REPO["name"]
            found = gh_json(
                "issue", "list", "--repo", f"{owner}/{repo}", "--state", "all",
                "--limit", "300", "--json", "number,title,url,state", default=[],
            )
            by_title = {issue["title"]: issue for issue in found}
            issues = {s["key"]: by_title[s["title"]] for s in plan.ISSUES if s["title"] in by_title}
        project = ensure_board(plan)
        if project.get("number"):
            fields = ensure_fields(plan, project)
            fill_board(plan, project, fields, issues)

    print("\nHotovo.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
