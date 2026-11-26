# Neon Command Center
The redesign is isolated on feature/neon-command-center, based on the tested public V3 revision. The default appearance is dark neon violet, with a selectable light theme and existing language preferences. Native WebGL draws a procedural illustrative road network with 3D trucks, optional animation and rotation; it is not connected to the actual synthetic trip simulation. No CDN, keys, new npm package or model files. The scene has a static SVG fallback, a capped pixel ratio, reduced-motion support, off-screen/background suspension and explicit cleanup. Three.js was not added: the bounded geometry does not justify a new dependency.
 
## User-supplied asset slots
Provide distinct, text-free WebP images (ideally 960×540, 16:9, under 180 KB each). Keep the important subject centered for narrow cards, without UI labels or artificial graphs that can be confused with live results.
 
- `assets/neon/routes.webp` — glowing 3D logistics paths and nodes.
- `assets/neon/twin.webp` — isometric fleet vehicles on a digital terrain.
- `assets/neon/maintenance.webp` — realistic exploded mechanical assembly or diagnostic illustration.
- `assets/neon/assistant.webp` — abstract violet intelligence core.
 
When supplied, add them under src/FleetManagement.Web/wwwroot/assets/neon and define the four `--asset-*` variables in neon.css using relative URLs. Hide the placeholder glyph on a populated slot. Until then, the cards use CSS artwork with no missing-image requests. The 3D scene is generated in code and does not require an image. No third-party asset has been added or invented.
 
## Acceptance
Desktop (1366–1440), tablet (768), mobile (390 and 320). Check keyboard navigation, light/dark, RU/EN, reduced motion, WebGL fallback, scene disposal, full operational smoke, media and the release evidence gate before promoting the preview. The existing public release must remain untouched during redesign tests.
