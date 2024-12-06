# mobi2stardict
Convert unpacked MOBI dictionaries to StarDict format.

# Usage
`python.exe mobi2stardict.py --help`
```
usage: mobi2stardict.py [-h] [--html-file HTML_FILE] [--fix-links] [--dict-name DICT_NAME] [--author AUTHOR]
                        [--chunked]

Convert unpacked Kindle MOBI dictionary files (book.html or part00000.html) to Stardict dictionary Format. StarDict
format can be converted to a wide-range of formats directly via PyGlossary. You can unpack MOBI files via
'KindleUnpack' or its Calibre plugin. Alternatively, you can use mobitool from libmobi project.

options:
  -h, --help            show this help message and exit
  --html-file HTML_FILE
                        Path of the HTML file.
  --fix-links           Try to convert in-dictionary references to glossary format.
  --dict-name DICT_NAME
                        Name of the dictionary file.
  --author AUTHOR       Name of the author or publisher.
  --chunked             Parse html in chunks to reduce memory usage.
```
You need to install [Beautiful Soup](https://www.crummy.com/software/BeautifulSoup/bs4/doc/#installing-beautiful-soup), [lxml](https://lxml.de/installation.html) and **PyGlossary** (`pip install pyglossary beautifulsoup4 lxml`) packages to run the script.
To convert the unpacked MOBI file to StarDict format you would call the script like this (assuming part00000.html is in the same directory with the script):
````
python.exe mobi2stardict.py --fix-links --dict-name "Name of the dictionary" --author "Author"
````
Change name and author accordingly.

Also, while converting particularly large files you may want to pass the `--chunked` option to bring down the memory usage to more moderate levels. Then the line would become:
````
python.exe mobi2stardict.py --fix-links --dict-name "Name of the dictionary" --author "Author" --chunked
````

# NOTE
You may come across some poorly formatted dictionaries that may result in inability to parse definitions.
