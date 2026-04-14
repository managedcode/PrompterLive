using System.ClientModel;
using Anthropic;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptChatClientFactory
{
    public static IChatClient Create(AgentRuntimeSettings runtimeSettings) =>
        runtimeSettings.ProviderId switch
        {
            AgentProviderIds.Anthropic => CreateAnthropicChatClient(runtimeSettings),
            AgentProviderIds.AzureOpenAi => CreateAzureOpenAiChatClient(runtimeSettings),
            AgentProviderIds.Ollama => new OllamaApiClient(new Uri(runtimeSettings.Endpoint), runtimeSettings.Model),
            _ => CreateOpenAiChatClient(runtimeSettings, runtimeSettings.Endpoint)
        };

    private static IChatClient CreateAnthropicChatClient(AgentRuntimeSettings runtimeSettings)
    {
        var client = new AnthropicClient
        {
            ApiKey = runtimeSettings.ApiKey,
            BaseUrl = runtimeSettings.Endpoint.Length == 0 ? "https://api.anthropic.com" : runtimeSettings.Endpoint
        };

        return client.AsIChatClient(runtimeSettings.Model, defaultMaxOutputTokens: null);
    }

    private static IChatClient CreateAzureOpenAiChatClient(AgentRuntimeSettings runtimeSettings)
    {
        var client = new AzureOpenAIClient(
            new Uri(CreateAzureOpenAiEndpoint(runtimeSettings.Endpoint)),
            new ApiKeyCredential(runtimeSettings.ApiKey));

        return client
            .GetChatClient(runtimeSettings.Model)
            .AsIChatClient();
    }

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
            ? normalized[..^"/openai/v1".Length]
            : normalized;
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
}
