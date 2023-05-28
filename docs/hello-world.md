# Building a Hello-World program

Pastel does not run on its own. It generates files containing helper functions
in other programming languages. The code that is generated is vanilla code and
is ideal for porting business logic of your program to multiple platforms
without having to use a framework that creates large dependencies or dictates
the structure of your program.

This means that in order to make a hello world program, you must first choose
one of the Pastel target languages to build the app in.

The Pastel code itself can be a file such as this:

`hello.pst`:

```
string GenerateGreeting(string name) {
    return "Hello, " + name + "!";
}
```

In order for Pastel to compile this, you need to create a build configuration
file. This file tells Pastel where your code is and which platforms you want to
export to and where you want the files to go.

A build configuration file is a plain JSON file that requires two fields in the
root object:
- `source` - this tells Pastel where your source code is. This path is
  relative to the JSON file itself.
- `targets` - this is a list of all the different places you want your code to
  go. In the sample below, feel free to remove all the platforms that you don't
  want to export to.

`HelloWorld.json`:
```
{
    "source": "hello.pst",
    "targets": [
        {
            "name": "csharp-demo",
            "language": "c#",
            "output": {
				"namespace": "PastelGeneratedCode",
				"structs-path": "../path/to/csharp/project/PastelGeneratedCode",
				"functions-path": "../path/to/csharp/project/FunctionWrapper.cs",
				"functions-wrapper-class": "FunctionWrapper"
            }
        }, {
            "name": "java-demo",
            "language": "java",
            "output": {
				"namespace": "gen",
				"structs-path": "../path/to/java/project/java/src/gen",
				"functions-path": "../path/to/java/project/java/src/gen/FunctionWrapper.java",
				"functions-wrapper-class": "FunctionWrapper"
            }
        }, {
            "name": "javascript-demo",
            "language": "javascript",
            "output": {
				"functions-path": "../path/to/javascript/project/gen/pastel_code.js"
            }
        }, {
			"name": "php",
			"language": "php",
			"output": {
				"structs-path": "../path/to/php/project/gen",
				"namespace": "PastelGeneratedCode",
				"functions-path": "../path/to/php/project/gen/functions.php",
				"functions-wrapper-class": "FunctionWrapper"
			}
		}, {
			"name": "python",
			"language": "python",
			"output": {
				"structs-path": "../path/to/pthon/project/pastelgenerated",
				"functions-path": "../path/to/pthon/project/pastelgenerated/__init__.py"
			}
		}
    ]
}
```

## Python

For Python, you can import the module that was created. Whether this is an
`__init__` file or a named file is up to you, based on the path you provide in
the build file. Suppose you use the pastelgenerated directory as in the example
build file above. A hello world script would look something like this...

`main.py` with a `pastelgenerated/__init__.py` file.
```python
#
import pastelgenerated

if __name__ == "__main__":
  msg = pastelgenerated.GenerateGreeting("World")
  print(msg) # 'Hello, World!'
```

## JavaScript (Web)

JavaScript files are exported as a list of functions in a file. Currently this
is only supported by `<script>` style inclusions.

`index.html` with a `gen/pastel_code.js`
```html
<!DOCTYPE html>
<html lang="en">
    <head>
        <meta charset="utf-8">
        <title>Hello, World</title>
        <script src="gen/pastel_code.js"></script>
        <script>
            window.addEventListener('load', () => {
                let msg = GenerateGreeting("World");
                document.body.append(msg);
            });
        </script>
    </head>
    <body></body>
</html>
```

## PHP

TODO: fill this in

## C#

TODO: fill this in
