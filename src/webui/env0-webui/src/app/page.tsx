"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { ServerMessage } from "@/lib/types";
import { applyOutputLines, createBuffer } from "@/lib/textBuffer";
import { createRenderCache, renderCrt, type CrtParams } from "@/lib/crtRenderer";

export default function Home() {
  const [connected, setConnected] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);

  const wsRef = useRef<WebSocket | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const hiddenInputRef = useRef<HTMLInputElement | null>(null);

  const bufRef = useRef(createBuffer(90, 26));
  const inputLineRef = useRef("");

  const offscreenRef = useRef<HTMLCanvasElement | null>(null);
  const ghostCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const renderCacheRef = useRef(createRenderCache());

  const wsUrl = useMemo(() => {
    // Avoid touching window during SSR/static build.
    if (typeof window === "undefined") return "";

    const proto = window.location.protocol === "https:" ? "wss" : "ws";
    const host = process.env.NEXT_PUBLIC_API_HOST ?? "localhost:5077";
    return `${proto}://${host}/ws`;
  }, []);

  const params: CrtParams = useMemo(
    () => ({
      fontPx: 22,
      lineHeight: 1.25,
      fg: "#b7ffd1",
      glow: "rgba(80,255,160,0.45)",
      bg: "#040807",
      scanlineAlpha: 0.09,
      glitchChance: 0.35,
      wobbleAmpPx: 1.4,
      wobbleSpeed: 1.2,
    }),
    []
  );

  const renderInputLine = useCallback(() => {
    const buf = bufRef.current;
    const now = performance.now();

    const row = buf.rows - 1;
    const prefix = "$ ";
    const text = prefix + inputLineRef.current;
    const padded = text.padEnd(buf.cols, " ");

    for (let i = 0; i < buf.cols; i++) {
      const cell = buf.cells[row * buf.cols + i];
      if (!cell) continue;
      cell.ch = padded[i] ?? " ";
      cell.touchedAt = now;
    }

    buf.cursorRow = row;
    buf.cursorCol = Math.min(buf.cols - 1, prefix.length + inputLineRef.current.length);
  }, []);

  useEffect(() => {
    // initialize the input line once
    renderInputLine();
  }, [renderInputLine]);

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
        // keep input line stable after output
        renderInputLine();
        return;
      }
    };

    return () => {
      ws.close();
    };
  }, [wsUrl, renderInputLine]);

  // Render loop (keep it lightweight to avoid input latency)
  useEffect(() => {
    let raf = 0;

    function ensureCanvases(canvas: HTMLCanvasElement) {
      if (!offscreenRef.current) offscreenRef.current = document.createElement("canvas");
      if (!ghostCanvasRef.current) ghostCanvasRef.current = document.createElement("canvas");

      const off = offscreenRef.current;
      const ghost = ghostCanvasRef.current;
      if (!off || !ghost) return;

      if (off.width !== canvas.width || off.height !== canvas.height) {
        off.width = canvas.width;
        off.height = canvas.height;
      }
      if (ghost.width !== canvas.width || ghost.height !== canvas.height) {
        ghost.width = canvas.width;
        ghost.height = canvas.height;
      }
    }

    function resize() {
      const canvas = canvasRef.current;
      if (!canvas) return;

      const dpr = Math.min(1.25, window.devicePixelRatio || 1);
      const rect = canvas.getBoundingClientRect();
      canvas.width = Math.floor(rect.width * dpr);
      canvas.height = Math.floor(rect.height * dpr);
      ensureCanvases(canvas);
    }

    function frame(t: number) {
      const canvas = canvasRef.current;
      if (!canvas) {
        raf = requestAnimationFrame(frame);
        return;
      }

      ensureCanvases(canvas);
      const off = offscreenRef.current;
      const ghost = ghostCanvasRef.current;
      if (!off || !ghost) {
        raf = requestAnimationFrame(frame);
        return;
      }

      const ctx = canvas.getContext("2d");
      const offCtx = off.getContext("2d");
      const ghostCtx = ghost.getContext("2d");
      if (!ctx || !offCtx || !ghostCtx) {
        raf = requestAnimationFrame(frame);
        return;
      }

      const dpr = window.devicePixelRatio || 1;

      // Build the next frame on offscreen.
      // Ghosting: blend previous frame in first.
      offCtx.globalAlpha = 0.16;
      offCtx.drawImage(ghost, 0, 0);
      offCtx.globalAlpha = 1;

      renderCrt(offCtx, bufRef.current, t, params, renderCacheRef.current);

      // Copy offscreen to visible canvas (scaled back to CSS pixels)
      ctx.setTransform(1, 0, 0, 1, 0, 0);
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(off, 0, 0);

      // Update ghost buffer (store the full rendered frame)
      ghostCtx.setTransform(1, 0, 0, 1, 0, 0);
      ghostCtx.clearRect(0, 0, ghost.width, ghost.height);
      ghostCtx.drawImage(off, 0, 0);

      // scale back to CSS pixels (when canvas uses DPR)
      // note: we draw at device pixels already; CSS sizing handles display.
      void dpr;

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
    if (e.key === "Enter") {
      e.preventDefault();
      const toSend = inputLineRef.current;
      inputLineRef.current = "";
      applyOutputLines(bufRef.current, [{ text: `> ${toSend}`, newLine: true, type: 0 }]);
      sendInput(toSend);
      renderInputLine();
      return;
    }

    if (e.key === "Backspace") {
      e.preventDefault();
      inputLineRef.current = inputLineRef.current.slice(0, -1);
      renderInputLine();
      return;
    }

    if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
      e.preventDefault();
      inputLineRef.current += e.key;
      renderInputLine();
      return;
    }
  }

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
          <div
            className="pointer-events-none absolute inset-0 opacity-30 mix-blend-screen"
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
