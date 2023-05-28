# Using `@import` for multiple files

The pastel build file only points to a single source file. However, you can load
multiple files by using the `@import()` function.

The `@import()` function takes in a string path that is relative to the current
file. The contents of that file will be inserted into the parser at the location
where the import occurred.

The `@import()` function does not take into consideration whether the file has
already been imported. This is a feature, not a bug, as the imported file does
not necessarily need to be top-level element definitions and so it can be
used as a sort of macro.

`main.pst`:
```
@import("util.pst");

int MyFunction(int arg) {
    @import("verify_arg.pst");
    if (!argOk) return -1;
    int result = Util_function_defined_in_util_pst(arg);
    return result;
}
```

`verify_arg.pst`:
```
bool argOk = arg >= 0;
```

In addition to `@import()`, there is `@importIfFalse` and `@importIfTrue` which
take in a flag as a string in addition to the file path. This will conditionally
import the file (see [flags](flags.md) documentation).
