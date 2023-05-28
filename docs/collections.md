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

|   | Array<Thing> | List<Thing> | Dictionary<Key, Thing> |
| --: | :-: | :-: | :-: |
| C# | Thing[] | List<Thing> | Dictionary<Key, Thing> |
| Java | Thing[] | ArrayList<Thing> | HashMap<Key, Thing> |
| JavaScript | array | array | object |
| Python | list | list | dictionary |
| PHP* | obj-wrapped array | obj-wrapped array | obj-wrapped associative array |

> * PHP note: because arrays are value and not reference types in PHP, all
> collections are wrapped in an object instance to ensure all collections are
> treated as references.

## List methods

- `list.Add(item)` - adds `item` to the end of `list`.
- `list.Concat(otherList)` - creates a new list as a combination of `list` and
  `otherList`.
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


