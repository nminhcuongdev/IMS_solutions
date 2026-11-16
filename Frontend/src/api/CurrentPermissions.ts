export interface CurrentPermissions {
  userCode: string;
  roleCode: string;
  isSuperAdmin: boolean;
  isBuiltin: boolean;
  permissions: string[];
  functions: { functionCode: string; actions: string[] }[];
}
