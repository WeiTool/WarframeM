# Warframe Market 查询工具

一个基于 WPF 的桌面应用，用于查询 Warframe Market 上的物品价格，支持多平台查询和价格通知功能。

## 功能特性

- 🔍 **多平台查询**：同时查询 PC/PS4/Xbox/Switch 平台的物品价格
- 🔔 **价格通知**：设置定时查询（1-5分钟），通过 Toast 通知显示最低价
- 🚀 **实时状态**：过滤离线玩家，优先显示在线/游戏中玩家
- 🎨 **响应式界面**：自适应窗口大小，美观的数据展示
- 🧹 **一键清除**：快速清除查询结果和搜索框

## 使用说明

1. **输入物品名称**：在搜索框输入要查询的物品名称
2. **点击查询**：查看所有平台的出售订单
3. **开启通知**：
   - 点击"开启价格通知"按钮
   - 选择通知间隔（1-5分钟）
   - 应用会自动定时查询并通知最低价
4. **清除结果**：点击"清除"按钮重置搜索

## 技术栈

- .NET Framework WPF
- Newtonsoft.Json (JSON 处理)
- Microsoft.Toolkit.Uwp.Notifications (Toast 通知)
- Warframe Market API

## 安装依赖

```bash
Install-Package Newtonsoft.Json
Install-Package Microsoft.Toolkit.Uwp.Notifications