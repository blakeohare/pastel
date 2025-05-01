package main

import "fmt"

func do_fail(args []any) any {
	msg := args[0].(*pstring)
	fmt.Println(*msg.str)
	panic("panic!")
}

func main() {
	PST_RegisterExtensibleCallback("fail", do_fail)
	fn_runner()
}
