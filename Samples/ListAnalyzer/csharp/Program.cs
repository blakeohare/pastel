using System;
using PastelGeneratedNamespace;

namespace ListAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] nums = { 5, 9, 1, 3, 11, 13, 2, 10 };
            NumAnalysis result = FunctionWrapper.perform_analysis(nums, nums.Length);

            Console.WriteLine("Count: " + result.count);
            Console.WriteLine("Total: " + result.total);
            Console.WriteLine("Min: " + result.min);
            Console.WriteLine("Max: " + result.max);
            Console.WriteLine("Mean: " + result.mean);
            Console.WriteLine("Median: " + result.median);
            Console.WriteLine("Standard Deviation: " + result.std_dev);
        }
    }
}
