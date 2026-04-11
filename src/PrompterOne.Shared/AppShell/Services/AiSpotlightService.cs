using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Services;

public sealed class AiSpotlightService(ScriptDocumentEditService documentEditService)
{
    public event Action? StateChanged;

    private Func<ScriptDocumentEditPlan, Task<ScriptDocumentEditResult>>? _documentEditHandler;

    public AiSpotlightState State { get; private set; } = AiSpotlightState.Closed;

    public void SetRouteContext(AppShellScreen screen, string route, string title)
    {
        var existing = State.Context;
        var next = existing.Editor is not null && screen == AppShellScreen.Editor
            ? existing with { Title = title, Source = "PrompterOne", Route = route, Screen = screen.ToString() }
            : new ScriptArticleContext(Title: title, Source: "PrompterOne", Route: route, Screen: screen.ToString());

        SetContext(next);
    }

    public IDisposable RegisterDocumentEditTarget(Func<ScriptDocumentEditPlan, Task<ScriptDocumentEditResult>> applyAsync)
    {
        ArgumentNullException.ThrowIfNull(applyAsync);
        _documentEditHandler = applyAsync;
        return new CallbackDisposable(() =>
        {
            if (_documentEditHandler == applyAsync)
            {
                _documentEditHandler = null;
            }
        });
    }

    public void SetContext(ScriptArticleContext context) =>
        SetState(State with { Context = context });

    public void Open() =>
        SetState(State with { IsOpen = true });

    public void Toggle()
    {
        if (State.IsOpen)
        {
            Close();
            return;
        }

        Open();
    }

    public void Close() =>
        SetState(State with { IsOpen = false });

    public void UpdatePrompt(string prompt) =>
        SetState(State with { Prompt = prompt ?? string.Empty });

    public Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(State.Prompt))
        {
            return Task.CompletedTask;
        }

        SetState(State with
        {
            IsOpen = true,
            Mode = AiSpotlightMode.Plan,
            Plan = AiSpotlightPlanBuilder.BuildPlan(State.Context),
            Log = [],
            RequiresApproval = false,
            ApprovalRequest = null,
            ErrorMessage = null
        });

        return Task.CompletedTask;
    }

    public Task RunPlanAsync()
    {
        if (AiSpotlightApprovalBuilder.TryCreate(
                State,
                documentEditService,
                out var approvalRequest) &&
            approvalRequest is not null)
        {
            SetState(State with
            {
                Mode = AiSpotlightMode.Approval,
                Plan = State.Plan.Select(item => item with { IsComplete = true }).ToArray(),
                Log = AiSpotlightPlanBuilder.BuildApprovalLog(approvalRequest),
                RequiresApproval = true,
                ApprovalRequest = approvalRequest,
                ErrorMessage = null
            });

            return Task.CompletedTask;
        }

        SetState(State with
        {
            Mode = AiSpotlightMode.Running,
            Plan = State.Plan.Select(item => item with { IsComplete = true }).ToArray(),
            Log = AiSpotlightPlanBuilder.BuildRunningLog(State.Context),
            RequiresApproval = false,
            ApprovalRequest = null,
            ErrorMessage = null
        });

        return Task.CompletedTask;
    }

    public async Task ApproveAsync()
    {
        if (State.ApprovalRequest is not { } approvalRequest)
        {
            return;
        }

        if (_documentEditHandler is null)
        {
            SetState(State with
            {
                ErrorMessage = "There is no active editor target for this document edit.",
                Log = AiSpotlightPlanBuilder.AddLog(
                    State.Log,
                    new AiSpotlightLogEntry("Approval failed", "Open the source editor and try again."))
            });
            return;
        }

        try
        {
            var result = await _documentEditHandler(approvalRequest.Plan);
            SetState(State with
            {
                Mode = AiSpotlightMode.Running,
                Log = AiSpotlightPlanBuilder.AddLog(
                    State.Log,
                    new AiSpotlightLogEntry("Applied edit", $"Document revision {result.Revision.Value[..8]}", true)),
                RequiresApproval = false,
                ApprovalRequest = null,
                ErrorMessage = null
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            SetState(State with
            {
                ErrorMessage = exception.Message,
                Log = AiSpotlightPlanBuilder.AddLog(
                    State.Log,
                    new AiSpotlightLogEntry("Approval failed", exception.Message))
            });
        }
    }

    public void RejectApproval()
    {
        SetState(State with
        {
            Mode = AiSpotlightMode.Running,
            Log = AiSpotlightPlanBuilder.AddLog(
                State.Log,
                new AiSpotlightLogEntry("Change rejected", "No document text was changed.", true)),
            RequiresApproval = false,
            ApprovalRequest = null,
            ErrorMessage = null
        });
    }

    private void SetState(AiSpotlightState state)
    {
        if (EqualityComparer<AiSpotlightState>.Default.Equals(State, state))
        {
            return;
        }

        State = state;
        StateChanged?.Invoke();
    }
}
