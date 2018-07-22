typedef struct NumAnalysis NumAnalysis;

struct NumAnalysis {
	int count;
	int min;
	int max;
	int total;
	double mean;
	double median;
	double std_dev;
};


NumAnalysis* NumAnalysis_new(int _count, int _min, int _max, int _total, double _mean, double _median, double _std_dev)
{
    NumAnalysis* t = (NumAnalysis*)malloc(sizeof(NumAnalysis));
    t->count = _count;
    t->min = _min;
    t->max = _max;
    t->total = _total;
    t->mean = _mean;
    t->median = _median;
    t->std_dev = _std_dev;
    return t;
}


double v_calculate_standard_deviation(int* v_nums, int v_length, double v_mean);
NumAnalysis* v_perform_analysis(int* v_nums, int v_length);
