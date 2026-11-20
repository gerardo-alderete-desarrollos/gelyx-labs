export type ActiveSession = {
  id: string;
  createdAt: string;
  expiresAt: string;
  createdByIp: string | null;
  agent: string | null;
  current: boolean;
};

export type RevokeOtherSessionsResult = {
  revokedCount: number;
};
