# mobi2stardict
Convert unpacked MOBI dictionaries to StarDict input formats: Babylon Glossary Source (GLS) format and StarDict Textual Dictionary Format

# Usage
`python.exe mobi2stardict.py --help`
```
usage: mobi2stardict.py [-h] [--html-file HTML_FILE] [--fix-links] [--dict-name DICT_NAME] [--author AUTHOR]
                            [--gls] [--textual] [--chunked]

Convert unpacked Kindle MOBI dictionary files (book.html or part00000.html) to Babylon Glossary source files (.gls) or
to Stardict Textual Dictionary Format. These source files can later be converted to StarDict format via StarDict
Editor. Textual xml format can be converted to a wide-range of formats directly via PyGlossary. You can unpack MOBI
files via 'KindleUnpack' or its Calibre plugin. Alternatively, you can use mobitool from libmobi project.

optional arguments:
  -h, --help            show this help message and exit
  --html-file HTML_FILE
                        Path of the HTML file.
  --fix-links           Try to convert in-dictionary references to glossary format.
  --dict-name DICT_NAME
                        Name of the dictionary file.
  --author AUTHOR       Name of the author or publisher.
  --gls                 Convert dictionary to Babylon glossary source.
  --textual             Convert dictionary to Stardict Textual Dictionary Format.
  --chunked             Parse html in chunks to reduce memory usage.
```
You need to install [Beautiful Soup](https://www.crummy.com/software/BeautifulSoup/bs4/doc/#installing-beautiful-soup) and [lxml](https://lxml.de/installation.html) packages to run the script.
To convert the unpacked MOBI file to both GLS and Textual format you would call the script like this (assuming part00000.html is in the same directory with the script):
````
python.exe mobi2stardict.py --fix-links --dict-name "Name of the dictionary" --author "Author" --gls --textual
````
Change name and author accordingly.

Also, while converting particularly large files you may want to pass the `--chunked` option to bring down the memory usage to more moderate levels. Then the line would become:
````
python.exe mobi2stardict.py --fix-links --dict-name "Name of the dictionary" --author "Author" --gls --textual --chunked
````

# NOTE
You may come across some malformatted dictionaries that may result in inability to parse definitions.
