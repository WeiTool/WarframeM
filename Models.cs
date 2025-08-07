using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace WarframeM
{

    public class ApiResponse
    {
        /// API返回的有效负载数据
        public Payload Payload { get; set; }
    }

    /// API返回的有效负载
    public class Payload
    {
        /// 物品订单列表
        public List<Order> Orders { get; set; }
    }

    /// 市场订单模型
    public class Order
    {
        /// 订单类型 (buy/sell)
        [JsonProperty("order_type")]
        public string OrderType { get; set; }

        /// 白金价格
        [JsonProperty("platinum")]
        public int Platinum { get; set; }

        /// 物品数量
        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        /// 下单用户信息
        [JsonProperty("user")]
        public User User { get; set; }

        // 添加平台字段
        [JsonProperty("platform")]
        public string Platform { get; set; }
    }

    /// 用户信息模型
    public class User
    {
        /// 玩家游戏内名称
        [JsonProperty("ingame_name")]
        public string IngameName { get; set; }

        /// 玩家在线状态 (online/ingame/offline)
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    /// 用于UI展示的订单视图模型
    public class OrderViewModel
    {
        /// 订单类型 (出售/收购)
        public string OrderType { get; set; }

        /// 白金价格
        public int Platinum { get; set; }

        /// 物品数量
        public int Quantity { get; set; }

        /// 玩家状态 (在线/游戏中/离线)
        public string UserStatus { get; set; }

        /// 玩家游戏内名称
        public string UserName { get; set; }
        public string Platform { get; set; }
    }
}