# Env0.GameApi

WebSocket API wrapper around the backend game logic.

## Run

```bash
cd src/api/Env0.GameApi

dotnet run --urls http://localhost:5077
```

## WebSocket

Connect to:

- `ws://localhost:5077/ws`

Messages:

Client → Server:

- `{ "type": "input", "text": "status" }`
- `{ "type": "reset" }`

Server → Client:

- `{ "type": "session", "data": { "sessionId": "..." } }`
- `{ "type": "output", "data": { "lines": [ { "text": "...", "newLine": true, "type": 0 } ] } }`
