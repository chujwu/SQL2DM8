import {
  Card,
  Button,
  Space,
  Tag,
  Table,
  Progress,
  Statistic,
  Row,
  Col,
} from 'antd';
import {
  SwapOutlined,
  DownloadOutlined,
  LeftOutlined,
  RightOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import type { ConvertResult, BatchConvertResult } from '../types';

interface ConversionPanelProps {
  results: ConvertResult[];
  batchResult: BatchConvertResult | null;
  currentIndex: number;
  loading: boolean;
  onNavigate: (index: number) => void;
  onConvert: () => void;
  onExport: () => void;
  onReset: () => void;
}

export default function ConversionPanel({
  results,
  batchResult,
  currentIndex,
  loading,
  onNavigate,
  onConvert,
  onExport,
  onReset,
}: ConversionPanelProps) {
  const getConfidenceColor = (confidence: number) => {
    if (confidence >= 0.9) return '#52c41a';
    if (confidence >= 0.6) return '#faad14';
    return '#ff4d4f';
  };

  const getWarningCount = (result: ConvertResult) => {
    return result.warnings.length;
  };

  const getErrorCount = (result: ConvertResult) => {
    return result.warnings.filter((w) => w.severity === 'Error').length;
  };

  const columns = [
    {
      title: '对象名称',
      key: 'name',
      render: (_: any, record: ConvertResult) => (
        <span>
          {record.schema}.{record.objectName}
        </span>
      ),
    },
    {
      title: '类型',
      dataIndex: 'objectType',
      key: 'type',
      render: (type: string) => (
        <Tag color={type === 'View' ? 'blue' : type === 'Function' ? 'green' : 'purple'}>
          {type === 'View' ? '视图' : type === 'Function' ? '函数' : '存储过程'}
        </Tag>
      ),
    },
    {
      title: '置信度',
      dataIndex: 'confidence',
      key: 'confidence',
      render: (confidence: number) => (
        <Progress
          percent={Math.round(confidence * 100)}
          size="small"
          strokeColor={getConfidenceColor(confidence)}
          format={(percent) => `${percent}%`}
        />
      ),
    },
    {
      title: '状态',
      key: 'status',
      render: (_: any, record: ConvertResult) => {
        const errors = getErrorCount(record);
        const warnings = getWarningCount(record);

        if (!record.convertible) {
          return <Tag color="error">无法转换</Tag>;
        }
        if (errors > 0) {
          return <Tag color="error">{errors} 个错误</Tag>;
        }
        if (warnings > 0) {
          return <Tag color="warning">{warnings} 个警告</Tag>;
        }
        return <Tag color="success">成功</Tag>;
      },
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, _record: any, index: number) => (
        <Button
          type="link"
          size="small"
          onClick={() => onNavigate(index)}
        >
          查看
        </Button>
      ),
    },
  ];

  return (
    <Card
      title="转换面板"
      extra={
        <Space>
          <Button
            icon={<SwapOutlined />}
            type="primary"
            onClick={onConvert}
            loading={loading}
            disabled={results.length === 0}
          >
            批量转换
          </Button>
          <Button
            icon={<DownloadOutlined />}
            onClick={onExport}
            disabled={results.length === 0}
          >
            导出 ZIP
          </Button>
          <Button icon={<ReloadOutlined />} onClick={onReset}>
            重置
          </Button>
        </Space>
      }
    >
      {batchResult && (
        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col span={6}>
            <Statistic title="总计" value={batchResult.totalCount} />
          </Col>
          <Col span={6}>
            <Statistic
              title="成功"
              value={batchResult.successCount}
              valueStyle={{ color: '#3f8600' }}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="警告"
              value={batchResult.warningCount}
              valueStyle={{ color: '#faad14' }}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="错误"
              value={batchResult.errorCount}
              valueStyle={{ color: '#cf1322' }}
            />
          </Col>
        </Row>
      )}

      <Row gutter={16} style={{ marginBottom: 16 }}>
        <Col>
          <Button
            icon={<LeftOutlined />}
            onClick={() => onNavigate(currentIndex - 1)}
            disabled={currentIndex <= 0}
          >
            上一个
          </Button>
        </Col>
        <Col>
          <span style={{ lineHeight: '32px' }}>
            {results.length > 0
              ? `${currentIndex + 1} / ${results.length}`
              : '0 / 0'}
          </span>
        </Col>
        <Col>
          <Button
            icon={<RightOutlined />}
            onClick={() => onNavigate(currentIndex + 1)}
            disabled={currentIndex >= results.length - 1}
          >
            下一个
          </Button>
        </Col>
      </Row>

      <Table
        columns={columns}
        dataSource={results}
        rowKey={(item) => `${item.schema}.${item.objectName}`}
        size="small"
        pagination={{ pageSize: 10 }}
        loading={loading}
        onRow={(_record, index) => ({
          onClick: () => index !== undefined && onNavigate(index),
          style: {
            cursor: 'pointer',
            backgroundColor: index === currentIndex ? '#e6f7ff' : undefined,
          },
        })}
      />
    </Card>
  );
}
