using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ModelContextProtocol.Client;
//using ModelContextProtocol.Protocol.Transport;

#pragma warning disable SKEXP0001

class Program
{
    static async Task Main(string[] args)
    {
        // Step 1: Build Kernel with Azure OpenAI
        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            deploymentName: "dep name",
            endpoint: "end point",
            apiKey: "api key here",
            serviceId: "azure-openai"
        );

        var kernel = builder.Build();

        // Step 2: Connect to GitHub MCP Server via stdio
        await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(
            new StdioClientTransport(new()
            {
                Name = "GitHub",
                Command = "npx",
                Arguments = ["-y", "@modelcontextprotocol/server-github"]
            }));

        // Step 3: Load MCP tools and add to Kernel
        var tools = await mcpClient.ListToolsAsync();
        kernel.Plugins.AddFromFunctions("GitHub", tools.Select(tool => tool.AsKernelFunction()));

        // Step 4: Prompt the model
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var messages = new ChatHistory("You are a GitHub assistant. Use tools if needed.");
        messages.AddUserMessage("Search repositories");

        var responses = await chat.GetChatMessageContentsAsync(messages, new AzureOpenAIPromptExecutionSettings());
        //Console.WriteLine($"Model response:\n{responses.Select(a => a.Content)}");

        foreach (var response in responses)
        {
            {
                
                Console.Write("Enter GitHub repository search term: ");
                string userQuery = Console.ReadLine();
                var result = await kernel.InvokeAsync("GitHub", "search_repositories", new()
                {
                   //["query"] = "kernel"
                    ["query"] = userQuery
                });

                messages.AddUserMessage($"Here are the issues: {result}");
                var finalMessages = await chat.GetChatMessageContentsAsync(messages, new AzureOpenAIPromptExecutionSettings());
                //Console.WriteLine($"\nFinal response:\n{final.Content}");

                foreach (var message in finalMessages)
                {
                    Console.WriteLine($"\nFinal response:\n{message.Content}");
                }
            }
        }

        Console.ReadLine();
    }
}

#pragma warning disable SKEXP0001