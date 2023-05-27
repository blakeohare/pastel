package gen;

public class NumAnalysis {
  public int count;
  public int min;
  public int max;
  public int total;
  public double mean;
  public double median;
  public double std_dev;
  public static final NumAnalysis[] EMPTY_ARRAY = new NumAnalysis[0];

  public NumAnalysis(int count, int min, int max, int total, double mean, double median, double std_dev) {
    this.count = count;
    this.min = min;
    this.max = max;
    this.total = total;
    this.mean = mean;
    this.median = median;
    this.std_dev = std_dev;
  }
}
