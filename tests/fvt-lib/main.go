package main

import "fmt"

func do_fail(args []any) any {
	msg := *args[0].(*string)
	fmt.Println(msg)
	panic("panic!")
}

func main() {
	PST_RegisterExtensibleCallback(PST_strPtr("fail"), do_fail)
	fn_runner()
}
