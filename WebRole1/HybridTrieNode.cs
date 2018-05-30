using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class HybridTrieNode
    {
        public List<string> next;
        public Dictionary<char, HybridTrieNode> dictionary;
        public bool isEnd;

        // Creates Hybrid Trie Node
        public HybridTrieNode()
        {
            this.dictionary = null;
            this.next = new List<string>();
            this.isEnd = false;
        }

        // Adds given word to the list
        public void AddNext(string word)
        {
            this.next.Add(word);
        }

        // Adds given character to the dictionary
        public void AddDictionary(char character)
        {
            this.dictionary.Add(character, new HybridTrieNode());
        }
    }
}