import { useState, useEffect } from 'react';
import { Card, Table, Tag, Input, Select, Space, message, Tabs } from 'antd';
import { SearchOutlined, BookOutlined } from '@ant-design/icons';
import type { ConvertRule } from '../types';
import { convertApi } from '../services/api';

const { Search } = Input;

export default function RulesPage() {
  const [rules, setRules] = useState<ConvertRule[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchText, setSearchText] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string>('');

  useEffect(() => {
    loadRules();
  }, []);

  const loadRules = async () => {
    setLoading(true);
    try {
      const data = await convertApi.getRules();
      setRules(data);
    } catch (error) {
      message.error('加载转换规则失败');
    } finally {
      setLoading(false);
    }
  };

  const categories = [...new Set(rules.map((r) => r.category))];

  const filteredRules = rules.filter((rule) => {
    const matchSearch =
      !searchText ||
      rule.description.toLowerCase().includes(searchText.toLowerCase()) ||
      rule.sqlServerPattern.toLowerCase().includes(searchText.toLowerCase()) ||
      rule.dm8Replacement.toLowerCase().includes(searchText.toLowerCase());

    const matchCategory = !categoryFilter || rule.category === categoryFilter;

    return matchSearch && matchCategory;
  });

  const columns = [
    {
      title: '规则ID',
      dataIndex: 'id',
      key: 'id',
      width: 100,
      render: (id: string) => <Tag>{id}</Tag>,
    },
    {
      title: '分类',
      dataIndex: 'category',
      key: 'category',
      width: 120,
      render: (category: string) => {
        const colorMap: Record<string, string> = {
          '数据类型': 'blue',
          '内置函数': 'green',
          '日期函数': 'orange',
          '系统函数': 'purple',
          '语法结构': 'red',
        };
        return <Tag color={colorMap[category] || 'default'}>{category}</Tag>;
      },
    },
    {
      title: '说明',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: 'SQL Server',
      dataIndex: 'sqlServerPattern',
      key: 'sqlServerPattern',
      render: (text: string) => (
        <code style={{ background: '#fff1f0', padding: '2px 6px', borderRadius: 4 }}>
          {text}
        </code>
      ),
    },
    {
      title: 'DM8',
      dataIndex: 'dm8Replacement',
      key: 'dm8Replacement',
      render: (text: string) => (
        <code style={{ background: '#f6ffed', padding: '2px 6px', borderRadius: 4 }}>
          {text}
        </code>
      ),
    },
  ];

  const categoryStats = categories.map((cat) => ({
    category: cat,
    count: rules.filter((r) => r.category === cat).length,
  }));

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title={
          <Space>
            <BookOutlined />
            <span>SQL Server → DM8 转换规则</span>
          </Space>
        }
      >
        <Tabs
          defaultActiveKey="rules"
          items={[
            {
              key: 'rules',
              label: '规则列表',
              children: (
                <>
                  <Space style={{ marginBottom: 16 }}>
                    <Search
                      placeholder="搜索规则..."
                      allowClear
                      style={{ width: 300 }}
                      onChange={(e) => setSearchText(e.target.value)}
                      prefix={<SearchOutlined />}
                    />
                    <Select
                      placeholder="选择分类"
                      allowClear
                      style={{ width: 150 }}
                      onChange={(value) => setCategoryFilter(value || '')}
                    >
                      {categories.map((cat) => (
                        <Select.Option key={cat} value={cat}>
                          {cat}
                        </Select.Option>
                      ))}
                    </Select>
                    <span style={{ color: '#666' }}>
                      共 {filteredRules.length} 条规则
                    </span>
                  </Space>
                  <Table
                    columns={columns}
                    dataSource={filteredRules}
                    rowKey="id"
                    loading={loading}
                    pagination={{ pageSize: 20 }}
                    size="middle"
                  />
                </>
              ),
            },
            {
              key: 'overview',
              label: '规则概览',
              children: (
                <div>
                  <Card title="规则统计" style={{ marginBottom: 16 }}>
                    <Table
                      columns={[
                        { title: '分类', dataIndex: 'category', key: 'category' },
                        { title: '规则数量', dataIndex: 'count', key: 'count' },
                      ]}
                      dataSource={categoryStats}
                      rowKey="category"
                      pagination={false}
                      size="small"
                    />
                  </Card>

                  <Card title="数据类型映射">
                    <Table
                      columns={[
                        { title: 'SQL Server 类型', dataIndex: 'sqlServerPattern', key: 'from' },
                        { title: 'DM8 类型', dataIndex: 'dm8Replacement', key: 'to' },
                        { title: '说明', dataIndex: 'description', key: 'desc' },
                      ]}
                      dataSource={rules.filter((r) => r.category === '数据类型')}
                      rowKey="id"
                      pagination={false}
                      size="small"
                    />
                  </Card>

                  <Card title="内置函数映射" style={{ marginTop: 16 }}>
                    <Table
                      columns={[
                        { title: 'SQL Server 函数', dataIndex: 'sqlServerPattern', key: 'from' },
                        { title: 'DM8 函数', dataIndex: 'dm8Replacement', key: 'to' },
                        { title: '说明', dataIndex: 'description', key: 'desc' },
                      ]}
                      dataSource={rules.filter((r) => r.category === '内置函数' || r.category === '日期函数')}
                      rowKey="id"
                      pagination={false}
                      size="small"
                    />
                  </Card>

                  <Card title="语法结构转换" style={{ marginTop: 16 }}>
                    <Table
                      columns={[
                        { title: 'SQL Server 语法', dataIndex: 'sqlServerPattern', key: 'from' },
                        { title: 'DM8 语法', dataIndex: 'dm8Replacement', key: 'to' },
                        { title: '说明', dataIndex: 'description', key: 'desc' },
                      ]}
                      dataSource={rules.filter((r) => r.category === '语法结构')}
                      rowKey="id"
                      pagination={false}
                      size="small"
                    />
                  </Card>
                </div>
              ),
            },
          ]}
        />
      </Card>
    </div>
  );
}
