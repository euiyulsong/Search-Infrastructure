using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    /// <summary>
    /// class word that represents the record in each container
    /// record will have the content of the word and its pagecount 
    /// </summary>
    public class Word
    {
        public string content { get; set; }
        public int pageCount { get; set; }

        public Word(string content, int pageCount)
        {
            this.content = content;
            this.pageCount = pageCount;
        }
    }

    /// <summary>
    /// class compare to be used by the sorted set
    /// </summary>
    public class WordComparer : IComparer<Word>
    {
        /// <summary>
        /// Compare two word, place to the front if the word is larger than the other word
        /// if equal then we compare content of the word
        /// </summary>
        /// <param name="x">word 1</param>
        /// <param name="y">word 2</param>
        /// <returns></returns>
        public int Compare(Word x, Word y)
        {
            int result = x.pageCount - y.pageCount;
            if (result < 0)
            {
                return 1;
            }
            else if (result > 0)
            {
                return -1;
            }
            else
            {
                return x.content.CompareTo(y.content);
            }
        }
    }
}