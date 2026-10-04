import type { Location } from "@/api/locations";


const PITCH_X = 2.1;
const PITCH_Y = 0.9;
const PITCH_Z = 1.0;

const BOARD_H = 0.06;
const BASE_H = 0.12;
const POST = 0.08;
const BIN_W = 1.28;
const BIN_D = 0.84;
const BIN_H = 0.62;
const RACK_W = 1.5;
const RACK_TOP = 0.2;

export interface PlacedBox {
  loc: Location;
  x: number; y: number; z: number;
  w: number; h: number; d: number;
  rackHeight?: number;
}

export interface LocationLayout {
  larges: PlacedBox[];
  middles: PlacedBox[];
  smalls: PlacedBox[];
  bounds: { width: number; depth: number; height: number };
}

function axisIndex(segment: string | null | undefined): number | null {
  if (!segment) return null;
  const m = /(\d+)\s*$/.exec(segment);
  if (!m) return null;
  const n = Number(m[1]);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function axisFamily(segment: string | null | undefined): string {
  if (!segment) return "";
  return segment.replace(/\d+\s*$/, "").trim().toUpperCase();
}

function assignAxis(segments: (string | null | undefined)[]): number[] {
  const wanted = segments.map(axisIndex);
  const families = segments.map(axisFamily);
  const order = [...new Set(families)].sort();

  const result = new Array<number>(segments.length);
  let offset = 0;

  for (const fam of order) {
    const members = segments.map((_, i) => i).filter(i => families[i] === fam);
    const taken = new Set<number>();
    for (const i of members) if (wanted[i] !== null) taken.add(wanted[i]!);

    let next = 1;
    const freeSlot = () => {
      while (taken.has(next)) next++;
      taken.add(next);
      return next;
    };

    const used = new Set<number>();
    let localMax = 0;
    for (const i of members) {
      const w = wanted[i];
      const local = w !== null && !used.has(w) ? w : freeSlot();
      used.add(local);
      localMax = Math.max(localMax, local);
      result[i] = offset + local;
    }
    offset += localMax;
  }

  return result;
}

const byCode = (a: Location, b: Location) => a.locationCode.localeCompare(b.locationCode);

export function layoutLocations(all: Location[]): LocationLayout {
  const larges = all.filter(l => l.locationType === "Large").sort(byCode);
  const middles = all.filter(l => l.locationType === "Middle").sort(byCode);
  const smalls = all.filter(l => l.locationType === "Small").sort(byCode);

  const out: LocationLayout = {
    larges: [], middles: [], smalls: [],
    bounds: { width: 0, depth: 0, height: 0 },
  };
  if (larges.length === 0) return out;

  const xs = assignAxis(larges.map(l => l.largeCode));

  type Tier = { loc: Location; y: number; bays: { loc: Location; z: number }[] };
  const columns = larges.map((large, li) => {
    const tiers = middles.filter(m => m.largeCode === large.largeCode);
    const ys = assignAxis(tiers.map(m => m.middleCode));
    return {
      loc: large,
      x: xs[li],
      tiers: tiers.map((tier, ti): Tier => {
        const bays = smalls.filter(
          s => s.largeCode === large.largeCode && s.middleCode === tier.middleCode);
        const zs = assignAxis(bays.map(s => s.smallCode));
        return {
          loc: tier,
          y: ys[ti],
          bays: bays.map((bay, bi) => ({ loc: bay, z: zs[bi] })),
        };
      }),
    };
  });

  const maxX = Math.max(...columns.map(c => c.x));
  const maxZ = Math.max(1, ...columns.flatMap(c => c.tiers.flatMap(t => t.bays.map(b => b.z))));

  const offX = -((maxX - 1) * PITCH_X) / 2;
  const offZ = -((maxZ - 1) * PITCH_Z) / 2;
  const posX = (x: number) => offX + (x - 1) * PITCH_X;
  const posZ = (z: number) => offZ + (z - 1) * PITCH_Z;
  const boardY = (y: number) => BASE_H + (y - 1) * PITCH_Y;

  for (const col of columns) {
    const cx = posX(col.x);
    const topTier = col.tiers.length ? Math.max(...col.tiers.map(t => t.y)) : 1;
    const rackHeight = boardY(topTier) + PITCH_Y + RACK_TOP;

    const colMaxZ = Math.max(1, ...col.tiers.flatMap(t => t.bays.map(b => b.z)));
    const rackDepth = colMaxZ * PITCH_Z;
    const rackZ = (posZ(1) + posZ(colMaxZ)) / 2;

    out.larges.push({
      loc: col.loc, x: cx, y: BASE_H / 2, z: rackZ,
      w: RACK_W, h: BASE_H, d: rackDepth, rackHeight,
    });

    for (const tier of col.tiers) {
      out.middles.push({
        loc: tier.loc, x: cx, y: boardY(tier.y) + BOARD_H / 2, z: rackZ,
        w: RACK_W, h: BOARD_H, d: rackDepth,
      });

      for (const bay of tier.bays) {
        out.smalls.push({
          loc: bay.loc,
          x: cx,
          y: boardY(tier.y) + BOARD_H + BIN_H / 2,
          z: posZ(bay.z),
          w: BIN_W, h: BIN_H, d: BIN_D,
        });
      }
    }
  }

  out.bounds = {
    width: (maxX - 1) * PITCH_X + RACK_W,
    depth: maxZ * PITCH_Z,
    height: Math.max(...out.larges.map(b => b.rackHeight ?? 0)),
  };
  return out;
}

export const RACK_POST = POST;

export function orphanCount(all: Location[], layout: LocationLayout): number {
  const placed = new Set([...layout.larges, ...layout.middles, ...layout.smalls]
    .map(b => b.loc.locationCode));
  return all.filter(l => !placed.has(l.locationCode)).length;
}
