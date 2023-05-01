#include <algorithm>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <regex>
#include <vector>

#include "mobi.h"

// https://github.com/bfabiszewski/libmobi/blob/public/src/index.c#L798
size_t mobi_get_indxentry_tagarray(uint32_t **tagarr, const MOBIIndexEntry *entry, const size_t tagid) {
    if (entry == NULL) {
        // debug_print("%s", "INDX entry not initialized\n");
        return 0;
    }
    size_t i = 0;
    while (i < entry->tags_count) {
        if (entry->tags[i].tagid == tagid) {
            *tagarr = entry->tags[i].tagvalues;
            return entry->tags[i].tagvalues_count;
        }
        i++;
    }
    //debug_print("tag[%zu] not found in entry: %s\n", tagid, entry->label);
    return 0;
}

// https://github.com/bfabiszewski/libmobi/blob/public/src/index.c#L960
MOBI_RET mobi_decode_infl(unsigned char *decoded, int *decoded_size, const unsigned char *rule) {
    int pos = *decoded_size;
    char mod = 'i';
    char dir = '<';
    char olddir;
    unsigned char c;
    while ((c = *rule++)) {
        if (c <= 4) {
            mod = (c <= 2) ? 'i' : 'd'; /* insert, delete */
            olddir = dir;
            dir = (c & 2) ? '<' : '>'; /* left, right */
            if (olddir != dir && olddir) {
                pos = (c & 2) ? *decoded_size : 0;
            }
        }
        else if (c > 10 && c < 20) {
            if (dir == '>') {
                pos = *decoded_size;
            }
            pos -= c - 10;
            dir = 0;
        }
        else {
            if (mod == 'i') {
                const unsigned char *s = decoded + pos;
                unsigned char *d = decoded + pos + 1;
                const int l = *decoded_size - pos;
                if (pos < 0 || l < 0 || d + l > decoded + 500) {
                    // debug_print("Out of buffer in %s at pos: %i\n", decoded, pos);
                    return MOBI_DATA_CORRUPT;
                }
                memmove(d, s, (size_t) l);
                decoded[pos] = c;
                (*decoded_size)++;
                if (dir == '>') { pos++; }
            } else {
                if (dir == '<') { pos--; }
                const unsigned char *s = decoded + pos + 1;
                unsigned char *d = decoded + pos;
                const int l = *decoded_size - pos;
                if (pos < 0 || l < 0 || s + l > decoded + 500) {
                    // debug_print("Out of buffer in %s at pos: %i\n", decoded, pos);
                    return MOBI_DATA_CORRUPT;
                }
                if (decoded[pos] != c) {
                    // debug_print("Character mismatch in %s at pos: %i (%c != %c)\n", decoded, pos, decoded[pos], c);
                    return MOBI_DATA_CORRUPT;
                }
                memmove(d, s, (size_t) l);
                (*decoded_size)--;
            }
        }
    }
    return MOBI_SUCCESS;
}

struct Entry
{
    std::string HEADWORD;
    std::vector<std::string> INFL;
    std::string DEF;
};

void freeResources(MOBIData* mobiData, MOBIRawml* mobiRawml)
{
    mobi_free(mobiData);
    mobi_free_rawml(mobiRawml);
}

MOBI_RET convert(char* fileName, char* serial = NULL)
{
    MOBIData* mobiData;
    MOBIRawml* mobiRawml;
    std::regex link("<a\\s+filepos[^>]+>(.*?)</a>", std::regex_constants::ECMAScript);

    mobiData = mobi_init();
    if (mobiData == nullptr)
    {
        std::cout << "MOBI_MALLOC_FAILED" << std::endl;
        freeResources(mobiData, mobiRawml);
        return MOBI_MALLOC_FAILED;
    }

    FILE* file = fopen(fileName, "rb");
    if (file == nullptr)
    {
        std::cout << "MOBI_ERROR" << std::endl;
        freeResources(mobiData, mobiRawml);
        return MOBI_ERROR;
    }

    auto mobiRet = mobi_load_file(mobiData, file);
    fclose(file);

    if (serial != NULL)
    {
        auto ret = mobi_drm_setkey_serial(mobiData, serial);
        if (ret != MOBI_SUCCESS)
        {
            std::cout << "Invalid serial key." << std::endl;
            freeResources(mobiData, mobiRawml);
            return MOBI_FILE_ENCRYPTED;
        }
    }

    if (mobi_is_encrypted(mobiData))
    {
        std::cout << "MOBI_FILE_ENCRYPTED" << std::endl;
        freeResources(mobiData, mobiRawml);
        return MOBI_FILE_ENCRYPTED;
    }

    if (mobiRet != MOBI_SUCCESS)
    {
        std::cout << "Couldn't load MOBI file." << std::endl;
        freeResources(mobiData, mobiRawml);
        return mobiRet;
    }

    auto cTitle     = mobi_meta_get_title(mobiData);
    auto cLang      = mobi_meta_get_language(mobiData);
    auto cAuthor    = mobi_meta_get_author(mobiData);
    auto cDesc      = mobi_meta_get_description(mobiData);
    auto cPublisher = mobi_meta_get_publisher(mobiData);
    auto cPubDate   = mobi_meta_get_publishdate(mobiData);

    auto title     = cTitle     ? std::string(cTitle)     : std::string();
    auto lang      = cLang      ? std::string(cLang)      : std::string();
    auto author    = cAuthor    ? std::string(cAuthor)    : std::string();
    auto desc      = cDesc      ? std::string(cDesc)      : std::string();
    auto publisher = cPublisher ? std::string(cPublisher) : std::string();
    auto pubDate   = cPubDate   ? std::string(cPubDate)   : std::string();

    free(cTitle);
    free(cLang);
    free(cAuthor);
    free(cDesc);
    free(cPublisher);
    free(cPubDate);

    mobiRawml = mobi_init_rawml(mobiData);
    if (mobiRawml == nullptr)
    {
        std::cout << "MOBI_MALLOC_FAILED" << std::endl;
        freeResources(mobiData, mobiRawml);
        return MOBI_MALLOC_FAILED;
    }

    auto mobiRawmlParseRet = mobi_parse_rawml_opt(mobiRawml, mobiData, true, true, false);
    if (mobiRawmlParseRet != MOBI_SUCCESS)
    {
        std::cout << "Couldn't parse rawml" << std::endl;
        freeResources(mobiData, mobiRawml);
        return mobiRawmlParseRet;
    }

    if (!mobiRawml->orth)
    {
        std::cout << "Not a dictionary file." << std::endl;
        freeResources(mobiData, mobiRawml);
        return MOBI_FILE_UNSUPPORTED;
    }

    const size_t entryCount = mobiRawml->orth->total_entries_count;
    std::cout << "Total entry count: " << entryCount << std::endl;
    std::vector<Entry> allEntries;

    uint32_t entryStartPos = 0;
    uint32_t entryTextLen  = 0;
    const bool isInfl = mobi_exists_infl(mobiData);

    for (size_t i = 0; i < entryCount; ++i)
    {
        const auto orthEntry = &mobiRawml->orth->entries[i];
        entryStartPos = mobi_get_orth_entry_offset(orthEntry);
        entryTextLen  = mobi_get_orth_entry_length(orthEntry);

        if (entryStartPos == 0 || entryTextLen == 0)
        {
            std::cout << "Poorly formatted definition body." << std::endl;
            ++i;
            continue;
        }
        Entry entry;
        entry.HEADWORD = std::string(orthEntry->label);
        auto def = std::string(
            mobiRawml->flow->data + entryStartPos,
            mobiRawml->flow->data + entryStartPos + entryTextLen
        );

        auto defBword = std::regex_replace(def, link, ("<a href=\"bword://$1\">$1</a>"));
        entry.DEF = defBword;

        if (isInfl)
        {
            uint32_t* inflGroups = NULL;
            size_t inflCount = mobi_get_indxentry_tagarray(&inflGroups, orthEntry, 42);

            if (inflCount == 0 || !inflGroups)
            {
                allEntries.push_back(entry);
                continue;
            }

            if (mobiRawml->infl->cncx_record == NULL)
            {
                std::cout << "Missing cncx record" << std::endl;
                freeResources(mobiData, mobiRawml);
                return MOBI_DATA_CORRUPT;
            }

            for (size_t i = 0; i < inflCount; i++)
            {
                size_t offset = inflGroups[i];
                if (offset >= mobiRawml->infl->entries_count)
                {
                    std::cout << "Invalid entry offset" << std::endl;
                    freeResources(mobiData, mobiRawml);
                    return MOBI_DATA_CORRUPT;
                }

                uint32_t *groups;
                size_t groupCnt = mobi_get_indxentry_tagarray(&groups, &mobiRawml->infl->entries[offset], 5); // INDX_TAGARR_INFL_GROUPS
                uint32_t *parts;
                size_t partCnt = mobi_get_indxentry_tagarray(&parts, &mobiRawml->infl->entries[offset], 26); // INDX_TAGARR_INFL_PARTS_V2

                if (groupCnt != partCnt)
                {
                    std::cout << "groupCnt != partCnt" << std::endl;
                    freeResources(mobiData, mobiRawml);
                    return MOBI_DATA_CORRUPT;
                }

                for (size_t j = 0; j < partCnt; j++)
                {
                    auto rule = (unsigned char*)mobiRawml->infl->entries[parts[j]].label;
                    unsigned char decoded[501];
                    int decodedLength = 0;
                    auto inflRet = mobi_decode_infl(decoded, &decodedLength, rule);
                    if (inflRet != MOBI_SUCCESS || decodedLength == 0)
                    {
                        continue;
                    }
                    auto inflRule = std::string(decoded, decoded+decodedLength);
                    auto infl = entry.HEADWORD + inflRule;
                    entry.INFL.push_back(infl);
                }
                allEntries.push_back(entry);
            }
        }
    }

    auto const outName = std::filesystem::path(fileName).stem().string();
    std::ofstream textualFile((outName + ".xml"));

    textualFile << "<?xml version='1.0' encoding='UTF-8'?>\n";
    textualFile << "<stardict>\n";
    textualFile << "  <info>\n";
    textualFile << "    <version>" << "3.0.0" << "</version>\n";
    textualFile << "    <bookname>" << (title.empty() ? "" : title) << "</bookname>\n";
    textualFile << "    <author>" << (author.empty() ? "" : author) << "</author>\n";
    textualFile << "    <description>" << (desc.empty() ? "" : desc) << "</description>\n";
    textualFile << "    <email>" << "" << "</email>\n";
    textualFile << "    <website>" << "" << "</website>\n";
    textualFile << "    <date>" << "" << "</date>\n";
    textualFile << "    <dicttype>" << "" << "</dicttype>\n";
    textualFile << "  </info>\n";

    for (auto &&entry : allEntries)
    {
        textualFile << "  <article>\n";
        textualFile << "    <key>" << entry.HEADWORD << "</key>\n";
        if (!entry.INFL.empty())
        {
            for (auto &&infl : entry.INFL)
            {
                textualFile << "    <synonym>" << infl << "</synonym>\n";
            }
        }
        textualFile << "    <definition type=\"h\">" << "<![CDATA[" << entry.DEF << "]]>" << "</definition>\n";
        textualFile << "  </article>\n";
    }
    textualFile << "</stardict>";
    textualFile.close();

    freeResources(mobiData, mobiRawml);
    return MOBI_SUCCESS;
}

int main(int argc, char *argv[])
{
    if (argc < 2)
    {
        std::cout << "Missing filename." << std::endl;
        return -1;
    }

    if (argc > 2)
    {
        auto ret = convert(argv[1], argv[2]);
        std::cout << ret << std::endl;
    }

    else
    {
        auto ret = convert(argv[1]);
        std::cout << ret << std::endl;
    }
    return 0;
}