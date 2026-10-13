// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Radzen;

namespace PPMTool.Services;

/// <summary>
/// Extends Radzen's AI chat service by automatically adding the
/// generated CapX database schema / rules prompt to every conversation as the system prompt.
/// </summary>
public sealed class CapXAIChatService : AIChatService, IAIChatService
{
    private readonly DatabaseSchemaPromptService schemaPromptService;
    private readonly ILogger<CapXAIChatService> logger;

    /// <summary>
    /// Creates a new CapX AI chat service.
    /// </summary>
    /// <param name="serviceProvider">The application's service provider.</param>
    /// <param name="options">The configured Radzen AI chat options.</param>
    /// <param name="schemaPromptService">The service that generates the CapX database system prompt.</param>
    /// <param name="logger">Logging service.</param>
    public CapXAIChatService(
        IServiceProvider serviceProvider,
        IOptions<AIChatServiceOptions> options,
        DatabaseSchemaPromptService schemaPromptService,
        ILogger<CapXAIChatService> logger)
        : base(serviceProvider, options)
    {
        Debug.WriteLine("*** CapXAIChatService constructed ***");
        this.schemaPromptService = schemaPromptService;
        this.logger = logger;
    }

    /// <summary>
    /// Gets chat completions using Radzen's existing implementation,
    /// with the generated CapX database prompt supplied as the system
    /// prompt.
    /// Have to do this as a new method because the base class does not mark the method as virtual.
    /// </summary>
    public new async IAsyncEnumerable<string> GetCompletionsAsync(
        string userInput,
        string sessionId = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default,
        string model = null,
        string systemPrompt = null,
        double? temperature = null,
        int? maxTokens = null,
        string endpoint = null,
        string proxy = null,
        string apiKey = null,
        string apiKeyHeader = null)
    {

        Debug.WriteLine("*** CapXAIChatService.GetCompletionsAsync called ***");

        // Get the generated CapX system prompt from the DatabaseSchemaPromptService.
        var capXSystemPrompt =
            await schemaPromptService.GetPromptAsync(
                cancellationToken);

        // Merge the CapX system prompt with any additional instructions supplied through the RadzenAIChat component.
        var effectiveSystemPrompt =
            MergeSystemPrompts(
                capXSystemPrompt,
                systemPrompt);

        logger.LogInformation($"Calling Model {model} | User Input: {userInput} | System Prompt: ({effectiveSystemPrompt.Length} chars)");

        // Start a stopwatch to time the interaction
        var stopwatch = Stopwatch.StartNew();

        var firstChunk = true;
        var totalChunks = 0;

        // Call the base class's GetCompletionsAsync method with the effective system prompt.
        await foreach (var responseChunk in
            base.GetCompletionsAsync(
                userInput: userInput,
                sessionId: sessionId,
                cancellationToken: cancellationToken,
                model: model,
                systemPrompt: effectiveSystemPrompt,
                temperature: temperature,
                maxTokens: maxTokens,
                endpoint: endpoint,
                proxy: proxy,
                apiKey: apiKey,
                apiKeyHeader: apiKeyHeader))
        {
            if (firstChunk)
            {
                logger.LogInformation(
                    $"FIRST TOKEN after {stopwatch.Elapsed.TotalSeconds:F1}s");

                firstChunk = false;
            }

            totalChunks++;

            yield return responseChunk;
        }

        logger.LogInformation(
            $"COMPLETED after {stopwatch.Elapsed.TotalSeconds:F1}s " +
            $"({totalChunks} chunks)");
    }

    /// <summary>
    /// Combines the generated CapX system prompt with any additional
    /// instructions supplied through the RadzenAIChat component.
    /// </summary>
    private static string MergeSystemPrompts(
        string capXSystemPrompt,
        string additionalSystemPrompt)
    {
        if (string.IsNullOrWhiteSpace(additionalSystemPrompt))
        {
            return capXSystemPrompt;
        }

        return $"""
            {capXSystemPrompt}

            ADDITIONAL INSTRUCTIONS
            =======================

            {additionalSystemPrompt.Trim()}
            """;
    }
}