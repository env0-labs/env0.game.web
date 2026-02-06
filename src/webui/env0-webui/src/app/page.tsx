"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import type { ServerMessage } from "@/lib/types";
import { applyOutputLines, createBuffer } from "@/lib/textBuffer";
import { renderCrt, type CrtParams } from "@/lib/crtRenderer";

export default function Home() {
  const [connected, setConnected] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);

  const wsRef = useRef<WebSocket | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const hiddenInputRef = useRef<HTMLInputElement | null>(null);

  const bufRef = useRef(createBuffer(110, 34));
  const ghostRef = useRef<ImageData | null>(null);
  const inputLineRef = useRef("");

  const wsUrl = useMemo(() => {
    // Avoid touching window during SSR/static build.
    if (typeof window === "undefined") return "";

    const proto = window.location.protocol === "https:" ? "wss" : "ws";
    const host = process.env.NEXT_PUBLIC_API_HOST ?? "localhost:5077";
    return `${proto}://${host}/ws`;
  }, []);

  const params: CrtParams = useMemo(() => ({
    fontPx: 16,
    lineHeight: 1.25,
    fg: "#b7ffd1",
    glow: "rgba(80,255,160,0.45)",
    bg: "#040807",
    scanlineAlpha: 0.09,
    noiseAlpha: 0.22,
    ghostAlpha: 0.12,
    glitchChance: 0.35,
    wobbleAmpPx: 1.4,
    wobbleSpeed: 1.2,
  }), []);

  useEffect(() => {
    if (!wsUrl) return;

    const ws = new WebSocket(wsUrl);
    wsRef.current = ws;

    ws.onopen = () => setConnected(true);
    ws.onclose = () => setConnected(false);

    ws.onmessage = (ev) => {
      let msg: ServerMessage;
      try {
        msg = JSON.parse(ev.data);
      } catch {
        return;
      }

      if (msg.type === "session") {
        setSessionId(msg.data.sessionId);
        return;
      }

      if (msg.type === "output") {
        applyOutputLines(bufRef.current, msg.data.lines);
        return;
      }
    };

    return () => {
      ws.close();
    };
  }, [wsUrl]);

  // Render loop
  useEffect(() => {
    let raf = 0;

    function resize() {
      const canvas = canvasRef.current;
      if (!canvas) return;

      const dpr = window.devicePixelRatio || 1;
      const rect = canvas.getBoundingClientRect();
      canvas.width = Math.floor(rect.width * dpr);
      canvas.height = Math.floor(rect.height * dpr);
    }

    function frame(t: number) {
      const canvas = canvasRef.current;
      if (canvas) {
        const ctx = canvas.getContext("2d");
        if (ctx) {
          ctx.setTransform(1, 0, 0, 1, 0, 0);
          const dpr = window.devicePixelRatio || 1;
          ctx.scale(dpr, dpr);

          // draw at CSS pixel resolution by using a temp canvas state
          const off = document.createElement("canvas");
          off.width = canvas.width;
          off.height = canvas.height;
          const offCtx = off.getContext("2d");
          if (offCtx) {
            const img = renderCrt(offCtx, bufRef.current, t, params, ghostRef.current);
            ghostRef.current = img;
            ctx.drawImage(off, 0, 0, canvas.width / dpr, canvas.height / dpr);
          }
        }
      }

      raf = requestAnimationFrame(frame);
    }

    resize();
    window.addEventListener("resize", resize);
    raf = requestAnimationFrame(frame);

    return () => {
      window.removeEventListener("resize", resize);
      cancelAnimationFrame(raf);
    };
  }, [params]);

  function sendInput(text: string) {
    const ws = wsRef.current;
    if (!ws || ws.readyState !== WebSocket.OPEN) return;
    ws.send(JSON.stringify({ type: "input", text }));
  }

  function onKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    // We keep the DOM input empty and manage our own input line.
    if (e.key === "Enter") {
      e.preventDefault();
      const toSend = inputLineRef.current;
      inputLineRef.current = "";
      applyOutputLines(bufRef.current, [{ text: `> ${toSend}`, newLine: true, type: 0 }]);
      sendInput(toSend);
      return;
    }

    if (e.key === "Backspace") {
      e.preventDefault();
      inputLineRef.current = inputLineRef.current.slice(0, -1);
      return;
    }

    if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
      e.preventDefault();
      inputLineRef.current += e.key;
      return;
    }
  }

  // Periodically re-render the current input line into the buffer bottom.
  useEffect(() => {
    const id = window.setInterval(() => {
      const buf = bufRef.current;
      // redraw last line: simple hack for now, later we’ll do proper prompt + caret in buffer
      const savedRow = buf.cursorRow;
      const savedCol = buf.cursorCol;

      // move cursor to last row and clear it
      buf.cursorRow = buf.rows - 1;
      buf.cursorCol = 0;
      // overwrite the line
      const line = (`$ ${inputLineRef.current}`).padEnd(buf.cols, " ");
      for (let i = 0; i < buf.cols; i++) {
        const cell = buf.cells[(buf.rows - 1) * buf.cols + i];
        if (!cell) continue;
        cell.ch = line[i] ?? " ";
        cell.touchedAt = performance.now();
      }

      // restore cursor position to visual caret position
      buf.cursorRow = buf.rows - 1;
      buf.cursorCol = Math.min(buf.cols - 1, 2 + inputLineRef.current.length);

      // keep scroll behavior stable by not restoring (cursor is the caret)
      void savedRow;
      void savedCol;
    }, 33);

    return () => window.clearInterval(id);
  }, []);

  return (
    <main
      className="min-h-screen bg-neutral-950 text-neutral-100"
      onClick={() => hiddenInputRef.current?.focus()}
    >
      <div className="mx-auto max-w-5xl p-6">
        <div className="mb-3 flex items-end justify-between gap-4">
          <div>
            <div className="text-sm text-neutral-400">env0.game.web</div>
            <div className="font-mono text-xs text-neutral-500">
              ws: {connected ? "connected" : "disconnected"}
              {sessionId ? ` | session: ${sessionId}` : ""}
            </div>
          </div>

          <div className="text-xs text-neutral-600">Click the screen, type, Enter.</div>
        </div>

        <div className="relative overflow-hidden rounded border border-neutral-800 bg-black">
          {/* CSS overlays for vibe */}
          <div className="pointer-events-none absolute inset-0 opacity-30 mix-blend-screen"
               style={{
                 background:
                   "radial-gradient(ellipse at center, rgba(80,255,160,0.12) 0%, rgba(0,0,0,0) 55%), radial-gradient(ellipse at center, rgba(0,0,0,0) 0%, rgba(0,0,0,0.85) 75%)",
               }}
          />

          <canvas ref={canvasRef} className="block h-[70vh] w-full" />

          <input
            ref={hiddenInputRef}
            className="absolute left-0 top-0 h-1 w-1 opacity-0"
            onKeyDown={onKeyDown}
            autoFocus
            spellCheck={false}
            autoComplete="off"
          />
        </div>

        <div className="mt-3 text-xs text-neutral-500">
          Renderer: custom canvas CRT (scanlines, glow, ghosting, per-char glitch, line wobble).
        </div>
      </div>
    </main>
  );
}
