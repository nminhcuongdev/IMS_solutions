export const FUNCTION_LABEL_KEY: Record<string, string> = {
  dashboard: "dashboard",

  m_factory: "mFactory",
  m_zone: "mZone",
  m_location: "mLocation",
  m_location_3d: "mLocation3D",
  m_itemgroup: "mItemGroup",
  m_item: "mItem",
  m_partner: "mPartner",
  m_device: "mDevice",
  m_user: "mUser",
  m_reason: "mShortageReason",

  inv: "invInquiry",
  inv_history: "invHistory",
  inv_hold: "invHold",
  alert_center: "alertCenter",

  pick_import: "pickImport",
  pick_result: "pickResult",
  shp_import: "shpImport",
  shp_result: "shpResult",

  st_rfid_demo: "stRfidDemo",

  m_permission: "mUserPermission",
  settings: "mSettings",
  lang_data: "langDataMgmt",
  print_history: "printHistory",
  rfid_gate_history: "rfidGateHistory",
  super_admin: "superAdminMode",
};

export const NAV_LABEL_KEY: Record<string, string> = {
  alert_center: "alertCenterTitle",
  rcv_import: "rcvImport",
  returns: "returnsMgmt",
  rcv_result: "rcvResult",
  st_label: "stLabel",
  st_ref: "stRef",
  st_rfid_demo: "stRfidDemo",
};

export const groupLabelKey = (largeCode: string) => largeCode;
