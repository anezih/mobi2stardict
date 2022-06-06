# mobi2stardict
Convert unpacked MOBI dictionaries to Babylon glossary source files, an intermediary format for StarDict.

# Usage
`python.exe mobi2stardict.py --help`
```
usage: mobi2stardict.py [-h] [--fix-links] [--dict-name DICT_NAME] [--author AUTHOR]

Convert unpacked Kindle MOBI dictionary files (book.html) to Babylon Glossary source files (.gls). This source file
can later be converted to stardict format via StarDict Editor. Put this script in the same folder where 'book.html'
exists. You can unpack MOBI files via 'KindleUnpack' or its Calibre plugin.

optional arguments:
  -h, --help            show this help message and exit
  --fix-links           Try to convert in-dictionary references to glossary format.
  --dict-name DICT_NAME
                        Name of the dictionary file.
  --author AUTHOR       Name of the author or publisher.
```

# NOTE
You may come across some malformatted dictionaries that may result in inability to parse definitions.
