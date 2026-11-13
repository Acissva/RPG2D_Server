using Microsoft.Extensions.Options;
using RPG2D_Server.Features.NpcChat;
using RPG2D_Server.Features.NpcChat.Contracts;
using RPG2D_Server.Features.NpcChat.Validation;
using RPG2D_Server.Shared.Network;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG2D_Server.Infrastructure.Ai
{
    public sealed class PythonNpcChatGateway : INpcChatGateway
    {
        public const string ClientName = "PythonAi";
        private const int MaxRequestBytes = 64 * 1024;
        private const int MaxResponseBytes = 16 * 1024;
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = false, NumberHandling = JsonNumberHandling.Strict, RespectNullableAnnotations = true };
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PythonAiOptions _options;

        public PythonNpcChatGateway(IHttpClientFactory httpClientFactory, IOptions<PythonAiOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<NpcChatResponse> ChatAsync(NpcChatRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));
            var token = deadline.Token;
            var payload = JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions);
            if (payload.Length > MaxRequestBytes) throw new InvalidOperationException("内部交谈请求超过发送上限。");

            try
            {
                using var client = _httpClientFactory.CreateClient(ClientName);
                using var message = new HttpRequestMessage(HttpMethod.Post, "internal/npc-chat");
                message.Content = new ByteArrayContent(payload);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var kind = response.StatusCode switch
                    {
                        HttpStatusCode.TooManyRequests => UpstreamServiceException.FailureKind.Busy,
                        HttpStatusCode.ServiceUnavailable => UpstreamServiceException.FailureKind.Unavailable,
                        HttpStatusCode.GatewayTimeout => UpstreamServiceException.FailureKind.Timeout,
                        _ => UpstreamServiceException.FailureKind.InvalidResponse
                    };
                    throw new UpstreamServiceException(kind);
                }

                if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)) throw new UpstreamServiceException(UpstreamServiceException.FailureKind.InvalidResponse);
                var raw = await ReadLimitedAsync(response.Content, token);
                var result = JsonSerializer.Deserialize<NpcChatResponse>(StrictUtf8.GetString(raw), JsonOptions);
                if (result is null || result.ProtocolVersion != 1 || !string.Equals(result.RequestId, request.RequestId, StringComparison.Ordinal) || !string.Equals(result.NpcId, request.NpcId, StringComparison.Ordinal) || !NpcChatContentRules.IsValidText(result.Reply, 1024)) throw new UpstreamServiceException(UpstreamServiceException.FailureKind.InvalidResponse);
                token.ThrowIfCancellationRequested();
                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new UpstreamServiceException(UpstreamServiceException.FailureKind.Timeout);
            }
            catch (HttpRequestException)
            {
                throw new UpstreamServiceException(UpstreamServiceException.FailureKind.Unavailable);
            }
            catch (IOException)
            {
                throw new UpstreamServiceException(UpstreamServiceException.FailureKind.Unavailable);
            }
            catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
            {
                throw new UpstreamServiceException(UpstreamServiceException.FailureKind.InvalidResponse);
            }
        }

        private static async Task<byte[]> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
        {
            if (content.Headers.ContentLength is > MaxResponseBytes) throw new UpstreamServiceException(UpstreamServiceException.FailureKind.InvalidResponse);
            using var stream = await content.ReadAsStreamAsync(cancellationToken);
            using var output = new MemoryStream();
            var buffer = new byte[4096];
            while (true)
            {
                var count = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (count == 0) break;
                if (output.Length + count > MaxResponseBytes) throw new UpstreamServiceException(UpstreamServiceException.FailureKind.InvalidResponse);
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
    }
}
