﻿using System;
using System.Text.RegularExpressions;

namespace com.csutil {

    public static class StringExtensions {

        /// <summary> 
        /// Examples: 
        /// <para> "abc)]".Substring(")", includeEnd: false) == "abc"   </para>
        /// <para> AND                                                  </para>
        /// <para> "abc)]".Substring("bc", includeEnd: true) == "abc"   </para>
        /// </summary>
        public static string Substring(this string self, string end, bool includeEnd) { return Substring(self, 0, end, includeEnd); }

        /// <summary> 
        /// Examples: 
        /// <para> "[(abc)]".Substring(2, ")", includeEnd: false) == "abc"  </para>
        /// <para> AND                                                      </para>
        /// <para> "abc)]".Substring(2, "bc", includeEnd: true) == "abc"    </para>
        /// </summary>
        public static string Substring(this string self, int startIndex, string end, bool includeEnd) {
            var lengthUntilEndStarts = self.LastIndexOf(end);
            if (lengthUntilEndStarts < 0) { return self.Substring(startIndex); }
            var lengthOfEnd = (includeEnd ? end.Length : 0);
            return self.Substring(startIndex, lengthUntilEndStarts + lengthOfEnd - startIndex);
        }

        public static string SubstringAfter(this string self, string startAfter, bool startFromBack = false) {
            var pos = startFromBack ? self.LastIndexOf(startAfter) : self.IndexOf(startAfter);
            if (pos < 0) { throw Log.e("Substring " + startAfter + " not found in " + self); }
            return self.Substring(pos + startAfter.Length);
        }

        public static bool EndsWith(this string self, char end) { return self.EndsWith("" + end); }

        public static string[] Split(this string self, string separator) {
            return self.Split(new string[] { separator }, StringSplitOptions.None);
        }

        /// <summary> "An {0} with {1} placeholders!".With("example", "multiple") </summary>
        public static string With(this string self, params object[] args) {
            return string.Format(self, args);
        }

        /// <summary>
        /// Examples: 
        /// <para> Assert.True("abc".IsRegexMatch("a*"));               </para>
        /// <para> Assert.True("Abc".IsRegexMatch("[A-Z][a-z][a-z]"));  </para>
        /// <para> Assert.True("hat".IsRegexMatch("?at"));              </para>
        /// <para> Assert.True("joe".IsRegexMatch("[!aeiou]*"));        </para>
        /// <para> Assert.False("joe".IsRegexMatch("?at"));             </para>
        /// <para> Assert.False("joe".IsRegexMatch("[A-Z][a-z][a-z]")); </para>
        /// </summary>
        public static bool IsRegexMatch(this string self, string regexToMatch) {
            if (self == null || regexToMatch.IsNullOrEmpty()) return false;
            // turn into regex pattern, and match the whole string with ^$
            var patt = "^" + Regex.Escape(regexToMatch) + "$";
            // add support for ?, #, *, [], and [!]
            patt = patt.Replace(@"\[!", "[^").Replace(@"\[", "[").Replace(@"\]", "]")
                       .Replace(@"\?", ".").Replace(@"\*", ".*").Replace(@"\#", @"\d");
            try { return Regex.IsMatch(self, patt); } catch (ArgumentException e) {
                throw new ArgumentException("Invalid pattern: {0}".With(regexToMatch), e);
            }
        }

    }

}
