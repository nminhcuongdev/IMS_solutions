import type { ProductionOrderRow } from "./ProductionOrderRow";
import type { ProductionLineRow } from "./ProductionLineRow";

export interface ProductionDetail {
  order: ProductionOrderRow; lines: ProductionLineRow[]; outputQty: number;
  progress: number; overPlan: boolean; startedAt: string | null; completedAt: string | null;
  htScanRequiredCount: number; pickedStatus: string; receiptStatus: string; htInputStatus: string;
}
