export interface ProductionFg {
  itemName?: string | null;
  productionLotNo?: string | null;
  unit?: string | null;
  locationCode?: string | null;
  serialFrom?: string | null;
  serialTo?: string | null;
  worker?: string | null;
  no: number; requestId: string; productionOrderNo: string; itemCode: string;
  lotNo: string; qty: number; serialCount: number; createdAt: string;
  createdBy: string; labelIssued: boolean; isReversed: boolean;
}
