import { useNavigate } from 'react-router-dom';
import { Card, Typography, Row, Col, Divider } from 'antd';
import { DatabaseOutlined, SwapOutlined } from '@ant-design/icons';
import ConnectionForm from '../components/ConnectionForm';
import type { ConnectionInfo } from '../types';

const { Title, Paragraph } = Typography;

export default function ConnectionPage() {
  const navigate = useNavigate();

  const handleConnect = (info: ConnectionInfo, database: string) => {
    // 保存连接信息到 sessionStorage
    sessionStorage.setItem('connectionInfo', JSON.stringify(info));
    sessionStorage.setItem('selectedDatabase', database);

    // 跳转到对象浏览页
    navigate('/browse');
  };

  return (
    <div style={{ padding: '24px', maxWidth: 1200, margin: '0 auto' }}>
      <Row gutter={[24, 24]}>
        <Col span={24}>
          <Card>
            <Title level={2}>
              <SwapOutlined style={{ marginRight: 12 }} />
              SQL Server → 达梦8 数据库对象转换工具
            </Title>
            <Paragraph type="secondary">
              本工具帮助您将 SQL Server 数据库中的视图、函数、存储过程等对象转换为达梦8 (DM8) 兼容的 SQL 语法。
              支持数据类型映射、内置函数转换、语法结构转换等功能。
            </Paragraph>
          </Card>
        </Col>
      </Row>

      <Row gutter={[24, 24]} style={{ marginTop: 24 }}>
        <Col xs={24} lg={16}>
          <Card title="数据库连接配置" extra={<DatabaseOutlined />}>
            <ConnectionForm onConnect={handleConnect} />
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Card title="使用说明">
            <Paragraph>
              <ol style={{ paddingLeft: 20 }}>
                <li>填写 SQL Server 连接信息</li>
                <li>点击"测试连接"验证配置</li>
                <li>点击"加载数据库"获取数据库列表</li>
                <li>选择目标数据库</li>
                <li>点击"连接并浏览对象"</li>
              </ol>
            </Paragraph>
            <Divider />
            <Title level={5}>支持的转换</Title>
            <ul>
              <li>视图 (Views)</li>
              <li>函数 (Functions)</li>
              <li>存储过程 (Stored Procedures)</li>
            </ul>
            <Divider />
            <Title level={5}>转换特性</Title>
            <ul>
              <li>数据类型自动映射</li>
              <li>内置函数智能转换</li>
              <li>语法结构自动调整</li>
              <li>差异高亮对比</li>
              <li>批量导出支持</li>
            </ul>
          </Card>
        </Col>
      </Row>
    </div>
  );
}
