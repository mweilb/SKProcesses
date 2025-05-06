﻿// Program.cs


using AgenticWorkflowSKSample.Workflow1.Steps;
using AgenticWorkflowSKSample;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using AgenticWorkflowSKSample.Workflow1;
using AgenticWorkflowSK;
using System.Text.Json;

namespace cli_client
{
#pragma warning disable SKEXP0080

    class Program
    {
     
        static async Task Main(string[] args)
        {
            

            // Determine if AI mode is enabled
            bool useAI = true;// args.Any(a => a.Equals("--ai", StringComparison.OrdinalIgnoreCase));
 
            // Load configuration (appsettings.json, localsettings.json, env vars)
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            IConfiguration configuration = configBuilder.Build();

            // Determine LLM setup from config
            var setupForLlmRequested = configuration.GetValue("LlmSetup", "Ollama");

            // Initialize Semantic Kernel
            Kernel kernel = KernelSetup.SetupKernel(configuration, setupForLlmRequested);

            // Determine configuration.yml path
            string? configLocation = configuration.GetValue<string>("ConfigurationLocation");
            string configYmlPath = !string.IsNullOrEmpty(configLocation)
                ? Path.Combine(configLocation, "Contexts\\configuration.yml")
                : "../shared/AgenticWorflowSKSample/Contexts/configuration.yml";


            var config = Workflow1.LoadConfigurations(configYmlPath);
            var steps = Workflow1.BuildSteps(config).Build();

            // Use the iterator pattern for the process
            var workflow = new WorkflowProcess<PropertyBag> (kernel, steps);

            // Generate and save Mermaid graph after session ends
            var mermaidGraph = workflow.GetMermaidGraph();
            File.WriteAllText("currentgraph.md", "```mermaid\n"+ mermaidGraph + "```");
            Console.WriteLine("Mermaid graph saved to currentgraph.md.");

            var state = new PropertyBag
            {
                ["KeyWord"] = "stock options trading in non-normal times",
                ["History"] = new List<string>()
            };
        

      
            await foreach (var evt in workflow.IterateAsync(state))
            {
                if (evt == null) // End or error
                {
                    Console.WriteLine("No state available.");
                    break;
                }

                var localState = evt.Data;
                if (localState == null)
                {
                    Console.WriteLine("No local state available.");
                    continue;
                }

               
                switch (evt.EventId)
                {
                    case Workflow1.WaitingOnHumanIterate:
                    {
                        PrintHistory(localState);
                        PrintSuggestions(localState);

                        if (useAI)
                        {
                            workflow.SetNextIteraction(Workflow1.AIDoesHumanStepEvent);
                            Console.WriteLine("Proceed with AI choosing a suggestion.");
                            Console.ReadLine();
                        }
                        else
                        {
                            SelectSuggestion(localState, workflow);
                        }
                      
                    }
                    break;

                    case Workflow1.RequestSystemToDoWork:
                    {
                        HandleRequestSystemToDoWork(localState, workflow);
                    }
                    break;
 
                }
            }
            
            Console.WriteLine("Session ended.");

          
        }

        static void PrintHistory(PropertyBag localState)
        {
            if (localState.TryGetValue<List<string>>("History", out var history) && history != null && history.Count > 0)
            {
                Console.WriteLine("History:");
                foreach (var entry in history)
                {
                    Console.WriteLine($"- {entry}");
                }
            }
        }

        static void PrintSuggestions(PropertyBag localState)
        {
            if (localState.TryGetValue<AIChoices?>("Suggestions", out var suggestions))
            {
                var options = suggestions?.Options ?? new List<string>();
                if (options.Count > 0 && suggestions != null)
                {
                    Console.WriteLine("Available Fix Suggestions:");
                    for (int i = 0; i < options.Count; i++)
                    {
                        Console.WriteLine($"\t{i + 1}: {options[i]}");
                    }
                }
            }
        }

        static int? SelectSuggestion(PropertyBag localState, WorkflowProcess<PropertyBag> workflow)
        {
            if (localState.TryGetValue<AIChoices?>("Suggestions", out var suggestions))
            {
                var options = suggestions?.Options ?? new List<string>();
                if (options.Count > 0 && suggestions != null)
                {
                    Console.Write("Select a suggestion by number: ");
                    if (int.TryParse(Console.ReadLine(), out int selected) &&
                        selected > 0 && selected <= options.Count)
                    {
                        suggestions.SelectedIndex = selected - 1;
                        workflow.SetNextIteraction(AskAppToDoWorkStep.RequestActivity);
                        localState.Update("Suggestions", suggestions);

                        Console.WriteLine($"You selected: {options[selected - 1]}");
                        return selected - 1;
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection. No suggestion selected.");
                    }
                }
            }
            return null;
        }

 

        static void HandleRequestSystemToDoWork(PropertyBag localState, WorkflowProcess<PropertyBag> workflow)
        {
            if (localState.TryGetValue<AIChoices?>("Suggestions", out var suggestions))
            {
                if (localState.TryGetValue<List<string>>("History", out var history))
                {
                    if (history != null && suggestions != null)
                    {
                        history.Add($"{suggestions.Options[suggestions.SelectedIndex]}");
                        localState.Update("History", history);
                    }
                }

                workflow.SetNextIteraction(WorkflowProcess<PropertyBag>.StartEvent);
            }
        }
    }
}
