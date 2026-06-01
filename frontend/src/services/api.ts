import axios from 'axios';
import type {
  ConnectionInfo,
  ConnectionTestResult,
  DatabaseInfo,
  DatabaseObject,
  DatabaseObjectType,
  ObjectTreeNode,
  SqlDefinition,
  ConvertResult,
  BatchConvertRequest,
  BatchConvertResult,
  ConvertRule,
} from '../types';

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// 连接管理
export const connectionApi = {
  test: async (info: ConnectionInfo): Promise<ConnectionTestResult> => {
    const { data } = await api.post('/connection/test', info);
    return data;
  },

  getDatabases: async (info: ConnectionInfo): Promise<DatabaseInfo[]> => {
    const { data } = await api.post('/connection/databases', info);
    return data;
  },
};

// 数据库对象
export const objectApi = {
  testConnection: async (database: string, info: ConnectionInfo): Promise<any> => {
    const { data } = await api.post(`/objects/${database}/test`, info);
    return data;
  },

  debugObjectTypes: async (database: string, info: ConnectionInfo): Promise<any> => {
    const { data } = await api.post(`/objects/${database}/debug`, info);
    return data;
  },

  debugObjectMapping: async (database: string, info: ConnectionInfo): Promise<any> => {
    const { data } = await api.post(`/objects/${database}/debug-objects`, info);
    return data;
  },

  getTree: async (database: string, info: ConnectionInfo): Promise<ObjectTreeNode[]> => {
    const { data } = await api.post(`/objects/${database}/tree`, info);
    return data;
  },

  getViews: async (database: string, info: ConnectionInfo): Promise<DatabaseObject[]> => {
    const { data } = await api.post(`/objects/${database}/views`, info);
    return data;
  },

  getFunctions: async (database: string, info: ConnectionInfo): Promise<DatabaseObject[]> => {
    const { data } = await api.post(`/objects/${database}/functions`, info);
    return data;
  },

  getProcedures: async (database: string, info: ConnectionInfo): Promise<DatabaseObject[]> => {
    const { data } = await api.post(`/objects/${database}/procedures`, info);
    return data;
  },

  getSql: async (
    database: string,
    type: DatabaseObjectType,
    schema: string,
    name: string,
    info: ConnectionInfo
  ): Promise<SqlDefinition> => {
    const { data } = await api.post(
      `/objects/${database}/${type}/${schema}/${name}/sql`,
      info
    );
    return data;
  },
};

// SQL 转换
export const convertApi = {
  convertSingle: async (
    sql: string,
    objectType: DatabaseObjectType,
    objectName: string,
    schema?: string
  ): Promise<ConvertResult> => {
    const { data } = await api.post('/convert/single', {
      sql,
      objectType,
      objectName,
      schema: schema || 'dbo',
    });
    return data;
  },

  convertBatch: async (
    request: BatchConvertRequest,
    connectionInfo: ConnectionInfo
  ): Promise<BatchConvertResult> => {
    const params = new URLSearchParams({
      server: connectionInfo.server,
      port: connectionInfo.port.toString(),
      useWindowsAuth: connectionInfo.useWindowsAuth.toString(),
    });
    if (connectionInfo.username) params.append('username', connectionInfo.username);
    if (connectionInfo.password) params.append('password', connectionInfo.password);

    const { data } = await api.post(`/convert/batch?${params.toString()}`, request);
    return data;
  },

  getRules: async (): Promise<ConvertRule[]> => {
    const { data } = await api.get('/convert/rules');
    return data;
  },

  exportToZip: async (results: ConvertResult[]): Promise<Blob> => {
    const response = await api.post('/convert/export', results, {
      responseType: 'blob',
    });
    return response.data;
  },
};

export default api;
