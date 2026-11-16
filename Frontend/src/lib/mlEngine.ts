import { MlEngineError, printErrorOf } from "@/lib/printErrors";

const ML_SERVER_URL = "http://localhost:8080/";


function parse(xmlText: string) {
  const xmlDoc = new DOMParser().parseFromString(xmlText, "text/xml");
  const node = xmlDoc.getElementsByTagName("result")[0];
  return { xmlDoc, result: node?.childNodes[0]?.nodeValue ?? null };
}

function tag(xmlDoc: Document, name: string): string | null {
  return xmlDoc.getElementsByTagName(name)[0]?.childNodes[0]?.nodeValue ?? null;
}

function throwIfError(parsed: ReturnType<typeof parse>, step: string) {
  if (parsed.result === "OK") return;
  const cc = tag(parsed.xmlDoc, "componentCode");
  const { code, fallback } = printErrorOf(cc, step);
  throw new MlEngineError(code, fallback, step, cc);
}

async function get(path: string, params: Record<string, string>) {
  const qs = new URLSearchParams(params).toString();
  const res = await fetch(`${ML_SERVER_URL}${path}?${qs}`, { method: "GET" });
  return parse(await res.text());
}

async function createInstance(): Promise<string> {
  const p = await get("CreateInstance", { app: "IMS", disposeTime: "5", apiVersion: "1.0" });
  throwIfError(p, "CreateInstance");
  const id = tag(p.xmlDoc, "instanceId");
  if (!id) throw new MlEngineError("PRN-004", "The label print engine did not return an instance ID.", "CreateInstance", null);
  return id;
}

async function disposeInstance(instanceId: string) {
  try { await get("DisposeInstance", { instanceId }); } catch { }
}

export async function mlGetDriverNames(): Promise<string[]> {
  let instanceId: string;
  try { instanceId = await createInstance(); } catch { return []; }
  try {
    const p = await get("GetDriverNameList", { instanceId, printerType: "0" });
    throwIfError(p, "GetDriverNameList");
    const list = tag(p.xmlDoc, "driverNameList");
    return list ? list.split(",").map(s => s.trim()).filter(Boolean) : [];
  } catch {
    return [];
  } finally {
    await disposeInstance(instanceId);
  }
}

export async function mlPing(): Promise<boolean> {
  try {
    const id = await createInstance();
    await disposeInstance(id);
    return true;
  } catch {
    return false;
  }
}

export async function mlGetDriverStatus(): Promise<{ names: string[]; live: boolean }> {
  if (!(await mlPing())) return { names: [], live: false };
  return { names: await mlGetDriverNames(), live: true };
}

export interface MlLabelJob {
  layoutFile: string;
  driver?: string;
  setting?: string;
  fields: Record<string, string | number>;
}

export const lanSetting = (ip: string) => `LAN:${ip.trim()}`;

function jobParams(instanceId: string, job: MlLabelJob): Record<string, string> {
  const params: Record<string, string> = {
    instanceId,
    setting: job.setting ?? `DRV:${job.driver ?? ""}`,
    protocol: "1",
    layoutFile: job.layoutFile,
  };
  for (const [k, v] of Object.entries(job.fields)) params[`prnData.${k}`] = String(v);
  return params;
}

export async function mlPrintLabel(job: MlLabelJob): Promise<void> {
  const instanceId = await createInstance();
  try {
    const pa = await get("PropertyAccess", jobParams(instanceId, job));
    throwIfError(pa, "PropertyAccess");

    const op = await get("OpenPort", { instanceId });
    throwIfError(op, "OpenPort");
    try {
      const out = await get("Output", { instanceId, outputMode: "0" });
      throwIfError(out, "Output");
    } finally {
      try { await get("ClosePort", { instanceId }); } catch { }
    }
  } finally {
    await disposeInstance(instanceId);
  }
}

export async function mlPreviewLabel(opts: MlLabelJob): Promise<string> {
  const instanceId = await createInstance();
  try {
    const pa = await get("PropertyAccess", jobParams(instanceId, opts));
    throwIfError(pa, "PropertyAccess");

    const pv = await get("Preview", {
      instanceId, outputMode: "0", printAreaBorder: "False", zoom: "150",
    });
    throwIfError(pv, "Preview");

    const image = tag(pv.xmlDoc, "image");
    if (!image) throw new MlEngineError("PRT-505", "Cannot create the preview image.", "Preview", "505");
    return image;
  } finally {
    await disposeInstance(instanceId);
  }
}
