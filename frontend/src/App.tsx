import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Layout, Menu, Typography } from 'antd';
import {
  DatabaseOutlined,
  FolderOutlined,
  SwapOutlined,
  BookOutlined,
  ExperimentOutlined,
} from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import ConnectionPage from './pages/ConnectionPage';
import ObjectBrowser from './pages/ObjectBrowser';
import ConvertPage from './pages/ConvertPage';
import RulesPage from './pages/RulesPage';
import SampleDemo from './pages/SampleDemo';
import ConnectionStatus from './components/ConnectionStatus';

const { Header, Content, Footer } = Layout;
const { Title } = Typography;

function AppContent() {
  const navigate = useNavigate();
  const location = useLocation();

  const menuItems = [
    {
      key: '/',
      icon: <DatabaseOutlined />,
      label: '数据库连接',
    },
    {
      key: '/browse',
      icon: <FolderOutlined />,
      label: '对象浏览',
    },
    {
      key: '/convert',
      icon: <SwapOutlined />,
      label: 'SQL 转换',
    },
    {
      key: '/rules',
      icon: <BookOutlined />,
      label: '转换规则',
    },
    {
      key: '/demo',
      icon: <ExperimentOutlined />,
      label: '功能演示',
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', padding: '0 24px' }}>
        <div style={{ color: 'white', marginRight: 40 }}>
          <Title level={4} style={{ color: 'white', margin: 0 }}>
            SQL2DM8
          </Title>
        </div>
        <Menu
          theme="dark"
          mode="horizontal"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ flex: 1 }}
        />
        <ConnectionStatus />
      </Header>
      <Content style={{ background: '#f0f2f5' }}>
        <Routes>
          <Route path="/" element={<ConnectionPage />} />
          <Route path="/browse" element={<ObjectBrowser />} />
          <Route path="/convert" element={<ConvertPage />} />
          <Route path="/rules" element={<RulesPage />} />
          <Route path="/demo" element={<SampleDemo />} />
        </Routes>
      </Content>
      <Footer style={{ textAlign: 'center' }}>
        SQL Server to DM8 Converter ©2024 | 达梦8数据库对象转换工具
      </Footer>
    </Layout>
  );
}

export default function App() {
  return (
    <Router>
      <AppContent />
    </Router>
  );
}
