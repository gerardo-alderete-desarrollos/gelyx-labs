export type SettingValue = string | boolean;

export type AppSetting = {
  key: string;
  value: SettingValue;
  type: "string" | "boolean";
  updatedAt: string;
  updatedBy: string | null;
};
