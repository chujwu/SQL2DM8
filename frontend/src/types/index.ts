// 连接信息
export interface ConnectionInfo {
  server: string;
  port: number;
  useWindowsAuth: boolean;
  username?: string;
  password?: string;
  database?: string;
}

export interface ConnectionTestResult {
  success: boolean;
  message?: string;
  elapsedMs: number;
}

export interface DatabaseInfo {
  name: string;
  id: number;
  state?: string;
}

// 数据库对象
export type DatabaseObjectType = 'View' | 'Function' | 'Procedure';

export interface DatabaseObject {
  name: string;
  schema: string;
  type: DatabaseObjectType;
  definition?: string;
  modifyDate?: string;
}

export interface ObjectTreeNode {
  key: string;
  title: string;
  isLeaf: boolean;
  icon?: string;
  children?: ObjectTreeNode[];
}

export interface SqlDefinition {
  objectName: string;
  schema: string;
  objectType: DatabaseObjectType;
  sql: string;
}

// 转换结果
export interface ConvertResult {
  objectName: string;
  schema: string;
  objectType: DatabaseObjectType;
  originalSql: string;
  convertedSql: string;
  warnings: ConvertWarning[];
  confidence: number;
  convertible: boolean;
}

export interface ConvertWarning {
  line: number;
  column: number;
  message: string;
  severity: 'Info' | 'Warning' | 'Error';
}

export interface BatchConvertRequest {
  database: string;
  objects: ObjectIdentifier[];
}

export interface ObjectIdentifier {
  name: string;
  schema: string;
  type: DatabaseObjectType;
}

export interface BatchConvertResult {
  results: ConvertResult[];
  totalCount: number;
  successCount: number;
  warningCount: number;
  errorCount: number;
}

export interface ConvertRule {
  id: string;
  category: string;
  description: string;
  sqlServerPattern: string;
  dm8Replacement: string;
}
