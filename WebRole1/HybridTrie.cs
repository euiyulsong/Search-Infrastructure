using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class HybridTrie
    {
        private const int MAX = 10; 
        private HybridTrieNode overallRoot;

        public HybridTrie()
        {
            this.overallRoot = new HybridTrieNode();
        }

        public HybridTrie(string word) {
            this.overallRoot = new HybridTrieNode();

            Add(this.overallRoot, word);
        }

        public void Add(string word)
        {
            Add(this.overallRoot, word);
        }

        public HybridTrieNode getRoot()
        {
            return this.overallRoot;
        }

        public void Add(HybridTrieNode root, string word)
        {
            word = word.ToLower();
            if (root == null || word.Length < 0)
            {
                throw new ArgumentException("root is null or invalid length of word.");
            }
            if (word.Length == 0)
            {
                root.isEnd = true;
            }
            else
            {
                int character = word[0];

                if (root.next != null)
                {
                    root.next.Add(word);
                    if (root.next.Count > 10)
                    {
                        root.dictionary = new Dictionary<char, HybridTrieNode>();
                       foreach (string temp in root.next)
                        {
                            AddHelper(root, temp);
                        }
                        root.next = null;
                    }
                }
                else
                {
                    AddHelper(root, word);
                }
            }
        }

        private void AddHelper(HybridTrieNode root, string word)
        {
            if (!root.dictionary.ContainsKey(word[0]))
            {
                root.dictionary.Add(word[0], new HybridTrieNode());
            }
            if (word.Length == 1)
            {
                Add(root.dictionary[word[0]], "");
            }
            else
            {
                Add(root.dictionary[word[0]], word.Substring(1));
            }
        }

        public List<string> Search(string word)
        {
            if (word.Length < 0 || word == null)
            {
                throw new ArgumentException("word is null or word length is invalid");
            }
            word = word.Trim().ToLower();
            List<string> result = new List<string>();

            HybridTrieNode current = overallRoot;
            string search = "";
            string end = word;
            bool found = true;
            for (int i = 0; i < word.Length; i++)
            {
                if (current.dictionary != null && current.dictionary.ContainsKey(word[i]) && found)
                {
                    current = current.dictionary[word[i]];
                    search += word[i] + "";
                    end = end.Substring(1);
                } else
                {
                    found = !found;
                }
            }
            Search(current, search, end, result);
            return result;
        }

        private void Search(HybridTrieNode current, string search, string end, List<string> result)
        {
            if (current != null)
            {
                if (result.Count < 10)
                {
                    if (current.next == null) {
                        if (end == "") {
                            if (current.isEnd && !result.Contains(search) && result.Count < 10)
                            {
                                result.Add(search);
                            }

                            foreach (char i in current.dictionary.Keys)
                            {
                                Search(current.dictionary[i], search + i, end, result);
                            }
                        }
                    } else
                    {
                        foreach (string i in current.next)
                        {
                            if (result.Count() < 10 && i.StartsWith(end) && !result.Contains(search + i))
                            {
                                result.Add(search + i);

                            }
                        }
                    }
                }
            }
        }
    }
}