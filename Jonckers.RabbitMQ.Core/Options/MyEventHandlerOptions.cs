using System;
using System.Collections.Generic;
using System.Text;

namespace Jonckers.RabbitMQClient.Core.Options
{
    /// <summary>
    /// Handler的配置
    /// </summary>
    public class MyEventHandlerOptions
    {
        /// <summary>
        /// 禁用 byte[] 解析
        /// </summary>
        public bool DisableDeserializeObject { get; set; } = false;
        /// <summary>
        /// 配置Encoding
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// QoS预取大小，0表示不限制
        /// </summary>
        public ushort PrefetchSize { get; set; } = 0;

        /// <summary>
        /// QoS预取数量，1表示使用默认值
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;

        /// <summary>
        /// QoS是否全局生效
        /// </summary>
        public bool Global { get; set; } = false;
    }
}
