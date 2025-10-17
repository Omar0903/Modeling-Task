using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiQueueModels
{
    public class SimulationSystem
    {
        private Random rand = new Random();

        public SimulationSystem()
        {
            Servers = new List<Server>();
            InterarrivalDistribution = new List<TimeDistribution>();
            PerformanceMeasures = new PerformanceMeasures();
            SimulationTable = new List<SimulationCase>();
        }

        // ----- INPUTS -----
        public int NumberOfServers { get; set; }
        public int StoppingNumber { get; set; }
        public List<Server> Servers { get; set; }
        public List<TimeDistribution> InterarrivalDistribution { get; set; }
        public Enums.StoppingCriteria StoppingCriteria { get; set; }
        public Enums.SelectionMethod SelectionMethod { get; set; }

        // ----- OUTPUTS -----
        public List<SimulationCase> SimulationTable { get; set; }
        public PerformanceMeasures PerformanceMeasures { get; set; }

        public void RunSimulation()
        {
            SimulationTable.Clear();

            if (Servers == null || Servers.Count == 0)
                throw new InvalidOperationException("No servers defined.");

            if (InterarrivalDistribution == null || InterarrivalDistribution.Count == 0)
                throw new InvalidOperationException("Interarrival distribution not defined.");

            MyFunctions.NormalizeDistributionProbabilities(InterarrivalDistribution);
            foreach (var s in Servers)
                MyFunctions.NormalizeDistributionProbabilities(s.TimeDistribution);

            int customerNumber = 1;

            // العميل الأول
            SimulationCase firstCase = new SimulationCase
            {
                CustomerNumber = customerNumber,
                RandomInterArrival = 1,
                InterArrival = 0,
                ArrivalTime = 0
            };

            Server selectedServer = MyFunctions.SelectServer(Servers, SelectionMethod, 0);
            firstCase.RandomService = rand.Next(1, 101);
            firstCase.ServiceTime = MyFunctions.GetServiceTime(selectedServer.TimeDistribution, firstCase.RandomService);

            firstCase.StartTime = 0;
            firstCase.EndTime = firstCase.ServiceTime;
            firstCase.TimeInQueue = 0;
            firstCase.AssignedServer = selectedServer;

            selectedServer.TotalWorkingTime += firstCase.ServiceTime;
            selectedServer.FinishTime = firstCase.EndTime;

            SimulationTable.Add(firstCase);

            // باقي العملاء
            while (true)
            {
                if (StoppingCriteria == Enums.StoppingCriteria.NumberOfCustomers)
                {
                    if (customerNumber >= StoppingNumber)
                        break;
                }
                else if (StoppingCriteria == Enums.StoppingCriteria.SimulationEndTime)
                {
                    if (SimulationTable.Last().EndTime >= StoppingNumber)
                        break;
                }

                customerNumber++;
                var currentCase = MyFunctions.GenerateNextCustomer(
                    customerNumber, SimulationTable, Servers, InterarrivalDistribution, SelectionMethod);

                SimulationTable.Add(currentCase);
            }

            MyFunctions.CalculatePerformance(SimulationTable, Servers, PerformanceMeasures);
        }
    }
}
