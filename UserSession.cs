using System;

namespace Soldering_Mgmt;

public static class UserSession
{
    // 로그인한 사용자 ID
    public static string? UserId { get; set; }
    // 세션 타임아웃 설정(분)
    public static short tmOutMin;
}

