using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    //This is an abstract class to instantiate container and trie node class
    [Serializable]
    abstract class BurstNavigable
    {
        /// <summary>
        /// Add word into the trie
        /// </summary>
        /// <param name="word">char array</param>
        /// <param name="pageCount">page count</param>
        public abstract void Add(char[] word, int pageCount);

        /// <summary>
        /// Add word into the container using a starting index
        /// </summary>
        /// <param name="word">word</param>
        /// <param name="start">starting index</param>
        /// <param name="pageCount">pagecount of that word</param>
        public abstract void Add(char[] word, int start, int pageCount);

        /// <summary>
        /// Get back all the child node of the node (to be used only for trie node)
        /// </summary>
        /// <returns>array of children(can be node or container)</returns>
        public abstract BurstNavigable[] GetNexts();

        /// <summary>
        /// Get back all the words in the sorted set (to be used only for the container class)
        /// </summary>
        /// <returns>Sorted set of all the words in the container</returns>
        public abstract SortedSet<Word> GetChildren();

        /// <summary>
        /// Get the type of the class
        /// </summary>
        /// <returns>node(n) or container(c)</returns>
        public abstract char getType();

        /// <summary>
        /// Get the node child from the children array of the node class
        /// </summary>
        /// <param name="num">number or the char place of the node/container</param>
        /// <returns>the node or container</returns>
        public abstract BurstNavigable GetChild(int num);

        public bool ShouldBurst; //specify whether or not a container should burst
        public bool End = false; // note: can save a lot of space by moving this to a static
    }
}