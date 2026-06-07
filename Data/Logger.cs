using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Data;

/// <summary>
/// @brief Singleton diagnostic logger with async buffered file writing.
/// Uses a ConcurrentQueue to decouple producers (balls/logic) from the file writer thread,
/// preventing slowdowns in real-time simulation when disk throughput is temporarily reduced.
/// </summary>
public sealed class Logger : IDisposable
{
    private static readonly Lazy<Logger> _instance =
        new(() => new Logger(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static Logger Instance => _instance.Value;

    private readonly ConcurrentQueue<string> _queue = new();
    private readonly Thread _writerThread;
    private volatile bool _isRunning = true;

    /// <summary>Path of the currently active log file.</summary>
    public string LogFilePath { get; }

    private Logger()
    {
        // One log file per application run, timestamped for easy identification.
        string fileName = $"logger-{DateTime.Now:yyyyMMdd_HHmmss}.log";
        LogFilePath = Path.Combine(
            AppContext.BaseDirectory,
            fileName);

        _writerThread = new Thread(WriterLoop)
        {
            IsBackground = true,
            Name = "LoggerThread",
            Priority = ThreadPriority.BelowNormal   
        };
        _writerThread.Start();
    }
    
    /// <summary>
    /// Enqueues a ball-move diagnostic entry (non-blocking).
    /// </summary>
    public void LogMove(int ballId, double posX, double posY,
                        double velX, double velY)
    {
        if (!_isRunning) return;
        _queue.Enqueue(BuildEntry(ballId, posX, posY, velX, velY, "Move"));
    }

    /// <summary>Enqueues a wall-collision entry.</summary>
    public void LogWallCollision(int ballId, double posX, double posY,
                                 double velX, double velY)
    {
        if (!_isRunning) return;
        _queue.Enqueue(BuildEntry(ballId, posX, posY, velX, velY, "WallCollision"));
    }

    /// <summary>Enqueues a ball-to-ball collision entry.</summary>
    public void LogBallCollision(int ballId, int otherBallId,
                                 double posX, double posY,
                                 double velX, double velY)
    {
        if (!_isRunning) return;
        string entry = BuildEntry(ballId, posX, posY, velX, velY,
                                  $"BallCollision(other={otherBallId})");
        _queue.Enqueue(entry);
    }

    /// <summary>Flushes remaining entries and stops the writer thread.</summary>
    public void Dispose()
    {
        _isRunning = false;
        _writerThread.Join(timeout: TimeSpan.FromSeconds(3));
    }
    
    /// <summary>
    /// Serialises one diagnostic record to ASCII text.
    /// </summary>
    private static string BuildEntry(int ballId,
                                     double posX, double posY,
                                     double velX, double velY,
                                     string eventName)
    {
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"[{DateTime.UtcNow:yyyy-MM-dd--HH:mm:ss.fff}]--[ID-{ballId}-{eventName}]" +
            $"Pos:[{posX:F2},{posY:F2}]|Vel:[{velX:F2},{velY:F2}]");
    }

    /// <summary>
    /// Background writer loop: drains the queue in batches and writes to file.
    /// When the queue is temporarily empty the thread yields; when the disk is
    /// slow the queue acts as a buffer, keeping producers non-blocking.
    /// </summary>
    private void WriterLoop()
    {
        try
        {
            using var writer = new StreamWriter(
                new FileStream(LogFilePath, FileMode.Create, FileAccess.Write,
                               FileShare.Read, bufferSize: 65536,
                               FileOptions.SequentialScan),
                Encoding.ASCII,
                leaveOpen: false);

            const int maxBatchSize = 256;
            var batch = new string[maxBatchSize];

            while (_isRunning || !_queue.IsEmpty)
            {
                int count = 0;

                while (count < maxBatchSize && _queue.TryDequeue(out string? entry))
                {
                    batch[count++] = entry!;
                }

                if (count == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }
                
                for (int i = 0; i < count; i++)
                    writer.WriteLine(batch[i]);

                writer.Flush();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logger] Writer thread error: {ex.Message}");
        }
    }
}