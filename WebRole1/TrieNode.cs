using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieNode
    {
        public TrieNode[] Next;
        public char Value;
        public bool End;
        public TrieNode()
        {
            Next = new TrieNode[27];
            for (int i = 0; i < 27; i++)
            {
                Next[i] = null;
            }
            this.End = false;

        }
    }
}