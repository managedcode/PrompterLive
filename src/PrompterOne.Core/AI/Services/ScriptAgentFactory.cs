using System.ClientModel;
using Anthropic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OllamaSharp;
using OpenAI;
using PrompterOne.Core.AI.Abstractions;
using PrompterOne.Core.AI.Agents;
using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Providers;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptAgentFactory(
    IEnumerable<ScriptAgent> agents,
    IAgentRuntimeSettingsSource settingsSource,
    EmbeddedAgentSkillsProvider embeddedAgentSkillsProvider,
    ScriptAgentToolProvider scriptAgentToolProvider,
    IServiceProvider serviceProvider) : IScriptAgentFactory
{
    private readonly IReadOnlyDictionary<string, ScriptAgent> _agentsById = agents.ToDictionary(
        static agent => agent.Id,
        StringComparer.OrdinalIgnoreCase);
    private readonly EmbeddedAgentSkillsProvider _embeddedAgentSkillsProvider = embeddedAgentSkillsProvider;
    private readonly IAgentRuntimeSettingsSource _settingsSource = settingsSource;
    private readonly ScriptAgentToolProvider _scriptAgentToolProvider = scriptAgentToolProvider;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<AIAgent> CreateRequiredAsync(
        string agentId,
        ScriptAgentContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var runtimeSettings = await LoadRuntimeSettingsAsync(cancellationToken);
        var agent = GetRequiredProfile(agentId);
        return CreateAgent(runtimeSettings, agent, context);
    }

    public async Task<IReadOnlyList<AIAgent>> CreateRequiredAsync(
        IEnumerable<string> agentIds,
        ScriptAgentContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var ids = agentIds.ToArray();
        var runtimeSettings = await LoadRuntimeSettingsAsync(cancellationToken);
        var result = new List<AIAgent>(ids.Length);

        foreach (var agentId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(CreateAgent(runtimeSettings, GetRequiredProfile(agentId), context));
        }

        return result;
    }

    private ScriptAgent GetRequiredProfile(string agentId) =>
        _agentsById.TryGetValue(agentId, out var agent)
            ? agent
            : throw new InvalidOperationException($"Unknown script agent '{agentId}'.");

    private AIAgent CreateAgent(
        AgentRuntimeSettings runtimeSettings,
        ScriptAgent agent,
        ScriptAgentContext? context)
    {
        return runtimeSettings.ProviderId switch
        {
            AgentProviderIds.LlamaSharp => throw new NotSupportedException("LlamaSharp is not wired into the multi-agent runtime yet."),
            _ => new ChatClientAgent(
                CreateDecoratedChatClient(runtimeSettings, agent, context),
                agent.GetSystemPrompt(),
                agent.Name,
                agent.Description,
                _scriptAgentToolProvider.CreateTools(context),
                NullLoggerFactory.Instance,
                _serviceProvider)
        };
    }

    private IChatClient CreateDecoratedChatClient(
        AgentRuntimeSettings runtimeSettings,
        ScriptAgent agent,
        ScriptAgentContext? context)
    {
        var chatClient = CreateChatClient(runtimeSettings);
        var contextProviders = CreateContextProviders(agent, context);
        return contextProviders.Count == 0
            ? chatClient
            : chatClient
                .AsBuilder()
                .UseAIContextProviders(contextProviders.ToArray())
                .Build();
    }

    private IReadOnlyList<AIContextProvider> CreateContextProviders(ScriptAgent agent, ScriptAgentContext? context)
    {
        var providers = new List<AIContextProvider>(2);

        var skillsProvider = _embeddedAgentSkillsProvider.CreateProvider(agent.SkillIds);
        if (skillsProvider is not null)
        {
            providers.Add(skillsProvider);
        }

        if (context?.ArticleContext is { IsEmpty: false } articleContext)
        {
            providers.Add(new ArticleContextProvider(articleContext));
        }

        return providers;
    }

    private static IChatClient CreateAnthropicChatClient(AgentRuntimeSettings runtimeSettings)
    {
        var client = new AnthropicClient
        {
            ApiKey = runtimeSettings.ApiKey,
            BaseUrl = runtimeSettings.Endpoint.Length == 0 ? "https://api.anthropic.com" : runtimeSettings.Endpoint
        };

        return client.AsIChatClient(runtimeSettings.Model, defaultMaxOutputTokens: null);
    }

    private static IChatClient CreateChatClient(AgentRuntimeSettings runtimeSettings) =>
        runtimeSettings.ProviderId switch
        {
            AgentProviderIds.Anthropic => CreateAnthropicChatClient(runtimeSettings),
            AgentProviderIds.AzureOpenAi => CreateOpenAiChatClient(runtimeSettings, CreateAzureOpenAiEndpoint(runtimeSettings.Endpoint)),
            AgentProviderIds.Ollama => new OllamaApiClient(new Uri(runtimeSettings.Endpoint), runtimeSettings.Model),
            _ => CreateOpenAiChatClient(runtimeSettings, runtimeSettings.Endpoint)
        };

    private static IChatClient CreateOpenAiChatClient(AgentRuntimeSettings runtimeSettings, string endpoint)
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(runtimeSettings.ApiKey),
            CreateOpenAiOptions(endpoint));

        return client
            .GetChatClient(runtimeSettings.Model)
            .AsIChatClient();
    }

    private static string CreateAzureOpenAiEndpoint(string endpoint)
    {
        var normalized = endpoint.TrimEnd('/');
        return normalized.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{normalized}/openai/v1/";
    }

    private static OpenAIClientOptions CreateOpenAiOptions(string endpoint)
    {
        var options = new OpenAIClientOptions();
        if (endpoint.Length > 0)
        {
            options.Endpoint = new Uri(endpoint);
        }

        return options;
    }

    private async Task<AgentRuntimeSettings> LoadRuntimeSettingsAsync(CancellationToken cancellationToken)
    {
        var runtimeSettings = (await _settingsSource.LoadAsync(cancellationToken)).Normalize();
        return runtimeSettings.IsConfigured()
            ? runtimeSettings
            : throw new InvalidOperationException("No configured AI provider is available for script agents.");
    }
}
