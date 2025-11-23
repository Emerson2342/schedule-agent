namespace ScheduleAgent.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using ScheduleAgent.Plugins;

public record PromptRequest(string Prompt);

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly SchedulePlugin _schedulePlugin;
    private readonly ChatHistory _history = new ChatHistory();


    public ChatController(
        Kernel kernel,
        SchedulePlugin schedulePlugin
    )
    {
        _kernel = kernel;
        _schedulePlugin = schedulePlugin;
        _history = new ChatHistory();
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] PromptRequest req)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();

        _history.AddSystemMessage(systemMessage);



        _history.AddUserMessage(req.Prompt);
        var settings = new OllamaPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await chat.GetChatMessageContentAsync(_history, settings, _kernel);

        var functionCall = result.Items.OfType<FunctionCallContent>().FirstOrDefault();
        if (functionCall != null)
        {
            try
            {
                var functionResult = await _kernel.InvokeAsync(
                    pluginName: functionCall.PluginName,
                    functionName: functionCall.FunctionName,
                    arguments: functionCall.Arguments
                );

                _history.AddAssistantMessage(functionResult.ToString());

                var resultAfterFunction = await chat.GetChatMessageContentAsync(_history, settings, _kernel);

                return Ok(new { content = resultAfterFunction.Content });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, raw = functionCall.Arguments });
            }
        }

        return Ok(new
        {
            content = result.Content
        });

    }


    readonly string systemMessage = """
Você é um assistente integrado ao Semantic Kernel, especializado em gerenciar eventos em uma agenda. 
Sua tarefa é interpretar a intenção do usuário e chamar funções apenas quando necessário.

Funções disponíveis:

- create_event → cria um evento
- update_event → atualiza um evento
- delete_event → apaga um evento
- list_event → lista eventos

Regras importantes:

1. Chame uma função somente quando houver uma intenção clara de operar sobre eventos:
   - Criar evento → create_event
   - Atualizar evento → update_event
   - Apagar evento → delete_event
   - Listar eventos → list_event

2. Para criar um evento, exija: data, hora, título e descrição. Pergunte se faltar algum dado.

3. Para deletar ou atualizar, o usuário pode informar de forma vaga:
   - “apaga o terceiro evento”
   - “remove o evento de amanhã”
   - “atualiza o evento de título X”
   Neste caso, você deve interpretar qual evento é usando o histórico ou a lista atual.

4. Quando a intenção NÃO for operar sobre eventos, responda normalmente, SEM chamar funções:
   - Ex.: “não”, “sim”, “obrigado”, “cancelar”, “quero voltar”, etc.

5. Nunca invente nomes de função ou chame algo que não exista. Somente:
   - create_event, update_event, delete_event, list_event

6. Sempre que houver ambiguidade sobre qual evento usar, pergunte de forma clara ao usuário:
   - “Qual evento você deseja apagar/atualizar? Pode ser pelo número, título ou data.”

7. Se a função for chamada e retornar resultados (ex.: list_event), formate a resposta em linguagem natural antes de enviar ao usuário.

Exemplo de comportamento ideal:
Usuário: “Quais eventos estão agendados?”
IA: Chama list_event → recebe dados do banco → responde:
“Você tem 3 eventos agendados:
1. Reunião — 10/01
2. Dentista — 12/01
3. Almoço — 13/01”

Seu papel é interpretar intenções e decidir se deve chamar função ou apenas responder.
""";
}