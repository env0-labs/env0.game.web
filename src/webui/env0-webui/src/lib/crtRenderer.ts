import type { TextBuffer } from "./textBuffer";

export type CrtParams = {
  fontPx: number;
  lineHeight: number;
  fg: string;
  glow: string;
  bg: string;
  scanlineAlpha: number;
  glitchChance: number; // 0..1
  wobbleAmpPx: number;
  wobbleSpeed: number;
};

const GLYPH_SET =
  "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*()_+-={}[]|:;\"'<>,.?/\\";

function rand01(seed: number) {
  let x = seed | 0;
  x ^= x << 13;
  x ^= x >> 17;
  x ^= x << 5;
  return ((x >>> 0) % 100000) / 100000;
}

function pickGlitchChar(seed: number) {
  const i = Math.floor(rand01(seed) * GLYPH_SET.length);
  return GLYPH_SET[i] ?? "?";
}

export type RenderCache = {
  version: number;
  rows: string[];
};

export function createRenderCache(): RenderCache {
  return { version: -1, rows: [] };
}

export function renderCrt(
  ctx: CanvasRenderingContext2D,
  buf: TextBuffer,
  tMs: number,
  params: CrtParams,
  cache: RenderCache
) {
  const { fontPx, lineHeight } = params;

  const w = ctx.canvas.width;
  const h = ctx.canvas.height;

  // background
  ctx.fillStyle = params.bg;
  ctx.fillRect(0, 0, w, h);

  ctx.textBaseline = "top";
  ctx.font = `${fontPx}px ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace`;

  const cellW = Math.max(1, Math.floor(fontPx * 0.6));
  const cellH = Math.floor(fontPx * lineHeight);

  // Cache row strings when buffer changes.
  if (cache.version !== buf.version) {
    cache.rows = [];
    for (let r = 0; r < buf.rows; r++) {
      const start = r * buf.cols;
      const end = start + buf.cols;
      cache.rows.push(
        buf.cells
          .slice(start, end)
          .map((c) => c.ch)
          .join("")
      );
    }
    cache.version = buf.version;
  }

  // glow via shadow
  ctx.fillStyle = params.fg;
  ctx.shadowColor = params.glow;
  ctx.shadowBlur = 8;

  const wobbleT = tMs * 0.001 * params.wobbleSpeed;

  // Draw each row as a single string (fast) + sparse glitch overlays.
  for (let r = 0; r < buf.rows; r++) {
    const wobble = Math.sin(wobbleT + r * 0.35) * params.wobbleAmpPx;
    const x = 16 + wobble;
    const y = 16 + r * cellH;

    const rowText = cache.rows[r] ?? "";
    ctx.fillText(rowText, x, y);
  }

  // Sparse per-char glitch overlays (keeps the vibe without per-glyph draw cost)
  const glitchCount = Math.floor(buf.cols * buf.rows * 0.004);
  for (let k = 0; k < glitchCount; k++) {
    const seed = ((tMs / 33) | 0) * 1337 + k * 97;
    if (rand01(seed) > params.glitchChance) continue;

    const r = Math.floor(rand01(seed ^ 0xabc) * buf.rows);
    const c = Math.floor(rand01(seed ^ 0xdef) * buf.cols);
    const cell = buf.cells[r * buf.cols + c];
    if (!cell || cell.ch === " ") continue;

    const wobble = Math.sin(wobbleT + r * 0.35) * params.wobbleAmpPx;
    const x = 16 + c * cellW + wobble;
    const y = 16 + r * cellH;

    // chromatic pop for glitch only
    const ch = pickGlitchChar(seed ^ 0x9e3779b9);
    ctx.shadowBlur = 0;
    ctx.globalAlpha = 0.6;
    ctx.fillStyle = "#46f2ff";
    ctx.fillText(ch, x + 1, y);
    ctx.fillStyle = "#ff4bd8";
    ctx.fillText(ch, x - 1, y);
    ctx.globalAlpha = 1;
    ctx.fillStyle = params.fg;
    ctx.shadowColor = params.glow;
    ctx.shadowBlur = 8;
    ctx.fillText(ch, x, y);
  }

  ctx.shadowBlur = 0;

  // cursor
  const cx =
    16 +
    buf.cursorCol * cellW +
    Math.sin(wobbleT + buf.cursorRow * 0.35) * params.wobbleAmpPx;
  const cy = 16 + buf.cursorRow * cellH;
  const blink = Math.floor(tMs / 500) % 2 === 0;
  if (blink) {
    ctx.globalAlpha = 0.9;
    ctx.fillStyle = "rgba(200,255,220,0.35)";
    ctx.fillRect(cx, cy, Math.max(8, cellW), cellH);
    ctx.globalAlpha = 1;
  }

  // scanlines
  ctx.globalAlpha = params.scanlineAlpha;
  ctx.fillStyle = "rgba(0,0,0,1)";
  for (let y = 0; y < h; y += 4) {
    ctx.fillRect(0, y, w, 1);
  }
  ctx.globalAlpha = 1;
}
