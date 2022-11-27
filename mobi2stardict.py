from bs4 import BeautifulSoup
import argparse
import re
import sys

def fix(body_obj):
    new_body = body_obj
    temp = BeautifulSoup(body_obj, "lxml")
    links = temp.find_all("a", href=True)
    for link in links:
        new_body = re.sub(link["href"], f"bword://{link.getText()}", new_body)
    return new_body


def convert(html, dict_name, author, fix_links):
    try:
        with open(html, "r", encoding="utf-8") as f:
            book = f.read()
    except FileNotFoundError:
        sys.exit("Could not open the file. Check the filename.")
    
    soup = BeautifulSoup(book, "lxml")
    entries = soup.find_all("idx:entry")
    
    with open("book.gls", "w", encoding="utf-8") as d:
        d.write(f"\n#stripmethod=keep\n#sametypesequence=h\n#bookname={dict_name}\n#author={author}\n\n")
        
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
                inflections_list = None

                if inflections:
                    inflections = inflections.find_all("idx:iform")
                    inflections_list = [i.get("value") for i in inflections]

                body_re = re.search("</idx:orth>(.*?)</idx:entry>", str(e))
                body = body_re.group(1)
                if not body:
                    b = []
                    ns = e.next_siblings
                    while (t := next(ns, "")) and not str(t).startswith("<idx:entry"):
                        b.append(str(t))
                    body = "".join(b)
                    # body = "".join([str(i) for i in e.next_siblings if not str(i).startswith("<idx:entry>")])
                if not body:
                    continue

                if fix_links:
                    body = fix(body)

                if inflections_list:
                    headwords = f"{headword}|"
                    headwords += "|".join(inflections_list)
                    single_def = f"{headwords}\n{body}\n\n"
                    d.write(single_def)
                else:
                    single_def = f"{headword}\n{body}\n\n"
                    d.write(single_def)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=
    """
    Convert unpacked Kindle MOBI dictionary files (book.html) to Babylon Glossary source files (.gls). This source file can later
    be converted to stardict format via StarDict Editor.
    
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
    args = parser.parse_args()

    convert(args.html_file ,args.dict_name, args.author, args.fix_links)
