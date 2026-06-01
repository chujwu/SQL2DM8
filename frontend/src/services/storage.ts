import type { ConnectionInfo } from '../types';

const STORAGE_KEY_PROFILES = 'sql2dm8_connection_profiles';
const STORAGE_KEY_RECENT = 'sql2dm8_recent_connections';

export interface ConnectionProfile {
  id: string;
  name: string;
  connectionInfo: ConnectionInfo;
  database?: string;
  createdAt?: string;
  lastUsed?: string;
}

// 连接配置管理
export const profileStorage = {
  getAll: (): ConnectionProfile[] => {
    try {
      const data = localStorage.getItem(STORAGE_KEY_PROFILES);
      return data ? JSON.parse(data) : [];
    } catch {
      return [];
    }
  },

  save: (profile: ConnectionProfile): void => {
    const profiles = profileStorage.getAll();
    const existingIndex = profiles.findIndex((p) => p.id === profile.id);

    if (existingIndex >= 0) {
      profiles[existingIndex] = { ...profile, lastUsed: new Date().toISOString() };
    } else {
      profiles.push({ ...profile, createdAt: new Date().toISOString() });
    }

    localStorage.setItem(STORAGE_KEY_PROFILES, JSON.stringify(profiles));
  },

  delete: (id: string): void => {
    const profiles = profileStorage.getAll().filter((p) => p.id !== id);
    localStorage.setItem(STORAGE_KEY_PROFILES, JSON.stringify(profiles));
  },

  getById: (id: string): ConnectionProfile | undefined => {
    return profileStorage.getAll().find((p) => p.id === id);
  },
};

// 最近连接管理
export const recentStorage = {
  getAll: (): ConnectionProfile[] => {
    try {
      const data = localStorage.getItem(STORAGE_KEY_RECENT);
      return data ? JSON.parse(data) : [];
    } catch {
      return [];
    }
  },

  add: (profile: ConnectionProfile): void => {
    const recents = recentStorage.getAll().filter((p) => p.id !== profile.id);
    recents.unshift({ ...profile, lastUsed: new Date().toISOString() });

    // 只保留最近10个
    if (recents.length > 10) {
      recents.pop();
    }

    localStorage.setItem(STORAGE_KEY_RECENT, JSON.stringify(recents));
  },

  clear: (): void => {
    localStorage.removeItem(STORAGE_KEY_RECENT);
  },
};

// 生成唯一ID
export const generateId = (): string => {
  return Date.now().toString(36) + Math.random().toString(36).substr(2);
};
