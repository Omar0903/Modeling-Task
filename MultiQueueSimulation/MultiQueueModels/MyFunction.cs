using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiQueueModels
{
    public static class MyFunctions
    {
        private static Random rand = new Random();

        public static SimulationCase GenerateNextCustomer(
            int customerNumber,
            List<SimulationCase> simulationTable,
            List<Server> servers,
            List<TimeDistribution> interarrivalDist,
            Enums.SelectionMethod selectionMethod)
        {
            var c = new SimulationCase();
            c.CustomerNumber = customerNumber;

            c.RandomInterArrival = rand.Next(1, 101);
            c.InterArrival = GetInterarrivalTime(interarrivalDist, c.RandomInterArrival);
            c.ArrivalTime = simulationTable.Last().ArrivalTime + c.InterArrival;

            Server selectedServer = SelectServer(servers, selectionMethod, c.ArrivalTime);
            c.AssignedServer = selectedServer;

            c.RandomService = rand.Next(1, 101);
            c.ServiceTime = GetServiceTime(selectedServer.TimeDistribution, c.RandomService);

            if (c.ArrivalTime >= selectedServer.FinishTime)
            {
                c.StartTime = c.ArrivalTime;
                c.TimeInQueue = 0;
            }
            else
            {
                c.StartTime = selectedServer.FinishTime;
                c.TimeInQueue = selectedServer.FinishTime - c.ArrivalTime;
            }

            c.EndTime = c.StartTime + c.ServiceTime;

            selectedServer.TotalWorkingTime += c.ServiceTime;
            selectedServer.FinishTime = c.EndTime;

            return c;
        }

        public static Server SelectServer(List<Server> servers, Enums.SelectionMethod selectionMethod, int arrivalTime = 0)
        {
            if (selectionMethod == Enums.SelectionMethod.Random)
            {
                var freeServers = servers.Where(s => s.FinishTime <= arrivalTime).ToList();
                if (freeServers.Count > 0)
                    return freeServers[rand.Next(freeServers.Count)];

                return servers[rand.Next(servers.Count)];
            }
            else if (selectionMethod == Enums.SelectionMethod.HighestPriority)
            {
                var available = servers.FirstOrDefault(s => s.FinishTime <= arrivalTime);
                return available ?? servers.OrderBy(s => s.FinishTime).First();
            }
            else // ??? ???????
            {
                return servers.OrderBy(s => s.TotalWorkingTime).First();
            }
        }

        public static void CalculatePerformance(List<SimulationCase> table, List<Server> servers, PerformanceMeasures performance)
        {
            if (table.Count == 0) return;

            // -----------------------------
            // ?????? ????? ??????
            // -----------------------------
            performance.AverageWaitingTime = (decimal)table.Average(x => x.TimeInQueue);
            performance.WaitingProbability = (decimal)table.Count(x => x.TimeInQueue > 0) / table.Count;
            performance.MaxQueueLength = GetMaxQueueLength(table);

            // ?????? ??? ???????? (??? ???? ??? ?????)
            int totalSimulationTime = table.Max(c => c.EndTime);
            if (totalSimulationTime <= 0)
                totalSimulationTime = 1;

            // -----------------------------
            // ?????? ??? ?????
            // -----------------------------
            foreach (var s in servers)
            {
                var servedCases = table.Where(c => c.AssignedServer == s).ToList();

                if (servedCases.Count > 0)
                {
                    // ???? ???? ??????? ???????
                    s.Utilization = (decimal)s.TotalWorkingTime / totalSimulationTime;
                    s.IdleProbability = 1 - s.Utilization;

                    // ????? ??? ??????
                    s.AverageServiceTime = (decimal)s.TotalWorkingTime / servedCases.Count;
                }
                else
                {
                    // ??????? ?? ?????? ????
                    s.Utilization = 0;
                    s.IdleProbability = 1;
                    s.AverageServiceTime = 0;
                }
            }
        }



        public static void NormalizeDistributionProbabilities(List<TimeDistribution> distList)
        {
            if (distList == null || distList.Count == 0) return;

            decimal sum = distList.Sum(d => d.Probability);
            if (sum == 0m) return;
            if (Math.Abs((double)(sum - 1m)) < 0.0001) return;

            for (int i = 0; i < distList.Count; i++)
                distList[i].Probability /= sum;
        }

        public static int GetInterarrivalTime(List<TimeDistribution> interarrivalDist, int randomValue)
        {
            decimal cumulative = 0m;
            foreach (var dist in interarrivalDist)
            {
                cumulative += dist.Probability * 100m;
                if (randomValue <= cumulative)
                    return dist.Time;
            }
            return interarrivalDist.Last().Time;
        }

        public static int GetServiceTime(List<TimeDistribution> serviceDist, int randomValue)
        {
            decimal cumulative = 0m;
            foreach (var dist in serviceDist)
            {
                cumulative += dist.Probability * 100m;
                if (randomValue <= cumulative)
                    return dist.Time;
            }
            return serviceDist.Last().Time;
        }

        public static int GetMaxQueueLength(List<SimulationCase> table)
        {
            int maxQ = 0;

            var eventTimes = table
                .SelectMany(c => new[] { c.ArrivalTime, c.StartTime })
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            foreach (var t in eventTimes)
            {
                int waiting = table.Count(c => c.ArrivalTime <= t && c.StartTime > t);
                if (waiting > maxQ)
                    maxQ = waiting;
            }

            return maxQ;
        }
    }
}
