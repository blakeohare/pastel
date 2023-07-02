package main

type ExtFunc func(array []interface{}) interface{}

var PST_ExtCallbacks map[string]ExtFunc

func PST_RegisterExtensibleCallback(name string, fn ExtFunc) {
	PST_ExtCallbacks[name] = fn
}

// TODO: gen function

// TODO: gen function
