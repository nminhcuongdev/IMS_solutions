export interface ProductionWork {
  type: string; no: number; productionOrderNo: string; itemCode: string | null;
  lotNo: string | null; qty: number | null; createdAt: string; createdBy: string | null;
  isReversed: boolean;
}
