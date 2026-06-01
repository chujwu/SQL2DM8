import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, Row, Col, Button, Space, message, Spin } from 'antd';
import {
  ArrowLeftOutlined,
  DatabaseOutlined,
} from '@ant-design/icons';
import SqlDiffViewer from '../components/SqlDiffViewer';
import ConversionPanel from '../components/ConversionPanel';
import type {
  ConnectionInfo,
  ConvertResult,
  BatchConvertResult,
  DatabaseObjectType,
} from '../types';
import { convertApi } from '../services/api';
import { saveAs } from 'file-saver';

export default function ConvertPage() {
  const navigate = useNavigate();
  const [connectionInfo, setConnectionInfo] = useState<ConnectionInfo | null>(null);
  const [database, setDatabase] = useState<string>('');
  const [selectedObjects, setSelectedObjects] = useState<
    Array<{ type: DatabaseObjectType; schema: string; name: string }>
  >([]);
  const [results, setResults] = useState<ConvertResult[]>([]);
  const [batchResult, setBatchResult] = useState<BatchConvertResult | null>(null);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // 从 sessionStorage 获取数据
    const infoStr = sessionStorage.getItem('connectionInfo');
    const db = sessionStorage.getItem('selectedDatabase');
    const objectsStr = sessionStorage.getItem('selectedObjects');

    if (!infoStr || !db) {
      message.error('请先配置数据库连接');
      navigate('/');
      return;
    }

    const info = JSON.parse(infoStr) as ConnectionInfo;
    setConnectionInfo(info);
    setDatabase(db);

    if (objectsStr) {
      setSelectedObjects(JSON.parse(objectsStr));
    }
  }, [navigate]);

  const handleConvert = async () => {
    if (!connectionInfo || selectedObjects.length === 0) {
      message.warning('请先选择要转换的对象');
      return;
    }

    setLoading(true);
    try {
      const request = {
        database,
        objects: selectedObjects.map((obj) => ({
          name: obj.name,
          schema: obj.schema,
          type: obj.type,
        })),
      };

      const result = await convertApi.convertBatch(request, connectionInfo);
      setResults(result.results);
      setBatchResult(result);
      setCurrentIndex(0);

      message.success(
        `转换完成: ${result.successCount} 成功, ${result.warningCount} 警告, ${result.errorCount} 错误`
      );
    } catch (error: any) {
      message.error(error.response?.data?.message || '转换失败');
    } finally {
      setLoading(false);
    }
  };

  const handleNavigate = (index: number) => {
    if (index >= 0 && index < results.length) {
      setCurrentIndex(index);
    }
  };

  const handleConvertedSqlChange = (sql: string) => {
    const newResults = [...results];
    if (newResults[currentIndex]) {
      newResults[currentIndex] = {
        ...newResults[currentIndex],
        convertedSql: sql,
      };
      setResults(newResults);
    }
  };

  const handleExport = async () => {
    if (results.length === 0) {
      message.warning('没有可导出的结果');
      return;
    }

    try {
      const blob = await convertApi.exportToZip(results);
      saveAs(blob, 'dm8_converted_objects.zip');
      message.success('导出成功');
    } catch (error: any) {
      message.error(error.response?.data?.message || '导出失败');
    }
  };

  const handleReset = () => {
    setResults([]);
    setBatchResult(null);
    setCurrentIndex(0);
  };

  return (
    <div style={{ padding: '24px' }}>
      <Row gutter={[24, 24]}>
        <Col span={24}>
          <Card
            title={
              <Space>
                <Button
                  icon={<ArrowLeftOutlined />}
                  onClick={() => navigate('/browse')}
                >
                  返回
                </Button>
                <span>SQL 转换</span>
                <DatabaseOutlined />
                <span style={{ color: '#666' }}>{database}</span>
              </Space>
            }
          >
            <Row gutter={[16, 16]}>
              <Col span={24}>
                <ConversionPanel
                  results={results}
                  batchResult={batchResult}
                  currentIndex={currentIndex}
                  loading={loading}
                  onNavigate={handleNavigate}
                  onConvert={handleConvert}
                  onExport={handleExport}
                  onReset={handleReset}
                />
              </Col>
            </Row>

            <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
              <Col span={24}>
                <Spin spinning={loading}>
                  <SqlDiffViewer
                    result={results[currentIndex] || null}
                    onConvertedSqlChange={handleConvertedSqlChange}
                  />
                </Spin>
              </Col>
            </Row>
          </Card>
        </Col>
      </Row>
    </div>
  );
}
