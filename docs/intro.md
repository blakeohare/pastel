# Introduction

Pastel is a transpiled programming language. Rather than having a runtime or
compiling to machine code, it generates code in other programming languages.

Currently, the programmig languages that Pastel can export to are:
- C#
- Java
- JavaScript
- PHP
- Python

The purpose of Pastel is to allow for porting parts or all of the business logic
of your program to multiple platforms without the need to use a framework that
may potentially create lots of bloat or dictate the way you structure your app.

Because Pastel ONLY generates vanilla code in these languages, the way you use
this code is very **unopinionated**.

Pastel is a 1-to-1 transpiler with the goal of creating the most direct and
efficient code in the target language based on the original Pastel code. In
other words, it has no built-in protections that wrap your code. For example,
if you access and element in an array that is outside the bounds of an array in
Pastel, this will result in an `ArrayIndexOutOfBoundsException` in the generated
C# code, but at the same time, it will silently generate an `undefined` constant
in the generated JavaScript code. It is recommended to test your code on all
platforms you intend to use. Of course this means that some operations must be
simulated (such as clearing a list which is not available in some platforms) but
in general, direct built-in libraries are used.

Notably, Pastel does support switch statements despite Python not doing so
inherently. Switch statement behavior is simulated in Python in a way that gives
O(dictionary-lookup + log(case-count)) performance.

Various notes:
- Garbage collection is naturally handled by the underlying platform.
- All complex types are treated as reference types. Even structs and arrays. In
  PHP, arrays are wrapped in a reference object.
- There are no static values in Pastel. All persistent data must be stored in
  an object.
- There are no Exceptions or exception-handling in Pastel.
- You can create extension callbacks to call code that does things that aren't
  vanilla, such as calling other libraries.
