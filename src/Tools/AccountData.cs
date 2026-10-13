using System;
using System.Collections.Generic;

namespace Project.Launch.Tools
{
    /// <summary>
    /// 一个已通过 Microsoft 账号登录的账户
    /// A signed-in Microsoft account
    /// </summary>
    public class AccountInfo
    {
        /// <summary>Minecraft 玩家名</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Minecraft UUID（32 位十六进制，无连字符）</summary>
        public string Uuid { get; set; } = string.Empty;

        /// <summary>最近一次使用时间</summary>
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;

        // ============================================================
        // 令牌相关字段
        // ============================================================

        /// <summary>微软 refresh_token（长期，用于静默续期）</summary>
        public string MicrosoftRefreshToken { get; set; } = string.Empty;

        /// <summary>Minecraft access_token（短期，24 小时）</summary>
        public string MinecraftAccessToken { get; set; } = string.Empty;

        /// <summary>access_token 过期时间（UTC）</summary>
        public DateTime TokenExpireAt { get; set; } = DateTime.MinValue;

        /// <summary>皮肤纹理 URL（留空则使用官方默认皮肤）</summary>
        public string SkinUrl { get; set; } = string.Empty;

        /// <summary>当前 access_token 是否仍然可用</summary>
        public bool HasValidToken =>
            !string.IsNullOrEmpty(MinecraftAccessToken) && TokenExpireAt > DateTime.UtcNow;
    }

    public class AccountsRoot
    {
        public Dictionary<string, AccountInfo> Accounts { get; set; } = new();
        public string SelectedAccount { get; set; } = string.Empty;
        public bool LoginCheck { get; set; } = false;
    }
}
