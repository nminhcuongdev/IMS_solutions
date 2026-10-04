import { Suspense, useEffect, useMemo, useRef, useState } from "react";
import { Canvas, invalidate, useFrame, useThree } from "@react-three/fiber";
import { AdditiveBlending, Vector3, type Mesh, type MeshBasicMaterial } from "three";
import { Grid, Html, OrbitControls } from "@react-three/drei";
import type { OrbitControls as OrbitControlsImpl } from "three-stdlib";
import { listLocations, type Location } from "@/api/locations";
import { useT } from "@/i18n/LanguageContext";
import { layoutLocations, orphanCount, RACK_POST, type PlacedBox } from "@/lib/locationLayout";
import { qs, useFetch } from "@/lib/useFetch";


const ZONE_COLORS = ["#007AC9", "#28A745", "#FFC107", "#17A2B8", "#6F42C1", "#FD7E14"];
const DISABLED_COLOR = "#9AA5B1";
const SELECTED_COLOR = "#DC3545";

function zoneColorMap(rows: Location[]): Map<string, string> {
  const zones = [...new Set(rows.map(r => r.zoneCode).filter((z): z is string => !!z))].sort();
  return new Map(zones.map((z, i) => [z, ZONE_COLORS[i % ZONE_COLORS.length]]));
}

async function fetchAllLocations(): Promise<Location[]> {
  const PAGE = 200;
  const out: Location[] = [];
  for (let page = 1; page <= 20; page++) {
    const res = await listLocations({ page, pageSize: PAGE, sort: "location_code" });
    out.push(...res.data);
    if (out.length >= res.meta.total || res.data.length === 0) break;
  }
  return out;
}

export function Location3DPage() {
  const t = useT();
  const [selected, setSelected] = useState<string | null>(null);
  const [hovered, setHovered] = useState<string | null>(null);
  const [showDisabled, setShowDisabled] = useState(true);
  const [query, setQuery] = useState("");
  const [focusCode, setFocusCode] = useState<string | null>(null);
  const controls = useRef<OrbitControlsImpl>(null);

  const { data, loading, error } = useFetch(
    qs("locations3d", { all: 1 }), fetchAllLocations);

  const rows = useMemo(() => data ?? [], [data]);
  const visible = useMemo(
    () => showDisabled ? rows : rows.filter(r => r.isActive),
    [rows, showDisabled]);

  const layout = useMemo(() => layoutLocations(visible), [visible]);
  const colors = useMemo(() => zoneColorMap(rows), [rows]);
  const orphans = useMemo(() => orphanCount(visible, layout), [visible, layout]);

  const matches = useMemo(() => {
    const q = query.trim().toUpperCase();
    if (q.length < 1) return [];
    return rows
      .filter(r => r.locationCode.toUpperCase().includes(q)
                || r.locationName.toUpperCase().includes(q))
      .slice(0, 8);
  }, [rows, query]);

  const focusBox = useMemo(() => {
    if (!focusCode) return null;
    return [...layout.smalls, ...layout.middles, ...layout.larges]
      .find(b => b.loc.locationCode === focusCode) ?? null;
  }, [layout, focusCode]);

  const goTo = (code: string) => {
    setSelected(code);
    setFocusCode(null);
    requestAnimationFrame(() => setFocusCode(code));
  };

  const selectedLoc = useMemo(
    () => rows.find(r => r.locationCode === selected) ?? null,
    [rows, selected]);

  const span = Math.max(layout.bounds.width, layout.bounds.depth, 6);
  const camPos: [number, number, number] = [
    span * 0.62, Math.max(layout.bounds.height * 1.35, span * 0.42), span * 0.85];

  const colorOf = (box: PlacedBox) => {
    if (box.loc.locationCode === selected) return SELECTED_COLOR;
    if (!box.loc.isActive) return DISABLED_COLOR;
    return colors.get(box.loc.zoneCode ?? "") ?? ZONE_COLORS[0];
  };

  return (
    <div className="pg on" id="pg-loc_3d">
      <div className="search-box">
        <div className="search-row">
          <label>
            <input type="checkbox" checked={showDisabled}
              onChange={e => setShowDisabled(e.target.checked)} />
            {" "}{t("showDisabledLocations", "Hiện cả vị trí đã Disable")}
          </label>

          <span className="relative ml-4.5">
            <input className="w-52.5" type="text"
              value={query}
              placeholder={t("searchCodeName", "Code or Name...")}
              onChange={e => setQuery(e.target.value)}
              onKeyDown={e => {
                if (e.key === "Enter" && matches.length > 0) { goTo(matches[0].locationCode); setQuery(""); }
                if (e.key === "Escape") setQuery("");
              }} />
            {matches.length > 0 && (
              <div className="absolute top-full left-0 z-[5] max-h-60 min-w-[210px] overflow-y-auto rounded-lg border border-slate-300 bg-white shadow-lg">
                {matches.map(m => (
                  <div className="cursor-pointer px-[9px] py-[5px] text-[12.5px] hover:bg-brand-50" key={m.locationCode}
                    onMouseDown={() => { goTo(m.locationCode); setQuery(""); }}>
                    <b>{m.locationCode}</b>
                    <span className="text-slate-500"> · {m.locationName}</span>
                  </div>
                ))}
              </div>
            )}
          </span>

          <span className="ml-4.5 text-[12px] text-slate-600">
            {layout.larges.length} {t("thLocLarge", "Large")} (X) ·{" "}
            {layout.middles.length} {t("thLocMedium", "Middle")} (Y) ·{" "}
            {layout.smalls.length} {t("thLocSmall", "Small")} (Z)
          </span>

          <div className="ml-auto flex items-center gap-1.5">
            {[...colors].map(([zone, c]) => (
              <span className="text-[11px] text-slate-600 flex items-center gap-1" key={zone}>
                <i className="inline-block size-2.5 rounded-xs" style={{ background: c }} />
                {zone}
              </span>
            ))}
            <button className="btn btn-outline ml-2"
              onClick={() => { setFocusCode(null); controls.current?.reset(); invalidate(); }}>
              {t("btnResetView", "Reset view")}
            </button>
          </div>
        </div>

        {error && <div className="search-row text-rose-600 text-[12px]">{error}</div>}
        {orphans > 0 && (
          <div className="search-row text-amber-700 text-[12px]">
            {orphans} vị trí không gắn được vào sơ đồ (thiếu Large hoặc Middle cha) — xem lưới
            Location Master.
          </div>
        )}
      </div>

      <div className="flex gap-3 items-stretch">
        <div className="flex-1 min-h-140 bg-[#0f172a] rounded-lg overflow-hidden relative">
          {loading && (
            <div className="absolute inset-0 grid place-items-center text-[#cbd5e0] text-[13px] z-[1]">
              {t("loading", "Loading…")}
            </div>
          )}

          <Canvas shadows frameloop="demand" camera={{ position: camPos, fov: 45 }}
            onPointerMissed={() => setSelected(null)}>
            <color attach="background" args={["#0F172A"]} />
            <ambientLight intensity={0.85} />
            <directionalLight position={[span, span * 1.4, span * 0.6]} intensity={1.1} castShadow />
            <Grid args={[span * 3, span * 3]} cellSize={1} sectionSize={5}
              cellColor="#1E293B" sectionColor="#334155"
              fadeDistance={span * 4} infiniteGrid position={[0, -0.01, 0]} />

            <Suspense fallback={null}>
              {layout.larges.map(b => (
                <Rack key={b.loc.locationCode} box={b}
                  label={b.loc.largeCode}
                  selected={selected === b.loc.locationCode}
                  onSelect={() => setSelected(b.loc.locationCode)} />
              ))}
              {layout.middles.map(b => (
                <Slab key={b.loc.locationCode} box={b}
                  color={selected === b.loc.locationCode ? SELECTED_COLOR : "#475569"}
                  onSelect={() => setSelected(b.loc.locationCode)} />
              ))}
              {layout.smalls.map(b => (
                <Shelf key={b.loc.locationCode} box={b} color={colorOf(b)}
                  active={b.loc.isActive}
                  dimmed={focusCode !== null && b.loc.locationCode !== focusCode}
                  hovered={hovered === b.loc.locationCode}
                  selected={selected === b.loc.locationCode}
                  onHover={h => setHovered(h ? b.loc.locationCode : null)}
                  onSelect={() => setSelected(b.loc.locationCode)} />
              ))}
            </Suspense>

            <CameraRig target={focusBox} controls={controls} />
            <FocusBeacon box={focusBox} topY={layout.bounds.height + 1.6} />

            <OrbitControls ref={controls} makeDefault enableDamping
              maxPolarAngle={Math.PI / 2.05} />
          </Canvas>
        </div>

        <div className="w-80 shrink-0">
          <DetailPanel loc={selectedLoc} t={t} onGoTo={goTo} zoneColor={
            selectedLoc?.zoneCode ? colors.get(selectedLoc.zoneCode) : undefined} />
        </div>
      </div>
    </div>
  );
}

function FocusBeacon({ box, topY }: { box: PlacedBox | null; topY: number }) {
  const halo = useRef<Mesh>(null);
  const haloMat = useRef<MeshBasicMaterial>(null);
  const beamMat = useRef<MeshBasicMaterial>(null);
  const pinMat = useRef<MeshBasicMaterial>(null);

  useFrame(({ clock }) => {
    if (!box) return;
    const w = (Math.sin(clock.getElapsedTime() * 4.5) + 1) / 2;

    if (halo.current) {
      const k = 1.05 + w * 0.35;
      halo.current.scale.set(k, k, k);
    }
    if (haloMat.current) haloMat.current.opacity = 0.55 - w * 0.3;
    if (beamMat.current) beamMat.current.opacity = 0.25 + w * 0.35;
    if (pinMat.current) pinMat.current.opacity = 0.6 + w * 0.4;

    invalidate();
  });

  if (!box) return null;

  const beamH = Math.max(0.5, topY - box.y);

  return (
    <group position={[box.x, box.y, box.z]}>
      <mesh ref={halo} renderOrder={998}>
        <boxGeometry args={[box.w * 1.18, box.h * 1.25, box.d * 1.18]} />
        <meshBasicMaterial ref={haloMat} color={SELECTED_COLOR} wireframe
          transparent opacity={0.5} depthTest={false} />
      </mesh>

      <mesh position={[0, beamH / 2, 0]} renderOrder={999}>
        <cylinderGeometry args={[0.07, 0.07, beamH, 8]} />
        <meshBasicMaterial ref={beamMat} color={SELECTED_COLOR}
          transparent opacity={0.4} depthTest={false} blending={AdditiveBlending} />
      </mesh>

      <mesh position={[0, beamH + 0.22, 0]} rotation={[Math.PI, 0, 0]} renderOrder={1000}>
        <coneGeometry args={[0.22, 0.44, 12]} />
        <meshBasicMaterial ref={pinMat} color={SELECTED_COLOR}
          transparent opacity={0.9} depthTest={false} />
      </mesh>

      <Html className="pointer-events-none whitespace-nowrap" position={[0, beamH + 0.62, 0]} center zIndexRange={[100, 0]}>
        <span className="rounded-[10px] px-2 py-0.5 text-xs font-bold text-white" style={{ background: SELECTED_COLOR }}>
          {box.loc.locationCode}
        </span>
      </Html>
    </group>
  );
}

function CameraRig({ target, controls }: {
  target: PlacedBox | null;
  controls: React.RefObject<OrbitControlsImpl | null>;
}) {
  const { camera } = useThree();
  const goalTarget = useRef<Vector3 | null>(null);
  const goalPos = useRef(new Vector3());

  useEffect(() => {
    const c = controls.current;
    if (!target || !c) return;

    const to = new Vector3(target.x, target.y, target.z);
    const dist = Math.max(3.5, Math.max(target.w, target.h, target.d) * 3.2);
    const dir = camera.position.clone().sub(c.target);
    if (dir.lengthSq() < 1e-6) dir.set(1, 1, 1);
    dir.normalize();
    if (dir.y < 0.35) { dir.y = 0.35; dir.normalize(); }

    goalTarget.current = to;
    goalPos.current.copy(to).addScaledVector(dir, dist);
    invalidate();
  }, [target, camera, controls]);

  useFrame((_, dt) => {
    const to = goalTarget.current;
    const c = controls.current;
    if (!to || !c) return;

    const k = 1 - Math.pow(0.0015, Math.min(dt, 0.1));
    c.target.lerp(to, k);
    camera.position.lerp(goalPos.current, k);
    c.update();

    if (camera.position.distanceTo(goalPos.current) < 0.02) {
      camera.position.copy(goalPos.current);
      c.target.copy(to);
      c.update();
      goalTarget.current = null;
      return;
    }
    invalidate();
  });

  return null;
}

function Slab({ box, color, onSelect }: {
  box: PlacedBox; color: string; onSelect: () => void;
}) {
  return (
    <mesh position={[box.x, box.y, box.z]} onClick={e => { e.stopPropagation(); onSelect(); }}
      receiveShadow>
      <boxGeometry args={[box.w, box.h, box.d]} />
      <meshStandardMaterial color={color} />
    </mesh>
  );
}

function Rack({ box, label, selected, onSelect }: {
  box: PlacedBox; label: string; selected: boolean; onSelect: () => void;
}) {
  const h = box.rackHeight ?? box.h;
  const color = selected ? SELECTED_COLOR : "#1E293B";
  const px = box.w / 2 - RACK_POST / 2;
  const pz = box.d / 2 - RACK_POST / 2;
  const posts: [number, number][] = [[-px, -pz], [px, -pz], [-px, pz], [px, pz]];

  return (
    <group position={[box.x, 0, box.z]} onClick={e => { e.stopPropagation(); onSelect(); }}>
      <mesh position={[0, box.h / 2, 0]} receiveShadow>
        <boxGeometry args={[box.w, box.h, box.d]} />
        <meshStandardMaterial color={color} />
      </mesh>

      {posts.map(([ox, oz], i) => (
        <mesh key={i} position={[ox, h / 2, oz]} castShadow>
          <boxGeometry args={[RACK_POST, h, RACK_POST]} />
          <meshStandardMaterial color={selected ? SELECTED_COLOR : "#334155"} />
        </mesh>
      ))}

      <Html className="pointer-events-none whitespace-nowrap" position={[0, h + 0.28, 0]} center distanceFactor={20}>
        <span className="text-[#e2e8f0] text-[13px] font-bold text-shadow-[0_1px_3px_rgba(0,0,0,.85)]">{label}</span>
      </Html>
    </group>
  );
}

function Shelf({ box, color, active, dimmed, hovered, selected, onHover, onSelect }: {
  box: PlacedBox; color: string; active: boolean; dimmed: boolean;
  hovered: boolean; selected: boolean;
  onHover: (h: boolean) => void; onSelect: () => void;
}) {
  const lift = selected ? 0.18 : hovered ? 0.08 : 0;
  const faded = dimmed && !selected;
  return (
    <mesh
      position={[box.x, box.y + lift, box.z]}
      castShadow
      onClick={e => { e.stopPropagation(); onSelect(); }}
      onPointerOver={e => { e.stopPropagation(); onHover(true); document.body.style.cursor = "pointer"; }}
      onPointerOut={() => { onHover(false); document.body.style.cursor = "auto"; }}>
      <boxGeometry args={[box.w, box.h, box.d]} />
      <meshStandardMaterial
        color={color}
        transparent={!active || faded}
        opacity={faded ? 0.12 : active ? 1 : 0.45}
        emissive={selected || hovered ? color : "#000000"}
        emissiveIntensity={selected ? 0.55 : hovered ? 0.3 : 0} />
      {(hovered || selected) && (
        <Html className="pointer-events-none whitespace-nowrap" position={[0, box.h / 2 + 0.25, 0]} center distanceFactor={14}>
          <span className="bg-[rgba(15,23,42,.92)] text-white py-[3px] px-[7px] rounded text-[12px] font-semibold">{box.loc.locationCode}</span>
        </Html>
      )}
    </mesh>
  );
}

function DetailPanel({ loc, t, zoneColor, onGoTo }: {
  loc: Location | null; t: (k: string, f: string) => string; zoneColor?: string;
  onGoTo: (code: string) => void;
}) {
  if (!loc) {
    return (
      <div className="tbl-wrap p-4 text-slate-500 text-[13px]">
        {t("pick3dLocation", "Bấm vào một ô kệ để xem chi tiết.")}
      </div>
    );
  }
  const remarks = [loc.remark1, loc.remark2, loc.remark3, loc.remark4, loc.remark5]
    .filter((r): r is string => !!r && r.trim() !== "");

  return (
    <div className="tbl-wrap p-3.5">
      <div className="text-[15px] font-bold mb-0.5">{loc.locationCode}</div>
      <div className="text-[13px] text-slate-600 mb-2.5">{loc.locationName}</div>

      <button className="btn btn-outline w-full mb-3"
        onClick={() => onGoTo(loc.locationCode)}>
        {t("btnGoTo", "Đi tới")}
      </button>

      <Row label={t("thLocType", "Type")} value={loc.locationType} />
      <Row label={`${t("thLocLarge", "Large")} · X`} value={loc.largeCode} />
      <Row label={`${t("thLocMedium", "Middle")} · Y`} value={loc.middleCode} />
      <Row label={`${t("thLocSmall", "Small")} · Z`} value={loc.smallCode} />
      <Row label={t("thZone", "Zone")} value={loc.zoneCode} dot={zoneColor} />
      <Row label={t("thParent", "Parent")} value={loc.parentLocationCode} />

      <div className="flex justify-between py-[5px] px-0">
        <span className="text-[12px] text-slate-500">{t("thStatus", "Status")}</span>
        <span className={loc.isActive ? "badge badge-ok" : "badge badge-ng"}>
          {loc.isActive ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}
        </span>
      </div>

      {remarks.length > 0 && (
        <div className="mt-2.5 border-t border-t-[#e2e8f0] pt-2">
          {remarks.map((r, i) => (
            <div className="text-[12px] text-slate-600 py-0.5 px-0" key={i}>{r}</div>
          ))}
        </div>
      )}
    </div>
  );
}

function Row({ label, value, dot }: { label: string; value?: string | null; dot?: string }) {
  return (
    <div className="flex justify-between py-[5px] px-0 gap-3">
      <span className="text-[12px] text-slate-500">{label}</span>
      <span className="text-[12.5px] font-semibold text-right">
        {dot && (
          <i className="mr-[5px] inline-block size-[9px] rounded-xs" style={{ background: dot }} />
        )}
        {value && value.trim() !== "" ? value : "—"}
      </span>
    </div>
  );
}
