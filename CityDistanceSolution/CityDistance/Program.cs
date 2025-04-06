using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CityRouteOptimization
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("City Route Optimization Program");
            Console.WriteLine("===============================");

            // Load cities and distances
            CityManager cityManager = new CityManager();
            cityManager.LoadFromFile("miasta.txt");

            bool running = true;
            while (running)
            {
                Console.WriteLine("\nOptions:");
                Console.WriteLine("1. Display cities and distances");
                Console.WriteLine("2. Find route using Nearest Neighbor");
                Console.WriteLine("3. Find route using Genetic Algorithm with HGreX");
                Console.WriteLine("4. Exit");
                Console.Write("\nSelect option: ");

                string input = Console.ReadLine();
                Console.WriteLine();

                switch (input)
                {
                    case "1":
                        cityManager.DisplayCitiesAndDistances();
                        break;
                    case "2":
                        RunNearestNeighbor(cityManager);
                        break;
                    case "3":
                        RunGeneticAlgorithm(cityManager);
                        break;
                    case "4":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        static void RunNearestNeighbor(CityManager cityManager)
        {
            Console.Write("Enter starting city: ");
            string startCity = Console.ReadLine();

            try
            {
                var (route, distance) = cityManager.FindRouteWithNearestNeighbor(startCity);
                Console.WriteLine("\nRoute using Nearest Neighbor:");
                Console.WriteLine(string.Join(" -> ", route));
                Console.WriteLine($"Total distance: {distance} km");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void RunGeneticAlgorithm(CityManager cityManager)
        {
            Console.Write("Enter starting city: ");
            string startCity = Console.ReadLine();

            Console.Write("Population size (recommended 50-100): ");
            if (!int.TryParse(Console.ReadLine(), out int populationSize) || populationSize < 10)
                populationSize = 50;

            Console.Write("Number of generations (recommended 100-1000): ");
            if (!int.TryParse(Console.ReadLine(), out int generations) || generations < 10)
                generations = 100;

            try
            {
                var geneticAlgorithm = new GeneticAlgorithm(cityManager, startCity, populationSize);
                var (route, distance) = geneticAlgorithm.Run(generations);

                Console.WriteLine("\nRoute using Genetic Algorithm with HGreX:");
                Console.WriteLine(string.Join(" -> ", route));
                Console.WriteLine($"Total distance: {distance} km");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    class CityManager
    {
        private List<string> cities = new List<string>();
        private int[,] distances;

        public void LoadFromFile(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);

                // Process each line
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        cities.Add(parts[0]);
                    }
                }

                // Initialize distances matrix
                int cityCount = cities.Count;
                distances = new int[cityCount, cityCount];

                // Fill distances matrix
                for (int i = 0; i < cityCount; i++)
                {
                    string[] parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 1; j < parts.Length && j <= cityCount; j++)
                    {
                        if (int.TryParse(parts[j], out int distance))
                        {
                            distances[i, j - 1] = distance;
                        }
                    }
                }

                Console.WriteLine($"Loaded {cityCount} cities from file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading file: {ex.Message}");
            }
        }

        public void DisplayCitiesAndDistances()
        {
            if (cities.Count == 0)
            {
                Console.WriteLine("No cities loaded.");
                return;
            }

            Console.WriteLine("Cities:");
            for (int i = 0; i < cities.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {cities[i]}");
            }

            Console.WriteLine("\nDistance Matrix:");

            // Header row with city names
            Console.Write("          ");
            foreach (var city in cities)
            {
                Console.Write($"{city.PadRight(10)} ");
            }
            Console.WriteLine();

            // Distances
            for (int i = 0; i < cities.Count; i++)
            {
                Console.Write($"{cities[i].PadRight(10)} ");
                for (int j = 0; j < cities.Count; j++)
                {
                    Console.Write($"{distances[i, j].ToString().PadRight(10)} ");
                }
                Console.WriteLine();
            }
        }

        public (List<string>, int) FindRouteWithNearestNeighbor(string startCity)
        {
            int startIndex = cities.IndexOf(startCity);
            if (startIndex == -1)
                throw new Exception($"City '{startCity}' not found.");

            int cityCount = cities.Count;
            List<string> route = new List<string>();
            bool[] visited = new bool[cityCount];
            int totalDistance = 0;
            int currentCity = startIndex;

            route.Add(cities[currentCity]);
            visited[currentCity] = true;

            // Visit each city
            for (int i = 1; i < cityCount; i++)
            {
                int nearestCity = -1;
                int shortestDistance = int.MaxValue;

                // Find the nearest unvisited city
                for (int j = 0; j < cityCount; j++)
                {
                    if (!visited[j] && distances[currentCity, j] < shortestDistance && distances[currentCity, j] > 0)
                    {
                        nearestCity = j;
                        shortestDistance = distances[currentCity, j];
                    }
                }

                if (nearestCity == -1)
                    break;

                route.Add(cities[nearestCity]);
                visited[nearestCity] = true;
                totalDistance += shortestDistance;
                currentCity = nearestCity;
            }

            // Return to the starting city to complete the circuit
            totalDistance += distances[currentCity, startIndex];
            route.Add(cities[startIndex]);

            return (route, totalDistance);
        }

        public int GetDistance(int cityIndex1, int cityIndex2)
        {
            return distances[cityIndex1, cityIndex2];
        }

        public int GetCityCount()
        {
            return cities.Count;
        }

        public string GetCityName(int index)
        {
            return cities[index];
        }

        public int GetCityIndex(string cityName)
        {
            return cities.IndexOf(cityName);
        }
    }

    class GeneticAlgorithm
    {
        private CityManager cityManager;
        private int startCityIndex;
        private List<int[]> population;
        private int populationSize;
        private Random random = new Random();

        // GA parameters
        private double mutationRate = 0.01;
        private int tournamentSize = 5;
        private int eliteCount = 2;

        public GeneticAlgorithm(CityManager cityManager, string startCity, int populationSize = 50)
        {
            this.cityManager = cityManager;
            this.startCityIndex = cityManager.GetCityIndex(startCity);

            if (this.startCityIndex == -1)
                throw new Exception($"City '{startCity}' not found.");

            this.populationSize = populationSize;

            InitializePopulation();
        }

        private void InitializePopulation()
        {
            population = new List<int[]>();
            int cityCount = cityManager.GetCityCount();

            for (int i = 0; i < populationSize; i++)
            {
                int[] chromosome = new int[cityCount];

                // Start with the selected city
                chromosome[0] = startCityIndex;

                // Generate a random permutation for the remaining cities
                List<int> remainingCities = new List<int>();
                for (int j = 0; j < cityCount; j++)
                {
                    if (j != startCityIndex)
                        remainingCities.Add(j);
                }

                for (int j = 0; j < remainingCities.Count; j++)
                {
                    int k = random.Next(j, remainingCities.Count);
                    int temp = remainingCities[j];
                    remainingCities[j] = remainingCities[k];
                    remainingCities[k] = temp;
                }

                // Place the remaining cities in the chromosome
                for (int j = 0; j < remainingCities.Count; j++)
                {
                    chromosome[j + 1] = remainingCities[j];
                }

                population.Add(chromosome);
            }
        }

        public (List<string>, int) Run(int generations)
        {
            for (int gen = 0; gen < generations; gen++)
            {
                List<int[]> newPopulation = new List<int[]>();

                // Elitism - copy the best chromosomes directly to the new population
                var sortedPopulation = population.OrderBy(p => CalculateDistance(p)).ToList();
                for (int i = 0; i < eliteCount; i++)
                {
                    newPopulation.Add((int[])sortedPopulation[i].Clone());
                }

                // Create new individuals through crossover and mutation
                while (newPopulation.Count < populationSize)
                {
                    int[] parent1 = TournamentSelection();
                    int[] parent2 = TournamentSelection();

                    int[] child = HGreXCrossover(parent1, parent2);

                    if (random.NextDouble() < mutationRate)
                    {
                        Mutate(child);
                    }

                    newPopulation.Add(child);
                }

                population = newPopulation;

                // Display progress
                if (gen % 10 == 0 || gen == generations - 1)
                {
                    int bestDistance = CalculateDistance(sortedPopulation[0]);
                    Console.WriteLine($"Generation {gen}: Best distance = {bestDistance} km");
                }
            }

            // Find the best route
            int[] bestChromosome = population.OrderBy(p => CalculateDistance(p)).First();
            int totalDistance = CalculateDistance(bestChromosome);

            // Convert chromosome to city names
            List<string> route = new List<string>();
            foreach (int cityIndex in bestChromosome)
            {
                route.Add(cityManager.GetCityName(cityIndex));
            }

            // Add the starting city at the end to complete the route
            route.Add(cityManager.GetCityName(startCityIndex));

            return (route, totalDistance);
        }

        // HGreX (Heuristic Greedy Crossover) as explained in the paper
        private int[] HGreXCrossover(int[] parent1, int[] parent2)
        {
            int cityCount = cityManager.GetCityCount();
            int[] child = new int[cityCount];
            bool[] used = new bool[cityCount];

            // Always start with the same city as the parents
            child[0] = startCityIndex;
            used[startCityIndex] = true;

            // For each position in the child
            for (int i = 1; i < cityCount; i++)
            {
                int lastCity = child[i - 1];

                // Find the cities after the last selected city in both parents
                int next1 = FindNextCity(parent1, lastCity);
                int next2 = FindNextCity(parent2, lastCity);

                // If both are already used or not found, select a random unused city
                if ((next1 == -1 || used[next1]) && (next2 == -1 || used[next2]))
                {
                    List<int> availableCities = new List<int>();
                    for (int j = 0; j < cityCount; j++)
                    {
                        if (!used[j])
                            availableCities.Add(j);
                    }

                    if (availableCities.Count > 0)
                    {
                        // With HGreX, select the nearest available city when no parent suggestions available
                        int nearestCity = -1;
                        int shortestDistance = int.MaxValue;

                        foreach (int city in availableCities)
                        {
                            int distance = cityManager.GetDistance(lastCity, city);
                            if (distance < shortestDistance)
                            {
                                shortestDistance = distance;
                                nearestCity = city;
                            }
                        }

                        child[i] = nearestCity;
                        used[nearestCity] = true;
                    }
                }
                // If only one is available, use it
                else if (next1 == -1 || used[next1])
                {
                    child[i] = next2;
                    used[next2] = true;
                }
                else if (next2 == -1 || used[next2])
                {
                    child[i] = next1;
                    used[next1] = true;
                }
                // If both are available, choose the one with shorter distance (greedy approach)
                else
                {
                    int dist1 = cityManager.GetDistance(lastCity, next1);
                    int dist2 = cityManager.GetDistance(lastCity, next2);

                    if (dist1 <= dist2)
                    {
                        child[i] = next1;
                        used[next1] = true;
                    }
                    else
                    {
                        child[i] = next2;
                        used[next2] = true;
                    }
                }
            }

            return child;
        }

        private int FindNextCity(int[] parent, int city)
        {
            for (int i = 0; i < parent.Length - 1; i++)
            {
                if (parent[i] == city)
                    return parent[i + 1];
            }
            return -1;  // Not found or is the last city
        }

        private void Mutate(int[] chromosome)
        {
            // Skip the first city (starting city)
            int pos1 = random.Next(1, chromosome.Length);
            int pos2 = random.Next(1, chromosome.Length);

            // Swap two cities
            int temp = chromosome[pos1];
            chromosome[pos1] = chromosome[pos2];
            chromosome[pos2] = temp;
        }

        private int[] TournamentSelection()
        {
            int[] best = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < tournamentSize; i++)
            {
                int[] candidate = population[random.Next(population.Count)];
                int distance = CalculateDistance(candidate);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return (int[])best.Clone();
        }

        private int CalculateDistance(int[] chromosome)
        {
            int distance = 0;

            for (int i = 0; i < chromosome.Length - 1; i++)
            {
                distance += cityManager.GetDistance(chromosome[i], chromosome[i + 1]);
            }

            // Add distance back to starting city
            distance += cityManager.GetDistance(chromosome[chromosome.Length - 1], chromosome[0]);

            return distance;
        }
    }
}