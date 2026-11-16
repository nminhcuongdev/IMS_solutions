# IMS Frontend

React 18 + TypeScript single-page app built with Vite 5 and styled with Tailwind CSS 4. No state library and no UI
framework; the 3D racking view uses react-three-fiber.

## Run

Requirements: Node.js 22 and the API running (see [../Backend/README.md](../Backend/README.md)).

```bash
npm ci
npm run dev            # http://localhost:5173, talks to http://localhost:5271
npx tsc --noEmit       # type check
npx vite build         # production bundle in dist/
```

## API address

- `VITE_API_BASE` set to a full URL (`http://localhost:5271`) is used as is.
- `VITE_API_BASE` starting with `/` (for example `/api`) is resolved against the page's own origin, so one bundle
  works over HTTP and HTTPS behind a reverse proxy.
- When it is empty the app calls the host it was opened from on port `VITE_API_PORT` (default `8091`), so other
  machines on the LAN work without rebuilding.

## Structure

| Folder | Contents |
|---|---|
| `src/api` | Typed API clients, one file per feature; every response is unwrapped from the API envelope |
| `src/auth` | Session (kept in `sessionStorage`, shared between open tabs) and `PermissionContext` (`useCan`, screen guards) |
| `src/i18n` | Labels loaded from the API's `language_data`; English, Japanese and Vietnamese |
| `src/layout` | Sidebar (built from `GET /menu`), header, route guard |
| `src/lib` | Fetch cache (`useFetch`), sorting, paging, shared buttons, label-engine client |
| `src/pages` | One component per screen |

Screens follow one pattern: search box, toolbar buttons that hide themselves without permission, a sortable grid
with server-side paging, and modals for New / Edit / Copy. Pressing Search always refetches. Text visible to users
goes through `t(key, fallback)`.
