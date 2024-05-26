using Progress;

// Immediate progress increase
using (ProgressBar progress = new("Static progress"))
{
    const int nMax = 105;
    for (int i = 0; i <= nMax; i++)
    {
        // Incrementing progress based on n of subprocess
        progress.Report((i + 1) * 100 / nMax);
        // Main task
        Thread.Sleep(30);
    }
}

// Incremental progress increase
// To generate a sensation of movement in processes that cannot be fragmented into small steps
using (ProgressBar progress = new("Estimated progress"))
{
    for (int i = 10; i <= 100; i += 10)
    {
        // Increasing progress to 'nextProgress'% in 'estimatedTime' milliseconds
        _ = Task.Run(() => progress.Report(i, 1000));
        // Main task
        Thread.Sleep(900);
        //Releasing the threat and updating the progress
        progress.FinishReport();
    }
}
