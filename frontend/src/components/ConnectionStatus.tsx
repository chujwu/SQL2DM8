import { useState, useEffect } from 'react';
import { Tag, Tooltip } from 'antd';
import { CheckCircleOutlined, CloseCircleOutlined, SyncOutlined } from '@ant-design/icons';
import axios from 'axios';

export default function ConnectionStatus() {
  const [status, setStatus] = useState<'checking' | 'connected' | 'disconnected'>('checking');
  const [lastCheck, setLastCheck] = useState<Date | null>(null);

  const checkConnection = async () => {
    setStatus('checking');
    try {
      const response = await axios.get('/api/health', { timeout: 3000 });
      if (response.data.status === 'healthy') {
        setStatus('connected');
        setLastCheck(new Date());
      } else {
        setStatus('disconnected');
      }
    } catch (error) {
      setStatus('disconnected');
      setLastCheck(new Date());
    }
  };

  useEffect(() => {
    checkConnection();
    const interval = setInterval(checkConnection, 30000); // 每30秒检查一次
    return () => clearInterval(interval);
  }, []);

  const getStatusTag = () => {
    switch (status) {
      case 'checking':
        return (
          <Tag icon={<SyncOutlined spin />} color="processing">
            检查中...
          </Tag>
        );
      case 'connected':
        return (
          <Tooltip title={`上次检查: ${lastCheck?.toLocaleTimeString()}`}>
            <Tag icon={<CheckCircleOutlined />} color="success">
              后端已连接
            </Tag>
          </Tooltip>
        );
      case 'disconnected':
        return (
          <Tooltip title="点击重新检查">
            <Tag
              icon={<CloseCircleOutlined />}
              color="error"
              style={{ cursor: 'pointer' }}
              onClick={checkConnection}
            >
              后端未连接
            </Tag>
          </Tooltip>
        );
    }
  };

  return getStatusTag();
}
