using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class CityManager
{
    private List<string> cities = new List<string>();
    private int[,] distances;

    public void LoadFromFile(string filePath)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    cities.Add(parts[0]);
                }
            }

            int cityCount = cities.Count;
            distances = new int[cityCount, cityCount];

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

        for (int i = 1; i < cityCount; i++)
        {
            int nearestCity = -1;
            int shortestDistance = int.MaxValue;

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

        if (currentCity != startIndex && distances[currentCity, startIndex] > 0)
        {
            totalDistance += distances[currentCity, startIndex];
        }
        route.Add(cities[startIndex]);

        return (route, totalDistance);
    }

    public int GetDistance(int cityIndex1, int cityIndex2)
    {
        if (cityIndex1 < 0 || cityIndex1 >= distances.GetLength(0) ||
            cityIndex2 < 0 || cityIndex2 >= distances.GetLength(1))
        {
            Console.WriteLine($"Warning: Invalid distance lookup for indices {cityIndex1}, {cityIndex2}");
            return int.MaxValue;
        }
        return distances[cityIndex1, cityIndex2];
    }

    public int GetCityCount()
    {
        return cities.Count;
    }

    public string GetCityName(int index)
    {
        if (index < 0 || index >= cities.Count)
        {
            Console.WriteLine($"Warning: Invalid city name lookup for index {index}");
            return "Unknown";
        }
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

            chromosome[0] = startCityIndex;

            List<int> remainingCities = new List<int>();
            for (int j = 0; j < cityCount; j++)
            {
                if (j != startCityIndex)
                    remainingCities.Add(j);
            }

            for (int j = remainingCities.Count - 1; j > 0; j--)
            {
                int k = random.Next(j + 1);
                int temp = remainingCities[j];
                remainingCities[j] = remainingCities[k];
                remainingCities[k] = temp;
            }


            for (int j = 0; j < remainingCities.Count; j++)
            {
                chromosome[j + 1] = remainingCities[j];
            }

            population.Add(chromosome);
        }
    }

    public (List<string>, int) Run(int generations)
    {
        Console.WriteLine("Using Order Crossover (OX)");
        int cityCount = cityManager.GetCityCount();

        for (int gen = 0; gen < generations; gen++)
        {
            List<int[]> newPopulation = new List<int[]>();

            var sortedPopulation = population.OrderBy(p => CalculateDistance(p)).ToList();
            for (int i = 0; i < eliteCount && i < sortedPopulation.Count; i++)
            {
                newPopulation.Add((int[])sortedPopulation[i].Clone());
            }

            while (newPopulation.Count < populationSize)
            {
                int[] parent1 = TournamentSelection();
                int[] parent2 = TournamentSelection();

                int[] child = OrderCrossover(parent1, parent2);

                if (random.NextDouble() < mutationRate)
                {
                    Mutate(child);
                }

                if (child[0] != startCityIndex)
                {
                    int currentStartPos = Array.IndexOf(child, startCityIndex);
                    if (currentStartPos != -1)
                    {
                        int temp = child[0];
                        child[0] = child[currentStartPos];
                        child[currentStartPos] = temp;
                    }
                    else
                    {
                        Console.WriteLine("Error: Start city lost during mutation/crossover!");
                        child[0] = startCityIndex;
                    }
                }


                newPopulation.Add(child);
            }

            population = newPopulation;

            if (gen % 10 == 0 || gen == generations - 1)
            {
                int bestDistance = CalculateDistance(population.OrderBy(p => CalculateDistance(p)).First());
                Console.WriteLine($"Generation {gen}: Best distance = {bestDistance} km");
            }
        }

        int[] bestChromosome = population.OrderBy(p => CalculateDistance(p)).First();
        int totalDistance = CalculateDistance(bestChromosome);

        List<string> route = new List<string>();
        foreach (int cityIndex in bestChromosome)
        {
            if (cityIndex >= 0 && cityIndex < cityManager.GetCityCount())
            {
                route.Add(cityManager.GetCityName(cityIndex));
            }
            else
            {
                Console.WriteLine($"Error: Invalid city index {cityIndex} in best chromosome.");
            }
        }


        if (route.Count > 0 && route[0] != cityManager.GetCityName(startCityIndex))
        {
            route.Insert(0, cityManager.GetCityName(startCityIndex));
        }
        if (route.Count > 0 && route.Last() != cityManager.GetCityName(startCityIndex))
        {
            route.Add(cityManager.GetCityName(startCityIndex));
        }


        return (route, totalDistance);
    }


    private int[] OrderCrossover(int[] parent1, int[] parent2)
    {
        int cityCount = cityManager.GetCityCount();
        int[] child = new int[cityCount];
        for (int i = 0; i < cityCount; i++) child[i] = -1;

        int sequenceLength = cityCount - 1;

        int cut1 = random.Next(0, sequenceLength);
        int cut2 = random.Next(0, sequenceLength);

        int start = Math.Min(cut1, cut2);
        int end = Math.Max(cut1, cut2);

        child[0] = startCityIndex;
        HashSet<int> segmentCities = new HashSet<int>();

        for (int i = start; i <= end; i++)
        {
            int chromosomeIndex = i + 1;
            child[chromosomeIndex] = parent1[chromosomeIndex];
            segmentCities.Add(child[chromosomeIndex]);
        }

        int currentP2Index = (end + 1) % sequenceLength;
        int currentChildIndex = (end + 1) % sequenceLength;

        int filledCount = end - start + 1;

        while (filledCount < sequenceLength)
        {
            int p2ChromosomeIndex = currentP2Index + 1;
            int cityFromP2 = parent2[p2ChromosomeIndex];

            if (!segmentCities.Contains(cityFromP2))
            {
                int childChromosomeIndex = currentChildIndex + 1;
                while (child[childChromosomeIndex] != -1)
                {
                    currentChildIndex = (currentChildIndex + 1) % sequenceLength;
                    childChromosomeIndex = currentChildIndex + 1;
                }
                child[childChromosomeIndex] = cityFromP2;
                filledCount++;
                currentChildIndex = (currentChildIndex + 1) % sequenceLength;
            }

            currentP2Index = (currentP2Index + 1) % sequenceLength;
        }

        for (int i = 1; i < cityCount; i++)
        {
            if (child[i] == -1)
            {
                Console.WriteLine($"Error: OX failed to fill child position {i}");
            }
        }


        return child;
    }


    private void Mutate(int[] chromosome)
    {
        int cityCount = chromosome.Length;
        if (cityCount <= 2) return;

        int pos1 = random.Next(1, cityCount);
        int pos2 = random.Next(1, cityCount);

        if (cityCount > 2)
        {
            while (pos1 == pos2)
            {
                pos2 = random.Next(1, cityCount);
            }
        }


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
            if (population.Count == 0)
            {
                Console.WriteLine("Error: Population empty during selection.");
                return new int[cityManager.GetCityCount()];
            }
            int randomIndex = random.Next(population.Count);
            int[] candidate = population[randomIndex];
            int distance = CalculateDistance(candidate);

            if (best == null || distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        if (best == null)
        {
            Console.WriteLine("Warning: Tournament selection failed, returning random chromosome.");
            return (int[])population[random.Next(population.Count)].Clone();
        }
        return (int[])best.Clone();
    }

    private int CalculateDistance(int[] chromosome)
    {
        int distance = 0;
        int cityCount = cityManager.GetCityCount();

        if (chromosome == null || chromosome.Length != cityCount)
        {
            Console.WriteLine($"Error: Invalid chromosome length or null chromosome in CalculateDistance.");
            return int.MaxValue;
        }


        for (int i = 0; i < cityCount - 1; i++)
        {
            int dist = cityManager.GetDistance(chromosome[i], chromosome[i + 1]);
            if (dist == int.MaxValue)
            {
                return int.MaxValue;
            }
            distance += dist;
        }

        int returnDist = cityManager.GetDistance(chromosome[cityCount - 1], chromosome[0]);
        if (returnDist == int.MaxValue)
        {
            return int.MaxValue;
        }
        distance += returnDist;

        return distance;
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("City Route Optimization Program");
        Console.WriteLine("===============================");

        CityManager cityManager = new CityManager();
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "miasta.txt");
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found at {filePath}");
            Console.WriteLine("Please ensure 'miasta.txt' is in the same directory as the executable.");
            return;
        }
        cityManager.LoadFromFile(filePath);


        if (cityManager.GetCityCount() == 0)
        {
            Console.WriteLine("No cities loaded. Exiting.");
            return;
        }


        bool running = true;
        while (running)
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Find route using Nearest Neighbor");
            Console.WriteLine("2. Find route using Genetic Algorithm with Order Crossover (OX)");
            Console.WriteLine("3. Exit");
            Console.Write("\nSelect option: ");

            string input = Console.ReadLine();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    RunNearestNeighbor(cityManager);
                    break;
                case "2":
                    RunGeneticAlgorithm(cityManager);
                    break;
                case "3":
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

        if (cityManager.GetCityIndex(startCity) == -1)
        {
            Console.WriteLine($"Error: City '{startCity}' not found in the loaded data.");
            return;
        }


        Console.Write("Population size (recommended 50-100, default 50): ");
        if (!int.TryParse(Console.ReadLine(), out int populationSize) || populationSize < 10)
            populationSize = 50;

        Console.Write("Number of generations (recommended 100-1000, default 200): ");
        if (!int.TryParse(Console.ReadLine(), out int generations) || generations < 10)
            generations = 200;

        try
        {
            Console.WriteLine("\nStarting Genetic Algorithm...");
            var geneticAlgorithm = new GeneticAlgorithm(cityManager, startCity, populationSize);
            var (route, distance) = geneticAlgorithm.Run(generations);

            Console.WriteLine("\nRoute using Genetic Algorithm with Order Crossover (OX):");
            Console.WriteLine(string.Join(" -> ", route));
            Console.WriteLine($"Total distance: {distance} km");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nAn error occurred during the Genetic Algorithm: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}