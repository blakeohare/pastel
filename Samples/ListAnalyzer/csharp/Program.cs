using System;
using System.Linq;

namespace ListAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] nums;
            try
            {
                nums = args.Select(n => int.Parse(n)).ToArray();
            }
            catch (Exception)
            {
                Console.WriteLine("Usage: ListAnalyzer.exe num1 num2 num3 ...");
                return;
            }

            PastelGeneratedNamespace.FunctionWrapper.perform_analysis(nums, nums.Length);
            Console.WriteLine("Hello World!");
        }
    }
}
