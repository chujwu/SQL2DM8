import { useState, useEffect } from 'react';
import { Tree, Input, Space, Spin, Empty, Tag, Badge } from 'antd';
import {
  EyeOutlined,
  FunctionOutlined,
  DatabaseOutlined,
  FolderOutlined,
  FileOutlined,
} from '@ant-design/icons';
import type { ObjectTreeNode } from '../types';

const { Search } = Input;

interface ObjectTreeProps {
  treeData: ObjectTreeNode[];
  loading: boolean;
  selectedObjects: string[];
  onSelect: (selectedKeys: string[]) => void;
}

export default function ObjectTree({
  treeData,
  loading,
  selectedObjects,
  onSelect,
}: ObjectTreeProps) {
  const [searchValue, setSearchValue] = useState('');
  const [expandedKeys, setExpandedKeys] = useState<string[]>([]);
  const [autoExpandParent, setAutoExpandParent] = useState(true);

  // 默认展开所有类型节点
  useEffect(() => {
    if (treeData.length > 0) {
      // 展开第一级和第二级节点
      const keys: string[] = [];
      treeData.forEach((node) => {
        keys.push(node.key);
        if (node.children) {
          node.children.forEach((child) => {
            keys.push(child.key);
          });
        }
      });
      setExpandedKeys(keys);
    }
  }, [treeData]);

  const getIcon = (icon?: string) => {
    switch (icon) {
      case 'eye':
        return <EyeOutlined style={{ color: '#1890ff' }} />;
      case 'function':
        return <FunctionOutlined style={{ color: '#52c41a' }} />;
      case 'database':
        return <DatabaseOutlined style={{ color: '#722ed1' }} />;
      case 'folder':
        return <FolderOutlined style={{ color: '#faad14' }} />;
      default:
        return <FileOutlined />;
    }
  };

  const getTypeColor = (type: string): string => {
    switch (type) {
      case 'View':
      case '视图':
        return 'blue';
      case 'Function':
      case '函数':
        return 'green';
      case 'Procedure':
      case '存储过程':
        return 'purple';
      default:
        return 'default';
    }
  };

  const filterTree = (
    nodes: ObjectTreeNode[],
    search: string
  ): ObjectTreeNode[] => {
    if (!search) return nodes;

    return nodes
      .map((node) => {
        // 检查当前节点是否匹配
        if (node.title.toLowerCase().includes(search.toLowerCase())) {
          return node;
        }

        // 检查子节点
        if (node.children) {
          const filteredChildren = filterTree(node.children, search);
          if (filteredChildren.length > 0) {
            return { ...node, children: filteredChildren };
          }
        }

        return null;
      })
      .filter(Boolean) as ObjectTreeNode[];
  };

  const onExpand = (newExpandedKeys: React.Key[]) => {
    setExpandedKeys(newExpandedKeys as string[]);
    setAutoExpandParent(false);
  };

  const filteredTree = filterTree(treeData, searchValue);

  // 计算每个类型的对象数量
  const getTypeCount = (type: string): number => {
    const typeNode = treeData.find((node) => node.key === type);
    if (!typeNode || !typeNode.children) return 0;
    
    let count = 0;
    typeNode.children.forEach((schemaNode) => {
      if (schemaNode.children) {
        count += schemaNode.children.length;
      }
    });
    return count;
  };

  const renderTreeNodes = (nodes: ObjectTreeNode[]): any[] => {
    return nodes.map((node) => {
      // 根据节点层级显示不同的标题样式
      const isRootType = ['View', 'Function', 'Procedure'].includes(node.key);
      const isSchema = node.key.split('/').length === 2;

      let title;
      if (isRootType) {
        // 类型节点：显示类型名称和数量
        const count = getTypeCount(node.key);
        title = (
          <Space size="small">
            <span style={{ fontWeight: 'bold', fontSize: 14 }}>{node.title}</span>
            <Badge count={count} style={{ backgroundColor: getTypeColor(node.key) === 'blue' ? '#1890ff' : getTypeColor(node.key) === 'green' ? '#52c41a' : '#722ed1' }} />
          </Space>
        );
      } else if (isSchema) {
        // Schema 节点
        const objectCount = node.children?.length || 0;
        title = (
          <Space size="small">
            <span>{node.title}</span>
            <Tag>{objectCount} 个对象</Tag>
          </Space>
        );
      } else {
        // 对象节点
        title = (
          <Space size="small">
            <span>{node.title}</span>
          </Space>
        );
      }

      if (node.children && node.children.length > 0) {
        return {
          ...node,
          title,
          icon: getIcon(node.icon),
          children: renderTreeNodes(node.children),
        };
      }

      return {
        ...node,
        title,
        icon: getIcon(node.icon),
      };
    });
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px 0' }}>
        <Spin tip="加载对象中..." />
      </div>
    );
  }

  if (treeData.length === 0) {
    return <Empty description="暂无数据" />;
  }

  // 统计信息
  const viewCount = getTypeCount('View');
  const functionCount = getTypeCount('Function');
  const procedureCount = getTypeCount('Procedure');

  return (
    <div>
      <div style={{ marginBottom: 16, padding: '8px 0', borderBottom: '1px solid #f0f0f0' }}>
        <Space>
          <Tag color="blue">视图: {viewCount}</Tag>
          <Tag color="green">函数: {functionCount}</Tag>
          <Tag color="purple">存储过程: {procedureCount}</Tag>
        </Space>
      </div>
      
      <Search
        style={{ marginBottom: 16 }}
        placeholder="搜索对象名称"
        onChange={(e) => setSearchValue(e.target.value)}
        allowClear
      />

      <Tree
        showIcon
        checkable
        onExpand={onExpand}
        expandedKeys={expandedKeys}
        autoExpandParent={autoExpandParent}
        treeData={renderTreeNodes(filteredTree)}
        checkedKeys={selectedObjects}
        onCheck={(checked) => onSelect(checked as string[])}
        style={{ overflow: 'auto' }}
      />
    </div>
  );
}
