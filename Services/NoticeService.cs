using RPG2D_Server.Contracts;

namespace RPG2D_Server.Services
{
    public sealed class NoticeService : INoticeService
    {
        private readonly ILogger<NoticeService> _logger;
        private readonly string _title = "服务器公告";
        private readonly string _content = "版本更新动态：v0.0.16 客户端已连接独立 C# 服务端。";

        public NoticeService(ILogger<NoticeService> logger)
        {
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_title) || _title.Length > 64)
            {
                throw new InvalidOperationException("公告标题必须为 1～64 个 UTF-16 代码单元，且不能只含空白。");
            }

            if (string.IsNullOrWhiteSpace(_content) || _content.Length > 512)
            {
                throw new InvalidOperationException("公告正文必须为 1～512 个 UTF-16 代码单元，且不能只含空白。");
            }

            _logger.LogInformation("内存公告服务已创建。");
        }

        public NoticeResponse GetNotice()
        {
            _logger.LogInformation("读取内存公告，协议版本 {ProtocolVersion}", 1);
            return new NoticeResponse { ProtocolVersion = 1, Title = _title, Content = _content };
        }
    }
}