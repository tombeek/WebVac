using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace WebVac
{
    partial class Form1 {
        //private string baseURL;
        //private string boolURL;
        //private string endURL;
        //private string searchURL;

        public void GoogleURL() {   // create URL to use with WebRequest
            if (boolString.Text == null) { return; }    // if no data there is nothing to do; exit method
            /* These are the optional parts of the Google Advanced Search URL:
             * Base: http://www.google.com/search?hl=en - base of advanced search url
             * &as_q=all+these+words                    - AND terms
             * &as_epq=this+exact+wording+or+phrase     - exact phrase
             * &as_oq=neither+neither+nor               - OR terms
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
            int temp0 = 0;  // these two integer variables are identicle
            // Int32 temp1 = 0;    // this uses the actual .Net type; the previous uses an alias
            string iStr0 = "";   // temp string
            // string iStr1 = "";   // temp string
            string bString = boolString.Text;
            string as_q = "";   // AND terms
            string as_epq = ""; // exact phrase
            string as_oq = ""; // OR terms
            int positionO = 0, positionC = 0;  // to hold position of opening & closing quotes
            string andtermsuse = "";
            if (bString.Contains("\"") || bString.Contains("OR"))  {
                if (bString.Contains("\"")) {
                    positionO = bString.IndexOf("\"") + 1;
                    positionC = bString.LastIndexOf("\"");
                    as_epq = "&as_epq=" + bString.Substring(positionO, positionC - positionO);
                } else {
                    temp0 = bString.IndexOf(" OR ");  // 1st step to getting index of first OR term
                    iStr0 = bString.Substring(0, temp0);    // substring = 0 to first " OR "
                    positionO = iStr0.LastIndexOf(" ") + 1; // index first OR term : GOAL #1!
                    positionC = bString.IndexOf(" ", bString.LastIndexOf(" OR ") + 4);  // index of final OR term
                    // &as_oq=+your+OR+search+OR+string+OR+will&
                    //as_oq = "&as_oq=" + bString.Substring(positionO, positionC - positionO).Trim();
                    //as_oq = as_oq.Replace(" ", "+");
                    //as_oq = as_oq.Replace("+OR+", "+");
                    //as_oq = as_oq.Replace("++", "+");
                }
                andtermsuse = bString.Substring(0, positionO);
            } else {
                andtermsuse = bString;
            }
            andtermsuse = andtermsuse.Trim();   // culls whitespace
            andtermsuse = andtermsuse.Replace("  ", " ");
            andtermsuse = andtermsuse.Replace(" ", "+");
            as_q = "&as_q=" + andtermsuse;  // end of and terms processing


            //if (exactPhrase.Text != "") { as_epq = "&as_epq=" + exactPhrase.Text.Replace(" ", "+"); }
            //string as_oq = "";      // OR terms
            //if (altTerm1.Text != "" || altTerm2.Text != "" || altTerm3.Text != "") { // if any OR terms
            //    string as_oq_base = "&as_oq=" + altTerm1.Text;
            //    as_oq = as_oq_base;
            //    if (altTerm2.Text != "") {
            //        if (altTerm1.Text != "") { as_oq += "+" + altTerm2.Text; } else { as_oq += altTerm2.Text; }
            //    }
            //    if (altTerm3.Text != "") {
            //        if (altTerm2.Text != "" || altTerm1.Text != "") {   // data in either 1st or 2nd OR fields
            //            as_oq += ("+" + altTerm3.Text);
            //        } else {                                            // data in 3rd OR field only
            //            as_oq += altTerm3.Text;
            //        }
            //    }
            //}

            //// terms to omit (elide)
            //string as_eq = ""; if (omitWords.Text != "") { as_eq = "&as_eq=" + omitWords.Text.Replace(" ", "+"); }
            //// results per page
            //string num = ""; if (resultCount.Text != "") { num = "&num=" + resultCount.Text; }
            //// language
            ////string lang = ""; if (languageBox.Text != "English") {
            ////    lang = "&lr=" + languageBox.Text.Substring(0, filetypeBox.Text.IndexOf("  ")); }
            //// any file type
            //string filetype = ""; if (filetypeBox.Text != "") {
            //    filetype = "&as_filetype=" + filetypeBox.Text.Substring(0, filetypeBox.Text.IndexOf("  "));
            //}
            //// site or domain focussed search
            //string domain = ""; if (domainField.Text != "") { domain = "&as_sitesearch=" + domainField.Text; }
            //// time period restricted search
            //string daterange = ""; if (daterangeBox.Text != "") {
            //    daterange = "&as_qdr=" + daterangeBox.Text.Substring(0, daterangeBox.Text.IndexOf("  "));
            //}
            //string rights = ""; // not used
            //string area = "&as_occt=" + "body"; // default of body is used            
            //// region
            //string region = ""; if (regionBox.Text != "") {
            //    region = "&cr=" + regionBox.Text.Substring(0, regionBox.Text.IndexOf("  "));
            //}
            //// numeric range lower boundery
            //string lower = ""; if (lowerBox.Text != "") { lower = "&as_nlo=" + lowerBox.Text; }
            //// numeric range upper boundery
            //string upper = ""; if (upperBox.Text != "") { upper = "&as_nhi=" + upperBox.Text; }
            //string safesearch = "&safe=" + "active";  // Activate SafeSearch

            baseURL = @"http://www.google.com/search?hl=en";
            boolURL = as_q + as_epq + as_oq;
            // + as_eq + num + filetype +
            //    domain + daterange + rights + area + region + lower + upper + safesearch;
            endURL = "&esrch=FT1";
            searchURL = baseURL + boolURL + endURL;
            urlBox.Text = searchURL;    // sets the text property of the textbox on the form
        }

        public void MakeYahooURL()
        {
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
            if (andTerms.Text != "") {
                string andtermsuse = qandTerms.Replace(" ", "+");
                pTerms = "&p=" + andtermsuse;
            }
            string as_epq = "";     // exact phrase
            if (exactPhrase.Text != "") { as_epq = "%22" + exactPhrase.Text.Replace(" ", "+") + "%22"; }

            string as_oq = "";      // OR terms
            if (altTerm1.Text != "" || altTerm2.Text != "" || altTerm3.Text != "") {    // if any OR terms exist
                string as_oq_base = altTerm1.Text;
                as_oq = as_oq_base;
                if (altTerm2.Text != "") {
                    if (altTerm1.Text != "") { as_oq += "+OR+" + altTerm2.Text; } else { as_oq += altTerm2.Text; }
                }
                if (altTerm3.Text != "") {
                    if (altTerm2.Text != "" || altTerm1.Text != "") {   // data in either 1st or 2nd OR fields
                        as_oq += ("+OR+" + altTerm3.Text);
                    } else {                                            // data in 3rd OR field only
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
            switch (ftValue) {
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
                daterange = "&vd=" + daterangeBox.Text.Substring(0, daterangeBox.Text.IndexOf("  "));
            }
            string rights = ""; // not used
            string area = "&as_occt=" + "body"; // default of body is used            
            // region
            string region = "&vc=us"; if (regionBox.Text != "") {
                region = "&vc=" + regionBox.Text.Substring(0, regionBox.Text.IndexOf("  "));
            }
            // results filter
            string safesearch = "&vm=r";

            baseURL = @"http://search.yahoo.com/search?";
            boolURL = num + "&va_vt=any&vo_vt=any&ve_vt=any&vp_vt=any" + pTerms + as_epq + as_oq + as_eq + lang + filetype +
                domain + daterange + rights + area + region + safesearch;
            endURL = "&vs=";
            searchURL = baseURL + boolURL + endURL;
            urlBox.Text = searchURL;    // textbox at the top of the form
        }
    }
}
