import gen.FunctionWrapper;
import gen.NumAnalysis;

public final class ListAnalyzer {

  public static void main(String[] args) {
  
	int[] nums = new int[] { 5, 9, 2, 4, 11, 13 };
    NumAnalysis result = FunctionWrapper.perform_analysis(nums, nums.length);
    
    System.out.println("Count: " + result.count);
    System.out.println("Min: " + result.min);
    System.out.println("Max: " + result.max);
    System.out.println("Mean: " + result.mean);
    System.out.println("Median: " + result.median);
    System.out.println("Standard Deviation: " + result.std_dev);
  }

}
