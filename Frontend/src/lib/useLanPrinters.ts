import { useCallback, useEffect, useMemo, useState } from "react";
import { listPrinters } from "@/api/devices";
import { lanSetting, mlGetDriverNames, mlPing } from "@/lib/mlEngine";

export type PrinterKind = "lan" | "driver";

export interface PrinterOption {
  code: string;
  label: string;
  setting: string;
  kind: PrinterKind;
}

export const driverPrinterCode = (name: string) => `DRV:${name}`;

export function useEngineStatus() {
  const [online, setOnline] = useState<boolean | null>(null);
  const recheck = useCallback(async () => {
    setOnline(null);
    const ok = await mlPing();
    setOnline(ok);
    return ok;
  }, []);
  useEffect(() => { recheck(); }, [recheck]);
  return { online, recheck };
}

export function useLanPrinters(preferred?: string | null) {
  const [lanOptions, setLanOptions] = useState<PrinterOption[] | null>(null);
  const [driverOptions, setDriverOptions] = useState<PrinterOption[]>([]);
  const [deviceCode, setDeviceCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const engine = useEngineStatus();

  useEffect(() => {
    let alive = true;
    listPrinters().then(
      r => {
        if (!alive) return;
        setLanOptions(r.data.filter(d => d.ipAddress).map(d => ({
          code: d.deviceCode,
          label: `${d.deviceName} (${d.ipAddress})`,
          setting: lanSetting(d.ipAddress!),
          kind: "lan" as const,
        })));
      },
      (e: any) => {
        if (!alive) return;
        setLanOptions([]);
        setError(e?.message ?? String(e));
      },
    );
    return () => { alive = false; };
  }, []);

  useEffect(() => {
    if (engine.online !== true) return;
    let alive = true;
    mlGetDriverNames().then(names => {
      if (!alive) return;
      setDriverOptions(names.map(name => ({
        code: driverPrinterCode(name),
        label: name,
        setting: driverPrinterCode(name),
        kind: "driver" as const,
      })));
    });
    return () => { alive = false; };
  }, [engine.online]);

  const printers = useMemo(
    () => (lanOptions === null ? null : [...lanOptions, ...driverOptions]),
    [lanOptions, driverOptions],
  );

  useEffect(() => {
    if (!printers || printers.some(p => p.code === deviceCode)) return;
    setDeviceCode(printers.find(p => p.code === preferred)?.code ?? printers[0]?.code ?? "");
  }, [printers, preferred, deviceCode]);

  const printer = printers?.find(p => p.code === deviceCode);
  const setting = engine.online && printer ? printer.setting : "";
  return { printers, deviceCode, setDeviceCode, setting, error, engineOnline: engine.online, recheck: engine.recheck };
}
