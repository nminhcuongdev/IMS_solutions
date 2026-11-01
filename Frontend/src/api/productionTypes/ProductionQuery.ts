export interface ProductionQuery {
  unit?: "order" | "line"; view?: "fg" | "work"; type?: string;
  orderNo?: string; itemCode?: string; itemName?: string; productionLotNo?: string;
  fgLotNo?: string; lotNo?: string; status?: number; startDateFrom?: string;
  startDateTo?: string; dateFrom?: string; dateTo?: string; showReversed?: boolean;
  page?: number; pageSize?: number; sort?: string;
}
