export interface ProductionOrderRow {
  no: number; productionOrderNo: string; orderDate: string; factoryCode: string;
  outputItemCode: string; outputItemName: string | null; plannedQty: number; outputQty: number;
  uomCode: string; productionLotNo: string | null; outputLotNo: string | null;
  storageLocationCode: string; plannedStartDate: string; plannedEndDate: string;
  startedAt: string | null; completedAt: string | null; statusCode: number;
}
