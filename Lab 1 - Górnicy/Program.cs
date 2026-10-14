using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Gornik
{
    internal class Program
    {
        static int coal = 0;
        static int source = 2000;
        static int vehicle = 200;
        static int coalTime = 10;
        static int depositTime = 10;
        static int travelTime = 1000;
        static int curr = 1;

        static string[] minerStatus;
        static SemaphoreSlim miningSema = new SemaphoreSlim(2, 2);
        static SemaphoreSlim depositSema = new SemaphoreSlim(1, 1);

        static object lockObject = new object();
        static object console = new object();

        static async Task Main(string[] args)
        {
            Console.Clear();

            int[] minersToTest = {1, 2, 3, 4, 5, 6};
            double baseTime = 0;

            foreach (int m in minersToTest)
            {
                curr = m;
                coal = 0;
                source = 2000;
                minerStatus = new string[curr];

                for (int i = 0; i < curr; i++)
                {
                    minerStatus[i] = "Idle";
                }

                Console.Clear();
                Console.WriteLine($"Liczba górników: {curr}");

                Stopwatch sw = Stopwatch.StartNew();

                Task[] tasks = new Task[curr];
                for (int i = 0; i < curr; i++)
                {
                    int minerid = i + 1;
                    tasks[i] = Task.Run(() => Mining(minerid));
                }

                bool isOn = true;
                Task displayTask = Task.Run(async () =>
                {
                    while (isOn)
                    {
                        drawDisplay();
                        await Task.Delay(100);
                    }
                });

                await Task.WhenAll(tasks);
                isOn = false;
                await displayTask;

                sw.Stop();
                double timeInSeconds = sw.ElapsedMilliseconds / 1000.0;
                if (curr == 1)
                {
                    baseTime = timeInSeconds;
                }

                double acc = baseTime / timeInSeconds;
                double efficiency = acc / curr;

                drawDisplay();
                Console.SetCursorPosition(0, curr + 3);
                Console.WriteLine($"\nWyniki dla {curr} górników:");
                Console.WriteLine($"Czas: {timeInSeconds} s");
                Console.WriteLine($"Przyśpieszenie: {acc}");
                Console.ReadKey();
            }

            Console.Clear();
            Console.WriteLine("Wszystkie testy zostały zakończone.");
            Console.CursorVisible = true;
        }

        static void Mining(int minerid)
        {
            while (true)
            {
                int minedCoal = 0;
                Update(minerid, "Czeka na wydobycie");
                miningSema.Wait();
                try
                {
                    lock (lockObject)
                    {
                        if (source <= 0)
                        {
                            break;
                        }
                        minedCoal = Math.Min(vehicle, source);
                        source -= minedCoal;
                    }

                    Update(minerid, $"Wydobywa {minedCoal} węgla");
                    Thread.Sleep(minedCoal * coalTime);
                }
                finally
                {
                    miningSema.Release();
                }

                Update(minerid, "Przewozi węgiel");
                Thread.Sleep(travelTime);

                Update(minerid, "Czeka na rozładunek");
                depositSema.Wait();
                try
                {
                    Update(minerid, "Rozładowuje węgiel");
                    Thread.Sleep(minedCoal * depositTime);

                    lock (lockObject)
                    {
                        coal += minedCoal;
                    }
                }
                finally
                {
                    depositSema.Release();
                }

                Update(minerid, "Wraca po węgiel");
                Thread.Sleep(travelTime);

                lock (lockObject)
                {
                    if (source <= 0) break;
                }
            }

            Update(minerid, "Zakończył pracę");
        }

        static void Update(int minerid, string status)
        {
            lock (console)
            {
                minerStatus[minerid - 1] = status;
            }
        }

        static void drawDisplay()
        {
            lock (console)
            {
                Console.SetCursorPosition(0, 1);
                Console.WriteLine($"Stan złoża: {source} jednostek węgla".PadRight(50));
                Console.WriteLine($"Stan magazynu: {coal} jednostek węgla".PadRight(50));

                for (int i = 0; i < curr; i++)
                {
                    Console.WriteLine($"Górnik {i + 1}: {minerStatus[i]}".PadRight(50));
                }
            }
        }
    }
}