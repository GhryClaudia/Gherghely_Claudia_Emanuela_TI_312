using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
class Program
{
    static void Main()
    {
        string folderTest = @"D:\An_III\Semestrul_1\Sisteme de recunoastere a formelor\ReutersDataSet\Reuters\Reuters_34\Testing";
        string folderAntrenare = @"D:\An_III\Semestrul_1\Sisteme de recunoastere a formelor\ReutersDataSet\Reuters\Reuters_34\Training";
        string fisierStopwords = @"D:\An_III\Semestrul_1\Gherghely_Claudia_Emanuela_TI_312\stopwords.txt";
        string fisierAntrenare = @"D:\An_III\Semestrul_1\Gherghely_Claudia_Emanuela_TI_312\fisierAntrenare.txt";
        string fisierTest = @"D:\An_III\Semestrul_1\Gherghely_Claudia_Emanuela_TI_312\fisierTest.txt";
        int nrAtributeDorite = 100;

        HashSet<string> stopwords = CitesteStopwords(fisierStopwords);

        List<string> vocabular = new List<string>();
        Dictionary<string, int> dictionarGlobal = new Dictionary<string, int>();
        List<Doc> documente = new List<Doc>();

        ProceseazaFolder(folderAntrenare, "Training", stopwords, vocabular, dictionarGlobal, documente);
        ProceseazaFolder(folderTest, "Testing", stopwords, vocabular, dictionarGlobal, documente);

        List<Doc> docsAntrenare = documente.Where(d => d.tipSet == "Training").ToList();
        List<Doc> docsTest = documente.Where(d => d.tipSet == "Testing").ToList();

        double[] castig = CalculeazaCastig(docsAntrenare, vocabular.Count);
        List<int> atributeAlese = AlegeAtribute(castig, nrAtributeDorite);
        Dictionary<int, int> indexNou = CreazaIndexNou(atributeAlese);

        ScrieFisier(fisierAntrenare, docsAntrenare, vocabular, castig, atributeAlese, indexNou);
        ScrieFisier(fisierTest, docsTest, vocabular, castig, atributeAlese, indexNou);

        Console.WriteLine("Fisiere generate.");
        Console.ReadLine();
    }

    static HashSet<string> CitesteStopwords(string cale)
    {
        HashSet<string> stopwords = new HashSet<string>();
        foreach (string linie in File.ReadAllLines(cale))
        {
            string cuvant = linie.Trim().ToLower();
            if (cuvant != "")
                stopwords.Add(cuvant);
        }
        return stopwords;
    }

    static string CurataCuvant(string cuvant)
    {
        foreach (char c in cuvant)
            if (char.IsDigit(c)) return "";
        return cuvant.ToLower();
    }

    static double Entropie(int nr1, int nr2)
    {
        int total = nr1 + nr2;
        double entropie = 0;
        if (nr1 > 0) entropie -= (double)nr1 / total * Math.Log((double)nr1 / total, 2);
        if (nr2 > 0) entropie -= (double)nr2 / total * Math.Log((double)nr2 / total, 2);
        return entropie;
    }

    static double[] CalculeazaCastig(List<Doc> docs, int nrAtribute)
    {
        int nrDocs = docs.Count;

        Dictionary<string, int> idCategorie = new Dictionary<string, int>();
        foreach (Doc doc in docs)
            foreach (string categorie in doc.categorii)
                if (!idCategorie.ContainsKey(categorie))
                    idCategorie[categorie] = idCategorie.Count;

        int nrCategorii = idCategorie.Count;
        int[] docsCuCategoria = new int[nrCategorii];
        int[] docsCuAtributul = new int[nrAtribute];
        int[,] docsCuAmbele = new int[nrAtribute, nrCategorii];

        foreach (Doc doc in docs)
        {
            int[] idCategoriiDoc = doc.categorii.Select(c => idCategorie[c]).ToArray();

            foreach (int cat in idCategoriiDoc)
                docsCuCategoria[cat]++;

            foreach (int atribut in doc.frecvente.Keys)
            {
                docsCuAtributul[atribut]++;
                foreach (int cat in idCategoriiDoc)
                    docsCuAmbele[atribut, cat]++;
            }
        }

        double[] entropieCategorie = new double[nrCategorii];
        for (int cat = 0; cat < nrCategorii; cat++)
            entropieCategorie[cat] = Entropie(docsCuCategoria[cat], nrDocs - docsCuCategoria[cat]);

        double[] castig = new double[nrAtribute];
        for (int atribut = 0; atribut < nrAtribute; atribut++)
        {
            if (docsCuAtributul[atribut] < 3) continue;

            int cuAtribut = docsCuAtributul[atribut];
            int faraAtribut = nrDocs - cuAtribut;
            double castigMax = 0;

            for (int cat = 0; cat < nrCategorii; cat++)
            {
                int cuAtributCuCat = docsCuAmbele[atribut, cat];
                int cuAtributFaraCat = cuAtribut - cuAtributCuCat;
                int faraAtributCuCat = docsCuCategoria[cat] - cuAtributCuCat;
                int faraAtributFaraCat = faraAtribut - faraAtributCuCat;

                double castigCurent = entropieCategorie[cat]
                    - (double)cuAtribut / nrDocs * Entropie(cuAtributCuCat, cuAtributFaraCat)
                    - (double)faraAtribut / nrDocs * Entropie(faraAtributCuCat, faraAtributFaraCat);

                if (castigCurent > castigMax) castigMax = castigCurent;
            }
            castig[atribut] = castigMax;
        }
        return castig;
    }

    static List<int> AlegeAtribute(double[] castig, int nrDorit)
    {
        return Enumerable.Range(0, castig.Length)
                         .OrderByDescending(i => castig[i])
                         .Take(nrDorit)
                         .OrderBy(i => i)
                         .ToList();
    }

    static Dictionary<int, int> CreazaIndexNou(List<int> atributeAlese)
    {
        Dictionary<int, int> indexNou = new Dictionary<int, int>();
        for (int poz = 0; poz < atributeAlese.Count; poz++)
            indexNou[atributeAlese[poz]] = poz;
        return indexNou;
    }

    static void ScrieFisier(string cale, List<Doc> docs, List<string> vocabular,
                            double[] castig, List<int> atributeAlese, Dictionary<int, int> indexNou)
    {
        using (StreamWriter writer = new StreamWriter(cale))
        {
            foreach (int indexVechi in atributeAlese)
                writer.WriteLine("@attribute " + vocabular[indexVechi] + " "
                    + castig[indexVechi].ToString("0.######", CultureInfo.InvariantCulture));

            writer.WriteLine("@data");

            foreach (Doc doc in docs)
            {
                string linie = doc.id + " #";
                foreach (KeyValuePair<int, int> pereche in doc.frecvente.OrderBy(p => p.Key))
                    if (indexNou.ContainsKey(pereche.Key))
                        linie += " " + (indexNou[pereche.Key] + 1) + ":" + pereche.Value;

                linie += " # " + string.Join(" ", doc.categorii) + " # " + doc.tipSet;
                writer.WriteLine(linie);
            }
        }
    }

    static void ProceseazaFolder(string folder, string tipSet, HashSet<string> stopwords,
                                 List<string> vocabular, Dictionary<string, int> dictionarGlobal, List<Doc> documente)
    {
        PorterStemmer stemmer = new PorterStemmer();

        string[] caiFisiere = Directory.GetFiles(folder, "*.xml");
        Array.Sort(caiFisiere);

        foreach (string caleFisier in caiFisiere)
        {
            try
            {
                XDocument xml = XDocument.Load(caleFisier);
                Dictionary<int, int> frecvente = new Dictionary<int, int>();
                List<string> categorii = new List<string>();

                foreach (XElement element in xml.Descendants())
                {
                    string numeElement = element.Name.LocalName;

                    if (numeElement == "title" || numeElement == "text")
                    {
                        string[] cuvinteBrute = Regex.Split(element.Value, "[^a-zA-Z0-9]+");

                        foreach (string cuvantBrut in cuvinteBrute)
                        {
                            string cuvant = CurataCuvant(cuvantBrut);

                            if (cuvant == "" || stopwords.Contains(cuvant))
                                continue;

                            string radacina = stemmer.StemWord(cuvant);

                            if (!dictionarGlobal.ContainsKey(radacina))
                            {
                                dictionarGlobal[radacina] = vocabular.Count;
                                vocabular.Add(radacina);
                            }

                            int index = dictionarGlobal[radacina];

                            if (frecvente.ContainsKey(index))
                                frecvente[index]++;
                            else
                                frecvente[index] = 1;
                        }
                    }

                    if (numeElement == "codes" && (string)element.Attribute("class") == "bip:topics:1.0")
                    {
                        foreach (XElement elementCod in element.Elements())
                        {
                            string codCategorie = (string)elementCod.Attribute("code");

                            if (!string.IsNullOrWhiteSpace(codCategorie))
                                categorii.Add(codCategorie);
                        }
                    }
                }

                Doc document = new Doc();
                document.id = Path.GetFileNameWithoutExtension(caleFisier);
                document.frecvente = frecvente;
                document.categorii = categorii;
                document.tipSet = tipSet;
                documente.Add(document);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare la " + caleFisier + ": " + ex.Message);
            }
        }
    }
}
class Doc
{
    public string id;
    public Dictionary<int, int> frecvente;
    public List<string> categorii;
    public string tipSet;
}
class PorterStemmer
{
    // The passed in word turned into a char array. 
    // Quicker to use to rebuilding strings each time a change is made.
    private char[] wordArray;

    // Current index to the end of the word in the character array. This will
    // change as the end of the string gets modified.
    private int endIndex;

    // Index of the (potential) end of the stem word in the char array.
    private int stemIndex;


    /// <summary>
    /// Stem the passed in word.
    /// </summary>
    /// <param name="word">Word to evaluate</param>
    /// <returns></returns>
    public string StemWord(string word)
    {

        // Do nothing for empty strings or short words.
        if (string.IsNullOrWhiteSpace(word) || word.Length <= 2) return word;

        wordArray = word.ToCharArray();

        stemIndex = 0;
        endIndex = word.Length - 1;
        Step1();
        Step2();
        Step3();
        Step4();
        Step5();
        Step6();

        var length = endIndex + 1;
        return new String(wordArray, 0, length);
    }


    // Step1() gets rid of plurals and -ed or -ing.
    /* Examples:
           caresses  ->  caress
           ponies    ->  poni
           ties      ->  ti
           caress    ->  caress
           cats      ->  cat

           feed      ->  feed
           agreed    ->  agree
           disabled  ->  disable

           matting   ->  mat
           mating    ->  mate
           meeting   ->  meet
           milling   ->  mill
           messing   ->  mess

           meetings  ->  meet  		*/
    private void Step1()
    {
        // If the word ends with s take that off
        if (wordArray[endIndex] == 's')
        {
            if (EndsWith("sses"))
            {
                endIndex -= 2;
            }
            else if (EndsWith("ies"))
            {
                SetEnd("i");
            }
            else if (wordArray[endIndex - 1] != 's')
            {
                endIndex--;
            }
        }
        if (EndsWith("eed"))
        {
            if (MeasureConsontantSequence() > 0)
                endIndex--;
        }
        else if ((EndsWith("ed") || EndsWith("ing")) && VowelInStem())
        {
            endIndex = stemIndex;
            if (EndsWith("at"))
                SetEnd("ate");
            else if (EndsWith("bl"))
                SetEnd("ble");
            else if (EndsWith("iz"))
                SetEnd("ize");
            else if (IsDoubleConsontant(endIndex))
            {
                endIndex--;
                int ch = wordArray[endIndex];
                if (ch == 'l' || ch == 's' || ch == 'z')
                    endIndex++;
            }
            else if (MeasureConsontantSequence() == 1 && IsCVC(endIndex)) SetEnd("e");
        }
    }

    // Step2() turns terminal y to i when there is another vowel in the stem.
    private void Step2()
    {
        if (EndsWith("y") && VowelInStem())
            wordArray[endIndex] = 'i';
    }

    // Step3() maps double suffices to single ones. so -ization ( = -ize plus
    // -ation) maps to -ize etc. note that the string before the suffix must give m() > 0. 
    private void Step3()
    {
        if (endIndex == 0) return;

        /* For Bug 1 */
        switch (wordArray[endIndex - 1])
        {
            case 'a':
                if (EndsWith("ational")) { ReplaceEnd("ate"); break; }
                if (EndsWith("tional")) { ReplaceEnd("tion"); }
                break;
            case 'c':
                if (EndsWith("enci")) { ReplaceEnd("ence"); break; }
                if (EndsWith("anci")) { ReplaceEnd("ance"); }
                break;
            case 'e':
                if (EndsWith("izer")) { ReplaceEnd("ize"); }
                break;
            case 'l':
                if (EndsWith("bli")) { ReplaceEnd("ble"); break; }
                if (EndsWith("alli")) { ReplaceEnd("al"); break; }
                if (EndsWith("entli")) { ReplaceEnd("ent"); break; }
                if (EndsWith("eli")) { ReplaceEnd("e"); break; }
                if (EndsWith("ousli")) { ReplaceEnd("ous"); }
                break;
            case 'o':
                if (EndsWith("ization")) { ReplaceEnd("ize"); break; }
                if (EndsWith("ation")) { ReplaceEnd("ate"); break; }
                if (EndsWith("ator")) { ReplaceEnd("ate"); }
                break;
            case 's':
                if (EndsWith("alism")) { ReplaceEnd("al"); break; }
                if (EndsWith("iveness")) { ReplaceEnd("ive"); break; }
                if (EndsWith("fulness")) { ReplaceEnd("ful"); break; }
                if (EndsWith("ousness")) { ReplaceEnd("ous"); }
                break;
            case 't':
                if (EndsWith("aliti")) { ReplaceEnd("al"); break; }
                if (EndsWith("iviti")) { ReplaceEnd("ive"); break; }
                if (EndsWith("biliti")) { ReplaceEnd("ble"); }
                break;
            case 'g':
                if (EndsWith("logi"))
                {
                    ReplaceEnd("log");
                }
                break;
        }
    }

    /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
    private void Step4()
    {
        switch (wordArray[endIndex])
        {
            case 'e':
                if (EndsWith("icate")) { ReplaceEnd("ic"); break; }
                if (EndsWith("ative")) { ReplaceEnd(""); break; }
                if (EndsWith("alize")) { ReplaceEnd("al"); }
                break;
            case 'i':
                if (EndsWith("iciti")) { ReplaceEnd("ic"); }
                break;
            case 'l':
                if (EndsWith("ical")) { ReplaceEnd("ic"); break; }
                if (EndsWith("ful")) { ReplaceEnd(""); }
                break;
            case 's':
                if (EndsWith("ness")) { ReplaceEnd(""); }
                break;
        }
    }

    /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
    private void Step5()
    {
        if (endIndex == 0) return;

        switch (wordArray[endIndex - 1])
        {
            case 'a':
                if (EndsWith("al")) break; return;
            case 'c':
                if (EndsWith("ance")) break;
                if (EndsWith("ence")) break; return;
            case 'e':
                if (EndsWith("er")) break; return;
            case 'i':
                if (EndsWith("ic")) break; return;
            case 'l':
                if (EndsWith("able")) break;
                if (EndsWith("ible")) break; return;
            case 'n':
                if (EndsWith("ant")) break;
                if (EndsWith("ement")) break;
                if (EndsWith("ment")) break;
                /* element etc. not stripped before the m */
                if (EndsWith("ent")) break; return;
            case 'o':
                if (EndsWith("ion") && stemIndex >= 0 && (wordArray[stemIndex] == 's' || wordArray[stemIndex] == 't')) break;
                /* j >= 0 fixes Bug 2 */
                if (EndsWith("ou")) break; return;
            /* takes care of -ous */
            case 's':
                if (EndsWith("ism")) break; return;
            case 't':
                if (EndsWith("ate")) break;
                if (EndsWith("iti")) break; return;
            case 'u':
                if (EndsWith("ous")) break; return;
            case 'v':
                if (EndsWith("ive")) break; return;
            case 'z':
                if (EndsWith("ize")) break; return;
            default:
                return;
        }
        if (MeasureConsontantSequence() > 1)
            endIndex = stemIndex;
    }

    /* step6() removes a final -e if m() > 1. */
    private void Step6()
    {
        stemIndex = endIndex;

        if (wordArray[endIndex] == 'e')
        {
            var a = MeasureConsontantSequence();
            if (a > 1 || a == 1 && !IsCVC(endIndex - 1))
                endIndex--;
        }
        if (wordArray[endIndex] == 'l' && IsDoubleConsontant(endIndex) && MeasureConsontantSequence() > 1)
            endIndex--;
    }

    // Returns true if the character at the specified index is a consonant.
    // With special handling for 'y'.
    private bool IsConsonant(int index)
    {
        var c = wordArray[index];
        if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u') return false;
        return c != 'y' || (index == 0 || !IsConsonant(index - 1));
    }

    /* m() measures the number of consonant sequences between 0 and j. if c is
       a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
       presence,

          <c><v>       gives 0
          <c>vc<v>     gives 1
          <c>vcvc<v>   gives 2
          <c>vcvcvc<v> gives 3
          ....		*/
    private int MeasureConsontantSequence()
    {
        var n = 0;
        var index = 0;
        while (true)
        {
            if (index > stemIndex) return n;
            if (!IsConsonant(index)) break; index++;
        }
        index++;
        while (true)
        {
            while (true)
            {
                if (index > stemIndex) return n;
                if (IsConsonant(index)) break;
                index++;
            }
            index++;
            n++;
            while (true)
            {
                if (index > stemIndex) return n;
                if (!IsConsonant(index)) break;
                index++;
            }
            index++;
        }
    }

    // Return true if there is a vowel in the current stem (0 ... stemIndex)
    private bool VowelInStem()
    {
        int i;
        for (i = 0; i <= stemIndex; i++)
        {
            if (!IsConsonant(i)) return true;
        }
        return false;
    }

    // Returns true if the char at the specified index and the one preceeding it are the same consonants.
    private bool IsDoubleConsontant(int index)
    {
        if (index < 1) return false;
        return wordArray[index] == wordArray[index - 1] && IsConsonant(index);
    }

    /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
       and also if the second c is not w,x or y. this is used when trying to
       restore an e at the end of a short word. e.g.

          cav(e), lov(e), hop(e), crim(e), but
          snow, box, tray.		*/
    private bool IsCVC(int index)
    {
        if (index < 2 || !IsConsonant(index) || IsConsonant(index - 1) || !IsConsonant(index - 2)) return false;
        var c = wordArray[index];
        return c != 'w' && c != 'x' && c != 'y';
    }

    // Does the current word array end with the specified string.
    private bool EndsWith(string s)
    {
        var length = s.Length;
        var index = endIndex - length + 1;
        if (index < 0) return false;

        for (var i = 0; i < length; i++)
        {
            if (wordArray[index + i] != s[i]) return false;
        }
        stemIndex = endIndex - length;
        return true;
    }

    // Set the end of the word to s.
    // Starting at the current stem pointer and readjusting the end pointer.
    private void SetEnd(string s)
    {
        var length = s.Length;
        var index = stemIndex + 1;
        for (var i = 0; i < length; i++)
        {
            wordArray[index + i] = s[i];
        }
        // Set the end pointer to the new end of the word.
        endIndex = stemIndex + length;
    }

    // Conditionally replace the end of the word
    private void ReplaceEnd(string s)
    {
        if (MeasureConsontantSequence() > 0) SetEnd(s);
    }
}
