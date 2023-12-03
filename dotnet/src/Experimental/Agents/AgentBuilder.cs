﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Experimental.Agents.Extensions;
using Microsoft.SemanticKernel.Experimental.Agents.Models;
using YamlDotNet.Serialization;

namespace Microsoft.SemanticKernel.Experimental.Agents;

/// <summary>
/// Fluent builder for initializing an <see cref="IAgent"/> instance.
/// </summary>
public partial class AgentBuilder
{
    private readonly AgentModel _model;

    private readonly List<AgentAssistantModel> _agents;

    private readonly List<IKernelPlugin> _plugins;

    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    private readonly KernelBuilder _kernelBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentBuilder"/> class.
    /// </summary>
    public AgentBuilder()
    {
        this._model = new AgentModel();
        this._agents = new List<AgentAssistantModel>();
        this._kernelBuilder = new KernelBuilder();
        this._plugins = new List<IKernelPlugin>();
    }

    /// <summary>
    /// Builds the agent.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="KernelException"></exception>
    public IAgent Build()
    {
        var kernel = this._kernelBuilder.Build();

        var agent = new Agent(this._model, kernel);

        foreach (var item in this._plugins)
        {
            kernel.Plugins.Add(item);
        }

        foreach (var item in this._agents)
        {
            kernel.ImportPluginFromAgent(agent, item);
        }

        return agent;
    }

    /// <summary>
    /// Defines the agent's name.
    /// </summary>
    /// <param name="name">The agent's name.</param>
    /// <returns></returns>
    public AgentBuilder WithName(string name)
    {
        this._model.Name = name;
        return this;
    }

    /// <summary>
    /// Defines the agent's description.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns></returns>
    public AgentBuilder WithDescription(string description)
    {
        this._model.Description = description;
        return this;
    }

    /// <summary>
    /// Defines the agent's instructions.
    /// </summary>
    /// <param name="instructions">The instructions.</param>
    /// <returns></returns>
    public AgentBuilder WithInstructions(string instructions)
    {
        this._model.Instructions = instructions;
        return this;
    }

    /// <summary>
    /// Define the Azure OpenAI chat completion service (required).
    /// </summary>
    /// <returns><see cref="AgentBuilder"/> instance for fluid expression.</returns>
    public AgentBuilder WithAzureOpenAIChatCompletion(string deploymentName, string model, string endpoint, string apiKey)
    {
        this._kernelBuilder.WithAzureOpenAIChatCompletion(deploymentName, model, endpoint, apiKey);
        return this;
    }

    /// <summary>
    /// Adds the agent's collaborative assistant.
    /// </summary>
    /// <param name="agent">The assistant to add to this agent.</param>
    /// <param name="agentDescription">The agent description for the assistant.</param>
    /// <param name="inputDescription">The agent input description for the assistant.</param>
    /// <returns></returns>
    public AgentBuilder WithAgent(IAgent agent, string agentDescription, string inputDescription)
    {
        this.WithAgent(new AgentAssistantModel
        {
            Agent = agent,
            Description = agentDescription,
            InputDescription = inputDescription
        });

        return this;
    }

    /// <summary>
    /// Adds a plugin to the agent.
    /// </summary>
    /// <param name="plugin"></param>
    /// <returns></returns>
    public AgentBuilder WithPlugin(IKernelPlugin plugin)
    {
        this._plugins.Add(plugin);
        return this;
    }

    /// <summary>
    /// Adds the agent's collaborative assistant.
    /// </summary>
    /// <param name="assistant">The assistant model.</param>
    /// <returns></returns>
    public AgentBuilder WithAgent(AgentAssistantModel assistant)
    {
        this._agents.Add(assistant);

        return this;
    }

    /// <summary>
    /// Sets the logger factory to use.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public AgentBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        this._loggerFactory = loggerFactory;
        this._kernelBuilder.WithLoggerFactory(loggerFactory);

        return this;
    }

    /// <summary>
    /// Creates a new agent from a yaml template.
    /// </summary>
    /// <param name="definitionPath">The yaml definition file path.</param>
    /// <param name="deploymentName">The deployment name.</param>
    /// <param name="modelId">The model id.</param>
    /// <param name="azureOpenAIEndpoint">The Azure OpenAI endpoint.</param>
    /// <param name="azureOpenAIKey">The Azure OpenAI key.</param>
    /// <param name="plugins">The plugins.</param>
    /// <param name="assistants">The assistants.</param>
    /// <param name="loggerFactory">The logger factory instance.</param>
    /// <returns></returns>
    public static IAgent FromTemplate(
        string definitionPath,
        string deploymentName,
        string modelId,
        string azureOpenAIEndpoint,
        string azureOpenAIKey,
        IEnumerable<IKernelPlugin>? plugins = null,
        IEnumerable<AgentAssistantModel>? assistants = null,
        ILoggerFactory? loggerFactory = null)
    {
        var deserializer = new DeserializerBuilder().Build();
        var yamlContent = File.ReadAllText(definitionPath);

        var agentModel = deserializer.Deserialize<AgentModel>(yamlContent);

        var agentBuilder = new AgentBuilder()
            .WithName(agentModel.Name!.Trim())
            .WithDescription(agentModel.Description!.Trim())
            .WithInstructions(agentModel.Instructions.Trim())
            .WithAzureOpenAIChatCompletion(deploymentName, modelId, azureOpenAIEndpoint, azureOpenAIKey);

        if (plugins is not null)
        {
            foreach (var plugin in plugins)
            {
                agentBuilder.WithPlugin(plugin);
            }
        }

        if (assistants is not null)
        {
            foreach (var assistant in assistants)
            {
                agentBuilder.WithAgent(assistant);
            }
        }

        if (loggerFactory is not null)
        {
            agentBuilder.WithLoggerFactory(loggerFactory);
        }

        return agentBuilder.Build();
    }
}
