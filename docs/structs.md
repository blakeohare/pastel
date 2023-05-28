# Structs

Structs are like classes, except they only contain fields and have no
inheritance.

> There is currently no high-level OOP support in Pastel due to how different
> various languages treat classes and instances. However this is planned in
> the future.

To create a struct, use the `struct` keyword followed by the name, followed by
the list of fields.

```
struct User {
    string name;
    int age;
    List<User> friends;
}
```

Struct definitions MUST be created in the top-level. The order that structs are
defined does not matter as name resolution is performed after a first pass in
the parser. Therefore structs can be self-referential or contain reference
cycles.

To instantiate a struct, use the `new` operator with the struct's name. The
parentheses should contain all the starting field values in order.

```
User user = new User("George", 8, new List<User>());
string greeting = "Hello, my name is " + user.name + ".";
```

## How structs are transpiled

Structs are transpiled in C#, Java, and PHP as class definitions, using the name
you provide as the class name and all the fields are public fields. In Python
and JavaScript, structs are created as list definitions and all fields are index
offsets in the list as this is measurably faster than more traditional
(readable) alternatives.
