namespace CSharpExercises.Simulations;

public sealed record QueueReport(int Customers, double AverageWait, double MaxWait, double Utilisation);

/// <summary>
/// A discrete-event simulation of customers queueing for k servers (tellers, checkout lanes, worker threads).
/// Instead of ticking a clock, jump straight to the next event: an arrival or a server becoming free. Shows the
/// counter-intuitive result of queueing theory: as utilisation approaches 100%, waiting time explodes.
/// </summary>
public static class QueueSimulation
{
    /// <param name="arrivalsPerMinute">Average arrival rate (arrivals are random, a Poisson process).</param>
    /// <param name="minutesPerService">Average service time (exponentially distributed).</param>
    public static QueueReport Run(int servers, double arrivalsPerMinute, double minutesPerService, int customers, Random random)
    {
        // Each server's next free time; the earliest one takes the next customer.
        var freeAt = new PriorityQueue<int, double>();
        for (var s = 0; s < servers; s++) freeAt.Enqueue(s, 0);

        double clock = 0, totalWait = 0, maxWait = 0, busyTime = 0, lastDeparture = 0;
        for (var i = 0; i < customers; i++)
        {
            clock += Exponential(1 / arrivalsPerMinute, random);
            freeAt.TryDequeue(out var server, out var available);

            var start = Math.Max(clock, available);
            var wait = start - clock;
            var service = Exponential(minutesPerService, random);

            totalWait += wait;
            maxWait = Math.Max(maxWait, wait);
            busyTime += service;
            lastDeparture = Math.Max(lastDeparture, start + service);
            freeAt.Enqueue(server, start + service);
        }

        return new QueueReport(customers, totalWait / customers, maxWait, busyTime / (servers * lastDeparture));
    }

    /// <summary>For one server the textbook (M/M/1) answer is known: average wait = ρ / (μ - λ).</summary>
    public static double TheoreticalWaitSingleServer(double arrivalsPerMinute, double minutesPerService)
    {
        var serviceRate = 1 / minutesPerService;
        var rho = arrivalsPerMinute / serviceRate;
        if (rho >= 1) return double.PositiveInfinity; // the queue grows forever
        return rho / (serviceRate - arrivalsPerMinute);
    }

    /// <summary>Inverse transform sampling: -mean · ln(U) is exponentially distributed.</summary>
    private static double Exponential(double mean, Random random) => -mean * Math.Log(1 - random.NextDouble());
}
