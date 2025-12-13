# mobi2stardict
Convert MOBI dictionaries to StarDict and TSV formats.

You can get both .NET Desktop and Cli applications from [releases](https://github.com/anezih/mobi2stardict/releases). Both programs does not need the intermediary unpacked HTML file and can directly read MOBI dictionary files.

[MobiDict.Reader](https://github.com/anezih/mobi2stardict/tree/main/MobiDictReader/MobiDict.Reader) class library is a direct and bare-minimum implementation of [KindleUnpack](https://github.com/kevinhendricks/KindleUnpack) in order to extract dictionary entries.

# .NET Desktop Application

![](/res/desktop.png)

# .NET Cli Application

`./MobiDict.Cli --help`
```
Usage: [options...] [-h|--help] [--version]

Convert Kindle MOBI dictionaries to TSV and StarDict formats.

Options:
  -i, --input <string>             MOBI file path [Required]
  --tsv                            Save as TSV (tab-separated-values)
  --stardict                       Save as StarDict
  -o, --output-folder <string?>    Output folder where the files will be saved [Default: null]
```

<details>
<summary><h1>Python Script (Old)</h1></summary>

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
</details>

# NOTE
You may come across some poorly formatted dictionaries that may result in inability to parse definitions.

# Credits

[KindleUnpack](https://github.com/kevinhendricks/KindleUnpack)

[Avalonia UI](https://avaloniaui.net/)

[AtomUI](https://github.com/AtomUI/AtomUI)

[ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework)