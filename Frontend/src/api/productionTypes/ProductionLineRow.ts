export interface ProductionLineRow {
  no: number; productionOrderNo: string; outputItemCode: string; lineNo: number;
  scheduledDate: string; processCode: string; itemCode: string; itemName: string | null;
  category: string | null; plannedQty: number; consumedQty: number; uomCode: string;
  productionLotNo: string | null; outputLotNo: string | null; storageLocationCode: string;
  picked: boolean; received: boolean; htInputCount: number; htScanRequired: boolean;
  plannedStartDate: string; plannedEndDate: string; startedAt: string | null;
  completedAt: string | null; statusCode: number;
}
