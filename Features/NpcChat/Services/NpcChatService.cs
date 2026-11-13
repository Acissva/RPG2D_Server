using RPG2D_Server.Features.NpcChat.Contracts;
using RPG2D_Server.Features.NpcChat.Validation;
using RPG2D_Server.Shared.Validation;
using System.Diagnostics;

namespace RPG2D_Server.Features.NpcChat.Services
{
    public sealed class NpcChatService : INpcChatService, IDisposable
    {
        private readonly INpcChatGateway _gateway;
        private readonly ILogger<NpcChatService> _logger;
        private readonly SemaphoreSlim _slot = new(1, 1);

        public NpcChatService(INpcChatGateway gateway, ILogger<NpcChatService> logger)
        {
            _gateway = gateway;
            _logger = logger;
        }

        public async Task<NpcChatResponse> ChatAsync(NpcChatRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            var errors = NpcChatContentRules.GetErrors(request);
            if (errors.Count > 0) throw new InputValidationException(errors);
            if (!string.Equals(request.NpcId, NpcChatContentRules.GuideNpcId, StringComparison.Ordinal)) throw new NpcChatRejectedException(NpcChatRejectedException.FailureKind.UnsupportedNpc);
            if (!await _slot.WaitAsync(0, cancellationToken)) throw new NpcChatRejectedException(NpcChatRejectedException.FailureKind.Busy);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await _gateway.ChatAsync(request, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation("NPC 交谈完成，RequestId {RequestId}，NpcId {NpcId}，ElapsedMs {ElapsedMs}。", request.RequestId, request.NpcId, stopwatch.ElapsedMilliseconds);
                return response;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("NPC 交谈已取消，RequestId {RequestId}。", request.RequestId);
                throw;
            }
            finally
            {
                _slot.Release();
            }
        }

        public void Dispose()
        {
            _slot.Dispose();
        }
    }
}
