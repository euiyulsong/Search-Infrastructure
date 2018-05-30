using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class Trie
    {
        private TrieNode overallRoot;
        public List<string> list;
        public Trie()
        {
            this.overallRoot = new TrieNode();
        }

        public Trie(string word)
        {
            this.overallRoot = new TrieNode();
            Add(word);
        }

        public void Add(string word)
        {
            word = word.ToLower();
            TrieNode node = overallRoot;

            for (int i = 0; i < word.Length; i++)
            {
                int character = word[i] - 97;
                if (word[i] - 6 == 26)
                {
                    character = word[i] - 6;
                }
                if (node.Next[character] == null)
                {
                    node.Next[character] = new TrieNode();
                }
                node = node.Next[character];
            }
            node.End = true;
        }

        public bool Exists(string word)
        {
            word = word.ToLower();
            TrieNode node = overallRoot;
            if (Helper(word) == null)
            {
                return false;
            }
            else
            {
                return node != null;
            }
        }

        private TrieNode Helper(string word)
        {
            TrieNode node = overallRoot;
            for (int i = 0; i < word.Length; i++)
            {
                int character = word[i];
                if (word[i] == ' ')
                {
                    character -= 6;
                }
                else
                {
                    character -= 97;
                }
                if (node.Next[character] == null)
                {
                    return null;
                }

                node = node.Next[character];
            }
            return node;
        }

        public List<string> Search(string word)
        {
            word = word.ToLower();
            TrieNode node = Helper(word);
            List<string> result = new List<string>();
            if (node != null)
            {
                result = SearchNode(node, word, result);
                if (result.Count == 0)
                {
                    result.Add("No Query Result");
                }
            }
            else
            {
                result.Add("No Query Result");
            }
            return result;
        }

        private List<string> SearchNode(TrieNode node, string word, List<string> list)
        {
            if (node == null)
            {
                return list;
            }
            else
            {
                if (list.Count < 10)
                {
                    if (node.End)
                    {
                        list.Add(word);
                    }
                    for (int i = 0; i < node.Next.Length; i++)
                    {
                        if (list.Count >= 10)
                        {
                            return list;
                        }
                        if (node.Next[i] != null && list.Count < 10)
                        {
                            if (i == 26)
                            {
                                word += " ";
                            }
                            else
                            {
                                word += (char)(i + 97);
                            }
                            list = SearchNode(node.Next[i], word, list);
                            word = word.Substring(0, word.Length - 1);
                        }

                    }
                }
            }
            return list;
        }
    }
}