using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiQueueModels
{
    public class SimulationSystem
    {
        public SimulationSystem()
        {
            this.Servers = new List<Server>();
            this.InterarrivalDistribution = new List<TimeDistribution>();
            this.PerformanceMeasures = new PerformanceMeasures();
            this.SimulationTable = new List<SimulationCase>();

            // واحد Random لكل instance عشان نتجنّب إعادة التهيئة المتكررة
            this._rand = new Random();
        }

        // private random shared for the instance
        private Random _rand;

        ///////////// INPUTS ///////////// 
        public int NumberOfServers { get; set; }
        public int StoppingNumber { get; set; } // used for either number-of-customers or end-time
        public List<Server> Servers { get; set; }
        public List<TimeDistribution> InterarrivalDistribution { get; set; }
        public Enums.StoppingCriteria StoppingCriteria { get; set; }
        public Enums.SelectionMethod SelectionMethod { get; set; }

        ///////////// OUTPUTS /////////////
        public List<SimulationCase> SimulationTable { get; set; }
        public PerformanceMeasures PerformanceMeasures { get; set; }

        /// <summary>
        /// Run the simulation.
        /// If StoppingCriteria == NumberOfCustomers -> runs until StoppingNumber customers are generated.
        /// If StoppingCriteria == SimulationEndTime -> runs until the last end time >= StoppingNumber (interpreted as end time).
        /// </summary>
        public void RunSimulation()
        {
            // clear previous results
            SimulationTable.Clear();

            if (Servers == null || Servers.Count == 0)
                throw new InvalidOperationException("No servers defined in the system.");

            if (InterarrivalDistribution == null || InterarrivalDistribution.Count == 0)
                throw new InvalidOperationException("Interarrival distribution not defined.");

            // normalize probabilities (optional, but helps if user entered percentages not summing to 1)
            NormalizeDistributionProbabilities(InterarrivalDistribution);
            foreach (var s in Servers)
                NormalizeDistributionProbabilities(s.TimeDistribution);

            int customerNumber = 1;

            // FIRST CUSTOMER (arrival at time 0)
            SimulationCase firstCase = new SimulationCase();
            firstCase.CustomerNumber = customerNumber;
            firstCase.RandomInterArrival = 0;
            firstCase.InterArrival = 0;
            firstCase.ArrivalTime = 0;

            // choose server for first customer
            Server chosenServer = SelectServer(0);
            firstCase.RandomService = GenerateRandomService();
            firstCase.ServiceTime = GetServiceTime(chosenServer, firstCase.RandomService);

            firstCase.StartTime = 0;
            firstCase.EndTime = firstCase.StartTime + firstCase.ServiceTime;
            firstCase.TimeInQueue = 0;
            firstCase.AssignedServer = chosenServer;

            chosenServer.TotalWorkingTime += firstCase.ServiceTime;
            chosenServer.FinishTime = firstCase.EndTime;

            SimulationTable.Add(firstCase);

            // Decide which stopping mode to use
            if (StoppingCriteria == Enums.StoppingCriteria.NumberOfCustomers)
            {
                // generate until number of customers reached
                while (customerNumber < StoppingNumber)
                {
                    customerNumber++;
                    var currentCase = GenerateNextCustomer(customerNumber);
                    SimulationTable.Add(currentCase);
                }
            }
            else if (StoppingCriteria == Enums.StoppingCriteria.SimulationEndTime)
            {
                // generate until simulation clock (last end time) reaches StoppingNumber
                // continue generating customers until last end time >= StoppingNumber
                while (SimulationTable.Last().EndTime < StoppingNumber)
                {
                    customerNumber++;
                    var currentCase = GenerateNextCustomer(customerNumber);
                    SimulationTable.Add(currentCase);
                }
            }
            else
            {
                // Default: if not set, behave like NumberOfCustomers using StoppingNumber
                while (customerNumber < StoppingNumber)
                {
                    customerNumber++;
                    var currentCase = GenerateNextCustomer(customerNumber);
                    SimulationTable.Add(currentCase);
                }
            }

            // calculate performance at the end
            CalculatePerformance();
        }

        /// <summary>
        /// Generate next customer case given number.
        /// </summary>
        private SimulationCase GenerateNextCustomer(int customerNumber)
        {
            SimulationCase currentCase = new SimulationCase();
            currentCase.CustomerNumber = customerNumber;

            // Random interarrival as an integer 1..100 used for mapping
            int randInter = _rand.Next(1, 100);
            currentCase.RandomInterArrival = randInter;
            currentCase.InterArrival = GetInterarrivalTime(randInter);

            // Arrival time = last arrival + interarrival
            currentCase.ArrivalTime = SimulationTable.Last().ArrivalTime + currentCase.InterArrival;

            // choose server based on selection policy and arrival time
            Server chosenServer = SelectServer(currentCase.ArrivalTime);
            currentCase.AssignedServer = chosenServer;

            // random service and service time using server distribution
            int randService = _rand.Next(1, 101);
            currentCase.RandomService = randService;
            currentCase.ServiceTime = GetServiceTime(chosenServer, randService);

            // determine start time and waiting
            if (currentCase.ArrivalTime >= chosenServer.FinishTime)
            {
                currentCase.StartTime = currentCase.ArrivalTime;
                currentCase.TimeInQueue = 0;
            }
            else
            {
                currentCase.StartTime = chosenServer.FinishTime;
                currentCase.TimeInQueue = chosenServer.FinishTime - currentCase.ArrivalTime;
            }

            // end time and update server status
            currentCase.EndTime = currentCase.StartTime + currentCase.ServiceTime;

            chosenServer.TotalWorkingTime += currentCase.ServiceTime;
            chosenServer.FinishTime = currentCase.EndTime;

            return currentCase;
        }

        /// <summary>
        /// Select server depending on selection method.
        /// If random: prefer available servers at arrival time, otherwise any server.
        /// If highest priority: pick first server free at arrival, otherwise server 1.
        /// If least utilization: pick server with smallest total working time.
        /// </summary>
        private Server SelectServer(int arrivalTime = 0)
        {
            if (Servers == null || Servers.Count == 0)
                throw new InvalidOperationException("No servers defined.");

            if (SelectionMethod == Enums.SelectionMethod.Random)
            {
                var available = Servers.Where(s => s.FinishTime <= arrivalTime).ToList();
                if (available.Count > 0)
                    return available[_rand.Next(available.Count)];
                return Servers[_rand.Next(Servers.Count)];
            }
            else if (SelectionMethod == Enums.SelectionMethod.HighestPriority)
            {
                // priority: server order as in list (1,2,3,...)
                foreach (var s in Servers)
                {
                    if (s.FinishTime <= arrivalTime)
                        return s;
                }
                return Servers.First();
            }
            else // LeastUtilization
            {
                return Servers.OrderBy(s => s.TotalWorkingTime).First();
            }
        }

        /// <summary>
        /// Generate a random integer 1..100 for service selection.
        /// Using shared Random instance.
        /// </summary>
        private int GenerateRandomService()
        {
            return _rand.Next(1, 101);
        }

        /// <summary>
        /// Get service time from server distribution using 1..100 randomValue.
        /// Probabilities in distribution are expected in 0..1 (sum ~1) or as decimals.
        /// This function compares randomValue to cumulative*(100).
        /// </summary>
        private int GetServiceTime(Server server, int randomValue)
        {
            if (server == null || server.TimeDistribution == null || server.TimeDistribution.Count == 0)
                throw new InvalidOperationException("Server distribution not defined.");

            decimal cumulative = 0m;
            foreach (var dist in server.TimeDistribution)
            {
                cumulative += dist.Probability * 100m; // convert to 0..100 scale
                if (randomValue <= cumulative)
                    return dist.Time;
            }

            // fallback to last defined time
            return server.TimeDistribution.Last().Time;
        }

        /// <summary>
        /// Get interarrival time from distribution using 1..100 randomValue.
        /// </summary>
        private int GetInterarrivalTime(int randomValue)
        {
            if (InterarrivalDistribution == null || InterarrivalDistribution.Count == 0)
                throw new InvalidOperationException("Interarrival distribution not defined.");

            decimal cumulative = 0m;
            foreach (var dist in InterarrivalDistribution)
            {
                cumulative += dist.Probability * 100m;
                if (randomValue <= cumulative)
                    return dist.Time;
            }

            return InterarrivalDistribution.Last().Time;
        }

        /// <summary>
        /// Ensure probabilities sum to 1 (or close). If not, normalize them.
        /// Useful when user input may be percentages or not normalized.
        /// </summary>
        private void NormalizeDistributionProbabilities(List<TimeDistribution> distList)
        {
            if (distList == null || distList.Count == 0) return;

            decimal sum = distList.Sum(d => d.Probability);
            if (sum == 0m) return;

            // If already ~1 (tolerance) do nothing
            if (Math.Abs((double)(sum - 1m)) < 0.0001) return;

            // Otherwise normalize
            for (int i = 0; i < distList.Count; i++)
            {
                distList[i].Probability = distList[i].Probability / sum;
            }
        }

        /// <summary>
        /// Calculate performance measures after simulation.
        /// </summary>
        private void CalculatePerformance()
        {
            if (SimulationTable == null || SimulationTable.Count == 0) return;

            // Average waiting (TimeInQueue)
            PerformanceMeasures.AverageWaitingTime =
                (decimal)SimulationTable.Average(x => x.TimeInQueue);

            // Waiting probability
            PerformanceMeasures.WaitingProbability =
                (decimal)SimulationTable.Count(x => x.TimeInQueue > 0) / SimulationTable.Count;

            // Max queue length (here interpreted as max number of customers waiting at any arrival; simplified)
            PerformanceMeasures.MaxQueueLength =
                SimulationTable.Max(x => x.TimeInQueue > 0 ? 1 : 0);

            int totalEndTime = SimulationTable.Last().EndTime;
            if (totalEndTime <= 0) totalEndTime = 1; // avoid division by zero

            foreach (var s in Servers)
            {
                s.Utilization = (decimal)s.TotalWorkingTime / totalEndTime;
                s.IdleProbability = 1m - s.Utilization;
            }
        }
    }
}
