import argparse
import re
import sys
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

from bs4 import BeautifulSoup
from pyglossary.glossary_v2 import Glossary

SCRIPT_DIR = Path(__file__).resolve().parent

@dataclass
class Entry:
    HW: str
    INFL: set[str]
    BODY: str

@dataclass
class Metadata:
    Title: str
    Description: str
    Creator: str
    Date: str
    InLang: str
    OutLang: str

def get_metadata(path: str) -> Metadata:
    def soup_gettext(_find) -> str:
        if _find:
            return _find.text.strip()
        return ""
    _base_dir = None
    if path == "part00000.html":
        _base_dir = Path.cwd()
    else:
        _base_dir = SCRIPT_DIR
    opf_path = next(_base_dir.glob("*.opf"), None)
    try:
        if opf_path:
            with open(opf_path, "r", encoding="utf-8") as f:
                opf_soup = BeautifulSoup(f.read(), "lxml-xml")
        else:
            return Metadata("","","","","","")
    except:
        print("Could not read opf file.")
        return Metadata("","","","","","")
    return Metadata(
        Title       = soup_gettext(opf_soup.find("dc:title")),
        Description = soup_gettext(opf_soup.find("dc:description")),
        Creator     = soup_gettext(opf_soup.find("dc:creator")),
        Date        = soup_gettext(opf_soup.find("dc:date")),
        InLang      = soup_gettext(opf_soup.find("DictionaryInLanguage")),
        OutLang     = soup_gettext(opf_soup.find("DictionaryOutLanguage"))
    )

def set_metadata(_key: str, _metadata: Metadata, dict_name: str, author: str):
    if _key == 'Title':
        if _metadata.Title:
            _title = f"{_metadata.Title}"
            if _metadata.InLang:
                _title += f" ({_metadata.InLang.replace('-','_')}"
            if _metadata.OutLang:
                _title += f"-{_metadata.OutLang.replace('-','_')})"
            else:
                _title += f")"
            return _title
        else:
            return dict_name
    if _key == 'Desc':
        return _metadata.Description
    if _key == 'Creator':
        if _metadata.Creator:
            return _metadata.Creator
        else:
            return author
    if _key == 'Date':
        if _metadata.Date:
            return _metadata.Date
        else:
            return f"{datetime.today().strftime('%d/%m/%Y')}"

def fix(body_str: str) -> str:
    temp = BeautifulSoup(body_str, "lxml")
    links = temp.find_all("a", href=True)
    for link in links:
        body_str = re.sub(link["href"], f"bword://{link.getText()}", body_str)
    return body_str

def read_with_correct_encoding(html_path: str) -> str:
    try:
        with open(html_path, "r", encoding="utf-8") as f:
            book = f.read()
            return book
    except:
        with open(html_path, "r", encoding="cp1252") as f:
            book = f.read()
            return book

def convert(html: str, dict_name: str, author: str, fix_links: bool, chunked: bool) -> None:
    try:
        book = read_with_correct_encoding(html)
    except FileNotFoundError:
        sys.exit("Could not open the file. Check the filename.")
    if "<idx:" not in book:
        sys.exit("Not a dictionary file.")
    entry_groups = []
    if chunked:
        cnt = 0
        temp = []
        parts = book.split("<idx:entry")[1:]
        last_part = parts[-1]
        for p in parts:
            if p:
                temp.append(("<idx:entry" + p))
                cnt += 1
                if cnt == 5000 or p == last_part:
                    entry_groups.append("".join(temp))
                    cnt = 0
                    temp = []
    else:
        entry_groups.append(book)
    arr: list[Entry] = []
    cnt = 0
    for group in entry_groups:
        soup = BeautifulSoup(group, "lxml")
        entries = soup.find_all("idx:entry")
        for entry in entries:
            entries_temp = []
            nested_entries = []
            str_entry = str(entry)
            delim = "<idx:entry"
            if delim in str_entry[10:]:
                nested_entries = [(delim + s) for s in str_entry.split(delim) if s]
            if nested_entries:
                entries_temp = [BeautifulSoup(n, "lxml") for n in nested_entries]
            else:
                entries_temp.append(entry) # FIXME find_all already gets nested ones and they are added to the out file twice.

            for e in entries_temp:
                if e.find("idx:orth"):
                    headword = e.find("idx:orth").get("value").strip()
                    inflections = e.find("idx:infl")
                    inflections_set = None

                    if inflections:
                        inflections_set = {i.get("value").strip() for i in inflections.find_all("idx:iform")}

                    body_re = re.search("</idx:orth>(.*?)</idx:entry>", str(e))
                    body = body_re.group(1)
                    if not body:
                        b = []
                        ns = e.next_siblings
                        while (t := next(ns, "")) and not (t.name == "idx:entry" or t.name == "mbp:pagebreak"):
                            b.append(str(t))
                        body = "".join(b)
                    if not body:
                        continue

                    if fix_links:
                        body = fix(body)

                    if inflections_set:
                        if headword in inflections_set:
                            inflections_set.remove(headword)
                        arr.append(Entry(headword, inflections_set, body))
                    else:
                        arr.append(Entry(headword, {}, body))
                    cnt += 1
                    print("> Parsed", f"{cnt:,}", "entries.", end="\r")

    print()

    meta = get_metadata(path=html)
    Glossary.init()
    glos = Glossary()
    glos.setInfo("title", set_metadata('Title',meta,dict_name,author))
    glos.setInfo("author", set_metadata('Creator',meta,dict_name,author))
    glos.setInfo("description", set_metadata('Desc',meta,dict_name,author))
    glos.setInfo("date", set_metadata('Date',meta,dict_name,author))

    arr.sort(key=lambda x: (x.HW.encode("utf-8").lower(), x.HW))

    for entry in arr:
        hw = [entry.HW]
        hw.extend(entry.INFL)
        glos.addEntry(
            glos.newEntry(
                word=hw,
                defi=entry.BODY,
                defiFormat="h"
            )
        )

    outfolder = SCRIPT_DIR / "output"
    if not outfolder.exists():
        outfolder.mkdir()
    fname = outfolder / "book_stardict"
    glos.write(str(fname), "Stardict", dictzip=False)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=
    """
    Convert unpacked Kindle MOBI dictionary files (book.html or part00000.html)
    to Stardict dictionary Format.
    StarDict format can be converted to a wide-range of formats directly via PyGlossary.

    You can unpack MOBI files via 'KindleUnpack' or its Calibre plugin. Alternatively, you can use mobitool from libmobi project.
    """)
    parser.add_argument('--html-file', default='part00000.html',
        help="Path of the HTML file.")
    parser.add_argument('--fix-links', action='store_true',
        help="Try to convert in-dictionary references to glossary format.")
    parser.add_argument('--dict-name', default="part00000",
        help="Name of the dictionary file.")
    parser.add_argument('--author', default="author",
        help="Name of the author or publisher.")
    parser.add_argument('--chunked', action='store_true',
        help="Parse html in chunks to reduce memory usage.")
    args = parser.parse_args()
    convert(args.html_file, args.dict_name, args.author, args.fix_links, args.chunked)
