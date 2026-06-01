import { useState, useEffect } from 'react';
import {
  Form,
  Input,
  InputNumber,
  Switch,
  Button,
  Select,
  Space,
  message,
  Spin,
  Modal,
  List,
  Tag,
  Popconfirm,
  Tooltip,
} from 'antd';
import {
  DatabaseOutlined,
  ApiOutlined,
  SaveOutlined,
  HistoryOutlined,
  DeleteOutlined,
  FolderOutlined,
} from '@ant-design/icons';
import type { ConnectionInfo, DatabaseInfo, ConnectionTestResult } from '../types';
import type { ConnectionProfile } from '../services/storage';
import { connectionApi } from '../services/api';
import { profileStorage, recentStorage, generateId } from '../services/storage';

interface ConnectionFormProps {
  onConnect: (info: ConnectionInfo, database: string) => void;
}

export default function ConnectionForm({ onConnect }: ConnectionFormProps) {
  const [form] = Form.useForm<ConnectionInfo>();
  const [testing, setTesting] = useState(false);
  const [loading, setLoading] = useState(false);
  const [databases, setDatabases] = useState<DatabaseInfo[]>([]);
  const [connected, setConnected] = useState(false);
  const [showProfiles, setShowProfiles] = useState(false);
  const [profiles, setProfiles] = useState<ConnectionProfile[]>([]);
  const [recentConnections, setRecentConnections] = useState<ConnectionProfile[]>([]);

  useEffect(() => {
    loadProfiles();
    loadRecentConnections();
  }, []);

  const loadProfiles = () => {
    setProfiles(profileStorage.getAll());
  };

  const loadRecentConnections = () => {
    setRecentConnections(recentStorage.getAll());
  };

  const handleTestConnection = async () => {
    try {
      const values = await form.validateFields();
      setTesting(true);

      const result: ConnectionTestResult = await connectionApi.test(values);

      if (result.success) {
        message.success(`连接成功！耗时 ${result.elapsedMs.toFixed(0)}ms`);
      } else {
        message.error(`连接失败: ${result.message}`);
      }
    } catch (error) {
      message.error('请填写连接信息');
    } finally {
      setTesting(false);
    }
  };

  const handleLoadDatabases = async () => {
    try {
      const values = await form.validateFields(['server', 'port', 'useWindowsAuth', 'username', 'password']);
      setLoading(true);

      const dbs = await connectionApi.getDatabases(values);
      setDatabases(dbs);
      setConnected(true);
      message.success(`已加载 ${dbs.length} 个数据库`);

      // 保存到最近连接
      const profile: ConnectionProfile = {
        id: generateId(),
        name: `${values.server}:${values.port}`,
        connectionInfo: values,
      };
      recentStorage.add(profile);
      loadRecentConnections();
    } catch (error: any) {
      message.error(error.response?.data?.message || '获取数据库列表失败');
    } finally {
      setLoading(false);
    }
  };

  const handleConnect = async () => {
    try {
      const values = await form.validateFields();
      const database = values.database;

      if (!database) {
        message.warning('请选择数据库');
        return;
      }

      onConnect(values, database);
    } catch (error) {
      message.error('请填写完整连接信息');
    }
  };

  const handleSaveProfile = () => {
    const values = form.getFieldsValue();
    const database = values.database;

    Modal.confirm({
      title: '保存连接配置',
      content: (
        <Form layout="vertical">
          <Form.Item label="配置名称">
            <Input
              id="profileName"
              placeholder="输入配置名称"
              defaultValue={`${values.server}:${values.port}/${database || ''}`}
            />
          </Form.Item>
        </Form>
      ),
      onOk: () => {
        const nameInput = document.getElementById('profileName') as HTMLInputElement;
        const name = nameInput?.value || `${values.server}:${values.port}`;

        const profile: ConnectionProfile = {
          id: generateId(),
          name,
          connectionInfo: values,
          database,
        };

        profileStorage.save(profile);
        loadProfiles();
        message.success('保存成功');
      },
    });
  };

  const handleLoadProfile = (profile: ConnectionProfile) => {
    form.setFieldsValue(profile.connectionInfo);
    if (profile.database) {
      form.setFieldValue('database', profile.database);
    }
    setShowProfiles(false);

    // 如果有数据库，自动加载
    if (profile.connectionInfo) {
      handleLoadDatabases();
    }
  };

  const handleDeleteProfile = (id: string) => {
    profileStorage.delete(id);
    loadProfiles();
    message.success('已删除');
  };

  const useWindowsAuth = Form.useWatch('useWindowsAuth', form);

  return (
    <>
      <Spin spinning={testing || loading}>
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            server: 'localhost',
            port: 1433,
            useWindowsAuth: false,
          }}
        >
          <Form.Item
            name="server"
            label="服务器地址"
            rules={[{ required: true, message: '请输入服务器地址' }]}
          >
            <Input placeholder="localhost 或 IP 地址" />
          </Form.Item>

          <Form.Item
            name="port"
            label="端口"
            rules={[{ required: true, message: '请输入端口' }]}
          >
            <InputNumber min={1} max={65535} style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item
            name="useWindowsAuth"
            label="Windows 认证"
            valuePropName="checked"
          >
            <Switch />
          </Form.Item>

          {!useWindowsAuth && (
            <>
              <Form.Item
                name="username"
                label="用户名"
                rules={[{ required: true, message: '请输入用户名' }]}
              >
                <Input placeholder="sa" />
              </Form.Item>

              <Form.Item
                name="password"
                label="密码"
                rules={[{ required: true, message: '请输入密码' }]}
              >
                <Input.Password placeholder="密码" />
              </Form.Item>
            </>
          )}

          <Form.Item>
            <Space wrap>
              <Button
                type="default"
                icon={<ApiOutlined />}
                onClick={handleTestConnection}
                loading={testing}
              >
                测试连接
              </Button>
              <Button
                type="default"
                icon={<DatabaseOutlined />}
                onClick={handleLoadDatabases}
                loading={loading}
              >
                加载数据库
              </Button>
              <Tooltip title="保存连接配置">
                <Button
                  icon={<SaveOutlined />}
                  onClick={handleSaveProfile}
                />
              </Tooltip>
              <Tooltip title="加载已保存的配置">
                <Button
                  icon={<FolderOutlined />}
                  onClick={() => setShowProfiles(true)}
                />
              </Tooltip>
            </Space>
          </Form.Item>

          {connected && (
            <Form.Item
              name="database"
              label="选择数据库"
              rules={[{ required: true, message: '请选择数据库' }]}
            >
              <Select placeholder="请选择数据库">
                {databases.map((db) => (
                  <Select.Option key={db.name} value={db.name}>
                    {db.name}
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>
          )}

          {connected && (
            <Form.Item>
              <Button type="primary" onClick={handleConnect} block>
                连接并浏览对象
              </Button>
            </Form.Item>
          )}
        </Form>

        {recentConnections.length > 0 && (
          <div style={{ marginTop: 16 }}>
            <h4>
              <HistoryOutlined /> 最近连接
            </h4>
            <List
              size="small"
              dataSource={recentConnections.slice(0, 5)}
              renderItem={(item) => (
                <List.Item
                  style={{ cursor: 'pointer', padding: '8px 0' }}
                  onClick={() => handleLoadProfile(item)}
                >
                  <Space>
                    <Tag color="blue">{item.connectionInfo.server}:{item.connectionInfo.port}</Tag>
                    {item.database && <Tag>{item.database}</Tag>}
                    {item.lastUsed && (
                      <span style={{ color: '#999', fontSize: 12 }}>
                        {new Date(item.lastUsed).toLocaleString()}
                      </span>
                    )}
                  </Space>
                </List.Item>
              )}
            />
          </div>
        )}
      </Spin>

      <Modal
        title="连接配置管理"
        open={showProfiles}
        onCancel={() => setShowProfiles(false)}
        footer={null}
        width={600}
      >
        <List
          dataSource={profiles}
          renderItem={(profile) => (
            <List.Item
              actions={[
                <Button
                  type="link"
                  onClick={() => handleLoadProfile(profile)}
                >
                  使用
                </Button>,
                <Popconfirm
                  title="确定删除此配置？"
                  onConfirm={() => handleDeleteProfile(profile.id)}
                >
                  <Button type="link" danger icon={<DeleteOutlined />} />
                </Popconfirm>,
              ]}
            >
              <List.Item.Meta
                title={profile.name}
                description={
                  <Space>
                    <Tag>{profile.connectionInfo.server}:{profile.connectionInfo.port}</Tag>
                    {profile.database && <Tag color="green">{profile.database}</Tag>}
                    {profile.createdAt && (
                      <span style={{ color: '#999' }}>
                        创建于 {new Date(profile.createdAt).toLocaleDateString()}
                      </span>
                    )}
                  </Space>
                }
              />
            </List.Item>
          )}
          locale={{ emptyText: '暂无保存的配置' }}
        />
      </Modal>
    </>
  );
}
