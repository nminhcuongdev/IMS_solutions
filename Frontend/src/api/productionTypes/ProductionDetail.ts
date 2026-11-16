import type { ProductionOrderRow } from "./ProductionOrderRow";
import type { ProductionLineRow } from "./ProductionLineRow";
import type { ProductionWork } from "./ProductionWork";

export interface ProductionDetail {
  order: ProductionOrderRow; lines: ProductionLineRow[]; works?: ProductionWork[]; outputQty: number;
  progress: { percent: number }; overPlan: boolean; startedAt: string | null; completedAt: string | null;
  htScanRequiredCount: number; pickedStatus: string; receiptStatus: string; htInputStatus: string;
}
