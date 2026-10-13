using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace GoidaLauncher.Services;

/// <summary>
/// Минимальный клиент Discord IPC (без стороннего NuGet-пакета) — подключается к локальному
/// именованному каналу Discord-клиента и отправляет статус "Играет в Minecraft ...".
/// Протокол: 8-байтовый заголовок (int32 opcode + int32 длина payload) + JSON-payload.
/// Требует свой Application ID из https://discord.com/developers/applications (создаётся за 1 минуту,
/// без модерации/approval, в отличие от Azure — просто скопировать Client ID).
/// </summary>
public sealed class DiscordRpcService : IDisposable
{
    private NamedPipeClientStream? _pipe;
    private readonly string _applicationId;
    private bool _connected;

    public DiscordRpcService(string applicationId)
    {
        _applicationId = applicationId;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        if (_connected) return true;

        for (int i = 0; i < 10; i++)
        {
            try
            {
                var pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(300, ct);

                await WriteFrameAsync(pipe, 0, JsonSerializer.Serialize(new
                {
                    v = 1,
                    client_id = _applicationId,
                }), ct);

                // Ответ на handshake — вычитываем, но не обязательно разбираем
                await ReadFrameAsync(pipe, ct);

                _pipe = pipe;
                _connected = true;
                return true;
            }
            catch
            {
                // Discord не запущен, либо не тот индекс канала — пробуем следующий
            }
        }

        return false;
    }

    public async Task SetActivityAsync(string details, string state, DateTimeOffset startedAt, CancellationToken ct = default)
    {
        if (!_connected || _pipe is null) return;

        var payload = JsonSerializer.Serialize(new
        {
            cmd = "SET_ACTIVITY",
            args = new
            {
                pid = Environment.ProcessId,
                activity = new
                {
                    details,
                    state,
                    timestamps = new { start = startedAt.ToUnixTimeSeconds() },
                    assets = new { large_image = "goida_icon", large_text = "GoidaLauncher" },
                },
            },
            nonce = Guid.NewGuid().ToString(),
        });

        try
        {
            await WriteFrameAsync(_pipe, 1, payload, ct);
        }
        catch
        {
            _connected = false; // Discord мог закрыться посреди сессии — просто перестаём слать
        }
    }

    public async Task ClearActivityAsync(CancellationToken ct = default)
    {
        if (!_connected || _pipe is null) return;

        var payload = JsonSerializer.Serialize(new
        {
            cmd = "SET_ACTIVITY",
            args = new { pid = Environment.ProcessId, activity = (object?)null },
            nonce = Guid.NewGuid().ToString(),
        });

        try { await WriteFrameAsync(_pipe, 1, payload, ct); }
        catch { /* не критично при закрытии */ }
    }

    private static async Task WriteFrameAsync(Stream pipe, int opcode, string json, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var header = new byte[8];
        BitConverter.GetBytes(opcode).CopyTo(header, 0);
        BitConverter.GetBytes(body.Length).CopyTo(header, 4);

        await pipe.WriteAsync(header, ct);
        await pipe.WriteAsync(body, ct);
        await pipe.FlushAsync(ct);
    }

    private static async Task<string> ReadFrameAsync(Stream pipe, CancellationToken ct)
    {
        var header = new byte[8];
        await pipe.ReadExactlyAsync(header, ct);
        var length = BitConverter.ToInt32(header, 4);

        var body = new byte[length];
        await pipe.ReadExactlyAsync(body, ct);
        return Encoding.UTF8.GetString(body);
    }

    public void Dispose()
    {
        _pipe?.Dispose();
        _connected = false;
    }
}
