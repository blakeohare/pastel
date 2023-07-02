package main

import "fmt"

func main() {
	fmt.Println("Hello, World!")

	nums := []int{5, 9, 2, 4, 11, 13}
	result := fn_perform_analysis(nums, len(nums)).o

	fmt.Println("Count:", result.f_count)
	fmt.Println("Min:", result.f_min)
	fmt.Println("Max:", result.f_max)
	fmt.Println("Mean:", result.f_mean)
	fmt.Println("Median:", result.f_median)
	fmt.Println("Standard Deviation:", result.f_std_dev)
}
