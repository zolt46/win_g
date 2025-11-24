// File: PublicPCControl.Client/Services/AuditService.cs
using System;

namespace PublicPCControl.Client.Services
{
    public class AuditService
    {
        public bool ScreenshotEnabled { get; set; }
        public TimeSpan ScreenshotInterval { get; set; } = TimeSpan.FromMinutes(5);

        public void CaptureIfNeeded()
        {
            if (!ScreenshotEnabled)
                return;
            // 스크린샷 저장 로직은 운영환경에서 구현
        }
    }
}