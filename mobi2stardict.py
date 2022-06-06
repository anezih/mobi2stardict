from bs4 import BeautifulSoup
import argparse
import re

def fix(body_obj):
    new_body = body_obj
    temp = BeautifulSoup(body_obj, "lxml")
    links = temp.find_all("a", href=re.compile("#filepos\d+"))
    for link in links:
        new_body = re.sub(link["href"], f"bword://{link.getText()}", new_body)
    return new_body


def convert(dict_name, author, fix_links):
    
    with open("book.html", "r", encoding="utf-8") as f:
        book = f.read()
    
    soup = BeautifulSoup(book, "lxml")
    entries = soup.find_all("idx:entry")

    with open("book.gls", "w", encoding="utf-8") as d:
        d.write(f"\n#stripmethod=keep\n#sametypesequence=h\n#bookname={dict_name}\n#author={author}\n\n")
        
        for entry in entries:
            headword = entry.find("idx:orth").get("value")
            inflections = entry.find("idx:infl")
            inflections_list = None
            
            if inflections:
                inflections = inflections.find_all("idx:iform")
                inflections_list = [i.get("value") for i in inflections]
            
            body_re = re.search("</idx:orth>(.*?)</idx:entry>", str(entry))
            body = body_re.group(1)

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
    
    Put this script in the same folder where 'book.html' exists.

    You can unpack MOBI files via 'KindleUnpack' or its Calibre plugin. 
    """)
    parser.add_argument('--fix-links', action='store_true', 
        help="Try to convert in-dictionary references to glossary format.")
    parser.add_argument('--dict-name', default="book", 
        help="Name of the dictionary file.")
    parser.add_argument('--author', default="author",
        help="Name of the author or publisher.")
    args = parser.parse_args()

    convert(args.dict_name, args.author, args.fix_links)