"use client";

import { useEffect, useMemo, useRef, useState } from "react";

type OutputLine = {
  text: string | null;
  newLine: boolean;
  type: number;
};

type ServerMessage =
  | { type: "session"; data: { sessionId: string } }
  | { type: "output"; data: { lines: OutputLine[] } }
  | { type: "error"; data: { message: string } };

export default function Home() {
  const [connected, setConnected] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [lines, setLines] = useState<OutputLine[]>([]);
  const [input, setInput] = useState("");
  const wsRef = useRef<WebSocket | null>(null);
  const bottomRef = useRef<HTMLDivElement | null>(null);

  const wsUrl = useMemo(() => {
    // In dev you’ll typically run:
    // - API: http://localhost:5077
    // - Web: http://localhost:3000
    // So connect directly to the API.
    const proto = window.location.protocol === "https:" ? "wss" : "ws";
    const host = process.env.NEXT_PUBLIC_API_HOST ?? "localhost:5077";
    return `${proto}://${host}/ws`;
  }, []);

  useEffect(() => {
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
        setLines((prev) => [...prev, ...msg.data.lines]);
        return;
      }
    };

    return () => {
      ws.close();
    };
  }, [wsUrl]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [lines]);

  function sendInput(text: string) {
    const ws = wsRef.current;
    if (!ws || ws.readyState !== WebSocket.OPEN) return;
    ws.send(JSON.stringify({ type: "input", text }));
  }

  function onKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter") {
      e.preventDefault();
      const toSend = input;
      setInput("");
      // Echo user input locally so it feels terminal-ish.
      setLines((prev) => [...prev, { text: toSend, newLine: true, type: 0 }]);
      sendInput(toSend);
    }
  }

  return (
    <main className="min-h-screen bg-neutral-950 text-neutral-100">
      <div className="mx-auto max-w-4xl p-6">
        <div className="mb-4 flex items-center justify-between gap-4">
          <div>
            <div className="text-sm text-neutral-400">env0.game.web</div>
            <div className="font-mono text-xs text-neutral-500">
              ws: {connected ? "connected" : "disconnected"}
              {sessionId ? ` | session: ${sessionId}` : ""}
            </div>
          </div>
        </div>

        <div className="rounded border border-neutral-800 bg-neutral-950 p-4 font-mono text-sm leading-6">
          {lines.map((l, idx) => (
            <span key={idx}>
              {l.text ?? ""}
              {l.newLine ? "\n" : ""}
            </span>
          ))}
          <div ref={bottomRef} />

          <div className="mt-2 flex items-center gap-2">
            <span className="text-neutral-500">$</span>
            <input
              className="w-full bg-transparent outline-none text-neutral-100"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={onKeyDown}
              autoFocus
              spellCheck={false}
              autoComplete="off"
            />
          </div>
        </div>

        <div className="mt-3 text-xs text-neutral-500">
          Notes: this is a deliberately minimal renderer (not xterm.js). We’ll evolve
          it into a true inline terminal model (single flow buffer + caret control)
          once the API loop is stable.
        </div>
      </div>
    </main>
  );
}
