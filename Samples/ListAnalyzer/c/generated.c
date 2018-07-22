#include <stdlib.h>
#include <string.h>

void* translation_helper_array_copy(void* original, int bytes) {
	void* copy = malloc(bytes);
	memcpy(original, copy, bytes);
	return copy;
}

void translation_helper_array_sort_int(int* nums, int length) {
	// TODO: this
}
double v_calculate_standard_deviation(int* v_nums, int v_length, double v_mean)
{
    double v_total_dev = 0.0;
    int v_i = 0;
    while (i < length)
    {
        double v_diff = (nums[i] - mean);
        total_dev += TranslationHelper_math_pow(diff, 2);
        i += 1;
    }
    return TranslationHelper_math_sqrt((1.0 * (total_dev) / (length)));
}

NumAnalysis* v_perform_analysis(int* v_nums, int v_length)
{
    NumAnalysis* v_output = NumAnalysis_new(0, 0, 0, 0, 0.0, 0.0, 0.0);
    output->count = length;
    if (length > 0)
    {
        output->min = nums[0];
        output->max = nums[0];
        output->total = 0;
        int v_i = 0;
        while (i < length)
        {
            int v_value = nums[i];
            output->total += value;
            if (value < output->min)
            {
                output->min = value;
            }
            if (value > output->max)
            {
                output->max = value;
            }
            i += 1;
        }
        output->mean = (1.0 * ((1.0 * output->total)) / (length));
        output->std_dev = calculate_standard_deviation(nums, length, output->mean);
        int* v_nums_copy = translation_helper_array_copy(nums, (length) * sizeof(int));
        translation_helper_array_sort_int(nums_copy, length);
        if ((length % 2) == 0)
        {
            output->median = (1.0 * ((nums_copy[((length) / (2) - 1)] + nums_copy[(length) / (2)])) / (2.0));
        }
        else
        {
            output->median = (0.0 + nums_copy[(length) / (2)]);
        }
        free(nums_copy);
    }
    return output;
}

