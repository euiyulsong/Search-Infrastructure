using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class MyTrie
    {
        private MyTrieNode _root;
        public MyTrie()
        {
            _root = new MyTrieNode();
        }

        /// <summary>
        /// Add a word into the trie, take in a string word and int pagecount
        /// </summary>
        /// <param name="word">word itself</param>
        /// <param name="pageCount">page count</param>
        public void Add(string word, int pageCount) { Add(word.ToLower().ToCharArray(), pageCount); }

        /// <summary>
        /// Add take in the character array of the word and the int page count of the page
        /// </summary>
        /// <param name="word">chracter array of the word</param>
        /// <param name="pageCount">page count of the word</param>
        public void Add(char[] word, int pageCount)
        {
            _root.Add(word, 0, pageCount);
        }

        /// <summary>
        /// Get all the words that have the passed in prefix
        /// </summary>
        /// <param name="prefix">search prefix</param>
        /// <returns>a list of 10 words</returns>
        public List<string> GetWords(string prefix)
        {
            return _root.GetWords(_root, prefix);
        }

        /// <summary>
        /// Get all the suggestions for the prefix, suggestions that can generate new words 
        /// </summary>
        /// <param name="prefix">the prefix or word that the user typed in</param>
        /// <returns>List of 10 suggestions</returns>
        public List<string> GetSuggestions(string prefix)
        {
            return _root.GetSuggestions(_root, prefix);
        }
    }
}