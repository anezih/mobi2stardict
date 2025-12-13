namespace MobiDict.Reader;

public static class Utils
{
    private static readonly Dictionary<uint, Dictionary<uint, string>> Languages = new()
    {
        { 54, new Dictionary<uint, string> { { 0, "af" } } },  // Afrikaans
        { 28, new Dictionary<uint, string> { { 0, "sq" } } },  // Albanian
        { 1, new Dictionary<uint, string> // Arabic
            {
                { 0, "ar" }, { 5, "ar-dz" }, { 15, "ar-bh" }, { 3, "ar-eg" },
                { 2, "ar-iq" }, { 11, "ar-jo" }, { 13, "ar-kw" }, { 12, "ar-lb" },
                { 4, "ar-ly" }, { 6, "ar-ma" }, { 8, "ar-om" }, { 16, "ar-qa" },
                { 1, "ar-sa" }, { 10, "ar-sy" }, { 7, "ar-tn" }, { 14, "ar-ae" },
                { 9, "ar-ye" }
            }
        },
        { 43, new Dictionary<uint, string> { { 0, "hy" } } },  // Armenian
        { 77, new Dictionary<uint, string> { { 0, "as" } } },  // Assamese
        { 44, new Dictionary<uint, string> { { 0, "az" } } },  // "Azeri (IANA: Azerbaijani)
        { 45, new Dictionary<uint, string> { { 0, "eu" } } },  // Basque
        { 35, new Dictionary<uint, string> { { 0, "be" } } },  // Belarusian
        { 69, new Dictionary<uint, string> { { 0, "bn" } } },  // Bengali
        { 2, new Dictionary<uint, string> { { 0, "bg" } } },  // Bulgarian
        { 3, new Dictionary<uint, string> { { 0, "ca" } } },  // Catalan
        { 4, new Dictionary<uint, string> // Chinese
            {
                { 0, "zh" }, { 3, "zh-hk" }, { 2, "zh-cn" }, { 4, "zh-sg" }, { 1, "zh-tw" }
            }
        },
        { 26, new Dictionary<uint, string> { { 0, "hr" }, { 3, "sr" } } },  // Croatian, Serbian
        { 5, new Dictionary<uint, string> { { 0, "cs" } } },  // Czech
        { 6, new Dictionary<uint, string> { { 0, "da" } } },  // Danish
        { 19, new Dictionary<uint, string> { { 0, "nl" }, { 1, "nl" }, { 2, "nl-be" } } },  // Dutch / Flemish
        { 9, new Dictionary<uint, string> // English
            {
                { 0, "en" }, { 1, "en" }, { 3, "en-au" }, { 40, "en-bz" }, { 4, "en-ca" },
                { 6, "en-ie" }, { 8, "en-jm" }, { 5, "en-nz" }, { 13, "en-ph" },
                { 7, "en-za" }, { 11, "en-tt" }, { 2, "en-gb" }, 
                // { 1, "en-us" } // 1 zaten yukarıda tanımlı
                { 12, "en-zw" }
            }
        },
        { 37, new Dictionary<uint, string> { { 0, "et" } } },  // Estonian
        { 56, new Dictionary<uint, string> { { 0, "fo" } } },  // Faroese
        { 41, new Dictionary<uint, string> { { 0, "fa" } } },  // Farsi / Persian
        { 11, new Dictionary<uint, string> { { 0, "fi" } } },  // Finnish
        { 12, new Dictionary<uint, string> // French
            {
                { 0, "fr" }, { 1, "fr" }, { 2, "fr-be" }, { 3, "fr-ca" },
                { 5, "fr-lu" }, { 6, "fr-mc" }, { 4, "fr-ch" }
            }
        },
        { 55, new Dictionary<uint, string> { { 0, "ka" } } },  // Georgian
        { 7, new Dictionary<uint, string> // German
            {
                { 0, "de" }, { 1, "de" }, { 3, "de-at" }, { 5, "de-li" },
                { 4, "de-lu" }, { 2, "de-ch" }
            }
        },
        { 8, new Dictionary<uint, string> { { 0, "el" } } },  // Greek
        { 71, new Dictionary<uint, string> { { 0, "gu" } } },  // Gujarati
        { 13, new Dictionary<uint, string> { { 0, "he" } } },  // Hebrew
        { 57, new Dictionary<uint, string> { { 0, "hi" } } },  // Hindi
        { 14, new Dictionary<uint, string> { { 0, "hu" } } },  // Hungarian
        { 15, new Dictionary<uint, string> { { 0, "is" } } },  // Icelandic
        { 33, new Dictionary<uint, string> { { 0, "id" } } },  // Indonesian
        { 16, new Dictionary<uint, string> { { 0, "it" }, { 1, "it" }, { 2, "it-ch" } } },  // Italian
        { 17, new Dictionary<uint, string> { { 0, "ja" } } },  // Japanese
        { 75, new Dictionary<uint, string> { { 0, "kn" } } },  // Kannada
        { 63, new Dictionary<uint, string> { { 0, "kk" } } },  // Kazakh
        { 87, new Dictionary<uint, string> { { 0, "x-kok" } } },  // Konkani
        { 18, new Dictionary<uint, string> { { 0, "ko" } } },  // Korean
        { 38, new Dictionary<uint, string> { { 0, "lv" } } },  // Latvian
        { 39, new Dictionary<uint, string> { { 0, "lt" } } },  // Lithuanian
        { 47, new Dictionary<uint, string> { { 0, "mk" } } },  // Macedonian
        { 62, new Dictionary<uint, string> { { 0, "ms" } } },  // Malay
        { 76, new Dictionary<uint, string> { { 0, "ml" } } },  // Malayalam
        { 58, new Dictionary<uint, string> { { 0, "mt" } } },  // Maltese
        { 78, new Dictionary<uint, string> { { 0, "mr" } } },  // Marathi
        { 97, new Dictionary<uint, string> { { 0, "ne" } } },  // Nepali
        { 20, new Dictionary<uint, string> { { 0, "no" } } },  // Norwegian
        { 72, new Dictionary<uint, string> { { 0, "or" } } },  // Oriya
        { 21, new Dictionary<uint, string> { { 0, "pl" } } },  // Polish
        { 22, new Dictionary<uint, string> { { 0, "pt" }, { 2, "pt" }, { 1, "pt-br" } } },  // Portuguese
        { 70, new Dictionary<uint, string> { { 0, "pa" } } },  // Punjabi
        { 23, new Dictionary<uint, string> { { 0, "rm" } } },  // Romansh
        { 24, new Dictionary<uint, string> { { 0, "ro" } } },  // Romanian
        { 25, new Dictionary<uint, string> { { 0, "ru" } } },  // Russian
        { 59, new Dictionary<uint, string> { { 0, "sz" } } },  // "Sami (Lappish)"
        { 79, new Dictionary<uint, string> { { 0, "sa" } } },  // Sanskrit
        { 27, new Dictionary<uint, string> { { 0, "sk" } } },  // Slovak
        { 36, new Dictionary<uint, string> { { 0, "sl" } } },  // Slovenian
        { 46, new Dictionary<uint, string> { { 0, "sb" } } },  // "Sorbian"
        { 10, new Dictionary<uint, string> // Spanish
            {
                { 0, "es" }, { 4, "es" }, { 44, "es-ar" }, { 64, "es-bo" }, { 52, "es-cl" },
                { 36, "es-co" }, { 20, "es-cr" }, { 28, "es-do" }, { 48, "es-ec" },
                { 68, "es-sv" }, { 16, "es-gt" }, { 72, "es-hn" }, { 8, "es-mx" },
                { 76, "es-ni" }, { 24, "es-pa" }, { 60, "es-py" }, { 40, "es-pe" },
                { 80, "es-pr" }, { 56, "es-uy" }, { 32, "es-ve" }
            }
        },
        { 48, new Dictionary<uint, string> { { 0, "sx" } } },  // "Sutu"
        { 65, new Dictionary<uint, string> { { 0, "sw" } } },  // Swahili
        { 29, new Dictionary<uint, string> { { 0, "sv" }, { 1, "sv" }, { 8, "sv-fi" } } },  // Swedish
        { 73, new Dictionary<uint, string> { { 0, "ta" } } },  // Tamil
        { 68, new Dictionary<uint, string> { { 0, "tt" } } },  // Tatar
        { 74, new Dictionary<uint, string> { { 0, "te" } } },  // Telugu
        { 30, new Dictionary<uint, string> { { 0, "th" } } },  // Thai
        { 49, new Dictionary<uint, string> { { 0, "ts" } } },  // Tsonga
        { 50, new Dictionary<uint, string> { { 0, "tn" } } },  // Tswana
        { 31, new Dictionary<uint, string> { { 0, "tr" } } },  // Turkish
        { 34, new Dictionary<uint, string> { { 0, "uk" } } },  // Ukrainian
        { 32, new Dictionary<uint, string> { { 0, "ur" } } },  // Urdu
        { 67, new Dictionary<uint, string> { { 0, "uz" }, { 2, "uz" } } },  // Uzbek
        { 42, new Dictionary<uint, string> { { 0, "vi" } } },  // Vietnamese
        { 52, new Dictionary<uint, string> { { 0, "xh" } } },  // Xhosa
        { 53, new Dictionary<uint, string> { { 0, "zu" } } },  // Zulu
    };

    public static readonly Dictionary<uint, string> MetadataStrings = new()
    {
        [1] = "Drm Server Id",
        [2] = "Drm Commerce Id",
        [3] = "Drm Ebookbase Book Id",
        [4] = "Drm Ebookbase Dep Id",
        [100] = "Creator",
        [101] = "Publisher",
        [102] = "Imprint",
        [103] = "Description",
        [104] = "ISBN",
        [105] = "Subject",
        [106] = "Published",
        [107] = "Review",
        [108] = "Contributor",
        [109] = "Rights",
        [110] = "SubjectCode",
        [111] = "Type",
        [112] = "Source",
        [113] = "ASIN",
        [114] = "versionNumber",
        [117] = "Adult",
        [118] = "Retail-Price",
        [119] = "Retail-Currency",
        [120] = "TSC",
        [122] = "fixed-layout",
        [123] = "book-type",
        [124] = "orientation-lock",
        [126] = "original-resolution",
        [127] = "zero-gutter",
        [128] = "zero-margin",
        [129] = "MetadataResourceURI",
        [132] = "RegionMagnification",
        [150] = "LendingEnabled",
        [200] = "DictShortName",
        [501] = "cdeType",
        [502] = "last_update_time",
        [503] = "Updated_Title",
        [504] = "CDEContentKey",
        [505] = "AmazonContentReference",
        [506] = "Title-Language",
        [507] = "Title-Display-Direction",
        [508] = "Title-Pronunciation",
        [509] = "Title-Collation",
        [510] = "Secondary-Title",
        [511] = "Secondary-Title-Language",
        [512] = "Secondary-Title-Direction",
        [513] = "Secondary-Title-Pronunciation",
        [514] = "Secondary-Title-Collation",
        [515] = "Author-Language",
        [516] = "Author-Display-Direction",
        [517] = "Author-Pronunciation",
        [518] = "Author-Collation",
        [519] = "Author-Type",
        [520] = "Publisher-Language",
        [521] = "Publisher-Display-Direction",
        [522] = "Publisher-Pronunciation",
        [523] = "Publisher-Collation",
        [524] = "Content-Language-Tag",
        [525] = "primary-writing-mode",
        [526] = "NCX-Ingested-By-Software",
        [527] = "page-progression-direction",
        [528] = "override-kindle-fonts",
        [529] = "Compression-Upgraded",
        [530] = "Soft-Hyphens-In-Content",
        [531] = "Dictionary_In_Langague",
        [532] = "Dictionary_Out_Language",
        [533] = "Font_Converted",
        [534] = "Amazon_Creator_Info",
        [535] = "Creator-Build-Tag",
        [536] = "HD-Media-Containers-Info",  //  CONT_Header is 0, Ends with CONTAINER_BOUNDARY (or Asset_Type?)
        [538] = "Resource-Container-Fidelity",
        [539] = "HD-Container-Mimetype",
        [540] = "Sample-For_Special-Purpose",
        [541] = "Kindletool-Operation-Information",
        [542] = "Container_Id",
        [543] = "Asset-Type",  // FONT_CONTAINER, BW_CONTAINER, HD_CONTAINER
        [544] = "Unknown_544",
    };

    public static string GetLangCode(uint langId, uint subLangId)
    {
        var lang = string.Empty;
        if (Languages.TryGetValue(langId, out var subDict))
        {
            if (subDict.TryGetValue(subLangId, out var code))
            {
                lang = code;
            }
            else if (subDict.TryGetValue(0, out var defaultCode))
            {
                lang = defaultCode;
            }
        }
        return lang;
    }
}
