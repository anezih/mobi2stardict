from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

from bs4 import BeautifulSoup

import argparse
import glob
import re
import os
import sys

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
        _base_dir = os.getcwd()
    else:
        _base_dir = str(Path(path).parent.absolute().resolve())
    opf_path = glob.glob(os.path.join(_base_dir, "*.opf"))[0]
    try:
        with open(opf_path, "r", encoding="utf-8") as f:
            opf_soup = BeautifulSoup(f.read(), "lxml-xml")
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

def convert(html: str, dict_name: str, author: str, fix_links: bool,
            gls: bool, textual: bool, chunked: bool) -> None:
    try:
        with open(html, "r", encoding="utf-8") as f:
            book = f.read()
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
    arr = []
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
                headword = e.find("idx:orth").get("value")
                inflections = e.find("idx:infl")
                inflections_set = None

                if inflections:
                    inflections_set = {i.get("value") for i in inflections.find_all("idx:iform")}

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
    if gls:
        gls_metadata  = f"\n#stripmethod=keep\n#sametypesequence=h\n"
        gls_metadata += f"#bookname={set_metadata('Title',meta,dict_name,author)}\n"
        gls_metadata += f"#author={set_metadata('Creator',meta,dict_name,author)}\n\n"
        with open("book.gls", "w", encoding="utf-8") as d:
            d.write(gls_metadata)
            for entry in arr:
                if entry.INFL:
                    headwords = f"{entry.HW}|{'|'.join(entry.INFL)}"
                else:
                    headwords = entry.HW
                single_def = f"{headwords}\n{entry.BODY}\n\n"
                d.write(single_def)
                d.flush()
    if textual:
        from lxml import etree as ET

        root = ET.Element("stardict")

        info     = ET.SubElement(root, "info")
        version  = ET.SubElement(info, "version").text = "3.0.0"
        bookname = ET.SubElement(info, "bookname").text = set_metadata('Title',meta,dict_name,author)
        author_  = ET.SubElement(info, "author").text = set_metadata('Creator',meta,dict_name,author)
        desc     = ET.SubElement(info, "description").text = set_metadata('Desc',meta,dict_name,author)
        email    = ET.SubElement(info, "email").text = ""
        website  = ET.SubElement(info, "website").text = ""
        date     = ET.SubElement(info, "date").text = set_metadata('Date',meta,dict_name,author)
        dicttype = ET.SubElement(info, "dicttype").text = ""

        for entry in arr:
            article = ET.SubElement(root, "article")
            key     = ET.SubElement(article, "key").text = entry.HW
            for i in entry.INFL:
                syn = ET.SubElement(article, "synonym").text = i
            cdata   = ET.CDATA(entry.BODY)
            defi    = ET.SubElement(article, "definition")
            defi.attrib["type"] = "h"
            defi.text = cdata

        xml_str = ET.tostring(root, xml_declaration=True, encoding="UTF-8", pretty_print=True)
        with open("book_stardict_textual.xml", "wb") as d:
            d.write(xml_str)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=
    """
    Convert unpacked Kindle MOBI dictionary files (book.html or part00000.html)
    to Babylon Glossary source files (.gls) or to Stardict Textual Dictionary Format.
    These source files can later be converted to StarDict format via StarDict Editor.
    Textual xml format can be converted to a wide-range of formats directly via PyGlossary.

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
    parser.add_argument('--gls', action='store_true',
        help="Convert dictionary to Babylon glossary source.")
    parser.add_argument('--textual', action='store_true',
        help="Convert dictionary to Stardict Textual Dictionary Format.")
    parser.add_argument('--chunked', action='store_true',
        help="Parse html in chunks to reduce memory usage.")
    args = parser.parse_args()
    if not (args.gls or args.textual):
        sys.exit("You need to specify at least 1 output format: --gls, --textual or both.")
    convert(args.html_file, args.dict_name, args.author, args.fix_links,
            args.gls, args.textual, args.chunked)