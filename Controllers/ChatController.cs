namespace ScheduleAgent.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using ScheduleAgent.Plugins;

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly SchedulePlugin _schedulePlugin;
    private readonly Dictionary<string, ChatHistory> _histories;

    public ChatController(
        Kernel kernel,
        SchedulePlugin schedulePlugin,
        Dictionary<string, ChatHistory> histories
    )
    {
        _kernel = kernel;
        _schedulePlugin = schedulePlugin;
        _histories = histories;
    }

    [HttpPost("{sessionId}")]
    public async Task<IActionResult> Chat(
        string sessionId,
        [FromBody] PromptRequest req
    )
    {
        _kernel.Plugins.AddFromObject(_schedulePlugin, "SchedulePlugin");

        var chat = _kernel.GetRequiredService<IChatCompletionService>();

        if (!_histories.ContainsKey(sessionId))
            _histories[sessionId] = new ChatHistory();

        var history = _histories[sessionId];
        history.AddSystemMessage(systemMessage);
        history.AddUserMessage(req.Prompt);
        var settings = new OllamaPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await chat.GetChatMessageContentAsync(history, executionSettings: settings, kernel: _kernel);

        var functionCall = result.Items.OfType<FunctionCallContent>().FirstOrDefault();
        if (functionCall != null)
        {
            try
            {
                var output = await _kernel.InvokeAsync(
                    pluginName: functionCall.PluginName,
                    functionName: functionCall.FunctionName,
                    arguments: functionCall.Arguments
                );

                history.AddAssistantMessage(output.ToString());
                return Ok(new { content = output.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, raw = functionCall.Arguments });
            }
        }

        return Ok(new
        {
            historyId = sessionId,
            content = result.Content
        });

    }


    string systemMessage = """
Você é um assistente integrado ao Semantic Kernel e deve sempre chamar funções seguindo um padrão obrigatório.

As funções existentes e permitidas devem SEMPRE seguir o formato:
- create_event
- delete_event
- update_event
- list_event

Sempre que o usuário pedir:
- Criar algo relacionado a eventos → use create_event.
- Atualizar ou editar algo de um evento → use update_event.
- Apagar ou remover um evento → use delete_event.
- Listar, consultar, buscar ou visualizar eventos → use list_event.

IMPORTANTE:
Você não deve inventar, derivar ou criar nomes de funções diferentes. 
Somente estas quatro funções existem:

- create_event
- delete_event
- update_event
- list_event

Qualquer outra função NÃO existe.


Nunca utilize outro nome de função além dessas quatro.
Seu trabalho é interpretar a intenção do usuário em linguagem natural e chamar a função correta no formato de Function Calling.
Se o usuário não pedir uma operação de evento, responda normalmente.
""";
}