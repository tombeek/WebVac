using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace WebVac {
    public partial class Form1 : Form {
        private int errorCount = 1;
        private static string desktop = Environment.GetEnvironmentVariable("UserProfile") + @"\Desktop\";
        private string baseURL = null;
        private string boolURL = null;
        private string endURL = null;
        private string filteredEmails = desktop + "filteredEmails.txt";
        private string filteredUrls = desktop + "filteredUrls.txt";
        private string oFile;
        private string pFile;
        private string pcode = null;
        private string qandTerms = null;
        private string rawEmails = desktop + "rawEmails.txt";
        private string rawUrls = desktop + "rawUrls.txt";
        private string searchURL = null;
        private string sourceFile = null;
        private StringBuilder gsb = new StringBuilder();    // global string builder object
        static int lines = 0;
        // private string location = null;
        
        public Form1() {
            InitializeComponent();

            Thread th = new Thread(new ThreadStart(DoSplash));
            th.Start();
            Thread.Sleep(1000);
            th.Abort();
            Thread.Sleep(1000);
        }

        private void Form1_Load(object sender, EventArgs e) {
        }

        private void DoSplash() {
            Splash sp = new Splash();
            sp.ShowDialog();
        }

        /// <summary>
        /// Sets boolString.Text to a Google styled Boolean search string.
        /// </summary>
        public void MakeString() {
            string positiontext = ""; if (position.Text != "") { positiontext = position.Text + " "; }
            string industrytext = ""; if (industry.Text != "") { industrytext = industry.Text + " "; }
            string citytext = ""; if (city.Text != "") { citytext = city.Text + " "; }
            string stateprovtext = ""; if (stateprov.Text != "") { stateprovtext = stateprov.Text + " "; }
            string andTermstext = ""; if (andTerms.Text != "") { andTermstext = andTerms.Text + " "; }
            // boolean AND terms
            qandTerms = positiontext + industrytext + citytext + stateprovtext + andTermstext;
            boolString.Text = qandTerms;
            if (exactPhrase.Text != "") { boolString.AppendText("\"" + exactPhrase.Text + "\""); }
            if (altTerm1.Text != "") { boolString.AppendText(" " + altTerm1.Text); }      // code for 1st OR field
            if (altTerm2.Text != "") {                  
                if (altTerm1.Text != "") { boolString.AppendText(" OR " + altTerm2.Text); // code for 2nd OR field 
                } else { boolString.AppendText(" " + altTerm2.Text); } }
            if (altTerm3.Text != "") {                  // code for 3rd OR field
                if (altTerm2.Text != "") {
                    boolString.AppendText(" OR " + altTerm3.Text);
                } else {
                    if (altTerm1.Text != "") {
                        boolString.AppendText(" OR " + altTerm3.Text);
                    } else { boolString.AppendText(" " + altTerm3.Text); }
                }
            }
            if (omitWords.Text != "") {               // code for unwated terms
                Array omitted = omitWords.Text.Split();
                foreach (var item in omitted) {
                    boolString.AppendText(" -" + item);
                }
            }
            if (domainField.Text != "") {            // site or domain specification code
                boolString.AppendText(" site:" + domainField.Text);
            }
            if (filetypeBox.Text != "") {           // file type code 
                boolString.AppendText(" filetype:" +
                filetypeBox.Text.Substring(0, filetypeBox.Text.IndexOf("  ")));
            }
        }

        /// <summary>
        /// Sets urlBox.Text to a Google styled search URL.
        /// </summary>
        public void BoolString2GoogleURL()
        {                   // create URL to use with WebRequest
            if (boolString.Text == null) { return; }    // if no data there is nothing to do; exit method
            /* These are the optional parts of the Google Advanced Search URL:
             * Base: http://www.google.com/search?hl=en - base of advanced search url
             * &as_q=all+these+words                    - AND search terms
             * &as_epq=this+exact+wording+or+phrase     - exact phrase
             * &as_oq=neither+neither+nor               - OR search terms
             * &as_eq=dumb+dumber+dumbest               - elided terms (unwanted)
             * &num=100                                 - results pages
             * &lr=lang_en                              - language
             * &as_filetype=pdf                         - results in specified file type only
             * &as_sitesearch=thisdomain.com            - limits search to site or domain
             * &as_qdr=m                                - date range
             * &as_rights=                              - usage rights
             * &as_occt=body                            - occurance in page
             * &cr=countryUS                            - region
             * &as_nlo=                                 - numeric range lower limit
             * &as_nhi=                                 - numeric range upper limit
             * &safe=active                             - SafeSearch
             * &esrch=FT1                               - escaped search type = last term                            
             */
            string as_q = "";   // AND terms
            if (andTerms.Text != "") {
                string andtermsuse = qandTerms.Replace(" ", "+");
                as_q = "&as_q=" + andtermsuse;
            }
            string as_epq = "";     // exact phrase
            if (exactPhrase.Text != "") { as_epq = "&as_epq=" + exactPhrase.Text.Replace(" ", "+"); }

            string as_oq = "";      // OR terms
            if (altTerm1.Text != "" || altTerm2.Text != "" || altTerm3.Text != "") {    // if any OR terms exist
                string as_oq_base = "&as_oq=" + altTerm1.Text;
                as_oq = as_oq_base;
                if (altTerm2.Text != "") {
                    if (altTerm1.Text != "") { as_oq += "+" + altTerm2.Text; } else { as_oq += altTerm2.Text; }
                }
                if (altTerm3.Text != "") {
                    if (altTerm2.Text != "" || altTerm1.Text != "") {   // data in either 1st or 2nd OR fields
                        as_oq += ("+" + altTerm3.Text);
                    } else {                                            // data in 3rd OR field only
                        as_oq += altTerm3.Text;
                    }
                }
            }

            // terms to omit (elide)
            string as_eq = ""; if (omitWords.Text != "") { as_eq = "&as_eq=" + omitWords.Text.Replace(" ", "+"); }
            // results per page
            string num = ""; if (resultCount.Text != "") { num = "&num=" + resultCount.Text; }
            // language
            //string lang = ""; if (languageBox.Text != "English") {
            //    lang = "&lr=" + languageBox.Text.Substring(0, filetypeBox.Text.IndexOf("  ")); }
            // any file type
            string filetype = ""; if (filetypeBox.Text != "") {
                filetype = "&as_filetype=" + filetypeBox.Text.Substring(0, filetypeBox.Text.IndexOf("  "));
            }
            // site or domain focussed search
            string domain = ""; if (domainField.Text != "") { domain = "&as_sitesearch=" + domainField.Text; }
            // time period restricted search
            string daterange = ""; if (daterangeBox.Text != "") {
                daterange = "&as_qdr=" + daterangeBox.Text.Substring(0, daterangeBox.Text.IndexOf("  "));
            }
            string rights = ""; // not used
            string area = "&as_occt=" + "body"; // default of body is used            
            // region
            string region = ""; if (regionBox.Text != "") {
                region = "&cr=" + regionBox.Text.Substring(0, regionBox.Text.IndexOf("  "));
            }
            // numeric range lower boundery
            string lower = ""; if (lowerBox.Text != "") { lower = "&as_nlo=" + lowerBox.Text; }
            // numeric range upper boundery
            string upper = ""; if (upperBox.Text != "") { upper = "&as_nhi=" + upperBox.Text; }
            string safesearch = "&safe=" + "active";  // Activate SafeSearch

            baseURL = @"http://www.google.com/search?hl=en";
            boolURL = as_q + as_epq + as_oq + as_eq + num + filetype +
                domain + daterange + rights + area + region + lower + upper + safesearch;
            endURL = "&esrch=FT1";
            searchURL = baseURL + boolURL + endURL;
            urlBox.Text = searchURL;    // sets the text property of the textbox on the form
        }

        /// <summary>
        /// Sets urlBox.Text to a Google styled search URL.
        /// </summary>
        public void MakeGoogleURL() {                   // create URL to use with WebRequest
            if (boolString.Text == null) { return; }    // if no data there is nothing to do; exit method
            /* These are the optional parts of the Google Advanced Search URL:
             * Base: http://www.google.com/search?hl=en - base of advanced search url
             * &as_q=all+these+words                    - AND search terms
             * &as_epq=this+exact+wording+or+phrase     - exact phrase
             * &as_oq=neither+neither+nor               - OR search terms
             * &as_eq=dumb+dumber+dumbest               - elided terms (unwanted)
             * &num=100                                 - results pages
             * &lr=lang_en                              - language
             * &as_filetype=pdf                         - results in specified file type only
             * &as_sitesearch=thisdomain.com            - limits search to site or domain
             * &as_qdr=m                                - date range
             * &as_rights=                              - usage rights
             * &as_occt=body                            - occurance in page
             * &cr=countryUS                            - region
             * &as_nlo=                                 - numeric range lower limit
             * &as_nhi=                                 - numeric range upper limit
             * &safe=active                             - SafeSearch
             * &esrch=FT1                               - escaped search type = last term
             */
            string as_q = "";   // AND terms
            if (andTerms.Text != "") {
                string andtermsuse = qandTerms.Replace(" ", "+");
                as_q = "&as_q=" + andtermsuse;
            }
            string as_epq = "";     // exact phrase
            if (exactPhrase.Text != "") { as_epq = "&as_epq=" + exactPhrase.Text.Replace(" ", "+"); }
            
            string as_oq = "";      // OR terms
            if (altTerm1.Text != "" || altTerm2.Text != "" || altTerm3.Text != "") { // if any OR terms
                string as_oq_base = "&as_oq=" + altTerm1.Text;
                as_oq = as_oq_base;
                if (altTerm2.Text != "") {
                    if (altTerm1.Text != "") { as_oq += "+" + altTerm2.Text; } else { as_oq += altTerm2.Text; }
                }
                if (altTerm3.Text != "") {
                    if (altTerm2.Text != "" || altTerm1.Text != "") {   // data in either 1st or 2nd OR fields
                        as_oq += ("+" + altTerm3.Text);
                    } else {                                            // data in 3rd OR field only
                        as_oq += altTerm3.Text;
                    }
                }
            }

            // terms to omit (elide)
            string as_eq = ""; if (omitWords.Text != "") { as_eq = "&as_eq=" + omitWords.Text.Replace(" ", "+"); }
            // results per page
            string num = ""; if (resultCount.Text != "") { num = "&num=" + resultCount.Text; }
            // language
            //string lang = ""; if (languageBox.Text != "English") {
            //    lang = "&lr=" + languageBox.Text.Substring(0, filetypeBox.Text.IndexOf("  ")); }
            // any file type
            string filetype = ""; if (filetypeBox.Text != "") {
                filetype = "&as_filetype=" + filetypeBox.Text.Substring(0, filetypeBox.Text.IndexOf("  ")); }
            // site or domain focussed search
            string domain = ""; if (domainField.Text != "") { domain = "&as_sitesearch=" + domainField.Text; }
            // time period restricted search
            string daterange = ""; if (daterangeBox.Text != "") {
                daterange = "&as_qdr=" + daterangeBox.Text.Substring(0, daterangeBox.Text.IndexOf("  ")); }
            string rights = ""; // not used
            string area = "&as_occt=" + "body"; // default of body is used            
            // region
            string region = ""; if (regionBox.Text != "") {
                region = "&cr=" + regionBox.Text.Substring(0, regionBox.Text.IndexOf("  ")); }
            // numeric range lower boundery
            string lower = ""; if (lowerBox.Text != "") { lower = "&as_nlo=" + lowerBox.Text; }
            // numeric range upper boundery
            string upper = ""; if (upperBox.Text != "") { upper = "&as_nhi=" + upperBox.Text; }
            string safesearch = "&safe=" + "active";  // Activate SafeSearch

            baseURL = @"http://www.google.com/search?hl=en";
            boolURL = as_q + as_epq + as_oq + as_eq + num + filetype +
                domain + daterange + rights + area + region + lower + upper + safesearch;
            endURL = "&esrch=FT1";
            searchURL = baseURL + boolURL + endURL;
            urlBox.Text = searchURL;    // sets the text property of the textbox on the form
        }

        public void YahooURL() {
            // Yahoo! string code here
            // &va_vt=any&vo_vt=any&ve_vt=any&vp_vt=any&vst=0&vf=msword&vc=us&vm=r&fr=sfp&p=all+these+words
            // +ONE+OR+THE+OR+OTHER+%22this+exact+phrase%22+-OMITTED+-TERMS+-ARE+-HERE&vs=
            if (boolString.Text == null) { return; }    // if no data there is nothing to do; exit method
            /* These are the optional parts of the Google Advanced Search URL:
             * Base: http://search.yahoo.com/search?    - base of advanced search url
             * n=100                                    - results pages
             * &vf=msword or &vf=pdf or &vf=all         - document type
             * &vc=us                                   - country is United States
             * &fl=1&vl=lang_en&vl=lang_de              - English and German results only
             * &fl=0                                    - all languages
             * &p=all+these+words                       - AND search terms
             * %22this+exact+phrase%22                  - exact phrase
             * +-OMITTED+-TERMS+-ARE+-HERE              - elided terms (unwanted)
             * &vm=r or &vm=i                           - search mode: i=filter off; r=restricted (no porn)
             * &ei=UTF-8                                - encoding, e.g., utf-8
             * &vd=all or &vd=y or &vd=m6               - updated within
             * &vst=0                                   - search all domains and websites
             * &vst=.gov&vs=.gov                        - limit search to .gov domain
             * &vst=on&vs=www.nrc.nl                    - search only website
             * 
             * 
             * &b=101                                   - show results from number 101 onwards
             */
            string pTerms = "";   // Search terms
            if (andTerms.Text != "")
            {
                string andtermsuse = qandTerms.Replace(" ", "+");
                pTerms = "&p=" + andtermsuse;
            }
            string as_epq = "";     // exact phrase
            if (exactPhrase.Text != "") { as_epq = "%22" + exactPhrase.Text.Replace(" ", "+") + "%22"; }

            string as_oq = "";      // OR terms
            if (altTerm1.Text != "" || altTerm2.Text != "" || altTerm3.Text != "")
            {    // if any OR terms exist
                string as_oq_base = altTerm1.Text;
                as_oq = as_oq_base;
                if (altTerm2.Text != "")
                {
                    if (altTerm1.Text != "") { as_oq += "+OR+" + altTerm2.Text; } else { as_oq += altTerm2.Text; }
                }
                if (altTerm3.Text != "")
                {
                    if (altTerm2.Text != "" || altTerm1.Text != "")
                    {   // data in either 1st or 2nd OR fields
                        as_oq += ("+OR+" + altTerm3.Text);
                    }
                    else
                    {                                            // data in 3rd OR field only
                        as_oq += altTerm3.Text;
                    }
                }
            }

            // terms to omit (elide)
            string as_eq = ""; if (omitWords.Text != "") { as_eq = "-" + omitWords.Text.Replace(" ", "+-"); }
            // results per page
            string num = ""; if (resultCount.Text != "") { num = "n=" + resultCount.Text; }
            // language
            string lang = "&fl=1&vl=lang_en";// if (languageBox.Text != "English") {
               // lang = "&fl=1&vl=lang_" + languageBox.Text.Substring(0, filetypeBox.Text.IndexOf("  ")); }
            // any file type
            string filetype = "";
            string ftValue = "";
            try {
                ftValue = filetypeBox.Text.Substring(0, filetypeBox.Text.IndexOf("  "));
            } catch (Exception) {
                ftValue = "";
            }
            switch(ftValue) {
                case "doc":
                    filetype = "&vf=msword";
                    break;
                case "pdf":
                    filetype = "&vf=pdf";
                    break;
                case "rtf":
                    filetype = "&vf=rtf";
                    break;
                default:
                    filetype = "&vf=all";
                    break;
            }
            // site or domain focussed search
            string domain = "&vst=0"; if (domainField.Text != "") { domain = "&vst=" + domainField.Text + "&vs=" + domainField.Text; }
            // time period restricted search
            string daterange = "&vd=all"; if (daterangeBox.Text != "") {
                daterange = "&vd=" + daterangeBox.Text.Substring(0, daterangeBox.Text.IndexOf("  ")); }
            string rights = ""; // not used
            string area = "&as_occt=" + "body"; // default of body is used            
            // region
            string region = "&vc=us"; if (regionBox.Text != "") {
                region = "&vc=" + regionBox.Text.Substring(0, regionBox.Text.IndexOf("  ")); }
            // results filter
            string safesearch = "&vm=r";

            baseURL = @"http://search.yahoo.com/search?";
            boolURL = num + "&va_vt=any&vo_vt=any&ve_vt=any&vp_vt=any" + pTerms + as_epq + as_oq + as_eq + lang + filetype +
                domain + daterange + rights + area + region + safesearch;
            endURL = "&vs=";
            searchURL = baseURL + boolURL + endURL;
            urlBox.Text = searchURL;    // textbox at the top of the form
        }

        /// <summary>
        /// Saves source code to sourceFile
        /// </summary>
        public void FetchSearch() {
            // build entire input
            StringBuilder sb = new StringBuilder(); // faster and better resource use than urlString += tempString
            // used on each read operation
            byte[] buf = new byte[3072];
            try {
                // prepare the web page we will be asking for
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(searchURL);
                // execute the request
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                // we will read data via the response stream
                Stream resStream = response.GetResponseStream();
                string tempString = null;
                int count = 0;
                do {
                    // fill the buffer with data
                    count = resStream.Read(buf, 0, buf.Length);

                    // make sure we read some data
                    if (count != 0) {
                        // translate from bytes to ASCII text
                        tempString = Encoding.ASCII.GetString(buf, 0, count);

                        // continue building the string
                        sb.Append(tempString);
                    }
                }
                while (count > 0); // any more data to read?
                // write file to desktop using default or user chosen name & location
                if (sourceFile == null) {
                    using (TextWriter w = File.CreateText(desktop + "ssource.txt")) {
                        w.WriteLine(sb.ToString());
                        sourceFile = desktop + "ssource.txt";
                        MessageBox.Show("Source saved as 'ssource.txt' to Desktop.");
                    }
                } else {
                    using (TextWriter w = File.CreateText(sourceFile)) {
                        w.WriteLine(sb.ToString()); // we convert sb to a string to write its contents
                    }
                }
            } catch (Exception) {
                MessageBox.Show("Create/enter Boolean string before attempting to search.");
            }
        }

        /// <summary>
        /// Parses and appends all URLs to pURLs.txt.
        /// Appends non-Google URLs to pURLs.txt.
        /// </summary>
        public void ParseURLs() {
            if (sourceFile == null) { sourceFile = (desktop + "ssource.txt"); }
            try {
                string fromfile = File.ReadAllText(sourceFile);
                string pattern = @"[http|https]{4,5}:\/\/[\w\.?=%&=\-@/$,]+";   // regex for URLs
                using (TextWriter w = File.AppendText(rawUrls)) { // write to file
                    foreach (Match match in Regex.Matches(fromfile, pattern))
                        w.WriteLine("{0}", match.Value, match.Index);
                }
            } catch (Exception) {
                MessageBox.Show("Source file is open or not found.");
            }
        }

        /// <summary>
        /// Removes dupes and urls that point to Google
        /// </summary>
        private void RemoveGoogleURLs() {
            List<String> urls = new List<String>();
            if (File.Exists(rawUrls)) {
                try {
                    string[] ulist = File.ReadAllLines(rawUrls); // create temp array
                    urls.AddRange(ulist);
                    List<string> results = urls.FindAll(FindNongoogle); // create new list of non-google urls
                    if (results != null) {
                        results.Sort();
                        string[] s = results.ToArray();
                        s = RemoveDuplicates(s);
                        results = s.ToList();
                        try {
                            using (TextWriter w = File.AppendText(filteredUrls)) {
                                foreach (var item in results)
                                    w.WriteLine(item);
                            }
                        } catch (Exception) {
                            MessageBox.Show("There was a problem writing to the filteredUrls file");
                        }
                    }
                    FileInfo obj = new FileInfo(filteredUrls);
                    if (obj.Length == 0) {
                        MessageBox.Show("Search unsuccessful.");
                        //File.Delete(filteredUrls);
                        //File.Delete(rawUrls);
                        //File.Delete(sourceFile);
                    }
                } catch (Exception) {
                    //
                }
            }
        }

        private static bool FindNongoogle(string url) {
            if (url.Contains("google") || url.Contains("youtube") || url.Contains("picassa")) {
                return false;
            }
            {
                return true;
            }
        }

        /// <summary>
        /// Removes dupes and URLs that point to Yahoo
        /// </summary>
        private void RemoveYahooURLs() {
            List<String> urls = new List<String>();
            if (File.Exists(rawUrls)) {
                try {
                    string[] ulist = File.ReadAllLines(rawUrls); // create temp array
                    urls.AddRange(ulist);
                    List<string> results = urls.FindAll(FindNonyahoo); // create new list of non-google urls
                    if (results != null) {
                        results.Sort();
                        string[] s = results.ToArray();
                        s = RemoveDuplicates(s);
                        results = s.ToList();
                        try {
                            using (TextWriter w = File.AppendText(filteredUrls)) {
                                foreach (var item in results)
                                    w.WriteLine(item);
                            }
                        } catch (Exception) {
                            MessageBox.Show("There was a problem writing to the filteredUrls file.");
                        }
                    }
                    FileInfo obj = new FileInfo(filteredUrls);
                    if (obj.Length == 0) {
                        MessageBox.Show("Search unsuccessful.");
                        //File.Delete(filteredUrls);
                        //File.Delete(rawUrls);
                        //File.Delete(sourceFile);
                    }

                } catch (Exception) {
                    MessageBox.Show("There was a problem reading from " + rawUrls);
                }
            }
        }
        
        private static bool FindNonyahoo(string url) {
            if (url.Contains("yahoo") || url.Contains("yelp") || url.Contains("yimg")) {
                return false;
            }
            {
                return true;
            }
        }

        /// <summary>
        /// Creates DialogSave as new SaveFileDialog instance
        /// </summary>
        private void SaveSource() {
            // Create new SaveFileDialog object
            SaveFileDialog DialogSave = new SaveFileDialog();
            // Default file extension
            DialogSave.DefaultExt = "txt";
            // Available file extensions
            DialogSave.Filter = "Text file (*.txt)|*.txt|HTML file (*.htm)|*.htm|All files (*.*)|*.*";
            // Adds a extension if the user does not
            DialogSave.AddExtension = true;
            // Restores the selected directory, next time
            DialogSave.RestoreDirectory = true;
            // Dialog title
            DialogSave.Title = "Enter name and location for html source text.";
            // Startup directory
            DialogSave.InitialDirectory = desktop;
            // Show the dialog and process the result
            if (DialogSave.ShowDialog() == DialogResult.OK) {
                // MessageBox.Show("You selected the file: " + DialogSave.FileName);
                sourceFile = DialogSave.FileName;
                
            } else {
                // user hit cancel or closed the dialog
            }
            DialogSave.Dispose();
            // DialogSave = null;
        }

        public void DoProcess() {
            Thread th = new Thread(new ThreadStart(ProcessURLs));
            th.Start();
            //Thread.Sleep(1000);
            //th.Abort();
            //Thread.Sleep(1000);
        }

        /// <summary>
        /// Loads source from each URL and 
        /// parses emails to rawEmails.txt
        /// </summary>
        public void ProcessURLs() { // step through each URL in file
            string line = null;
            string errorLog = desktop + "errorlog.txt";
            if (File.Exists(desktop + "filteredURLs.txt")) {
                StreamReader sr = new StreamReader(desktop + "filteredURLs.txt");
                while ((line = sr.ReadLine()) != null) {
                    try {
                        // Create an instance of StreamReader to read from a file.
                        // The using statement also closes the StreamReader.
                        GetPage(line);
                    } catch (Exception e) {
                        // MessageBox.Show("Problem with URL: " + line + " : " + e.Message);
                        if (File.Exists(errorLog)) {    // just checking
                        } else {
                            using (TextWriter newel = File.CreateText(errorLog)) {
                                newel.WriteLine("     =============   WebVac - Error Log   ===========");
                            }
                        }
                        using (TextWriter el = File.AppendText(errorLog)) {
                            el.WriteLine(errorCount++ + ") " + line + " : " + e.Message);
                        }
                    }
                }
                RemoveDupeEmails();
            }
        }

        public void GetPage(string URL) {    // open a single URL from file and store its source code in gsb
            gsb.Clear();                // initialize global stringbuilder for new data
            // used on each read operation
            byte[] buf = new byte[3072];
            // prepare the web page to ask for
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL); // listURL is a global Uri variable
            // execute the request
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // read data via the response stream
            Stream resStream = response.GetResponseStream();
            string tempString = null;
            int count = 0;
            do {
                // fill the buffer with data
                count = resStream.Read(buf, 0, buf.Length);

                // make sure we read some data
                if (count != 0) {
                    // translate from bytes to ASCII text
                    tempString = Encoding.ASCII.GetString(buf, 0, count);

                    // continue building the string
                    gsb.Append(tempString);
                }
            }
            while (count > 0); // any more data to read?
            pcode = gsb.ToString();
            ParseEmails(pcode);
        }

        public void ParseEmails(string source) {
            string fromgsb = source;
            string pattern = @"[\w\-\.]+@[\w\-]+\.+[cdegmnortu]{2,3}\b";      // regex for e-mails .com .edu .net .org
            using (TextWriter w = File.AppendText(rawEmails)) {
                foreach (Match match in Regex.Matches(fromgsb, pattern))
                    w.WriteLine("{0}", match.Value, match.Index);
            }
        }

        private void RemoveDupeEmails() {
            if (File.Exists(rawEmails)) {
                try {
                    string[] elist = File.ReadAllLines(rawEmails); // create temp array
                    List<string> etemplist = elist.ToList();    // some things can only be done with lists...
                    if (etemplist != null) {
                        etemplist.Sort();
                        string[] s = etemplist.ToArray();   // turn the list back into an array...
                        s = RemoveDuplicates(s);    // remove dupes here with call to func defined elsewhere
                        etemplist = s.ToList();     // reuse list
                        etemplist = RemoveBadEmails(etemplist); // remove bad emails
                        lines = -1;
                        using (TextWriter w = File.AppendText(filteredEmails)) {
                            w.WriteLine("Name,Email");
                            foreach (var item in etemplist)
                                AppendLines(w, item);
                                lines++;
                        }
                        MessageBox.Show(lines + " emails appended to " + filteredEmails);
                        // File.Delete(rawEmails);
                    } else {
                        MessageBox.Show("\nNo emails found.");
                    }
                } catch (Exception) {
                    MessageBox.Show("Problem writing to " + filteredEmails);
                }
            } else {
                MessageBox.Show("\nFile not found:\n" + rawEmails + "   ");
            }
        }

        /// <summary>
        /// Returns a filtered list
        /// </summary>
        private List<String> RemoveBadEmails(List<String> email) {
            // List<String> emails = new List<String>();
                List<string> results = email.FindAll(badEmailDef);
                return results;
        }

        private static bool badEmailDef(string email) {
            email = email.ToLower();
            if (email.Contains(".biz") || email.Contains(".gov") || email.Contains("alerts")
                || email.Contains("appli") || email.Contains("apply") || email.Contains("ask")
                || email.Contains("bank") || email.Contains("broker") || email.Contains("career")
                || email.Contains("chapelof") || email.Contains("churchof") || email.Contains("feedback")
                || email.Contains("employment") || email.Contains("help") || email.Contains("inquiry")
                || email.Contains("fill") || email.Contains("hiring") || email.Contains("hr")
                || email.Contains("info") || email.Contains("intern") || email.Contains("invest")
                || email.Contains("job") || email.Contains("lawyer") || email.Contains("lease")
                || email.Contains("lease") || email.Contains("mgmt") || email.Contains("post")
                || email.Contains("propert") || email.Contains("publish") || email.Contains("racing")
                || email.Contains("resume") || email.Contains("selling") || email.Contains("w3")
                || email.Contains("webmaster") || email.Contains("xx") || email.Contains("sales")
                || email.Contains("user") || email.Contains("team")) {
                return false;
            }
            {
                return true;
            }
        }


        /// <summary>
        /// Returns duplicate free string[]
        /// </summary>
        /// <param name="s">string array</param>
        /// <returns name="result">string array</returns>
        public static string[] RemoveDuplicates(string[] s) {
            HashSet<string> set = new HashSet<string>(s);
            string[] result = new string[set.Count];
            set.CopyTo(result);
            return result;
        }

        /// <summary>
        /// Used by form button
        /// </summary>
        public void RemoveDuplicateLines() {
            // Create new ReadFileDialog object
            OpenFileDialog DiagOpen = new OpenFileDialog();
            // Default file extension
            DiagOpen.DefaultExt = "txt";
            // Available file extensions
            DiagOpen.Filter = "Text file (*.txt)|*.txt|HTML file (*.htm)|*.htm|All files (*.*)|*.*";
            // Restores the selected directory, next time
            DiagOpen.RestoreDirectory = true;
            // Dialog title
            DiagOpen.Title = "Choose a source file";
            // Startup directory
            DiagOpen.InitialDirectory = desktop;
            // Show the dialog and process the result
            if (DiagOpen.ShowDialog() == DialogResult.OK) {
                // MessageBox.Show("You selected the file: " + DialogSave.FileName);
                oFile = DiagOpen.FileName;
            } else {
                // user hit cancel or closed the dialog
                DiagOpen.Dispose();
                return;
            }
            DiagOpen.Dispose();
            if (File.Exists(oFile)) {
                string[] elist = File.ReadAllLines(oFile); // create temp array
                List<string> etemplist = elist.ToList();
                if (etemplist != null) {
                    etemplist.Sort();
                    string[] s = etemplist.ToArray();   // RemoveDuplicates() works only on arrays of strings
                    s = RemoveDuplicates(s);    // money assignment + function call
                    etemplist = s.ToList();     // reuse etemplist variable
                    lines = 0;
                    // Create new SaveFileDialog object
                    SaveFileDialog DialogSave = new SaveFileDialog();
                    // Default file extension
                    DialogSave.DefaultExt = "txt";
                    // Available file extensions
                    DialogSave.Filter = "Text file (*.txt)|*.txt|HTML file (*.htm)|*.htm|All files (*.*)|*.*";
                    // Adds a extension if the user does not
                    DialogSave.AddExtension = true;
                    // Restores the selected directory, next time
                    DialogSave.RestoreDirectory = true;
                    // Dialog title
                    DialogSave.Title = "Enter a name for the processed file";
                    // Startup directory
                    DialogSave.InitialDirectory = desktop;
                    // Show the dialog and process the result
                    if (DialogSave.ShowDialog() == DialogResult.OK) {
                        pFile = DialogSave.FileName;
                    } else {
                        // user hit cancel or closed the dialog
                        DialogSave.Dispose();
                        return;
                    }
                    DialogSave.Dispose();   // although "dispose" occurs here, the save functionality follows
                    using (TextWriter w = File.AppendText(pFile)) {
                        foreach (var item in etemplist)
                            AppendLines(w, item);
                    }
                    MessageBox.Show(lines + " lines appended to " + pFile);
                    // File.Delete(oFile);
                } else {
                    MessageBox.Show("\nNo data in file.");
                }
            }
        }

        /// <summary>
        /// Used by form button
        /// </summary>
        public void SortLines() {
            // Create new ReadFileDialog object
            OpenFileDialog DiagOpen = new OpenFileDialog();
            // Default file extension
            DiagOpen.DefaultExt = "txt";
            // Available file extensions
            DiagOpen.Filter = "Text file (*.txt)|*.txt|HTML file (*.htm)|*.htm|All files (*.*)|*.*";
            // Restores the selected directory, next time
            DiagOpen.RestoreDirectory = true;
            // Dialog title
            DiagOpen.Title = "Choose a file";
            // Startup directory
            DiagOpen.InitialDirectory = desktop;
            // Show the dialog and process the result
            if (DiagOpen.ShowDialog() == DialogResult.OK) {
                // MessageBox.Show("You selected the file: " + DialogSave.FileName);
                oFile = DiagOpen.FileName;
            } else {
                // user hit cancel or closed the dialog
                DiagOpen.Dispose();
                return;
            }
            DiagOpen.Dispose();
            if (File.Exists(oFile)) {
                string[] elist = File.ReadAllLines(oFile); // create temp array
                List<string> etemplist = elist.ToList();
                if (etemplist != null) {
                    etemplist.Sort();
                    lines = -1;
                    using (TextWriter w = File.CreateText(oFile)) {
                        foreach (var item in etemplist)
                            AppendLines(w, item);
                        lines++;
                    }
                    MessageBox.Show(lines + " lines sorted.");
                    // File.Delete(oFile);
                } else {
                    MessageBox.Show("\nNo data in file.");
                }
            }
        }

        private static void AppendEmail(TextWriter w, string item) {
            w.WriteLine(item + "," + item);
            lines++;
        }

        private static void AppendLines(TextWriter w, string item) {
            w.WriteLine(item);
            lines++;
        }

        // Form actions
        private void makeString_Click(object sender, EventArgs e) {
            if (googleButton.Checked) {
                MakeString();
                GoogleURL();
            }
            if (yahooButton.Checked) {
                MakeString();
                YahooURL();
            }
            if (boolString.Text != "") {
                Color forecolor = Color.FromArgb(0, 0, 128);    // add Boolean string to form in dark blue
                boolString.ForeColor = forecolor;
            }
            if (urlBox.Text != "") {
                Color forecolor = Color.FromArgb(0, 0, 128);    // add URL to form field in dark blue
                urlBox.ForeColor = forecolor;
            }
        }

        private void searchButton_Click(object sender, EventArgs e) {
            SaveSource();
            FetchSearch();
        }

        private void urlButton_Click(object sender, EventArgs e) {
            ParseURLs();
            if (googleButton.Checked) {
                RemoveGoogleURLs();
            }
            if (yahooButton.Checked) {
                RemoveYahooURLs();
            }
        }

        private void xemailButton_Click(object sender, EventArgs e) {
            // open URLs and parse e-mails
            //ProcessURLs();
            DoProcess();
        }

        private void button1_Click(object sender, EventArgs e) {
            SaveSource();
        }

        private void clearButton_Click(object sender, EventArgs e) {
            boolString.Text = "";
            urlBox.Text = "";
            position.Text = "";
            industry.Text = "";
            city.Text = "";
            stateprov.Text = "";
            andTerms.Text = "";
            exactPhrase.Text = "";
            altTerm1.Text = "";
            altTerm2.Text = "";
            altTerm3.Text = "";
            omitWords.Text = "";
            resultCount.Text = "";
            domainField.Text = "";
            filetypeBox.Text = "";
            daterangeBox.Text = "";
            lowerBox.Text = "";
            upperBox.Text = "";
            position.Focus();
        }

        private void defaultOmittedTermsLabel_Click(object sender, EventArgs e)
        {
            omitWords.Text = "sample trial example apply estate trials applicants";
        }

        private void defaultAndTermsLabel_Click(object sender, EventArgs e)
        {
            andTerms.Text = "resume education";
        }

        private void helpLabel_Click(object sender, EventArgs e) {
            helpboxRemDupesBox.Visible = true;
        }

        private void helpRemDupesBox_Click(object sender, EventArgs e) {
            helpboxRemDupesBox.Visible = false;
        }

        private void removeDupeLinesButton_Click(object sender, EventArgs e) {
            RemoveDuplicateLines();
        }

        private void helpSortLabel_Click(object sender, EventArgs e) {
            helpboxSortLinesLabel.Visible = true;
        }

        private void helpboxSortLinesLabel_Click(object sender, EventArgs e) {
            helpboxSortLinesLabel.Visible = false;
        }

        private void sortButton_Click(object sender, EventArgs e) {
            SortLines();
        }

        private void boolToUrlButton_Click(object sender, EventArgs e)
        {
            if (googleButton.Checked) {
                MakeString();
                GoogleURL();
            }
            if (yahooButton.Checked) {
                MakeString();
                YahooURL();
            }
            if (boolString.Text != "") {
                Color forecolor = Color.FromArgb(0, 0, 128);    // add Boolean string to form in dark blue
                boolString.ForeColor = forecolor;
            }
            if (urlBox.Text != "") {
                Color forecolor = Color.FromArgb(0, 0, 128);    // add URL to form field in dark blue
                urlBox.ForeColor = forecolor;
            }
        }

        private void emailFilterButton_Click(object sender, EventArgs e)
        {
            RemoveDupeEmails();
        }

    }
}
