package gen;

public final class NumAnalysis {
  public NumAnalysis(int count, int min, int max, int total, double mean, double median, double std_dev) {
    this.count = count;
    this.min = min;
    this.max = max;
    this.total = total;
    this.mean = mean;
    this.median = median;
    this.std_dev = std_dev;
  }

  public int count;
  public int min;
  public int max;
  public int total;
  public double mean;
  public double median;
  public double std_dev;
}
