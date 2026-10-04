import { useEffect, useState } from "react";
import { listPrinters, type Device } from "@/api/devices";
import { lanSetting } from "@/lib/mlEngine";

export function useLanPrinters(preferred?: string | null) {
  const [printers, setPrinters] = useState<Device[] | null>(null);
  const [deviceCode, setDeviceCode] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    listPrinters().then(
      r => {
        if (!alive) return;
        const withIp = r.data.filter(d => d.ipAddress);
        setPrinters(withIp);
        setDeviceCode(withIp.find(p => p.deviceCode === preferred)?.deviceCode ?? withIp[0]?.deviceCode ?? "");
      },
      (e: any) => {
        if (!alive) return;
        setPrinters([]);
        setError(e?.message ?? String(e));
      },
    );
    return () => { alive = false; };
  }, [preferred]);

  const printer = printers?.find(p => p.deviceCode === deviceCode);
  const setting = printer?.ipAddress ? lanSetting(printer.ipAddress) : "";
  return { printers, deviceCode, setDeviceCode, setting, error };
}
