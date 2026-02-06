import type { OutputLine } from "./types";

export type Cell = {
  ch: string;
  bornAt: number;
  touchedAt: number;
  glitch: number; // 0..1
};

export type TextBuffer = {
  cols: number;
  rows: number;
  cells: Cell[]; // row-major, length = cols*rows
  cursorRow: number;
  cursorCol: number;
  scrollback: string[]; // plain for future copy/export
};

export function createBuffer(cols: number, rows: number): TextBuffer {
  const now = performance.now();
  const cells: Cell[] = Array.from({ length: cols * rows }, () => ({
    ch: " ",
    bornAt: now,
    touchedAt: now,
    glitch: 0,
  }));

  return {
    cols,
    rows,
    cells,
    cursorRow: 0,
    cursorCol: 0,
    scrollback: [],
  };
}

function idx(buf: TextBuffer, row: number, col: number): number {
  return row * buf.cols + col;
}

export function clearBuffer(buf: TextBuffer) {
  const now = performance.now();
  for (let i = 0; i < buf.cells.length; i++) {
    buf.cells[i].ch = " ";
    buf.cells[i].bornAt = now;
    buf.cells[i].touchedAt = now;
    buf.cells[i].glitch = 0;
  }
  buf.cursorRow = 0;
  buf.cursorCol = 0;
  buf.scrollback = [];
}

export function writeText(buf: TextBuffer, text: string, glitchSeed = 0) {
  const now = performance.now();

  for (let i = 0; i < text.length; i++) {
    const ch = text[i];

    if (ch === "\n") {
      newLine(buf);
      continue;
    }

    if (buf.cursorCol >= buf.cols) {
      newLine(buf);
    }

    if (buf.cursorRow >= buf.rows) {
      scrollUp(buf, 1);
      buf.cursorRow = buf.rows - 1;
    }

    const cell = buf.cells[idx(buf, buf.cursorRow, buf.cursorCol)];
    cell.ch = ch;
    cell.bornAt = now;
    cell.touchedAt = now;

    // deterministic-ish but cheap
    const g = (Math.sin(glitchSeed + (buf.cursorRow * 131 + buf.cursorCol * 17)) + 1) * 0.5;
    cell.glitch = g;

    buf.cursorCol++;
  }
}

export function newLine(buf: TextBuffer) {
  buf.scrollback.push(readLine(buf, buf.cursorRow));
  buf.cursorRow++;
  buf.cursorCol = 0;

  if (buf.cursorRow >= buf.rows) {
    scrollUp(buf, 1);
    buf.cursorRow = buf.rows - 1;
  }
}

function readLine(buf: TextBuffer, row: number): string {
  const start = idx(buf, row, 0);
  const end = start + buf.cols;
  return buf.cells
    .slice(start, end)
    .map((c) => c.ch)
    .join("")
    .replace(/\s+$/g, "");
}

function scrollUp(buf: TextBuffer, lines: number) {
  const now = performance.now();
  const cols = buf.cols;
  const rows = buf.rows;

  for (let r = 0; r < rows - lines; r++) {
    for (let c = 0; c < cols; c++) {
      buf.cells[idx(buf, r, c)] = { ...buf.cells[idx(buf, r + lines, c)] };
    }
  }

  for (let r = rows - lines; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      buf.cells[idx(buf, r, c)] = { ch: " ", bornAt: now, touchedAt: now, glitch: 0 };
    }
  }
}

export function applyOutputLines(buf: TextBuffer, lines: OutputLine[]) {
  const seed = performance.now() * 0.001;
  for (const line of lines) {
    const t = line.text ?? "";
    writeText(buf, t, seed);
    if (line.newLine) newLine(buf);
  }
}
