using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RcCar.WebApi;

public sealed class CameraService(ILogger<CameraService> logger, IOptions<CameraOptions> options) : IAsyncDisposable
{
    private readonly ILogger<CameraService> logger = logger;
    private readonly IOptions<CameraOptions> options = options;

    private readonly ConcurrentDictionary<Guid, Channel<ReadOnlyMemory<byte>>> channels = [];

    private readonly SemaphoreSlim cameraSemaphore = new(1);
    private (Task, CancellationTokenSource)? cameraTask = null;

    public async Task<CameraReader?> GetCameraReader(CancellationToken cancellationToken) =>
        await CreateCameraReader(cancellationToken);

    private async Task<CameraReader?> CreateCameraReader(CancellationToken cancellationToken)
    {
        var cameraStarted = await StartCamera(cancellationToken);
        if (!cameraStarted)
        {
            return null;
        }
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<ReadOnlyMemory<byte>>(
            new BoundedChannelOptions(1)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            }
        );
        if (!channels.TryAdd(id, channel))
        {
            throw new Exception();
        }
        return new(channel, () => RemoveChannel(id));
    }

    private async Task RemoveChannel(Guid id)
    {
        if (!channels.TryRemove(id, out _))
        {
            throw new CameraConsumersException($"Could not remove {id}, this should not be possible.");
        }
        await StopCamera();
    }

    private async Task<bool> StartCamera(CancellationToken cancellationToken)
    {
        await cameraSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (cameraTask is not null)
            {
                return true;
            }

            logger.LogInformation("Starting camera feed");
            var cts = new CancellationTokenSource();

            if (StartCameraProcess() is not Process process)
            {
                logger.LogWarning("Could not start camera process");
                return false;
            }

            var task = Task.Run(() => CameraLoop(process, cts.Token), cts.Token);
            cameraTask = (task, cts);
            return true;
        }
        finally
        {
            cameraSemaphore.Release();
        }
    }

    private Process? StartCameraProcess()
    {
        var arguments = new List<string> { "--output", "-", "--codec", "mjpeg", "--nopreview", "--timeout", "0" };

        if (options.Value.Height is not 0)
        {
            arguments.Add("--height");
            arguments.Add(options.Value.Height.ToString());
        }
        if (options.Value.Width is not 0)
        {
            arguments.Add("--width");
            arguments.Add(options.Value.Width.ToString());
        }

        if (options.Value.HFlip)
        {
            arguments.Add("--hflip");
        }
        if (options.Value.VFlip)
        {
            arguments.Add("--vflip");
        }

        arguments.AddRange(options.Value.AdditionalCameraArgs);

        return Process.Start(
            new ProcessStartInfo("rpicam-vid", arguments)
            {
                RedirectStandardError = !options.Value.EmitCameraLogs,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            }
        );
    }

    private void CameraLoop(Process process, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var frame in ReadNextJPEGFrame(process.StandardOutput.BaseStream, cancellationToken))
            {
                foreach (var channel in channels.Values)
                {
                    var didWrite = channel.Writer.TryWrite(frame);
                    if (!didWrite)
                    {
                        logger.LogWarning("Failed writing frame to channel");
                    }
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Camera stream ended");
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
            process.Dispose();
        }
    }

    private IEnumerable<ReadOnlyMemory<byte>> ReadNextJPEGFrame(Stream stream, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> startSequence = new byte[] { 0xFF, 0xD8 };
        ReadOnlyMemory<byte> endSequence = new byte[] { 0xFF, 0xD9 };

        var memoryOwner = MemoryPool<byte>.Shared.Rent(options.Value.InitialBufferSize);

        try
        {
            var isInside = false;
            int position = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (memoryOwner.Memory[position..].IsEmpty)
                {
                    var newLength = (int)double.Ceiling(memoryOwner.Memory.Length * 1.5);
                    logger.LogDebug(
                        "Ran out of memory, increasing from {CurrentLength} bytes to {AskedForLength} bytes",
                        memoryOwner.Memory.Length,
                        newLength
                    );
                    var newMemoryOwner = MemoryPool<byte>.Shared.Rent(newLength);
                    logger.LogDebug("Received new memory, {NewLength} bytes", newMemoryOwner.Memory.Length);
                    memoryOwner.Memory.CopyTo(newMemoryOwner.Memory);
                    memoryOwner.Dispose();
                    memoryOwner = newMemoryOwner;
                }
                var bytesRead = stream.Read(memoryOwner.Memory[position..].Span);
                if (bytesRead is 0)
                {
                    yield break;
                }
                position += bytesRead;
                if (!isInside)
                {
                    var startIndex = memoryOwner.Memory[..position].Span.IndexOf(startSequence.Span);
                    if (startIndex is not -1)
                    {
                        memoryOwner.Memory[startIndex..].CopyTo(memoryOwner.Memory);
                        position -= startIndex;
                        isInside = true;
                    }
                }
                if (isInside)
                {
                    var endIndex = memoryOwner.Memory[..position].Span.IndexOf(endSequence.Span);
                    if (endIndex is not -1)
                    {
                        endIndex += endSequence.Length;
                        yield return memoryOwner.Memory[..endIndex].ToArray();
                        memoryOwner.Memory[endIndex..].CopyTo(memoryOwner.Memory);
                        position -= endIndex;
                        isInside = false;
                    }
                }
            }
        }
        finally
        {
            memoryOwner.Dispose();
        }
    }

    private async Task StopCamera()
    {
        await cameraSemaphore.WaitAsync();
        try
        {
            if (!channels.IsEmpty || cameraTask is not (var task, var cts))
            {
                return;
            }

            logger.LogInformation("Stopping camera feed");
            await cts.CancelAsync();
            try
            {
                await task;
            }
            catch (OperationCanceledException) { }
            cameraTask = null;
        }
        finally
        {
            cameraSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopCamera();
    }
}

public sealed class CameraReader(ChannelReader<ReadOnlyMemory<byte>> reader, Func<Task> disposeAction)
    : IAsyncDisposable
{
    private bool hasBeenDisposed = false;

    public ValueTask<ReadOnlyMemory<byte>> ReadFrameAsync(CancellationToken cancellationToken) =>
        reader.ReadAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (!hasBeenDisposed)
        {
            hasBeenDisposed = true;
            await disposeAction();
        }
    }
}

public class CameraOptions
{
    [Range(0, int.MaxValue)]
    public int Width { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int Height { get; set; } = 0;

    public bool VFlip { get; set; }
    public bool HFlip { get; set; }

    [Range(1, int.MaxValue)]
    public int InitialBufferSize { get; set; } = 12_000;

    public string[] AdditionalCameraArgs { get; set; } = [];

    public bool EmitCameraLogs { get; set; } = false;
}

[OptionsValidator]
public partial class CameraOptionsValidator : IValidateOptions<CameraOptions>;

public static class CameraServiceCollectionExtensions
{
    public static IServiceCollection AddCamera(this IServiceCollection services)
    {
        services.AddOptions<CameraOptions>().BindConfiguration("Camera");
        services.AddTransient<IValidateOptions<CameraOptions>, CameraOptionsValidator>();
        services.AddSingleton<CameraService>();
        return services;
    }
}

public class CameraConsumersException(string message) : Exception(message);
