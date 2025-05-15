namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// 通话状态
    /// </summary>
    public enum CallState
    {
        Inviting = 0,
        Ringing = 1,
        Answered = 2,
        Rejected = 3,
        HungUp = 4,
        Busy = 5,
        Timeout = 6,
        Error = 7
    }
}