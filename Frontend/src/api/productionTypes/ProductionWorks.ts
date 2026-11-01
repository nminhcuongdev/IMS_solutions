import type { ProductionWork } from "./ProductionWork";

export interface ProductionWorks {
  receipts: Record<string, unknown>[]; consumptions: Record<string, unknown>[];
  outputs: Record<string, unknown>[]; labels: Record<string, unknown>[];
  returns: Record<string, unknown>[]; timeline: ProductionWork[];
}
