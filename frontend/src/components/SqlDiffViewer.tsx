import { useState } from 'react';
import Editor from '@monaco-editor/react';
import { Card, Tabs, Tag, Alert, Space, Typography, Button, Tooltip } from 'antd';
import {
  WarningOutlined,
  CloseCircleOutlined,
  InfoCircleOutlined,
  EditOutlined,
  UndoOutlined,
} from '@ant-design/icons';
import type { ConvertResult } from '../types';

const { Text } = Typography;

interface SqlDiffViewerProps {
  result: ConvertResult | null;
  onConvertedSqlChange?: (sql: string) => void;
}

export default function SqlDiffViewer({ result, onConvertedSqlChange }: SqlDiffViewerProps) {
  const [editMode, setEditMode] = useState(false);
  const [editedSql, setEditedSql] = useState('');

  if (!result) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: '50px 0', color: '#999' }}>
          请选择一个对象查看转换结果
        </div>
      </Card>
    );
  }

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'Error':
        return <CloseCircleOutlined style={{ color: '#ff4d4f' }} />;
      case 'Warning':
        return <WarningOutlined style={{ color: '#faad14' }} />;
      default:
        return <InfoCircleOutlined style={{ color: '#1890ff' }} />;
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'Error':
        return 'error';
      case 'Warning':
        return 'warning';
      default:
        return 'info';
    }
  };

  const handleEdit = () => {
    setEditedSql(result.convertedSql);
    setEditMode(true);
  };

  const handleSave = () => {
    onConvertedSqlChange?.(editedSql);
    setEditMode(false);
  };

  const handleCancel = () => {
    setEditMode(false);
    setEditedSql('');
  };

  const confidenceColor =
    result.confidence >= 0.9 ? 'green' : result.confidence >= 0.6 ? 'orange' : 'red';

  return (
    <Card
      title={
        <Space>
          <span>{result.schema}.{result.objectName}</span>
          <Tag color={confidenceColor}>
            置信度: {(result.confidence * 100).toFixed(0)}%
          </Tag>
          {!result.convertible && <Tag color="red">无法转换</Tag>}
        </Space>
      }
      extra={
        editMode ? (
          <Space>
            <Button icon={<UndoOutlined />} onClick={handleCancel}>
              取消
            </Button>
            <Button type="primary" onClick={handleSave}>
              保存
            </Button>
          </Space>
        ) : (
          <Tooltip title="手动编辑转换结果">
            <Button icon={<EditOutlined />} onClick={handleEdit}>
              编辑
            </Button>
          </Tooltip>
        )
      }
    >
      <Tabs
        defaultActiveKey="diff"
        items={[
          {
            key: 'diff',
            label: '对比视图',
            children: (
              <div style={{ display: 'flex', gap: 16 }}>
                <Card
                  title="原始 SQL (SQL Server)"
                  size="small"
                  style={{ flex: 1 }}
                >
                  <Editor
                    height="400px"
                    language="sql"
                    value={result.originalSql}
                    options={{
                      readOnly: true,
                      minimap: { enabled: false },
                      lineNumbers: 'on',
                      scrollBeyondLastLine: false,
                      fontSize: 13,
                    }}
                    theme="vs-light"
                  />
                </Card>
                <Card
                  title="转换后 SQL (DM8)"
                  size="small"
                  style={{ flex: 1 }}
                >
                  <Editor
                    height="400px"
                    language="sql"
                    value={editMode ? editedSql : result.convertedSql}
                    onChange={(value) => {
                      if (editMode && value) {
                        setEditedSql(value);
                      }
                    }}
                    options={{
                      readOnly: !editMode,
                      minimap: { enabled: false },
                      lineNumbers: 'on',
                      scrollBeyondLastLine: false,
                      fontSize: 13,
                    }}
                    theme="vs-light"
                  />
                </Card>
              </div>
            ),
          },
          {
            key: 'original',
            label: '原始 SQL',
            children: (
              <Editor
                height="500px"
                language="sql"
                value={result.originalSql}
                options={{
                  readOnly: true,
                  minimap: { enabled: false },
                  lineNumbers: 'on',
                  scrollBeyondLastLine: false,
                  fontSize: 13,
                }}
                theme="vs-light"
              />
            ),
          },
          {
            key: 'converted',
            label: '转换结果',
            children: (
              <Editor
                height="500px"
                language="sql"
                value={editMode ? editedSql : result.convertedSql}
                onChange={(value) => {
                  if (editMode && value) {
                    setEditedSql(value);
                  }
                }}
                options={{
                  readOnly: !editMode,
                  minimap: { enabled: false },
                  lineNumbers: 'on',
                  scrollBeyondLastLine: false,
                  fontSize: 13,
                }}
                theme="vs-light"
              />
            ),
          },
          {
            key: 'warnings',
            label: `警告 (${result.warnings.length})`,
            children: (
              <div style={{ maxHeight: 500, overflow: 'auto' }}>
                {result.warnings.length === 0 ? (
                  <Alert
                    message="无警告"
                    description="转换过程中没有发现警告或问题"
                    type="success"
                    showIcon
                  />
                ) : (
                  result.warnings.map((warning, index) => (
                    <Alert
                      key={index}
                      message={
                        <Space>
                          {getSeverityIcon(warning.severity)}
                          <Text strong>{warning.severity}</Text>
                        </Space>
                      }
                      description={
                        <div>
                          <div>{warning.message}</div>
                          {warning.line > 0 && (
                            <Text type="secondary">
                              位置: 行 {warning.line}, 列 {warning.column}
                            </Text>
                          )}
                        </div>
                      }
                      type={getSeverityColor(warning.severity) as any}
                      style={{ marginBottom: 8 }}
                      showIcon
                    />
                  ))
                )}
              </div>
            ),
          },
        ]}
      />
    </Card>
  );
}
