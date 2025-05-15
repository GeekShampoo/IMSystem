namespace IMSystem.Server.Core.Settings
{
    /// <summary>
    /// 消息相关业务参数配置。
    /// </summary>
    public class MessageSettings
    {
        /// <summary>
        /// 消息编辑时长限制（分钟）。
        /// </summary>
        public int EditTimeWindowMinutes { get; set; }

        /// <summary>
        /// 消息撤回时长限制（分钟）。
        /// </summary>
        public int RecallTimeWindowMinutes { get; set; }
    }
}