package gen;

public final class FunctionWrapper {

  private static int[] PST_sortedCopyOfIntArray(int[] nums) {
    int[] output = java.util.Arrays.copyOf(nums, nums.length);
    java.util.Arrays.sort(output);
    return output;
  }

  static abstract class PstExtWrapper {
    public abstract Object run(Object[] args);
  }
  private static final HashMap<String, PstExtWrapper> PST_ExtCallbacks = new HashMap<>();

  public static void PST_RegisterExtensibleCallback(String name, PstExtWrapper func)
  {
      PST_ExtCallbacks.put(name, func);
  }

  public static double calculate_standard_deviation(int[] nums, int length, double mean) {
    double total_dev = 0.0;
    int i = 0;
    while (i < length) {
      double diff = nums[i] - mean;
      total_dev += Math.pow(diff, 2);
      i += 1;
    }
    return Math.pow(total_dev / length, 0.5);
  }

  public static int NumAnalysis_getCount(NumAnalysis na) {
    return na.count;
  }

  public static int NumAnalysis_getMax(NumAnalysis na) {
    return na.max;
  }

  public static double NumAnalysis_getMean(NumAnalysis na) {
    return na.mean;
  }

  public static double NumAnalysis_getMedian(NumAnalysis na) {
    return na.median;
  }

  public static int NumAnalysis_getMin(NumAnalysis na) {
    return na.min;
  }

  public static double NumAnalysis_getStdDev(NumAnalysis na) {
    return na.std_dev;
  }

  public static int NumAnalysis_getTotal(NumAnalysis na) {
    return na.total;
  }

  public static NumAnalysis perform_analysis(int[] nums, int length) {
    String testValue = "Strings ought to be tested to some degree.\n";
    NumAnalysis output = new NumAnalysis(0, 0, 0, 0, 0.0, 0.0, 0.0);
    output.count = length;
    if (length > 0) {
      output.min = nums[0];
      output.max = nums[0];
      output.total = 0;
      int i = 0;
      while (i < length) {
        int value = nums[i];
        output.total += value;
        if (value < output.min) {
          output.min = value;
        }
        if (value > output.max) {
          output.max = value;
        }
        i += 1;
      }
      output.mean = 1.0 * output.total / length;
      output.std_dev = calculate_standard_deviation(nums, length, output.mean);
      int[] nums_copy = PST_sortedCopyOfIntArray(nums);
      if (length % 2 == 0) {
        output.median = (nums_copy[length / 2 - 1] + nums_copy[length / 2]) / 2.0;
      } else {
        output.median = 0.0 + nums_copy[length / 2];
      }
    }
    return output;
  }
}
