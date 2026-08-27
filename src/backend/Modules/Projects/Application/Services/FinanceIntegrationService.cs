using ERPSystem.Modules.Projects.Application.Events;
using ERPSystem.Modules.Projects.Application.Handlers;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IFinanceIntegrationService
{
    /// <summary>Subscribes all known finance handlers to the in-process event bus.</summary>
    void RegisterHandlers();

    /// <summary>True when all expected handlers have been registered.</summary>
    bool IsHealthy { get; }
}

/// <summary>
/// Sprint 65 / Wave 1A: Orchestrator that wires the Finance handlers into the in-process event
/// bus. <c>Program.cs</c> resolves this as a singleton and calls <see cref="RegisterHandlers"/>
/// once at startup.
///
/// <para><b>Why a dedicated service:</b> keeps the handler→bus wiring in one place so the
/// boot sequence is discoverable from a single line in <c>Program.cs</c>.</para>
/// </summary>
public sealed class FinanceIntegrationService : IFinanceIntegrationService
{
    private readonly IProjectEventBus _bus;
    private readonly IBillingApprovedHandler _billingHandler;
    private readonly ISubPaymentCreatedHandler _subPaymentHandler;
    private readonly ILogger<FinanceIntegrationService> _logger;
    private int _registered;

    public FinanceIntegrationService(
        IProjectEventBus bus,
        IBillingApprovedHandler billingHandler,
        ISubPaymentCreatedHandler subPaymentHandler,
        ILogger<FinanceIntegrationService> logger)
    {
        _bus = bus;
        _billingHandler = billingHandler;
        _subPaymentHandler = subPaymentHandler;
        _logger = logger;
    }

    public void RegisterHandlers()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            _logger.LogDebug("FinanceIntegrationService: handlers already registered — skip");
            return;
        }

        _bus.Subscribe<BillingApprovedEvent>((evt, ct) => _billingHandler.HandleAsync(evt, ct));
        _bus.Subscribe<SubPaymentCreatedEvent>((evt, ct) => _subPaymentHandler.HandleAsync(evt, ct));

        _logger.LogInformation(
            "FinanceIntegrationService: registered 2 handlers (BillingApproved, SubPaymentCreated)");
    }

    public bool IsHealthy => _registered == 1;
}
