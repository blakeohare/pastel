# Built-in Collections

There are three built-in collection types in Pastel:
- Arrays
- Lists
- Dictionaries

An Array is a fixed-length linear collection of items.

A List is a dynamically-allocated linear collection items.

A Dictionary is a lookup table that maps integers or strings to other values.

All collections must correspond to a specific item type and cannot contain
mixed types (however, you can declare the type as `object`).

In Java and C# these map directly to their natural counterparts.

## How collections are transpiled:

|   | `Array<Thing>` | `List<Thing>` | `Dictionary<Key, Thing>` |
| --: | :-: | :-: | :-: |
| C# | `Thing[]` | `List<Thing>` | `Dictionary<Key, Thing>` |
| Java | `Thing[]` | `ArrayList<Thing>` | `HashMap<Key, Thing>` |
| JavaScript | `Array` | `Array` | `Object` |
| Python | `list` | `list` | `dict` |
| PHP* | obj-wrapped `array` | obj-wrapped `array` | obj-wrapped associative `array` |

> * PHP note: because arrays are value and not reference types in PHP, all
> collections are wrapped in an object instance to ensure all collections are
> treated as references.

## Creating Collections

To create an array, the size must be provided as an argument to its constructor.

```
Array<int> numbers = new Array<int>(3);
```

This creates an empty array of size 3. 

> The starting values are not necessarily defined. 
> This is currently a [bug](https://github.com/blakeohare/pastel/issues/6).

Note that the type must be provided in brackets. No other type of item can be
stored in this array.

To create a list, it is the same, except you cannot provide a starting length.

```
List<int> numbers = new List<int>();
```

All lists start as empty.

Creating Dictionaries is also similar, except there must be a key type and a
value type provided.

```
Dictionary<string, int> numbers = new Dictionary<string, int>();
```

Dictionaries only support integer and string key types.

## Accessing and Setting items

Accessing and Modifying items by key and index is done by using bracket
notation. All indices are 0-indexed.

```
array[3] = 42; // sets the 4th item in the array to 42
int num = array[0]; // sets the variable num to the first item in the array.
```

```
dict["foo"] = 42; // sets the key at "foo" to 42, overwriting it if necessary
int num = dict["bar"]; // gets the key at num. 
```

If a key is accessed on a dictionary but it doesn't exist, the behavior is not
defined, as this is a direct call to the platform's implementation of its
dictionary-like value.

If you need to check if a key exists, use the `.TryGet()` or `.ContainsKey()` 
methods mentioned below.

## List methods

- `list.Add(item)` - adds `item` to the end of `list`.
- `list.Concat(otherList)` - creates a new list as a combination of `list` and
  `otherList` and returns the new list. The input lists are unaffected by this.
- `list.Clear()` - removes all items from the list.
- `list.Size()` - returns the size of the list as an integer.
- `list.Shuffle()` - shuffles the list randomly. Shuffle is in-place and so
  there is no return value.
- `list.Reverse()` - reverses the list in-place. No return value.
- `list.JoinStrings(string sep)` - Creates a new string as the result of joining
  together all the strings in the list. This is only applicable to
  `List<string>`.
- `list.JoinChars(string sep)` - Creates a new string as the result of joining
  together all the characters in the list. This is only applicable to
  `List<char>`.
- `list.RemoveAt(index)` - Removes the item at the given index. This must be a 
  number from 0 to length (exclusive).
- `list.Pop()` - Removes the last item in the list.

## Array methods

- `array.Join(string separator)` - Creates a new string as the result of joining
  together all the strings in the array. This is only applicable to
  `Array<string>`.
- `array.Length()` - Gets the length of the array as an integer.


## Dictionary methods

- `dictionary.Keys()` - Creates a new array of the keys in the dictionary. The
  order of these keys is not guaranteed to be in any particular order.
- `dictionary.Values()` - Creates a new array of the values in the dictionary.
  The order of these values is not guaranteed to be in any particular order.
- `dictionary.Remove(key)` - Removes the entry with the given key.
- `dictionary.Size()` - Returns the number of elements in the dictionary.
- `dictionary.TryGet(key, fallback)` - Attempts to get the value at the given
  key. If no key exists, the fallback value is returned instead.


