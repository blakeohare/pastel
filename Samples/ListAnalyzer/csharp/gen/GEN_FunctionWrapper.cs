using System;
using System.Collections.Generic;
using System.Linq;

namespace PastelGeneratedNamespace
{
    internal static class FunctionWrapper
    {
        public static double calculate_standard_deviation(int[] nums, int length, double mean)
        {
            double total_dev = 0.0;
            int i = 0;
            while ((i < length))
            {
                double diff = (nums[i] - mean);
                total_dev += Math.Pow(diff, 2);
                i += 1;
            }
            return Math.Sqrt(total_dev);
        }

        public static NumAnalysis perform_analysis(int[] nums, int length)
        {
            NumAnalysis output = new NumAnalysis(0, 0, 0, 0, 0.0, 0.0, 0.0);
            output.count = length;
            if ((length > 0))
            {
                output.min = nums[0];
                output.max = nums[0];
                output.total = 0;
                int i = 0;
                while ((i < length))
                {
                    int value = nums[i];
                    output.total += value;
                    if ((value < output.min))
                    {
                        output.min = value;
                    }
                    if ((value > output.max))
                    {
                        output.max = value;
                    }
                    i += 1;
                }
                output.mean = (output.total) / (length);
                output.std_dev = calculate_standard_deviation(nums, length, output.mean);
                int[] nums_copy = ((int[])TranslationHelper.ArrayCopy(nums));
                System.Array.Sort(nums_copy);
                if (((length % 2) == 0))
                {
                    output.median = ((nums[((length) / (2) - 1)] + nums[(length) / (2)])) / (2.0);
                }
                else
                {
                    output.median = (0.0 + nums[(length) / (2)]);
                }
            }
            return output;
        }

    }
}
