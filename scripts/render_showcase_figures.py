#!/usr/bin/env python
import argparse
import json
import math
from pathlib import Path
from xml.sax.saxutils import escape

from PIL import Image, ImageDraw, ImageFont

WIDTH = 1600
HEIGHT = 960
BG = "#f7f2ea"
PANEL = "#fffdf9"
EDGE = "#515860"
TEXT = "#1f2226"
MUTED = "#626a72"
BLUE = "#258acb"
GREEN = "#4d9c5b"
RED = "#c85454"
ORANGE = "#d5822b"
GOLD = "#b58b00"


def parse_args():
    parser = argparse.ArgumentParser(description="Render SyntheticBio showcase figures.")
    parser.add_argument("--data-root", required=True)
    parser.add_argument("--assets-root", required=True)
    parser.add_argument("--inventory-out", required=True)
    return parser.parse_args()


def load_font(size):
    candidates = [
        r"C:\Windows\Fonts\bahnschrift.ttf",
        r"C:\Windows\Fonts\segoeui.ttf",
        r"C:\Windows\Fonts\arial.ttf",
        "DejaVuSans.ttf",
    ]
    for candidate in candidates:
        try:
            return ImageFont.truetype(candidate, size=size)
        except OSError:
            continue
    return ImageFont.load_default()


FONT_12 = load_font(12)
FONT_14 = load_font(14)
FONT_16 = load_font(16)
FONT_20 = load_font(20)
FONT_26 = load_font(26)
FONT_34 = load_font(34)


class SvgCanvas:
    def __init__(self, width, height):
        self.width = width
        self.height = height
        self._parts = [
            f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>",
            "<style>",
            f"text {{ fill: {TEXT}; font-family: 'Bahnschrift', 'Segoe UI', Arial, sans-serif; }}",
            "</style>",
            f"<rect x='0' y='0' width='{width}' height='{height}' fill='{BG}' />",
        ]

    def rect(self, x, y, w, h, fill=PANEL, stroke=EDGE, stroke_width=1, radius=16):
        self._parts.append(
            f"<rect x='{x:.1f}' y='{y:.1f}' width='{w:.1f}' height='{h:.1f}' "
            f"rx='{radius}' ry='{radius}' fill='{fill}' stroke='{stroke}' stroke-width='{stroke_width}' />"
        )

    def line(self, x1, y1, x2, y2, color=EDGE, width=1):
        self._parts.append(
            f"<line x1='{x1:.1f}' y1='{y1:.1f}' x2='{x2:.1f}' y2='{y2:.1f}' "
            f"stroke='{color}' stroke-width='{width}' stroke-linecap='round' />"
        )

    def polyline(self, points, color=BLUE, width=2):
        if len(points) < 2:
            return
        path = " ".join(f"{x:.1f},{y:.1f}" for x, y in points)
        self._parts.append(
            f"<polyline points='{path}' fill='none' stroke='{color}' stroke-width='{width}' stroke-linecap='round' stroke-linejoin='round' />"
        )

    def circle(self, x, y, radius, fill=BLUE, stroke=None, stroke_width=1):
        stroke_attr = f" stroke='{stroke}' stroke-width='{stroke_width}'" if stroke else ""
        self._parts.append(
            f"<circle cx='{x:.1f}' cy='{y:.1f}' r='{radius:.1f}' fill='{fill}'{stroke_attr} />"
        )

    def text(self, x, y, text, size=14, color=TEXT, anchor="start"):
        safe = escape(str(text))
        anchor_attr = {
            "start": "start",
            "middle": "middle",
            "end": "end",
        }.get(anchor, "start")
        self._parts.append(
            f"<text x='{x:.1f}' y='{y:.1f}' font-size='{size}' fill='{color}' text-anchor='{anchor_attr}'>{safe}</text>"
        )

    def save(self, path):
        Path(path).write_text("\n".join(self._parts + ["</svg>"]), encoding="utf-8")


class PngCanvas:
    def __init__(self, width, height):
        self.width = width
        self.height = height
        self.image = Image.new("RGB", (width, height), BG)
        self.draw = ImageDraw.Draw(self.image)

    def rect(self, x, y, w, h, fill=PANEL, stroke=EDGE, stroke_width=1, radius=16):
        self.draw.rounded_rectangle(
            [x, y, x + w, y + h],
            radius=radius,
            fill=fill,
            outline=stroke,
            width=stroke_width,
        )

    def line(self, x1, y1, x2, y2, color=EDGE, width=1):
        self.draw.line([x1, y1, x2, y2], fill=color, width=width)

    def polyline(self, points, color=BLUE, width=2):
        if len(points) < 2:
            return
        self.draw.line(points, fill=color, width=width, joint="curve")

    def circle(self, x, y, radius, fill=BLUE, stroke=None, stroke_width=1):
        box = [x - radius, y - radius, x + radius, y + radius]
        self.draw.ellipse(box, fill=fill, outline=stroke, width=stroke_width)

    def text(self, x, y, text, size=14, color=TEXT, anchor="start"):
        font = font_for_size(size)
        content = str(text)
        left = x
        box = self.draw.textbbox((0, 0), content, font=font)
        width = box[2] - box[0]
        if anchor == "middle":
            left = x - (width / 2)
        elif anchor == "end":
            left = x - width
        self.draw.text((left, y - size), content, font=font, fill=color)

    def save(self, path):
        self.image.save(path, format="PNG")


def font_for_size(size):
    if size <= 12:
        return FONT_12
    if size <= 14:
        return FONT_14
    if size <= 16:
        return FONT_16
    if size <= 20:
        return FONT_20
    if size <= 26:
        return FONT_26
    return FONT_34


def load_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def downsample(points, max_points=320):
    if len(points) <= max_points:
        return points
    step = max(1, len(points) // max_points)
    reduced = points[::step]
    if reduced[-1] != points[-1]:
        reduced.append(points[-1])
    return reduced


def to_pairs(entries, x_key, y_key):
    return [(float(entry[x_key]), float(entry[y_key])) for entry in entries]


def draw_header(canvas, title, subtitle):
    canvas.text(48, 52, title, size=34, color=TEXT)
    canvas.text(48, 92, subtitle, size=16, color=MUTED)


def draw_panel(canvas, x, y, w, h, title, subtitle=None):
    canvas.rect(x, y, w, h, fill=PANEL, stroke=EDGE, stroke_width=1, radius=18)
    canvas.text(x + 20, y + 34, title, size=20, color=TEXT)
    if subtitle:
        canvas.text(x + 20, y + 58, subtitle, size=12, color=MUTED)


def normalize_zero(value):
    return 0.0 if math.isclose(value, 0.0, abs_tol=1e-9) else value


def format_axis_tick(value, span):
    value = normalize_zero(value)
    span = abs(span)
    magnitude = max(abs(value), span)

    if magnitude >= 1_000_000:
        return f"{value / 1_000_000:.1f}M"
    if magnitude >= 10_000:
        return f"{value / 1_000:.0f}k"
    if magnitude >= 1_000:
        return f"{value / 1_000:.1f}k"
    if span < 1.0:
        return f"{value:.2f}"
    if span < 10.0:
        return f"{value:.2f}"
    if span < 100.0:
        return f"{value:.1f}"
    return f"{value:.0f}"


def scenario_label(scenario_id):
    labels = {
        "coherence_high": "Coherence high",
        "coherence_low": "Coherence low",
        "hrv_high": "HRV high",
        "hrv_low": "HRV low",
        "entropy_high": "Entropy high",
        "entropy_low": "Entropy low",
        "resonance_010hz": "Resonance (0.10 Hz)",
        "off_10bpm": "10 bpm",
        "off_12bpm": "12 bpm",
        "off_18bpm": "18 bpm",
        "off_24bpm": "24 bpm",
        "irregular_rr": "Irregular RR",
        "entropy_rising": "Entropy rising",
        "jittered_breathing": "Entropy jittered",
        "flat_breathing": "Flat breathing",
        "breathing_pause": "Breathing pause",
    }
    return labels.get(scenario_id, scenario_id.replace("_", " ").title())


def tracking_state_label(kind, value):
    if isinstance(value, str):
        return value

    try:
        numeric = int(value)
    except (TypeError, ValueError):
        return str(value)

    mappings = {
        "coherence": {
            0: "Unavailable",
            1: "Stale",
            2: "Tracking",
        },
        "hrv": {
            0: "Unavailable",
            1: "Waiting for RR",
            2: "Warming up",
            3: "Tracking",
            4: "Stale",
        },
        "dynamics": {
            0: "Unavailable",
            1: "Waiting for calibration",
            2: "Waiting for breathing tracking",
            3: "Tracking",
            4: "Stale",
        },
    }
    return mappings.get(kind, {}).get(numeric, str(numeric))


def figure_role_label(scenario_id):
    canonical = {
        "coherence_high",
        "coherence_low",
        "hrv_high",
        "hrv_low",
        "entropy_high",
        "entropy_low",
    }
    appendix = {
        "resonance_010hz",
        "off_10bpm",
        "off_12bpm",
        "off_18bpm",
        "off_24bpm",
        "irregular_rr",
        "entropy_rising",
        "jittered_breathing",
    }
    edge_states = {
        "flat_breathing",
        "breathing_pause",
    }

    if scenario_id in canonical:
        return "Canonical pair"
    if scenario_id in appendix:
        return "Appendix comparison"
    if scenario_id in edge_states:
        return "Edge-state check"
    return "Additional scenario"


def draw_line_chart(canvas, x, y, w, h, title, series, x_label, y_label, x_range=None, y_range=None):
    draw_panel(canvas, x, y, w, h, title)
    plot_x = x + 78
    plot_y = y + 84
    plot_w = w - 108
    plot_h = h - 150
    canvas.line(plot_x, plot_y + plot_h, plot_x + plot_w, plot_y + plot_h, color=EDGE, width=1)
    canvas.line(plot_x, plot_y, plot_x, plot_y + plot_h, color=EDGE, width=1)

    all_points = [point for item in series for point in item["data"]]
    if not all_points:
        canvas.text(plot_x + 20, plot_y + 40, "No data available", size=16, color=MUTED)
        return

    xs = [point[0] for point in all_points]
    ys = [point[1] for point in all_points]
    x_min = x_range[0] if x_range else min(xs)
    x_max = x_range[1] if x_range else max(xs)
    y_min = y_range[0] if y_range else min(ys)
    y_max = y_range[1] if y_range else max(ys)
    if math.isclose(x_min, x_max):
        x_max = x_min + 1.0
    if math.isclose(y_min, y_max):
        y_max = y_min + 1.0
    x_span = x_max - x_min
    y_span = y_max - y_min

    for tick in range(5):
        factor = tick / 4.0
        tick_y = plot_y + plot_h - (plot_h * factor)
        tick_value = y_min + ((y_max - y_min) * factor)
        canvas.line(plot_x - 6, tick_y, plot_x, tick_y, color=EDGE, width=1)
        canvas.text(plot_x - 12, tick_y + 4, format_axis_tick(tick_value, y_span), size=12, color=MUTED, anchor="end")

    for tick in range(5):
        factor = tick / 4.0
        tick_x = plot_x + (plot_w * factor)
        tick_value = x_min + ((x_max - x_min) * factor)
        canvas.line(tick_x, plot_y + plot_h, tick_x, plot_y + plot_h + 6, color=EDGE, width=1)
        canvas.text(tick_x, plot_y + plot_h + 24, format_axis_tick(tick_value, x_span), size=12, color=MUTED, anchor="middle")

    canvas.text(plot_x + (plot_w / 2), y + h - 10, x_label, size=12, color=MUTED, anchor="middle")
    canvas.text(x + 20, y + 58, y_label, size=12, color=MUTED)

    legend_x = x + w - 220
    legend_y = y + 28
    for index, item in enumerate(series):
        ly = legend_y + (index * 18)
        canvas.line(legend_x, ly, legend_x + 18, ly, color=item["color"], width=3)
        canvas.text(legend_x + 28, ly + 4, item["label"], size=12, color=MUTED)

    for item in series:
        points = []
        for px, py in downsample(item["data"]):
            norm_x = (px - x_min) / (x_max - x_min)
            norm_y = (py - y_min) / (y_max - y_min)
            points.append((plot_x + (norm_x * plot_w), plot_y + plot_h - (norm_y * plot_h)))
        canvas.polyline(points, color=item["color"], width=3)
        for point_x, point_y in points[:: max(1, len(points) // 12 or 1)]:
            canvas.circle(point_x, point_y, 3, fill=item["color"])


def draw_bar_chart(canvas, x, y, w, h, title, entries, y_label, value_keys):
    draw_panel(canvas, x, y, w, h, title)
    plot_x = x + 78
    plot_y = y + 84
    plot_w = w - 108
    plot_h = h - 150
    canvas.line(plot_x, plot_y + plot_h, plot_x + plot_w, plot_y + plot_h, color=EDGE, width=1)
    canvas.line(plot_x, plot_y, plot_x, plot_y + plot_h, color=EDGE, width=1)
    if not entries:
        canvas.text(plot_x + 20, plot_y + 40, "No data available", size=16, color=MUTED)
        return

    series_defs = []
    palette = [BLUE, GREEN, ORANGE]
    for index, item in enumerate(value_keys):
        if isinstance(item, dict):
            key = item["key"]
            label = item.get("label", key)
            color = item.get("color", palette[index % len(palette)])
        elif isinstance(item, (list, tuple)) and len(item) >= 2:
            key = item[0]
            label = item[1]
            color = palette[index % len(palette)]
        else:
            key = str(item)
            label = key
            color = palette[index % len(palette)]
        series_defs.append({"key": key, "label": label, "color": color})

    max_value = max(float(entry[item["key"]]) for entry in entries for item in series_defs) or 1.0
    value_span = max_value
    group_width = plot_w / len(entries)
    bar_width = max(14, (group_width - 24) / len(series_defs))

    for tick in range(5):
        factor = tick / 4.0
        tick_y = plot_y + plot_h - (plot_h * factor)
        tick_value = max_value * factor
        canvas.line(plot_x - 6, tick_y, plot_x, tick_y, color=EDGE, width=1)
        canvas.text(plot_x - 12, tick_y + 4, format_axis_tick(tick_value, value_span), size=12, color=MUTED, anchor="end")

    for index, entry in enumerate(entries):
        group_x = plot_x + (index * group_width) + 12
        for key_index, item in enumerate(series_defs):
            value = float(entry[item["key"]])
            bar_height = 0 if max_value == 0 else (value / max_value) * plot_h
            bar_x = group_x + (key_index * bar_width)
            bar_y = plot_y + plot_h - bar_height
            color = item["color"]
            canvas.rect(bar_x, bar_y, bar_width - 6, bar_height, fill=color, stroke=color, radius=6)
        canvas.text(plot_x + (index * group_width) + (group_width / 2), plot_y + plot_h + 24, entry["label"], size=12, color=MUTED, anchor="middle")

    legend_x = x + w - 200
    for key_index, item in enumerate(series_defs):
        ly = y + 28 + (key_index * 18)
        color = item["color"]
        canvas.line(legend_x, ly, legend_x + 18, ly, color=color, width=3)
        canvas.text(legend_x + 26, ly + 4, item["label"], size=12, color=MUTED)
    canvas.text(x + 20, y + 58, y_label, size=12, color=MUTED)


def draw_table(canvas, x, y, w, h, title, headers, rows, alignments=None):
    draw_panel(canvas, x, y, w, h, title)
    top = y + 70
    left = x + 20
    column_count = max(1, len(headers))
    alignments = list(alignments or ["start"] * column_count)
    if len(alignments) < column_count:
        alignments.extend(["start"] * (column_count - len(alignments)))
    if column_count == 1:
        col_widths = [w - 48]
    else:
        first = w * 0.24
        last = w * 0.18
        middle_count = max(0, column_count - 2)
        remaining = max(0.0, w - 48 - first - last)
        middle = (remaining / middle_count) if middle_count > 0 else 0.0
        col_widths = [first]
        for _ in range(middle_count):
            col_widths.append(middle)
        col_widths.append(last if column_count > 1 else 0.0)

    def cell_anchor(column_index):
        alignment = alignments[column_index]
        if alignment == "end":
            return "end"
        if alignment == "middle":
            return "middle"
        return "start"

    def cell_x(column_index, cursor_x):
        alignment = alignments[column_index]
        if alignment == "end":
            return cursor_x + col_widths[column_index] - 12
        if alignment == "middle":
            return cursor_x + (col_widths[column_index] / 2)
        return cursor_x

    cursor_x = left
    for index, header in enumerate(headers):
        canvas.text(cell_x(index, cursor_x), top, header, size=14, color=MUTED, anchor=cell_anchor(index))
        cursor_x += col_widths[index]

    row_y = top + 28
    for row in rows:
        canvas.line(left, row_y - 16, x + w - 24, row_y - 16, color="#d7d2cb", width=1)
        cursor_x = left
        for index, cell in enumerate(row):
            canvas.text(cell_x(index, cursor_x), row_y, cell, size=14, color=TEXT, anchor=cell_anchor(index))
            cursor_x += col_widths[index]
        row_y += 28


def render_dual(canvas_factory, output_svg, output_png, draw_fn):
    svg = SvgCanvas(WIDTH, HEIGHT)
    png = PngCanvas(WIDTH, HEIGHT)
    draw_fn(svg)
    draw_fn(png)
    svg.save(output_svg)
    png.save(output_png)


def scenario_value(analysis, path, default="--"):
    node = analysis
    for key in path:
        node = node.get(key)
        if node is None:
            return default
    if isinstance(node, float):
        return f"{node:.3f}"
    return str(node)


def load_bundle(data_root):
    scenario_root = Path(data_root) / "scenarios"
    bundle = {}
    for scenario_dir in sorted(scenario_root.iterdir()):
        if not scenario_dir.is_dir():
            continue
        bundle[scenario_dir.name] = {
            "analysis": load_json(scenario_dir / "analysis.json"),
            "ground_truth": load_json(scenario_dir / "ground_truth.json"),
        }
    return bundle


def metric_row(label, analysis):
    coherence = analysis["Coherence"]["Telemetry"]
    hrv = analysis["Hrv"]["Telemetry"]
    dynamics = analysis["Dynamics"]["Telemetry"]
    interval_entropy = dynamics["Interval"]["SampleEntropy"] if dynamics["IntervalHasEntropyMetrics"] else None
    amplitude_entropy = dynamics["Amplitude"]["SampleEntropy"] if dynamics["AmplitudeHasEntropyMetrics"] else None
    return (
        scenario_label(label),
        format_optional(coherence.get("CurrentCoherence01"), decimals=3),
        format_optional(hrv.get("CurrentRmssdMs"), decimals=1) if hrv["HasMetricsSample"] else "--",
        format_optional(interval_entropy, decimals=3),
        format_optional(amplitude_entropy, decimals=3),
        figure_role_label(label),
    )


def format_optional(value, decimals=3, default="--"):
    if value is None:
        return default
    try:
        numeric = float(value)
    except (TypeError, ValueError):
        return default
    if not math.isfinite(numeric):
        return default
    return f"{numeric:.{decimals}f}"


def render_overview(bundle, assets_root):
    headers = ("Scenario", "Coherence (0-1)", "RMSSD (ms)", "Interval SampEn", "Amplitude SampEn", "Figure role")
    rows = [metric_row(name, bundle[name]["analysis"]) for name in sorted(bundle.keys())]

    def draw(canvas):
        draw_header(canvas, "Synthetic showcase matrix", "Deterministic showcase-v1 scenarios with publication-facing summary endpoints and figure roles exported from SyntheticBio.")
        draw_table(canvas, 40, 120, 1520, 760, "Scenario overview", headers, rows, alignments=("start", "end", "end", "end", "end", "start"))

    render_dual(
        canvas_factory=None,
        output_svg=assets_root / "showcase-overview.svg",
        output_png=assets_root / "showcase-overview.png",
        draw_fn=draw,
    )


def render_coherence(bundle, assets_root):
    high = bundle["coherence_high"]["analysis"]
    low = bundle["coherence_low"]["analysis"]
    rr_high = to_pairs(high["Coherence"]["Diagnostics"]["AcceptedRrSamples"], "TimeSeconds", "IbiMs")
    rr_low = to_pairs(low["Coherence"]["Diagnostics"]["AcceptedRrSamples"], "TimeSeconds", "IbiMs")
    psd_high = [(point["FrequencyHz"], point["Power"]) for point in high["Coherence"]["Diagnostics"]["PowerSpectrum"] if point["FrequencyHz"] <= 0.4]
    psd_low = [(point["FrequencyHz"], point["Power"]) for point in low["Coherence"]["Diagnostics"]["PowerSpectrum"] if point["FrequencyHz"] <= 0.4]
    rows = [
        (
            "High coherence",
            f"{high['Coherence']['Telemetry']['PeakFrequencyHz']:.3f}",
            f"{high['Coherence']['Telemetry']['PeakBandPower']:.0f}",
            f"{high['Coherence']['Telemetry']['TotalBandPower']:.0f}",
            f"{high['Coherence']['Telemetry']['PaperCoherenceRatio']:.3f}",
            f"{high['Coherence']['Telemetry']['NormalizedCoherence01']:.3f}",
            f"{high['Coherence']['Telemetry']['CurrentCoherence01']:.3f}",
        ),
        (
            "Low coherence",
            f"{low['Coherence']['Telemetry']['PeakFrequencyHz']:.3f}",
            f"{low['Coherence']['Telemetry']['PeakBandPower']:.0f}",
            f"{low['Coherence']['Telemetry']['TotalBandPower']:.0f}",
            f"{low['Coherence']['Telemetry']['PaperCoherenceRatio']:.3f}",
            f"{low['Coherence']['Telemetry']['NormalizedCoherence01']:.3f}",
            f"{low['Coherence']['Telemetry']['CurrentCoherence01']:.3f}",
        ),
    ]

    def draw(canvas):
        draw_header(canvas, "Coherence derivation", "Accepted RR intervals are resampled into a tachogram, transformed to a power spectrum, and summarized with both the cited paper ratio and the app-facing 0-1 scores.")
        draw_line_chart(
            canvas,
            40,
            120,
            740,
            360,
            "A. Accepted RR interval window",
            [
                {"label": "High coherence", "color": BLUE, "data": rr_high},
                {"label": "Low coherence", "color": RED, "data": rr_low},
            ],
            x_label="Time (s)",
            y_label="RR interval (ms)",
        )
        draw_line_chart(
            canvas,
            820,
            120,
            740,
            360,
            "B. Power spectral density",
            [
                {"label": "High coherence", "color": BLUE, "data": psd_high},
                {"label": "Low coherence", "color": RED, "data": psd_low},
            ],
            x_label="Frequency (Hz)",
            y_label="Spectral power (a.u.)",
            x_range=(0.0, 0.4),
        )
        draw_table(
            canvas,
            40,
            520,
            1520,
            340,
            "C. Coherence summary",
            ("Scenario", "Peak freq. (Hz)", "Peak window (a.u.)", "Total band (a.u.)", "Paper ratio", "Normalized (0-1)", "Displayed (0-1)"),
            rows,
            alignments=("start", "end", "end", "end", "end", "end", "end"),
        )
        canvas.text(64, 884, "Paper ratio = (peak-window power / (total band power - peak-window power))^2. Normalized coherence = peak-window power / total band power. Displayed coherence is the app-facing 0-1 readout.", size=14, color=MUTED)

    render_dual(None, assets_root / "coherence-derivation.svg", assets_root / "coherence-derivation.png", draw)


def render_hrv(bundle, assets_root):
    high = bundle["hrv_high"]["analysis"]
    low = bundle["hrv_low"]["analysis"]
    rr_high = to_pairs(high["Hrv"]["Diagnostics"]["AcceptedRrSamples"], "TimeSeconds", "IbiMs")
    rr_low = to_pairs(low["Hrv"]["Diagnostics"]["AcceptedRrSamples"], "TimeSeconds", "IbiMs")
    delta_high = to_pairs(high["Hrv"]["Diagnostics"]["AdjacentRrDeltas"], "TimeSeconds", "DeltaMs")
    delta_low = to_pairs(low["Hrv"]["Diagnostics"]["AdjacentRrDeltas"], "TimeSeconds", "DeltaMs")
    rows = [
        ("High HRV", f"{high['Hrv']['Telemetry']['CurrentRmssdMs']:.1f}", f"{high['Hrv']['Telemetry']['SdnnMs']:.1f}", f"{high['Hrv']['Telemetry']['Pnn50Percent']:.1f}", f"{high['Hrv']['Telemetry']['LnRmssd']:.3f}"),
        ("Low HRV", f"{low['Hrv']['Telemetry']['CurrentRmssdMs']:.1f}", f"{low['Hrv']['Telemetry']['SdnnMs']:.1f}", f"{low['Hrv']['Telemetry']['Pnn50Percent']:.1f}", f"{low['Hrv']['Telemetry']['LnRmssd']:.3f}"),
    ]

    def draw(canvas):
        draw_header(canvas, "HRV derivation", "Accepted RR intervals are differenced beat-to-beat and summarized with short-term time-domain HRV metrics following the Shaffer-Ginsberg framing.")
        draw_line_chart(
            canvas,
            40,
            120,
            740,
            360,
            "A. Accepted RR interval window",
            [
                {"label": "High HRV", "color": GREEN, "data": rr_high},
                {"label": "Low HRV", "color": ORANGE, "data": rr_low},
            ],
            x_label="Time (s)",
            y_label="RR interval (ms)",
        )
        draw_line_chart(
            canvas,
            820,
            120,
            740,
            360,
            "B. Successive RR differences",
            [
                {"label": "High HRV", "color": GREEN, "data": delta_high},
                {"label": "Low HRV", "color": ORANGE, "data": delta_low},
            ],
            x_label="Time (s)",
            y_label="Successive difference (ms)",
        )
        draw_table(
            canvas,
            40,
            520,
            1520,
            340,
            "C. Time-domain HRV summary",
            ("Scenario", "RMSSD (ms)", "SDNN (ms)", "pNN50 (%)", "ln(RMSSD)"),
            rows,
            alignments=("start", "end", "end", "end", "end"),
        )
        canvas.text(64, 884, "These traces are intended as reproducible short-term examples. They are not substitutes for 24 h norms or clinical artifact adjudication.", size=14, color=MUTED)

    render_dual(None, assets_root / "hrv-derivation.svg", assets_root / "hrv-derivation.png", draw)


def render_dynamics(bundle, assets_root):
    high = bundle["entropy_high"]["analysis"]
    low = bundle["entropy_low"]["analysis"]
    waveform_high = to_pairs(high["Dynamics"]["Diagnostics"]["WaveformSamples"], "TimeSeconds", "Value01")
    waveform_low = to_pairs(low["Dynamics"]["Diagnostics"]["WaveformSamples"], "TimeSeconds", "Value01")
    interval_high = to_pairs(high["Dynamics"]["Diagnostics"]["IntervalSeries"], "Index", "Value")
    interval_low = to_pairs(low["Dynamics"]["Diagnostics"]["IntervalSeries"], "Index", "Value")
    amplitude_high = to_pairs(high["Dynamics"]["Diagnostics"]["AmplitudeSeries"], "Index", "Value")
    amplitude_low = to_pairs(low["Dynamics"]["Diagnostics"]["AmplitudeSeries"], "Index", "Value")
    rows = [
        ("High", format_optional(high['Dynamics']['Telemetry']['Interval']['SampleEntropy']), format_optional(high['Dynamics']['Telemetry']['Amplitude']['SampleEntropy']), format_optional(high['Dynamics']['Telemetry']['Interval']['CoefficientOfVariation']), format_optional(high['Dynamics']['Telemetry']['Amplitude']['CoefficientOfVariation'])),
        ("Low", format_optional(low['Dynamics']['Telemetry']['Interval']['SampleEntropy']), format_optional(low['Dynamics']['Telemetry']['Amplitude']['SampleEntropy']), format_optional(low['Dynamics']['Telemetry']['Interval']['CoefficientOfVariation']), format_optional(low['Dynamics']['Telemetry']['Amplitude']['CoefficientOfVariation'])),
    ]

    def draw(canvas):
        draw_header(canvas, "Breathing-dynamics derivation", "An ACC-derived breathing waveform is converted to accepted extrema, breath-interval and breath-amplitude series, and entropy-oriented summary statistics.")
        draw_line_chart(
            canvas,
            40,
            120,
            740,
            250,
            "A. Breathing waveform",
            [
                {"label": "High entropy", "color": RED, "data": waveform_high},
                {"label": "Low entropy", "color": BLUE, "data": waveform_low},
            ],
            x_label="Time (s)",
            y_label="Normalized amplitude (a.u.)",
            y_range=(0.0, 1.0),
        )
        draw_line_chart(
            canvas,
            820,
            120,
            740,
            250,
            "B. Breath-interval series",
            [
                {"label": "High entropy", "color": RED, "data": interval_high},
                {"label": "Low entropy", "color": BLUE, "data": interval_low},
            ],
            x_label="Breath index",
            y_label="Interval (s)",
        )
        draw_line_chart(
            canvas,
            40,
            400,
            740,
            250,
            "C. Breath-amplitude series",
            [
                {"label": "High entropy", "color": RED, "data": amplitude_high},
                {"label": "Low entropy", "color": BLUE, "data": amplitude_low},
            ],
            x_label="Breath index",
            y_label="Amplitude (a.u.)",
        )
        draw_table(
            canvas,
            820,
            400,
            740,
            340,
            "D. Entropy and variability summary",
            ("Scenario", "Interval SampEn", "Amplitude SampEn", "Interval CV", "Amplitude CV"),
            rows,
            alignments=("start", "end", "end", "end", "end"),
        )
        canvas.text(64, 884, "SampEn settings: m = 2, delay = 1, r = 0.2 * SD. The waveform source is the repo's calibrated ACC breathing signal rather than a respiration belt.", size=14, color=MUTED)

    render_dual(None, assets_root / "dynamics-derivation.svg", assets_root / "dynamics-derivation.png", draw)


def render_coherence_appendix(bundle, assets_root):
    labels = ["0.10 Hz", "10 bpm", "12 bpm", "18 bpm", "24 bpm", "Irregular RR"]
    entries = [
        {"label": labels[0], "normalized": bundle["resonance_010hz"]["analysis"]["Coherence"]["Telemetry"]["NormalizedCoherence01"]},
        {"label": labels[1], "normalized": bundle["off_10bpm"]["analysis"]["Coherence"]["Telemetry"]["NormalizedCoherence01"]},
        {"label": labels[2], "normalized": bundle["off_12bpm"]["analysis"]["Coherence"]["Telemetry"]["NormalizedCoherence01"]},
        {"label": labels[3], "normalized": bundle["off_18bpm"]["analysis"]["Coherence"]["Telemetry"]["NormalizedCoherence01"]},
        {"label": labels[4], "normalized": bundle["off_24bpm"]["analysis"]["Coherence"]["Telemetry"]["NormalizedCoherence01"]},
        {"label": labels[5], "normalized": bundle["irregular_rr"]["analysis"]["Coherence"]["Telemetry"]["NormalizedCoherence01"]},
    ]
    rows = [
        (
            entry["label"],
            f"{scenario['analysis']['Coherence']['Telemetry']['PeakFrequencyHz']:.3f}",
            f"{scenario['analysis']['Coherence']['Telemetry']['PaperCoherenceRatio']:.3f}",
            f"{scenario['analysis']['Coherence']['Telemetry']['NormalizedCoherence01']:.3f}",
            f"{scenario['analysis']['Coherence']['Telemetry']['CurrentCoherence01']:.3f}",
        )
        for entry, scenario in zip(
            entries,
            [
                bundle["resonance_010hz"],
                bundle["off_10bpm"],
                bundle["off_12bpm"],
                bundle["off_18bpm"],
                bundle["off_24bpm"],
                bundle["irregular_rr"],
            ],
        )
    ]

    def draw(canvas):
        draw_header(canvas, "Coherence appendix", "The resonance condition remains separated from the off-resonance sweep and the irregular-RR comparison scenario.")
        draw_bar_chart(
            canvas,
            40,
            120,
            1520,
            460,
            "A. Normalized coherence across comparison scenarios",
            entries,
            "Normalized coherence (0-1)",
            [{"key": "normalized", "label": "Normalized coherence (0-1)", "color": BLUE}],
        )
        draw_table(
            canvas,
            40,
            620,
            1520,
            240,
            "B. Scenario summary",
            ("Scenario", "Peak freq. (Hz)", "Paper ratio", "Normalized (0-1)", "Displayed (0-1)"),
            rows,
            alignments=("start", "end", "end", "end", "end"),
        )

    render_dual(None, assets_root / "coherence-appendix.svg", assets_root / "coherence-appendix.png", draw)


def render_dynamics_appendix(bundle, assets_root):
    entries = [
        {
            "label": "Low",
            "interval": bundle["entropy_low"]["analysis"]["Dynamics"]["Telemetry"]["Interval"]["SampleEntropy"],
            "amplitude": bundle["entropy_low"]["analysis"]["Dynamics"]["Telemetry"]["Amplitude"]["SampleEntropy"],
        },
        {
            "label": "Rising",
            "interval": bundle["entropy_rising"]["analysis"]["Dynamics"]["Telemetry"]["Interval"]["SampleEntropy"],
            "amplitude": bundle["entropy_rising"]["analysis"]["Dynamics"]["Telemetry"]["Amplitude"]["SampleEntropy"],
        },
        {
            "label": "High",
            "interval": bundle["entropy_high"]["analysis"]["Dynamics"]["Telemetry"]["Interval"]["SampleEntropy"],
            "amplitude": bundle["entropy_high"]["analysis"]["Dynamics"]["Telemetry"]["Amplitude"]["SampleEntropy"],
        },
        {
            "label": "Jittered",
            "interval": bundle["jittered_breathing"]["analysis"]["Dynamics"]["Telemetry"]["Interval"]["SampleEntropy"],
            "amplitude": bundle["jittered_breathing"]["analysis"]["Dynamics"]["Telemetry"]["Amplitude"]["SampleEntropy"],
        },
    ]
    rows = [
        (
            "Flat breathing",
            tracking_state_label("dynamics", bundle["flat_breathing"]["analysis"]["Dynamics"]["Telemetry"]["TrackingState"]),
            "--",
            "--",
        ),
        (
            "Breathing pause",
            tracking_state_label("dynamics", bundle["breathing_pause"]["analysis"]["Dynamics"]["Telemetry"]["TrackingState"]),
            format_optional(bundle['breathing_pause']['analysis']['Dynamics']['Telemetry']['Interval']['SampleEntropy']),
            format_optional(bundle['breathing_pause']['analysis']['Dynamics']['Telemetry']['Amplitude']['SampleEntropy']),
        ),
    ]

    def draw(canvas):
        draw_header(canvas, "Dynamics appendix", "Entropy variants and tracker edge states are retained in the supplementary bundle for comparison and failure-mode reporting.")
        draw_bar_chart(
            canvas,
            40,
            120,
            1520,
            460,
            "A. Sample entropy across comparison scenarios",
            entries,
            "Sample entropy",
            [
                {"key": "interval", "label": "Interval SampEn", "color": BLUE},
                {"key": "amplitude", "label": "Amplitude SampEn", "color": GREEN},
            ],
        )
        draw_table(
            canvas,
            40,
            620,
            1520,
            220,
            "B. Edge-state summary",
            ("Scenario", "Tracking state", "Interval SampEn", "Amplitude SampEn"),
            rows,
            alignments=("start", "start", "end", "end"),
        )

    render_dual(None, assets_root / "dynamics-appendix.svg", assets_root / "dynamics-appendix.png", draw)


def build_pdf_pack(assets_root):
    png_paths = [
        assets_root / "showcase-overview.png",
        assets_root / "coherence-derivation.png",
        assets_root / "hrv-derivation.png",
        assets_root / "dynamics-derivation.png",
        assets_root / "coherence-appendix.png",
        assets_root / "dynamics-appendix.png",
    ]
    images = [Image.open(path).convert("RGB") for path in png_paths]
    pdf_path = assets_root / "synthetic-showcase-figure-pack.pdf"
    images[0].save(pdf_path, save_all=True, append_images=images[1:], resolution=150)
    for image in images:
        image.close()


def build_inventory(assets_root):
    assets = [
        ("showcase-overview-svg", "overview", "svg", "showcase-overview.svg", "Synthetic showcase matrix (SVG)"),
        ("showcase-overview-png", "overview", "png", "showcase-overview.png", "Synthetic showcase matrix (PNG)"),
        ("coherence-derivation-svg", "coherence-derivation", "svg", "coherence-derivation.svg", "Coherence derivation figure (SVG)"),
        ("coherence-derivation-png", "coherence-derivation", "png", "coherence-derivation.png", "Coherence derivation figure (PNG)"),
        ("hrv-derivation-svg", "hrv-derivation", "svg", "hrv-derivation.svg", "HRV derivation figure (SVG)"),
        ("hrv-derivation-png", "hrv-derivation", "png", "hrv-derivation.png", "HRV derivation figure (PNG)"),
        ("dynamics-derivation-svg", "dynamics-derivation", "svg", "dynamics-derivation.svg", "Breathing-dynamics derivation figure (SVG)"),
        ("dynamics-derivation-png", "dynamics-derivation", "png", "dynamics-derivation.png", "Breathing-dynamics derivation figure (PNG)"),
        ("coherence-appendix-svg", "coherence-appendix", "svg", "coherence-appendix.svg", "Coherence appendix figure (SVG)"),
        ("coherence-appendix-png", "coherence-appendix", "png", "coherence-appendix.png", "Coherence appendix figure (PNG)"),
        ("dynamics-appendix-svg", "dynamics-appendix", "svg", "dynamics-appendix.svg", "Dynamics appendix figure (SVG)"),
        ("dynamics-appendix-png", "dynamics-appendix", "png", "dynamics-appendix.png", "Dynamics appendix figure (PNG)"),
        ("figure-pack-pdf", "supplementary-pack", "pdf", "synthetic-showcase-figure-pack.pdf", "Synthetic showcase supplementary figure pack (PDF)"),
    ]
    return {
        "assets": [
            {
                "id": asset_id,
                "role": role,
                "format": asset_format,
                "path": path,
                "title": title,
            }
            for asset_id, role, asset_format, path, title in assets
        ]
    }


def main():
    args = parse_args()
    data_root = Path(args.data_root)
    assets_root = Path(args.assets_root)
    assets_root.mkdir(parents=True, exist_ok=True)
    bundle = load_bundle(data_root)

    render_overview(bundle, assets_root)
    render_coherence(bundle, assets_root)
    render_hrv(bundle, assets_root)
    render_dynamics(bundle, assets_root)
    render_coherence_appendix(bundle, assets_root)
    render_dynamics_appendix(bundle, assets_root)
    build_pdf_pack(assets_root)

    inventory = build_inventory(assets_root)
    Path(args.inventory_out).write_text(json.dumps(inventory, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
