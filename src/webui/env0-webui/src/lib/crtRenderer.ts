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
  // xorshift-ish
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

export function renderCrt(
  ctx: CanvasRenderingContext2D,
  buf: TextBuffer,
  tMs: number,
  params: CrtParams
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

  // subtle glow via shadow
  ctx.fillStyle = params.fg;
  ctx.shadowColor = params.glow;
  ctx.shadowBlur = 10;

  const wobbleT = tMs * 0.001 * params.wobbleSpeed;

  // Draw glyphs
  for (let r = 0; r < buf.rows; r++) {
    const wobble = Math.sin(wobbleT + r * 0.35) * params.wobbleAmpPx;

    for (let c = 0; c < buf.cols; c++) {
      const cell = buf.cells[r * buf.cols + c];
      if (!cell) continue;
      const ch0 = cell.ch;
      if (ch0 === " ") continue;

      // per-char glitch: occasionally swap character for a frame
      let ch = ch0;
      const glitchBase = cell.glitch;
      const p = params.glitchChance * 0.15 + glitchBase * 0.02;
      const gSeed = (r * 1000003 + c * 9176 + ((tMs / 33) | 0) * 13) | 0;
      if (rand01(gSeed) < p) {
        ch = pickGlitchChar(gSeed ^ 0x9e3779b9);
      }

      const x = 16 + c * cellW + wobble;
      const y = 16 + r * cellH;

      // faint chromatic aberration hack
      ctx.globalAlpha = 0.14;
      ctx.fillStyle = "#46f2ff";
      ctx.shadowBlur = 0;
      ctx.fillText(ch, x + 1, y);
      ctx.fillStyle = "#ff4bd8";
      ctx.fillText(ch, x - 1, y);

      ctx.globalAlpha = 1;
      ctx.fillStyle = params.fg;
      ctx.shadowColor = params.glow;
      ctx.shadowBlur = 10;
      ctx.fillText(ch, x, y);
    }
  }

  ctx.shadowBlur = 0;

  // cursor (block-ish)
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
