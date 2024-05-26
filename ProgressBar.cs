//using System.Dynamic;
using System.Text;

// An ASCII progress bar

namespace Progress;

public class ProgressBar : IDisposable, IProgress<int>
{
    private const int _BLOCK_COUNT = 10;
    private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    private const string _ANIMATION = @"|||//--\\";
    private readonly Timer _timer;
    private int _currentProgress;
    private string _currentText = string.Empty;
    private bool _disposed;
    private int _animationIndex;
    private bool _subProgressCompleted;

    public ProgressBar(string taskName)
    {
        Console.Write($"{taskName}... ");
        _timer = new Timer(TimerHandler);

        // A progress bar is only for temporary display in a console window.
        // If the console output is redirected to a file, draw nothing.
        // Otherwise, we'll end up with a lot of garbage in the target file.
        if (!Console.IsOutputRedirected)
        {
            _ = _timer.Change(_animationInterval, _animationInterval);
        }
    }

    public void Report(int value)
    {
        /*
        Increasing progress based on n of subprocess

        Parameters:
        - subProgress -> % of the progress bar when this subtask is completed
        */

        // Make sure value is in [0..100] range
        value = Math.Max(0, Math.Min(100, value));

        _ = Interlocked.Exchange(ref _currentProgress, value);
    }

    public void Report(int nextProgress, int estimatedTime)
    {
        /*
        Increasing progress on an estimated basis while the task is running

        Parameters:
        - nextProgress -> % of the progress bar when this subtask finishes
        - estimatedTime -> Estimated time in milliseconds for this subtask to complete
        */

        // Waiting for a minimum increase of 1%
        if (nextProgress - _currentProgress < 1)
        {
            return;
        }

        // Timing the progress
        int incrementTime = estimatedTime / (nextProgress - _currentProgress);

        _subProgressCompleted = false;

        for (int i = _currentProgress; i <= nextProgress; i++)
        {
            if (_subProgressCompleted)
            {
                i = nextProgress;
            }
            else
            {
                Thread.Sleep(incrementTime);
            }

            Report(i);
        }
    }

    public void FinishReport()
    {
        /*
        Use it if the task is completed before the stipulated time
        */
        _subProgressCompleted = true;
    }

    private void TimerHandler(object? state)
    {
        lock (_timer)
        {
            if (_disposed)
            {
                return;
            }

            int progressBlockCount = _currentProgress * _BLOCK_COUNT / 100;

            string text = string.Format(
                "[{0}{1}] {2,3}% {3}",
                new string('#', progressBlockCount),
                new string('-', _BLOCK_COUNT - progressBlockCount),
                _currentProgress,
                _ANIMATION[_animationIndex++ % _ANIMATION.Length]
            );
            UpdateText(text);
        }
    }

    private void UpdateText(string text)
    {
        // Get length of common portion
        int commonPrefixLength = 0;
        int commonLength = Math.Min(_currentText.Length, text.Length);
        while (
            commonPrefixLength < commonLength
            && text[commonPrefixLength] == _currentText[commonPrefixLength]
        )
        {
            commonPrefixLength++;
        }

        // Backtrack to the first differing character
        StringBuilder outputBuilder = new();
        _ = outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

        // Output new suffix
        _ = outputBuilder.Append(text.AsSpan(commonPrefixLength));

        // If the new text is shorter than the old one: delete overlapping characters
        int overlapCount = _currentText.Length - text.Length;
        if (overlapCount > 0)
        {
            _ = outputBuilder.Append(' ', overlapCount);
            _ = outputBuilder.Append('\b', overlapCount);
        }

        Console.Write(outputBuilder);
        _currentText = text;
    }

    public void Dispose()
    {
        lock (_timer)
        {
            _disposed = true;
            UpdateText(string.Empty);
            _timer.Dispose();
            Console.WriteLine("Done.");
            GC.SuppressFinalize(this);
        }
    }
}
