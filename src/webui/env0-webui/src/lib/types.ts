export type OutputLine = {
  text: string | null;
  newLine: boolean;
  type: number;
};

export type ServerMessage =
  | { type: "session"; data: { sessionId: string } }
  | { type: "output"; data: { lines: OutputLine[] } }
  | { type: "error"; data: { message: string } };
