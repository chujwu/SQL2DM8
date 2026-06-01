import { useState, useEffect } from 'react';
import { Card, Row, Col, Button, Space, message, Tag, Descriptions, List } from 'antd';
import {
  ExperimentOutlined,
  CodeOutlined,
} from '@ant-design/icons';
import SqlDiffViewer from '../components/SqlDiffViewer';
import type { ConvertResult, DatabaseObjectType } from '../types';
import axios from 'axios';

interface SampleObject {
  name: string;
  schema: string;
  type: DatabaseObjectType;
  description: string;
  sqlServerSql: string;
}

export default function SampleDemo() {
  const [samples, setSamples] = useState<SampleObject[]>([]);
  const [results, setResults] = useState<ConvertResult[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const [converting, setConverting] = useState(false);

  useEffect(() => {
    loadSamples();
  }, []);

  const loadSamples = async () => {
    setLoading(true);
    try {
      const { data } = await axios.get('/api/sample/objects');
      setSamples(data);
    } catch (error) {
      message.error('加载示例数据失败');
    } finally {
      setLoading(false);
    }
  };

  const handleConvertAll = async () => {
    setConverting(true);
    try {
      const { data } = await axios.post('/api/sample/convert');
      setResults(data);
      setCurrentIndex(0);
      message.success(`转换完成：${data.length} 个对象`);
    } catch (error) {
      message.error('转换失败');
    } finally {
      setConverting(false);
    }
  };

  const handleConvertSingle = async (index: number) => {
    setConverting(true);
    try {
      const { data } = await axios.post(`/api/sample/convert/${index}`);
      
      // 更新或添加结果
      const newResults = [...results];
      newResults[index] = data;
      setResults(newResults);
      setCurrentIndex(index);
      
      message.success('转换完成');
    } catch (error) {
      message.error('转换失败');
    } finally {
      setConverting(false);
    }
  };

  const getTypeColor = (type: DatabaseObjectType) => {
    switch (type) {
      case 'View': return 'blue';
      case 'Function': return 'green';
      case 'Procedure': return 'purple';
      default: return 'default';
    }
  };

  const getTypeLabel = (type: DatabaseObjectType) => {
    switch (type) {
      case 'View': return '视图';
      case 'Function': return '函数';
      case 'Procedure': return '存储过程';
      default: return type;
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <Row gutter={[24, 24]}>
        <Col span={24}>
          <Card
            title={
              <Space>
                <ExperimentOutlined />
                <span>转换功能演示</span>
              </Space>
            }
            extra={
              <Button
                type="primary"
                icon={<CodeOutlined />}
                onClick={handleConvertAll}
                loading={converting}
              >
                一键转换所有示例
              </Button>
            }
          >
            <p>
              此页面展示了 SQL Server 到达梦8 (DM8) 的转换功能。
              包含视图、函数、存储过程等多种示例，涵盖数据类型、内置函数、语法结构等转换场景。
            </p>
          </Card>
        </Col>
      </Row>

      <Row gutter={[24, 24]} style={{ marginTop: 24 }}>
        <Col xs={24} lg={8}>
          <Card title="示例对象列表" loading={loading}>
            <List
              dataSource={samples}
              renderItem={(item, index) => (
                <List.Item
                  style={{
                    cursor: 'pointer',
                    backgroundColor: index === currentIndex ? '#e6f7ff' : undefined,
                    padding: '12px',
                    borderRadius: '4px',
                  }}
                  onClick={() => setCurrentIndex(index)}
                  actions={[
                    <Button
                      type="link"
                      size="small"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleConvertSingle(index);
                      }}
                      loading={converting}
                    >
                      转换
                    </Button>,
                  ]}
                >
                  <List.Item.Meta
                    title={
                      <Space>
                        <Tag color={getTypeColor(item.type)}>
                          {getTypeLabel(item.type)}
                        </Tag>
                        <span>{item.schema}.{item.name}</span>
                      </Space>
                    }
                    description={item.description}
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>

        <Col xs={24} lg={16}>
          {samples.length > 0 && (
            <Card title="示例详情">
              <Descriptions size="small" style={{ marginBottom: 16 }}>
                <Descriptions.Item label="对象名称">
                  {samples[currentIndex]?.schema}.{samples[currentIndex]?.name}
                </Descriptions.Item>
                <Descriptions.Item label="类型">
                  <Tag color={getTypeColor(samples[currentIndex]?.type)}>
                    {getTypeLabel(samples[currentIndex]?.type)}
                  </Tag>
                </Descriptions.Item>
                <Descriptions.Item label="说明">
                  {samples[currentIndex]?.description}
                </Descriptions.Item>
              </Descriptions>

              {results[currentIndex] ? (
                <SqlDiffViewer result={results[currentIndex]} />
              ) : (
                <div style={{ textAlign: 'center', padding: '40px', color: '#999' }}>
                  <p>点击"转换"按钮查看转换结果</p>
                  <p>或点击"一键转换所有示例"查看所有转换结果</p>
                </div>
              )}
            </Card>
          )}
        </Col>
      </Row>

      {results.length > 0 && (
        <Row gutter={[24, 24]} style={{ marginTop: 24 }}>
          <Col span={24}>
            <Card title="转换统计">
              <Space size="large">
                <Descriptions size="small" column={1}>
                  <Descriptions.Item label="总对象数">
                    {results.length}
                  </Descriptions.Item>
                  <Descriptions.Item label="成功转换">
                    {results.filter((r) => r.convertible).length}
                  </Descriptions.Item>
                  <Descriptions.Item label="有警告">
                    {results.filter((r) => r.warnings.length > 0).length}
                  </Descriptions.Item>
                  <Descriptions.Item label="平均置信度">
                    {(results.reduce((sum, r) => sum + r.confidence, 0) / results.length * 100).toFixed(1)}%
                  </Descriptions.Item>
                </Descriptions>
              </Space>
            </Card>
          </Col>
        </Row>
      )}
    </div>
  );
}
