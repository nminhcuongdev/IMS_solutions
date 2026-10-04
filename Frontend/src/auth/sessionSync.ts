
const CHANNEL = "ims.session";

const ADOPT_TIMEOUT_MS = 300;

export interface SessionPayload {
  token: string;
  user: string;
}

type Msg =
  | { type: "request" }
  | { type: "share"; payload: SessionPayload }
  | { type: "clear" };

function open(): BroadcastChannel | null {
  return typeof BroadcastChannel === "undefined" ? null : new BroadcastChannel(CHANNEL);
}

export function requestSession(): Promise<SessionPayload | null> {
  const ch = open();
  if (!ch) return Promise.resolve(null);
  return new Promise(resolve => {
    const done = (v: SessionPayload | null) => {
      clearTimeout(timer);
      ch.close();
      resolve(v);
    };
    const timer = setTimeout(() => done(null), ADOPT_TIMEOUT_MS);
    ch.onmessage = (e: MessageEvent<Msg>) => {
      if (e.data?.type === "share") done(e.data.payload);
    };
    ch.postMessage({ type: "request" } satisfies Msg);
  });
}

export function serveSession(
  read: () => SessionPayload | null,
  onRemoteClear: () => void,
): () => void {
  const ch = open();
  if (!ch) return () => {};
  ch.onmessage = (e: MessageEvent<Msg>) => {
    const m = e.data;
    if (m?.type === "request") {
      const p = read();
      if (p) ch.postMessage({ type: "share", payload: p } satisfies Msg);
    } else if (m?.type === "clear") {
      onRemoteClear();
    }
  };
  return () => ch.close();
}

export function broadcastClear(): void {
  const ch = open();
  if (!ch) return;
  ch.postMessage({ type: "clear" } satisfies Msg);
  ch.close();
}
