#!/usr/bin/env python3
"""Reads FriLens session logs and prints the numbers a run is supposed to produce.

The app writes a CSV four times a second plus a row on every event. Reading that by
hand is how "it looked about right" ends up in a report instead of a figure, so this
does it the same way every time.

Everything the baseline results document worked out by hand is here as a function:
distance against a tape, the size and vertical component of relocalisation jumps, and
what happens in the minute after tracking comes back. Metrics need nothing but the
standard library; plots need matplotlib and are optional.

    python3 tools/frilens_eval.py frilens-20260904-145117.csv
    python3 tools/frilens_eval.py run.csv --tape 8.00 --plot run.png
    python3 tools/frilens_eval.py runs/*.csv --table
    python3 tools/frilens_eval.py --selftest

Output is markdown so it can be pasted straight into docs/.
"""

from __future__ import annotations

import argparse
import glob
import json
import math
import os
import re
import sys
import tempfile

# The schema the app writes. Older logs are shorter — 0.1.5 had no path_raw_m and no
# jump counters — so every column is looked up by name and a missing one degrades the
# metrics that need it instead of failing the whole read.
NUMERIC = {
    "time_s", "cam_x", "cam_y", "cam_z", "cam_yaw", "cam_pitch", "cam_roll",
    "walked_m", "path_raw_m", "from_origin_m", "jumps", "jumped_m",
    "blind_s", "losses", "verified", "origin_anchored", "overlay_anchored",
    "probes", "eye_m", "since_align_s", "spread_cm", "spread_deg",
}

# Counters that only ever grow, until an alignment restarts them.
CUMULATIVE = ("walked_m", "path_raw_m", "jumps", "jumped_m", "blind_s", "losses")

RE_REGAINED = re.compile(r"tracking-regained after ([\d.]+) s; was (\S+)")
RE_LOST = re.compile(r"tracking-lost (\S+)")
RE_MARK = re.compile(r"^mark-(\d+)$")
RE_NAV_PROBE = re.compile(r"^probe-(\d+) via navmesh;.*?model floor (-?[\d.]+) cm below height")
RE_HEIGHT_PROBE = re.compile(r"^probe-(\d+) via height;.*?floor offset (-?[\d.]+) cm")
RE_HEADER_EVENT = re.compile(r"^frilens (\S+); device (.*?); android (.*?); ")


def parse_segments(spec):
    """Turns "1-2,3-4" into {(1, 2), (3, 4)}."""
    chosen = set()
    for part in spec.split(","):
        part = part.strip()
        if not part:
            continue
        if "-" not in part:
            raise ValueError(f"úsek '{part}' nemá tvar OD-DO")
        start, end = part.split("-", 1)
        chosen.add((int(start), int(end)))
    return chosen


def measured_segments(segments, chosen=None):
    """Splits mark segments into the measured passes and everything between them.

    The protocol has the walker press Mark at both ends of each pass, so the passes are
    the alternate segments and the ones between them are standing and turning around.
    Averaging over all of them mixes a walk with a pause and produces a figure that
    describes neither — which is why the baseline results table kept them apart.
    """
    if chosen:
        passes = [s for s in segments if (s["from"], s["to"]) in chosen]
        between = [s for s in segments if (s["from"], s["to"]) not in chosen]
        return passes, between, "vybrané cez --segments"
    return segments[0::2], segments[1::2], "každý druhý úsek, počnúc prvým"


class Run:
    """One session log, parsed."""

    def __init__(self, path, header, rows):
        self.path = path
        self.name = os.path.basename(path)
        self.header = header
        self.rows = rows
        self.version = ""
        self.device = ""
        self.android = ""
        for row in rows:
            match = RE_HEADER_EVENT.match(row.get("event", ""))
            if match:
                self.version, self.device, self.android = match.groups()
                break

    def has(self, column):
        return column in self.header

    def column(self, name):
        """Raw values of one column, None where absent or unparseable."""
        return [row.get(name) for row in self.rows]

    def increments(self, name):
        """Per-row growth of a cumulative counter, reset-aware.

        An alignment restarts walked_m and the jump counters. Subtracting the endpoints
        of a span that contains a restart gives a negative number; summing per-row
        growth gives the right one, and treats the first value after a restart as the
        distance covered since it.
        """
        values = self.column(name)
        out = [0.0] * len(values)
        previous = None
        for index, value in enumerate(values):
            if value is None:
                continue
            if previous is None:
                out[index] = 0.0
            elif value >= previous:
                out[index] = value - previous
            else:
                out[index] = value  # counter restarted; this is the growth since
            previous = value
        return out

    def total(self, name):
        if not self.has(name):
            return None
        return sum(self.increments(name))

    def resets(self, name):
        """Row indices where a cumulative counter went backwards."""
        values = self.column(name)
        out = []
        previous = None
        for index, value in enumerate(values):
            if value is not None and previous is not None and value < previous:
                out.append(index)
            if value is not None:
                previous = value
        return out

    @property
    def duration(self):
        times = [t for t in self.column("time_s") if t is not None]
        return (times[-1] - times[0]) if len(times) >= 2 else 0.0

    def events(self):
        """(index, time, label) for every row that carries an event label."""
        out = []
        for index, row in enumerate(self.rows):
            label = row.get("event", "")
            if label:
                out.append((index, row.get("time_s"), label))
        return out


def read_log(path):
    """Parses one CSV. Tolerates the two ways these files get malformed."""
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        lines = [line.rstrip("\n").rstrip("\r") for line in handle if line.strip()]

    if not lines:
        raise ValueError(f"{path}: prázdny súbor")

    header = [name.strip() for name in lines[0].split(",")]
    if "time_s" not in header:
        raise ValueError(f"{path}: prvý riadok nevyzerá ako hlavička FriLens logu")

    rows = []
    for line in lines[1:]:
        fields = line.split(",")
        # Logs before 0.1.7 formatted event labels in the phone's culture, so a decimal
        # comma split "probe-1 eye 1,70 m" across two columns. The surplus belongs to
        # the last column, which is the label — glue it back rather than dropping the row.
        if len(fields) > len(header):
            fields = fields[:len(header) - 1] + [",".join(fields[len(header) - 1:])]
        elif len(fields) < len(header):
            fields = fields + [""] * (len(header) - len(fields))

        row = {}
        for name, raw in zip(header, fields):
            raw = raw.strip()
            if name in NUMERIC:
                try:
                    row[name] = float(raw)
                except ValueError:
                    row[name] = None
            else:
                row[name] = raw
        rows.append(row)

    return Run(path, header, rows)


# --- metrics ---------------------------------------------------------------------


def summarise(run):
    """The one-paragraph description of a run."""
    verified = [v for v in run.column("verified") if v is not None]
    walked = run.total("walked_m")
    raw = run.total("path_raw_m")

    cam_y = [v for v in run.column("cam_y") if v is not None]
    from_origin = [v for v in run.column("from_origin_m") if v is not None]

    return {
        "file": run.name,
        "version": run.version,
        "device": run.device,
        "rows": len(run.rows),
        "duration_s": run.duration,
        "mode": run.rows[0].get("mode", "") if run.rows else "",
        "walked_m": walked,
        "path_raw_m": raw,
        "raw_over_walked": (raw / walked) if walked else None,
        "from_origin_max_m": max(from_origin) if from_origin else None,
        "jumps": int(run.total("jumps") or 0) if run.has("jumps") else None,
        "jumped_m": run.total("jumped_m") if run.has("jumped_m") else None,
        "losses": int(run.total("losses") or 0) if run.has("losses") else None,
        "blind_s": run.total("blind_s") if run.has("blind_s") else None,
        "verified_fraction": (sum(verified) / len(verified)) if verified else None,
        "alignments": sum(1 for _, _, label in run.events() if label == "aligned"),
        "cam_y_range_m": (max(cam_y) - min(cam_y)) if cam_y else None,
        "probes": int(max([p for p in run.column("probes") if p is not None] or [0])),
    }


def jump_events(run):
    """Every relocalisation jump: when, how big, and how much of it was vertical.

    The vertical component matters on its own. Run 174812 ended three metres below where
    it started, and that was not a slope or a drift — it sat on three jumps. Logging only
    the 3D length hid it.
    """
    if not run.has("jumps"):
        return []

    growth = run.increments("jumps")
    size = run.increments("jumped_m") if run.has("jumped_m") else [0.0] * len(run.rows)
    cam_y = run.column("cam_y")

    out = []
    for index, count in enumerate(growth):
        if count <= 0:
            continue
        previous_y = cam_y[index - 1] if index > 0 else None
        this_y = cam_y[index]
        vertical = None
        if previous_y is not None and this_y is not None:
            vertical = this_y - previous_y
        out.append({
            "time_s": run.rows[index].get("time_s"),
            "size_m": size[index],
            "vertical_m": vertical,
            "count": int(count),
        })
    return out


def loss_episodes(run, window_s=60.0):
    """Tracking losses paired with their recovery, and the jumps that followed.

    The window exists because of the fifteen-second cover in run 001103: the jumps did
    not arrive at once and they grew — 13 m immediately, 22 m after eighteen seconds,
    36 m after a minute. Attributing only the first one to the loss understates it by a
    factor of three.
    """
    episodes = []
    open_loss = None

    for _, time_s, label in run.events():
        lost = RE_LOST.match(label)
        if lost:
            open_loss = {"start_s": time_s, "reason": lost.group(1)}
            continue
        regained = RE_REGAINED.match(label)
        if regained and open_loss is not None:
            open_loss["end_s"] = time_s
            open_loss["blind_s"] = float(regained.group(1))
            open_loss["reason"] = regained.group(2) or open_loss["reason"]
            episodes.append(open_loss)
            open_loss = None

    if open_loss is not None:
        open_loss["end_s"] = None
        open_loss["blind_s"] = None
        episodes.append(open_loss)

    jumps = jump_events(run)
    for episode in episodes:
        end = episode.get("end_s")
        if end is None:
            episode["after"] = []
            continue
        following = [
            {"delay_s": j["time_s"] - end, "size_m": j["size_m"], "vertical_m": j["vertical_m"]}
            for j in jumps
            if j["time_s"] is not None and end <= j["time_s"] <= end + window_s
        ]
        episode["after"] = following
        episode["after_total_m"] = sum(f["size_m"] for f in following)
        episode["after_max_m"] = max((f["size_m"] for f in following), default=0.0)

    return episodes


def mark_segments(run, tape_m=None):
    """Distance covered between consecutive mark-N presses.

    This is test A. The marks are pressed at the ends of a stretch measured with a tape,
    so the difference between what the app counted and that tape is the whole result.
    """
    marks = []
    for index, time_s, label in run.events():
        match = RE_MARK.match(label)
        if match:
            marks.append((index, time_s, int(match.group(1))))

    segments = []
    walked = run.increments("walked_m")
    raw = run.increments("path_raw_m") if run.has("path_raw_m") else [0.0] * len(run.rows)
    jumps = run.increments("jumps") if run.has("jumps") else [0.0] * len(run.rows)
    losses = run.increments("losses") if run.has("losses") else [0.0] * len(run.rows)
    reset_rows = set(run.resets("walked_m"))

    for (start_index, start_time, start_n), (end_index, end_time, end_n) in zip(marks, marks[1:]):
        span = slice(start_index + 1, end_index + 1)
        segment = {
            "from": start_n,
            "to": end_n,
            "duration_s": (end_time - start_time) if (start_time is not None and end_time is not None) else None,
            "walked_m": sum(walked[span]),
            "path_raw_m": sum(raw[span]) if run.has("path_raw_m") else None,
            "jumps": int(sum(jumps[span])),
            "losses": int(sum(losses[span])),
            # An alignment inside a segment restarts the counters. The sum is still right,
            # but the segment no longer measures one uninterrupted stretch, so say so.
            "realigned": any(start_index < r <= end_index for r in reset_rows),
        }
        if tape_m:
            segment["error_pct"] = (segment["walked_m"] - tape_m) / tape_m * 100.0
            if segment["path_raw_m"] is not None:
                segment["raw_error_pct"] = (segment["path_raw_m"] - tape_m) / tape_m * 100.0
        segments.append(segment)

    return segments


def probe_gaps(run):
    """Discs dropped on the nav mesh, with the model-against-reality figure each carries.

    'model floor N cm below height' compares the model's floor to the one implied by a
    measured eye height. The eye height comes off a tape, so it is independent of ARCore
    — which makes this the only figure in the log that says anything about the model.
    """
    nav, height = [], []
    for _, time_s, label in run.events():
        match = RE_NAV_PROBE.match(label)
        if match:
            nav.append({"n": int(match.group(1)), "time_s": time_s, "gap_cm": float(match.group(2))})
            continue
        match = RE_HEIGHT_PROBE.match(label)
        if match:
            height.append({"n": int(match.group(1)), "time_s": time_s, "offset_cm": float(match.group(2))})
    return {"navmesh": nav, "height": height}


# --- report ----------------------------------------------------------------------


def fmt(value, digits=2, dash="—"):
    if value is None:
        return dash
    if isinstance(value, float) and math.isnan(value):
        return dash
    return f"{value:.{digits}f}"


def report(run, tape_m=None, window_s=60.0, chosen_segments=None):
    """The markdown a run produces. Sections with nothing to say are left out."""
    out = []
    summary = summarise(run)

    out.append(f"# {summary['file']}")
    out.append("")
    meta = [f"**Verzia:** {summary['version'] or '?'}"]
    if summary["device"]:
        meta.append(f"**Zariadenie:** {summary['device']}")
    meta.append(f"**Režim:** {summary['mode'] or '?'}")
    meta.append(f"**Trvanie:** {fmt(summary['duration_s'], 0)} s")
    meta.append(f"**Riadkov:** {summary['rows']}")
    out.append(" · ".join(meta))
    out.append("")

    out.append("## Súhrn")
    out.append("")
    out.append("| | |")
    out.append("|---|---:|")
    out.append(f"| prejdená vzdialenosť `walked_m` | {fmt(summary['walked_m'])} m |")
    if summary["path_raw_m"] is not None:
        out.append(f"| surová dráha `path_raw_m` | {fmt(summary['path_raw_m'])} m |")
        if summary["raw_over_walked"]:
            out.append(f"| pomer raw / walked | {fmt(summary['raw_over_walked'])} × |")
    if summary["from_origin_max_m"] is not None:
        out.append(f"| najväčšia vzdialenosť od počiatku | {fmt(summary['from_origin_max_m'])} m |")
    if summary["jumps"] is not None:
        out.append(f"| skoky | {summary['jumps']} v objeme {fmt(summary['jumped_m'])} m |")
    if summary["losses"] is not None:
        out.append(f"| straty trackingu | {summary['losses']}, naslepo {fmt(summary['blind_s'], 1)} s |")
    if summary["verified_fraction"] is not None:
        out.append(f"| riadky s `verified = 1` | {fmt(summary['verified_fraction'] * 100, 0)} % |")
    out.append(f"| zosúladenia | {summary['alignments']} |")
    if summary["cam_y_range_m"] is not None:
        out.append(f"| rozsah `cam_y` | {fmt(summary['cam_y_range_m'])} m |")
    if summary["probes"]:
        out.append(f"| disky | {summary['probes']} |")
    out.append("")

    segments = mark_segments(run, tape_m)
    if segments:
        out.append("## Úseky medzi značkami")
        out.append("")

        def segment_table(rows, with_error):
            head = "| úsek | čas (s) | `walked_m` | `path_raw_m` |"
            rule = "|---|---:|---:|---:|"
            if with_error:
                head += " chyba | chyba raw |"
                rule += "---:|---:|"
            head += " skoky | straty |"
            rule += "---:|---:|"
            lines = [head, rule]
            for segment in rows:
                label = f"mark-{segment['from']} → mark-{segment['to']}"
                if segment["realigned"]:
                    label += " ⚠"
                line = (f"| {label} | {fmt(segment['duration_s'], 1)} | "
                        f"{fmt(segment['walked_m'])} | {fmt(segment['path_raw_m'])} |")
                if with_error:
                    line += (f" {fmt(segment.get('error_pct'), 1)} % | "
                             f"{fmt(segment.get('raw_error_pct'), 1)} % |")
                line += f" {segment['jumps']} | {segment['losses']} |"
                lines.append(line)
            return lines

        if not tape_m:
            out.extend(segment_table(segments, False))
            out.append("")
        else:
            passes, between, how = measured_segments(segments, chosen_segments)
            out.append(f"Referenčná dĺžka úseku: **{fmt(tape_m)} m**. "
                       f"Za merané prechody sa berie *{how}*; ostatné úseky sú nižšie zvlášť, "
                       f"lebo státie a otočka nie sú prechod a priemer cez oboje neopisuje ani jedno.")
            out.append("")
            out.append("### Merané prechody")
            out.append("")
            out.extend(segment_table(passes, True))

            walked_values = [p["walked_m"] for p in passes]
            if walked_values:
                mean = sum(walked_values) / len(walked_values)
                raw_values = [p["path_raw_m"] for p in passes if p["path_raw_m"] is not None]
                raw_mean = (sum(raw_values) / len(raw_values)) if raw_values else None
                cells = ["**priemer**", "", f"**{fmt(mean)}**", f"**{fmt(raw_mean)}**",
                         f"**{fmt((mean - tape_m) / tape_m * 100, 1)} %**",
                         f"**{fmt(((raw_mean - tape_m) / tape_m * 100) if raw_mean else None, 1)} %**",
                         "", ""]
                out.append("| " + " | ".join(cells) + " |")
                out.append("")
                out.append(f"Rozptyl `walked_m` cez prechody: "
                           f"**{fmt(max(walked_values) - min(walked_values))} m** "
                           f"({fmt(min(walked_values))} až {fmt(max(walked_values))}).")
                out.append("")

            if between:
                out.append("### Medzi prechodmi (státie a otočka)")
                out.append("")
                out.append("Tu sa `walked_m` blízke nule číta ako správna odpoveď, nie ako chyba.")
                out.append("")
                out.extend(segment_table(between, False))
                out.append("")

        if any(s["realigned"] for s in segments):
            out.append("⚠ = v úseku prebehlo zosúladenie, takže nejde o jednu neprerušenú chôdzu.")
            out.append("")

    jumps = jump_events(run)
    if jumps:
        out.append("## Skoky")
        out.append("")
        out.append("Zvislá zložka je zmena `cam_y` cez ten istý riadok. Pri štyroch riadkoch za "
                   "sekundu je to prakticky celý skok, nie chôdza.")
        out.append("")
        out.append("| čas (s) | veľkosť (m) | z toho zvisle (m) |")
        out.append("|---:|---:|---:|")
        for jump in jumps:
            out.append(f"| {fmt(jump['time_s'], 1)} | {fmt(jump['size_m'])} | {fmt(jump['vertical_m'])} |")
        vertical = [abs(j["vertical_m"]) for j in jumps if j["vertical_m"] is not None]
        if vertical:
            out.append("")
            out.append(f"Najväčšia zvislá zložka: **{fmt(max(vertical))} m**.")
        out.append("")

    episodes = loss_episodes(run, window_s)
    if episodes:
        out.append("## Straty trackingu a čo po nich")
        out.append("")
        out.append(f"Okno po obnove: **{fmt(window_s, 0)} s**.")
        out.append("")
        out.append("| začiatok (s) | naslepo (s) | príčina | skokov po obnove | objem (m) | najväčší (m) |")
        out.append("|---:|---:|---|---:|---:|---:|")
        for episode in episodes:
            out.append(
                f"| {fmt(episode['start_s'], 1)} | {fmt(episode.get('blind_s'), 1)} | "
                f"{episode.get('reason', '?')} | {len(episode.get('after', []))} | "
                f"{fmt(episode.get('after_total_m'))} | {fmt(episode.get('after_max_m'))} |")
        out.append("")
        detailed = [e for e in episodes if len(e.get("after", [])) > 1]
        for episode in detailed:
            out.append(f"**Strata v čase {fmt(episode['start_s'], 1)} s** — skoky po obnove:")
            out.append("")
            out.append("| oneskorenie po obnove (s) | veľkosť (m) |")
            out.append("|---:|---:|")
            for follow in episode["after"]:
                out.append(f"| {fmt(follow['delay_s'], 1)} | {fmt(follow['size_m'])} |")
            out.append("")

    probes = probe_gaps(run)
    if probes["navmesh"]:
        out.append("## Disky na navmeshi")
        out.append("")
        out.append("`model floor … below height` porovnáva podlahu modelu s tou, ktorú implikuje "
                   "meraná výška oka. Meraná výška je z pásma, teda nezávislá od ARCore — je to "
                   "jediné číslo v logu, ktoré hovorí o modeli.")
        out.append("")
        out.append("| disk | čas (s) | podlaha modelu nižšie o (cm) |")
        out.append("|---:|---:|---:|")
        for probe in probes["navmesh"]:
            out.append(f"| {probe['n']} | {fmt(probe['time_s'], 1)} | {fmt(probe['gap_cm'], 1)} |")
        gaps = [p["gap_cm"] for p in probes["navmesh"]]
        mean = sum(gaps) / len(gaps)
        out.append("")
        out.append(f"Priemer **{fmt(mean, 1)} cm**, rozptyl **{fmt(max(gaps) - min(gaps), 1)} cm** "
                   f"({fmt(min(gaps), 1)} až {fmt(max(gaps), 1)}).")
        out.append("")

    return "\n".join(out)


def table(runs):
    """One row per run. This is the shape the ablation study needs."""
    out = ["| beh | verzia | režim | trvanie (s) | walked (m) | raw/walked | skoky | objem (m) "
           "| straty | naslepo (s) | verified |",
           "|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|"]
    for run in runs:
        s = summarise(run)
        out.append(
            f"| {s['file']} | {s['version'] or '?'} | {s['mode'] or '?'} | {fmt(s['duration_s'], 0)} "
            f"| {fmt(s['walked_m'], 1)} | {fmt(s['raw_over_walked'])} | "
            f"{s['jumps'] if s['jumps'] is not None else '—'} | {fmt(s['jumped_m'], 1)} | "
            f"{s['losses'] if s['losses'] is not None else '—'} | {fmt(s['blind_s'], 1)} | "
            f"{fmt((s['verified_fraction'] or 0) * 100, 0)} % |")
    return "\n".join(out)


# --- plots -----------------------------------------------------------------------

# Categorical slots 1 and 2 and the status steps, from the validated reference palette.
COLOR_WALKED = "#2a78d6"
COLOR_RAW = "#eb6834"
COLOR_CRITICAL = "#d03b3b"
COLOR_WARNING = "#fab219"
COLOR_SURFACE = "#fcfcfb"
COLOR_TEXT = "#0b0b0b"
COLOR_MUTED = "#898781"
COLOR_GRID = "#e1e0d9"


def plot(run, path, window_s=60.0):
    """Two panels on one time axis: distance, and height.

    Distance and height are both metres but they are not the same measure, so they get a
    panel each rather than two scales on one frame.
    """
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except ImportError:
        raise SystemExit("Grafy potrebujú matplotlib:  pip install matplotlib")

    times = run.column("time_s")
    figure, (top, bottom) = plt.subplots(
        2, 1, figsize=(11, 6.5), sharex=True,
        gridspec_kw={"height_ratios": [2, 1], "hspace": 0.12})
    figure.patch.set_facecolor(COLOR_SURFACE)

    for axis in (top, bottom):
        axis.set_facecolor(COLOR_SURFACE)
        axis.grid(True, color=COLOR_GRID, linewidth=0.8)
        axis.set_axisbelow(True)
        for spine in ("top", "right"):
            axis.spines[spine].set_visible(False)
        for spine in ("left", "bottom"):
            axis.spines[spine].set_color(COLOR_GRID)
        axis.tick_params(colors=COLOR_MUTED, labelsize=9)

    # Losses first, so the marks sit on top of the shading.
    for episode in loss_episodes(run, window_s):
        end = episode.get("end_s") or (times[-1] if times else episode["start_s"])
        top.axvspan(episode["start_s"], end, color=COLOR_WARNING, alpha=0.25, linewidth=0)
        bottom.axvspan(episode["start_s"], end, color=COLOR_WARNING, alpha=0.25, linewidth=0)

    for jump in jump_events(run):
        for axis in (top, bottom):
            axis.axvline(jump["time_s"], color=COLOR_CRITICAL, linewidth=1.2, alpha=0.75)

    top.plot(times, run.column("walked_m"), color=COLOR_WALKED, linewidth=2,
             label="walked_m (prevzorkovaná)")
    if run.has("path_raw_m"):
        top.plot(times, run.column("path_raw_m"), color=COLOR_RAW, linewidth=2,
                 label="path_raw_m (surová)")
    top.set_ylabel("prejdená vzdialenosť (m)", color=COLOR_TEXT, fontsize=10)

    # Legend entries for the two annotation layers, so neither is carried by colour alone.
    handles, labels = top.get_legend_handles_labels()
    from matplotlib.patches import Patch
    from matplotlib.lines import Line2D
    handles += [Patch(facecolor=COLOR_WARNING, alpha=0.25, label="strata trackingu"),
                Line2D([0], [0], color=COLOR_CRITICAL, linewidth=1.2, label="skok")]
    top.legend(handles=handles, loc="upper left", frameon=False, fontsize=9,
               labelcolor=COLOR_TEXT)

    bottom.plot(times, run.column("cam_y"), color=COLOR_WALKED, linewidth=2)
    bottom.set_ylabel("cam_y (m)", color=COLOR_TEXT, fontsize=10)
    bottom.set_xlabel("čas (s)", color=COLOR_TEXT, fontsize=10)

    top.set_title(f"{run.name} — {run.version or '?'} — {run.device or 'neznáme zariadenie'}",
                  color=COLOR_TEXT, fontsize=11, loc="left", pad=12)

    figure.savefig(path, dpi=140, bbox_inches="tight", facecolor=COLOR_SURFACE)
    plt.close(figure)
    return path


# --- self-test -------------------------------------------------------------------


def selftest():
    """Checks the metrics against a log whose answers are known by construction."""
    header = ("time_s,mode,session_state,not_tracking_reason,cam_x,cam_y,cam_z,cam_yaw,"
              "cam_pitch,cam_roll,walked_m,path_raw_m,from_origin_m,jumps,jumped_m,"
              "blind_s,losses,verified,origin_anchored,overlay_anchored,probes,eye_m,"
              "since_align_s,spread_cm,spread_deg,event")

    def row(t, walked, raw, y=0.0, jumps=0, jumped=0.0, blind=0.0, losses=0,
            verified=1, event=""):
        return (f"{t:.3f},Ar,SessionTracking,None,0.0000,{y:.4f},0.0000,0.00,0.00,0.00,"
                f"{walked:.3f},{raw:.3f},0.000,{jumps},{jumped:.3f},{blind:.2f},{losses},"
                f"{verified},1,1,0,1.70,1.00,2.00,0.500,{event}")

    lines = [header]
    lines.append(row(0.0, 0, 0, event="frilens 0.9.9-test; device TestPhone; android 11; gyro True"))
    # An 8 m stretch between two marks, walked exactly.
    lines.append(row(1.0, 0.0, 0.0, event="mark-1"))
    lines.append(row(2.0, 4.0, 4.2))
    lines.append(row(3.0, 8.0, 8.4, event="mark-2"))
    # A loss, then two jumps inside the window and one well outside it.
    lines.append(row(4.0, 8.0, 8.4, losses=1, event="tracking-lost InsufficientLight"))
    lines.append(row(9.0, 8.0, 8.4, blind=5.0, losses=1, verified=0,
                     event="tracking-regained after 5.0 s; was InsufficientLight"))
    lines.append(row(10.0, 8.0, 8.4, y=2.0, jumps=1, jumped=3.0, blind=5.0, losses=1, verified=0))
    lines.append(row(20.0, 8.0, 8.4, y=1.0, jumps=2, jumped=10.0, blind=5.0, losses=1, verified=0))
    lines.append(row(200.0, 8.0, 8.4, y=1.0, jumps=3, jumped=11.0, blind=5.0, losses=1, verified=0))
    # An alignment restarts walked_m; the next stretch is another 8 m.
    lines.append(row(201.0, 0.0, 0.0, blind=5.0, losses=1, event="aligned"))
    lines.append(row(202.0, 0.0, 0.0, blind=5.0, losses=1, event="mark-3"))
    lines.append(row(203.0, 8.0, 8.4, blind=5.0, losses=1, event="mark-4"))
    # A nav-mesh probe, and one label carrying the pre-0.1.7 decimal-comma bug.
    lines.append(row(204.0, 8.0, 8.4, blind=5.0, losses=1,
                     event="probe-1 via navmesh; eye 1.70 m; model floor 12.5 cm below height"))
    lines.append(row(205.0, 8.0, 8.4, blind=5.0, losses=1,
                     event="probe-2 via navmesh; eye 1,70 m; model floor 7.5 cm below height"))

    directory = tempfile.mkdtemp(prefix="frilens-selftest-")
    path = os.path.join(directory, "frilens-19700101-000000.csv")
    with open(path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")

    run = read_log(path)
    checks = []

    def check(name, got, expected, tolerance=1e-6):
        ok = (got is not None) and abs(got - expected) <= tolerance
        checks.append((name, got, expected, ok))

    check("riadkov prečítaných", len(run.rows), 14)
    check("verzia rozpoznaná", 1.0 if run.version == "0.9.9-test" else 0.0, 1.0)
    # 8 m before the alignment plus 8 m after it. Endpoint subtraction would give 8.
    check("walked_m spolu (cez reset)", run.total("walked_m"), 16.0)
    check("path_raw_m spolu", run.total("path_raw_m"), 16.8)
    check("skokov spolu", run.total("jumps"), 3.0)
    check("objem skokov", run.total("jumped_m"), 11.0)

    segments = mark_segments(run, tape_m=8.0)
    check("úsekov medzi značkami", len(segments), 3)
    check("úsek 1 walked", segments[0]["walked_m"], 8.0)
    check("úsek 1 chyba %", segments[0]["error_pct"], 0.0)
    # mark-2 → mark-3 contains the alignment, so it must be flagged.
    check("úsek 2 označený ako prezarovnaný", 1.0 if segments[1]["realigned"] else 0.0, 1.0)
    check("úsek 3 walked", segments[2]["walked_m"], 8.0)

    jumps = jump_events(run)
    check("skokov nájdených", len(jumps), 3)
    check("prvý skok veľkosť", jumps[0]["size_m"], 3.0)
    check("prvý skok zvisle", jumps[0]["vertical_m"], 2.0)
    check("druhý skok zvisle", jumps[1]["vertical_m"], -1.0)

    episodes = loss_episodes(run, window_s=60.0)
    check("epizód straty", len(episodes), 1)
    check("naslepo", episodes[0]["blind_s"], 5.0)
    # Two jumps inside the 60 s window, the one at 200 s is outside it.
    check("skokov v okne", len(episodes[0]["after"]), 2)
    check("objem v okne", episodes[0]["after_total_m"], 10.0)
    check("oneskorenie prvého", episodes[0]["after"][0]["delay_s"], 1.0)

    probes = probe_gaps(run)
    check("navmesh diskov", len(probes["navmesh"]), 2)
    check("prvá medzera cm", probes["navmesh"][0]["gap_cm"], 12.5)
    # The second label carries a decimal comma; the parser must glue the row back together.
    check("druhá medzera cm (log s čiarkou)", probes["navmesh"][1]["gap_cm"], 7.5)

    width = max(len(name) for name, _, _, _ in checks)
    for name, got, expected, ok in checks:
        status = "ok  " if ok else "CHYBA"
        print(f"{status} {name.ljust(width)}  dostal {got!r}, čakal {expected!r}")

    failed = [c for c in checks if not c[3]]
    print()
    if failed:
        print(f"{len(failed)} z {len(checks)} kontrol NEPREŠLO")
        return 1
    print(f"VŠETKÝCH {len(checks)} KONTROL PREŠLO")
    return 0


# --- cli -------------------------------------------------------------------------


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Vyhodnotí CSV logy z FriLens.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__)
    parser.add_argument("logs", nargs="*", help="CSV logy (podporuje aj zástupné znaky)")
    parser.add_argument("--tape", type=float, metavar="M",
                        help="známa dĺžka úseku medzi značkami, na výpočet chyby")
    parser.add_argument("--segments", metavar="SPEC",
                        help="ktoré dvojice značiek sú merané prechody, napr. 1-2,3-4,5-6,7-8; "
                             "bez toho sa berie každý druhý úsek")
    parser.add_argument("--after", type=float, default=60.0, metavar="S",
                        help="okno po obnove trackingu, v ktorom sa skoky pripisujú strate (60)")
    parser.add_argument("--plot", metavar="CESTA",
                        help="uloží graf; pri viacerých behoch je to priečinok")
    parser.add_argument("--table", action="store_true",
                        help="namiesto správ vypíše jeden riadok na beh")
    parser.add_argument("--json", action="store_true", help="strojovo čitateľný výstup")
    parser.add_argument("--selftest", action="store_true",
                        help="overí metriky na logu so známymi odpoveďami")
    args = parser.parse_args(argv)

    if args.selftest:
        return selftest()

    chosen = None
    if args.segments:
        try:
            chosen = parse_segments(args.segments)
        except ValueError as error:
            parser.error(str(error))

    paths = []
    for pattern in args.logs:
        expanded = sorted(glob.glob(pattern))
        paths.extend(expanded if expanded else [pattern])

    if not paths:
        parser.error("treba aspoň jeden log, alebo --selftest")

    runs = []
    for path in paths:
        try:
            runs.append(read_log(path))
        except (OSError, ValueError) as error:
            print(f"preskočené — {error}", file=sys.stderr)

    if not runs:
        return 1

    if args.json:
        payload = []
        for run in runs:
            payload.append({
                "summary": summarise(run),
                "segments": mark_segments(run, args.tape),
                "passes": measured_segments(mark_segments(run, args.tape), chosen)[0],
                "jumps": jump_events(run),
                "losses": loss_episodes(run, args.after),
                "probes": probe_gaps(run),
            })
        print(json.dumps(payload, indent=2, ensure_ascii=False))
    elif args.table:
        print(table(runs))
    else:
        for index, run in enumerate(runs):
            if index:
                print("\n---\n")
            print(report(run, args.tape, args.after, chosen))

    if args.plot:
        if len(runs) == 1 and not os.path.isdir(args.plot):
            print(f"\ngraf: {plot(runs[0], args.plot, args.after)}", file=sys.stderr)
        else:
            os.makedirs(args.plot, exist_ok=True)
            for run in runs:
                target = os.path.join(args.plot, os.path.splitext(run.name)[0] + ".png")
                print(f"graf: {plot(run, target, args.after)}", file=sys.stderr)

    return 0


if __name__ == "__main__":
    sys.exit(main())
