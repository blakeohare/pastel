#include <stdio.h>
#include <stdlib.h>

#include <generated.h>

int main() {
	int* nums = { 5, 9, 1, 11, 13, 2, 4 };
	int nums_length = 7;
	NumAnalysis* result = perform_analysis(nums, nums_length);
	printf("Nums: ");
	for (int i = 0; i < nums_length; ++i) {
		if (i > 0) printf(", ");
		printf("%d", nums[i]);
	}
	printf("\n\n");
	
	printf("Count: %d\n", result->count);
	printf("Total: %d\n", result->total);
	printf("Min: %d\n", result->min);
	printf("Max: %d\n", result->max);
	printf("Mean: %lf\n", result->mean);
	printf("Median: %lf\n", result->median);
	printf("Standard Deviation: %lf\n", result->std_dev);
	
	free(result);
}
