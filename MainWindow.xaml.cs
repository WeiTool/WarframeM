using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WarframeM
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://api.warframe.market/v1/items/";

        // 支持的平台列表
        private readonly List<string> platforms = new List<string> { "pc", "ps4", "xbox", "switch" };

        // 定时器用于自动查询
        private DispatcherTimer autoQueryTimer;
        private bool isNotificationEnabled = false;
        private double lowestPrice = 0;
        private double averagePrice = 0;

        // 可配置的查询间隔选项（分钟）
        private readonly Dictionary<string, TimeSpan> intervalOptions = new Dictionary<string, TimeSpan>
        {
            { "1分钟", TimeSpan.FromMinutes(1) },
            { "2分钟", TimeSpan.FromMinutes(2) },
            { "3分钟", TimeSpan.FromMinutes(3) },
            { "4分钟", TimeSpan.FromMinutes(4) },
            { "5分钟", TimeSpan.FromMinutes(5) }
        };

        public MainWindow()
        {
            InitializeComponent();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            // 初始化定时器
            autoQueryTimer = new DispatcherTimer();
            autoQueryTimer.Interval = intervalOptions["5分钟"]; // 默认5分钟
            autoQueryTimer.Tick += AutoQueryTimer_Tick;

            // 初始化间隔选择器
            InitializeIntervalSelector();
        }

        private void InitializeIntervalSelector()
        {
            // 添加间隔选项
            foreach (var option in intervalOptions.Keys)
            {
                intervalComboBox.Items.Add(option);
            }

            // 设置默认选择为5分钟
            intervalComboBox.SelectedItem = "5分钟";
            intervalComboBox.IsEnabled = false; // 默认禁用，仅在通知开启时可用
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSearch();
        }

        private void NotifyButton_Click(object sender, RoutedEventArgs e)
        {
            isNotificationEnabled = !isNotificationEnabled;

            if (isNotificationEnabled)
            {
                // 获取当前选择的间隔
                var selectedInterval = intervalComboBox.SelectedItem as string;
                if (selectedInterval != null && intervalOptions.TryGetValue(selectedInterval, out var interval))
                {
                    autoQueryTimer.Interval = interval;
                }

                notifyButton.Content = "关闭价格通知";
                intervalComboBox.IsEnabled = true; // 开启通知后允许修改间隔
                autoQueryTimer.Start();

                // 只在没有查询结果时才立即执行查询
                if (ordersDataGrid.Items.Count == 0)
                {
                    ShowNotification("价格通知已开启", $"将每{intervalComboBox.SelectedItem}检查一次最低价格");
                    _ = ExecuteSearch();
                }
                else
                {
                    ShowNotification("价格通知已开启", $"将每{intervalComboBox.SelectedItem}检查一次最低价格\n下次检查时更新");
                }
            }
            else
            {
                notifyButton.Content = "开启价格通知";
                intervalComboBox.IsEnabled = false; // 关闭通知后禁用间隔选择
                autoQueryTimer.Stop();
                ShowNotification("价格通知已关闭", "停止自动检查价格");
            }
        }

        private void IntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isNotificationEnabled || intervalComboBox.SelectedItem == null)
                return;

            var selectedInterval = intervalComboBox.SelectedItem as string;
            if (selectedInterval != null && intervalOptions.TryGetValue(selectedInterval, out var interval))
            {
                autoQueryTimer.Stop();
                autoQueryTimer.Interval = interval;
                autoQueryTimer.Start();

                ShowNotification("通知间隔已更新", $"价格通知间隔变更为{selectedInterval}");
            }
        }

        private async void AutoQueryTimer_Tick(object sender, EventArgs e)
        {
            if (isNotificationEnabled && !string.IsNullOrWhiteSpace(itemNameTextBox.Text.Trim()))
            {
                await ExecuteSearch(showNotificationOnly: true);
            }
        }

        private async Task ExecuteSearch(bool showNotificationOnly = false)
        {
            string itemName = itemNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                if (!showNotificationOnly) // 只有手动查询时才显示输入提示
                    statusText.Text = "请输入物品名称";
                return;
            }

            try
            {
                searchButton.IsEnabled = false;
                notifyButton.IsEnabled = false;

                bool shouldUpdateUI = !showNotificationOnly; // 控制UI更新的标志

                if (shouldUpdateUI)
                {
                    statusText.Text = "正在查询所有平台...";
                }

                var allOrders = new List<OrderViewModel>();
                int totalOrders = 0;

                // 查询所有平台
                foreach (var platform in platforms)
                {
                    // 自动查询时不更新平台查询状态，减少UI刷新
                    if (shouldUpdateUI)
                    {
                        statusText.Text = $"正在查询 {platform.ToUpper()} 平台...";
                    }

                    var platformOrders = await GetOrdersForPlatform(itemName, platform);
                    allOrders.AddRange(platformOrders);
                    totalOrders += platformOrders.Count;
                }

                // 按状态优先级和价格排序
                allOrders.Sort((a, b) =>
                {
                    int statusCompare = GetStatusPriority(b.UserStatus).CompareTo(GetStatusPriority(a.UserStatus));
                    return statusCompare != 0 ? statusCompare : a.Platinum.CompareTo(b.Platinum);
                });

                // 只有手动查询时才更新DataGrid和状态文本
                if (shouldUpdateUI)
                {
                    ordersDataGrid.ItemsSource = allOrders;
                    statusText.Text = $"找到 {totalOrders} 个出售订单（来自所有平台）";
                }

                // 计算最低价和平均价格
                if (allOrders.Count > 0)
                {
                    double newLowestPrice = allOrders[0].Platinum;
                    double newAveragePrice = allOrders.Average(o => o.Platinum);

                    // 只在价格有变化时才更新UI和发送通知（针对自动查询）
                    bool pricesChanged = Math.Abs(newLowestPrice - lowestPrice) > 0.1 ||
                                         Math.Abs(newAveragePrice - averagePrice) > 0.1;

                    // 更新价格数据
                    lowestPrice = newLowestPrice;
                    averagePrice = newAveragePrice;

                    // 更新概览卡片（始终更新，因为数据量小）
                    lowestPriceCard.Text = $"{lowestPrice}";
                    averagePriceCard.Text = $"{averagePrice:F1}";

                    // 价格变化或手动查询时才显示通知
                    if (shouldUpdateUI || pricesChanged)
                    {
                        ShowNotification($"{itemName} 最低价", $"当前最低价: {lowestPrice} 白金");
                    }
                }
                else if (shouldUpdateUI)
                {
                    lowestPriceCard.Text = "-";
                    averagePriceCard.Text = "-";
                    ShowNotification("查询结果", $"未找到 {itemName} 的出售订单");
                }
            }
            catch (Exception ex)
            {
                if (!showNotificationOnly)
                {
                    statusText.Text = $"错误: {ex.Message}";
                }
                ShowNotification("查询错误", $"查询 {itemName} 时出错: {ex.Message}");
            }
            finally
            {
                searchButton.IsEnabled = true;
                notifyButton.IsEnabled = true;
            }
        }

        private async Task<List<OrderViewModel>> GetOrdersForPlatform(string itemName, string platform)
        {
            using (var request = new HttpRequestMessage())
            {
                request.RequestUri = new Uri($"{BaseUrl}{itemName}/orders");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Platform", platform);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse>(json);

                if (result?.Payload?.Orders == null)
                {
                    return new List<OrderViewModel>();
                }

                var viewOrders = new List<OrderViewModel>();
                foreach (var order in result.Payload.Orders)
                {
                    // 只显示出售订单
                    if (order.OrderType != "sell") continue;

                    // 过滤离线玩家
                    if (order.User.Status == "offline") continue;

                    // 处理物品等级
                    string modRankDisplay = "None";
                    if (order.ModRank.HasValue)
                    {
                        modRankDisplay = order.ModRank.Value.ToString();
                    }

                    viewOrders.Add(new OrderViewModel
                    {
                        Platform = platform.ToUpper(),
                        OrderType = "出售",
                        Platinum = order.Platinum,
                        Quantity = order.Quantity,
                        UserStatus = order.User.Status switch
                        {
                            "online" => "在线",
                            "ingame" => "游戏中",
                            _ => order.User.Status
                        },
                        UserName = order.User.IngameName,
                        ModRankDisplay = modRankDisplay,
                    });
                }

                // 平台内排序
                viewOrders.Sort((a, b) =>
                {
                    int statusCompare = GetStatusPriority(b.UserStatus).CompareTo(GetStatusPriority(a.UserStatus));
                    return statusCompare != 0 ? statusCompare : a.Platinum.CompareTo(b.Platinum);
                });

                return viewOrders;
            }
        }

        // 状态优先级辅助方法
        private int GetStatusPriority(string status)
        {
            return status switch
            {
                "游戏中" => 2,
                "在线" => 1,
                _ => 0
            };
        }

        // 显示Windows Toast通知
        private void ShowNotification(string title, string message)
        {
            try
            {
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Toast通知发送失败: {ex.Message}");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            itemNameTextBox.Clear();
            ordersDataGrid.ItemsSource = null;
            ordersDataGrid.Items.Refresh();
            statusText.Text = "已清除";
            lowestPriceCard.Text = "-";
            averagePriceCard.Text = "-";
            isNotificationEnabled = false;
            autoQueryTimer.Stop();
            notifyButton.Content = "开启价格通知";
            intervalComboBox.IsEnabled = false; // 清除时禁用间隔选择器
        }

        protected override void OnClosed(EventArgs e)
        {
            autoQueryTimer.Stop();
            base.OnClosed(e);
        }

    }
}