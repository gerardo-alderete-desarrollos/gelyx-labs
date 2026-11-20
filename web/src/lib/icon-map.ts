import {
  Activity,
  Boxes,
  Briefcase,
  Inbox,
  ClipboardList,
  FileSearch,
  LayoutDashboard,
  LockKeyhole,
  Package,
  KeyRound,
  Shield,
  ShieldCheck,
  Settings,
  UserRound,
  UsersRound,
  type LucideIcon,
} from "lucide-react";

export const iconMap: Record<string, LucideIcon> = {
  activity: Activity,
  audit: ClipboardList,
  box: Boxes,
  briefcase: Briefcase,
  inbox: Inbox,
  dashboard: LayoutDashboard,
  "layout-dashboard": LayoutDashboard,
  "clipboard-list": ClipboardList,
  key: KeyRound,
  lock: LockKeyhole,
  package: Package,
  permission: ShieldCheck,
  report: FileSearch,
  role: Shield,
  shield: Shield,
  settings: Settings,
  user: UserRound,
  users: UsersRound,
};

export function getModuleIcon(icon: string | null): LucideIcon {
  return icon ? (iconMap[icon.toLowerCase()] ?? Boxes) : Boxes;
}
