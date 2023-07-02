package main

type S_NumAnalysis struct {
  f_count   int
  f_min     int
  f_max     int
  f_total   int
  f_mean    float64
  f_median  float64
  f_std_dev float64
}
type PtrBox_NumAnalysis struct {
  o *S_NumAnalysis
}
