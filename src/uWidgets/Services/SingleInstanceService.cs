using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace uWidgets.Services;

/// <summary>
/// Enforces a single running instance per user and forwards secondary launch activations over a named pipe.
/// </summary>
public static class SingleInstanceService
{
    private static readonly string UserSuffix = ComputeUserScopeSuffix();
    private static readonly string MutexName = $@"Local\uWidgets_Instance_{UserSuffix}";
    private static readonly string PipeName = $"uWidgets_Pipe_{UserSuffix}";

    private static Mutex? instanceMutex;
    private static bool ownsMutex;
    private static CancellationTokenSource? serverCts;

    public static bool TryAcquire(string[] args)
    {
        var waitMs = args.Any(a => string.Equals(a, "--restarted", StringComparison.OrdinalIgnoreCase)) ? 3000 : 0;

        try
        {
            instanceMutex = new Mutex(false, MutexName);
            try
            {
                ownsMutex = instanceMutex.WaitOne(waitMs, false);
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            if (ownsMutex) return true;

            SignalPrimaryInstance();
            ReleaseMutex();
            return false;
        }
        catch
        {
            // Never block startup if OS mutex creation fails unexpectedly.
            return true;
        }
    }

    public static void ReleaseMutex()
    {
        if (instanceMutex == null) return;

        try
        {
            if (ownsMutex)
                instanceMutex.ReleaseMutex();
        }
        catch
        {
        }
        finally
        {
            ownsMutex = false;
            instanceMutex.Dispose();
            instanceMutex = null;
        }
    }

    public static void StartPipeServer(Action onActivationRequested)
    {
        StopPipeServer();
        serverCts = new CancellationTokenSource();
        var ct = serverCts.Token;

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(ct);
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var message = await reader.ReadLineAsync(ct);
                    if (string.Equals(message, "OPEN_SETTINGS", StringComparison.Ordinal))
                        onActivationRequested();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(250, CancellationToken.None);
                }
            }
        }, ct);
    }

    public static void StopPipeServer()
    {
        if (serverCts == null) return;
        try
        {
            serverCts.Cancel();
            serverCts.Dispose();
        }
        catch
        {
        }
        serverCts = null;
    }

    private static void SignalPrimaryInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1200);
            using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            writer.WriteLine("OPEN_SETTINGS");
        }
        catch
        {
        }
    }

    private static string ComputeUserScopeSuffix()
    {
        var key = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash)[..16];
    }
}
