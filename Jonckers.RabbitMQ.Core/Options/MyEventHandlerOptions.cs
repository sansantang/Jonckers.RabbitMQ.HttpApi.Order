using System;
using System.Collections.Generic;
using System.Text;

namespace Jonckers.RabbitMQ.Core.Options
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
    }
}
