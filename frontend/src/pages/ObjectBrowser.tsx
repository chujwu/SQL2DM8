import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Space, message, Tag, Spin, Empty, Alert, Modal } from 'antd';
import {
  ArrowLeftOutlined,
  SwapOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import ObjectTree from '../components/ObjectTree';
import Editor from '@monaco-editor/react';
import type { ConnectionInfo, ObjectTreeNode, DatabaseObjectType } from '../types';
import { objectApi } from '../services/api';

export default function ObjectBrowser() {
  const navigate = useNavigate();
  const [connectionInfo, setConnectionInfo] = useState<ConnectionInfo | null>(null);
  const [database, setDatabase] = useState<string>('');
  const [treeData, setTreeData] = useState<ObjectTreeNode[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedObjects, setSelectedObjects] = useState<string[]>([]);
  const [previewSql, setPreviewSql] = useState<string>('');
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewObject, setPreviewObject] = useState<{
    name: string;
    schema: string;
    type: DatabaseObjectType;
  } | null>(null);

  useEffect(() => {
    const infoStr = sessionStorage.getItem('connectionInfo');
    const db = sessionStorage.getItem('selectedDatabase');

    if (!infoStr || !db) {
      message.error('请先配置数据库连接');
      navigate('/');
      return;
    }

    try {
      const info = JSON.parse(infoStr) as ConnectionInfo;
      setConnectionInfo(info);
      setDatabase(db);
      loadObjectTree(info, db);
    } catch (parseError) {
      message.error('连接信息格式错误，请重新连接');
      navigate('/');
    }
  }, [navigate]);

  const loadObjectTree = async (info: ConnectionInfo, db: string) => {
    setLoading(true);
    setError(null);
    try {
      const tree = await objectApi.getTree(db, info);
      setTreeData(tree);
    } catch (error: any) {
      let errorMsg = '加载对象树失败';
      if (error.code === 'ERR_NETWORK') {
        errorMsg = '无法连接到后端服务，请确保后端已启动 (http://localhost:5000)';
      } else if (error.response?.status === 404) {
        errorMsg = 'API 端点不存在，请检查后端服务是否正确运行';
      } else if (error.response?.data?.message) {
        errorMsg = error.response.data.message;
      } else if (error.message) {
        errorMsg = error.message;
      }
      setError(errorMsg);
      message.error(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const handleRetry = () => {
    if (connectionInfo && database) {
      loadObjectTree(connectionInfo, database);
    }
  };

  const handleTestConnection = async () => {
    if (!connectionInfo || !database) return;
    try {
      const result = await objectApi.testConnection(database, connectionInfo);
      message.success(`连接测试成功！视图: ${result.views}, 函数: ${result.functions}, 存储过程: ${result.procedures}`);
    } catch (error: any) {
      message.error(`测试连接失败: ${error.response?.data?.message || error.message}`);
    }
  };

  const handleDebugTypes = async () => {
    if (!connectionInfo || !database) return;
    try {
      const result = await objectApi.debugObjectTypes(database, connectionInfo);
      const typeInfo = result.objectTypes.map((t: any) => `${t.type} (${t.typeDesc}): ${t.count}`).join('\n');
      Modal.info({
        title: `数据库 ${database} 的对象类型统计`,
        content: <pre style={{ maxHeight: 400, overflow: 'auto' }}>{typeInfo}</pre>,
        width: 500,
      });
    } catch (error: any) {
      message.error(`调试失败: ${error.response?.data?.message || error.message}`);
    }
  };

  const handleSelect = (selectedKeys: string[]) => {
    setSelectedObjects(selectedKeys);
    const leafKey = selectedKeys.find((key) => key.split('/').length === 3);
    if (leafKey && connectionInfo) {
      const [type, schema, name] = leafKey.split('/');
      loadObjectSql(type as DatabaseObjectType, schema, name);
    }
  };

  const loadObjectSql = async (type: DatabaseObjectType, schema: string, name: string) => {
    if (!connectionInfo) return;
    setPreviewLoading(true);
    try {
      const sqlDef = await objectApi.getSql(database, type, schema, name, connectionInfo);
      setPreviewSql(sqlDef.sql);
      setPreviewObject({ name, schema, type });
    } catch (error: any) {
      message.error(error.response?.data?.message || '获取 SQL 定义失败');
    } finally {
      setPreviewLoading(false);
    }
  };

  const handleConvert = () => {
    if (selectedObjects.length === 0) {
      message.warning('请至少选择一个对象');
      return;
    }
    const objects = selectedObjects
      .filter((key) => key.split('/').length === 3)
      .map((key) => {
        const [type, schema, name] = key.split('/');
        return { type, schema, name };
      });
    sessionStorage.setItem('selectedObjects', JSON.stringify(objects));
    navigate('/convert');
  };

  const getTypeTag = (type: DatabaseObjectType) => {
    const colorMap: Record<DatabaseObjectType, string> = {
      View: 'blue',
      Function: 'green',
      Procedure: 'purple',
    };
    const labelMap: Record<DatabaseObjectType, string> = {
      View: '视图',
      Function: '函数',
      Procedure: '存储过程',
    };
    return <Tag color={colorMap[type]}>{labelMap[type]}</Tag>;
  };

  const selectedCount = selectedObjects.filter((k) => k.split('/').length === 3).length;

  return (
    <div style={{ height: 'calc(100vh - 64px - 69px)', display: 'flex', flexDirection: 'column', padding: '16px 24px' }}>
      {/* 顶部工具栏 */}
      <div style={{ marginBottom: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexShrink: 0 }}>
        <Space>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/')}>返回</Button>
          <span style={{ fontSize: 16, fontWeight: 500 }}>数据库对象浏览</span>
          <Tag color="blue">{database}</Tag>
        </Space>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={handleRetry} loading={loading}>刷新</Button>
          <Button onClick={handleTestConnection}>测试连接</Button>
          <Button onClick={handleDebugTypes}>对象类型</Button>
          <Button type="primary" icon={<SwapOutlined />} onClick={handleConvert} disabled={selectedCount === 0}>
            开始转换 ({selectedCount} 个对象)
          </Button>
        </Space>
      </div>

      {/* 错误提示 */}
      {error && (
        <Alert
          message="加载失败"
          description={error}
          type="error"
          showIcon
          closable
          style={{ marginBottom: 12, flexShrink: 0 }}
        />
      )}

      {/* 主内容区域 - 占满剩余空间 */}
      <div style={{ flex: 1, display: 'flex', gap: 12, minHeight: 0 }}>
        {/* 左侧：对象树 */}
        <div style={{ width: 400, flexShrink: 0, background: '#fff', borderRadius: 8, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
          <div style={{ padding: '12px 16px', borderBottom: '1px solid #f0f0f0', fontWeight: 500 }}>
            对象树
          </div>
          <div style={{ flex: 1, overflow: 'auto', padding: '8px 0' }}>
            <ObjectTree
              treeData={treeData}
              loading={loading}
              selectedObjects={selectedObjects}
              onSelect={handleSelect}
            />
          </div>
        </div>

        {/* 右侧：SQL 预览 */}
        <div style={{ flex: 1, background: '#fff', borderRadius: 8, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
          <div style={{ padding: '12px 16px', borderBottom: '1px solid #f0f0f0', fontWeight: 500 }}>
            SQL 预览
          </div>
          <div style={{ flex: 1, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
            {previewObject ? (
              <>
                <div style={{ padding: '8px 16px', borderBottom: '1px solid #f0f0f0' }}>
                  <Space>
                    <span>对象: <strong>{previewObject.schema}.{previewObject.name}</strong></span>
                    <span>类型: {getTypeTag(previewObject.type)}</span>
                  </Space>
                </div>
                <div style={{ flex: 1 }}>
                  <Spin spinning={previewLoading}>
                    <Editor
                      height="100%"
                      language="sql"
                      value={previewSql}
                      options={{
                        readOnly: true,
                        minimap: { enabled: false },
                        lineNumbers: 'on',
                        scrollBeyondLastLine: false,
                        fontSize: 13,
                        automaticLayout: true,
                      }}
                      theme="vs-light"
                    />
                  </Spin>
                </div>
              </>
            ) : (
              <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Empty description="请从左侧选择一个对象查看 SQL" />
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
