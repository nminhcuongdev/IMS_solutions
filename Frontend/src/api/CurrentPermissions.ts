export interface CurrentPermissions {
  userCode: string;
  isSuperAdmin: boolean;
  functions: { functionCode: string; actions: string[] }[];
}
