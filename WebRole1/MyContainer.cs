using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    sealed class MyContainer : BurstNavigable
    {
        public static int BurstThreshold = 45; //Threshhold specify whether a container should be burst into a node

        private SortedSet<Word> _records; //keeps all the record prefix 

        public MyContainer()
        {
            _records = new SortedSet<Word>(new WordComparer()); //Word compare to sort the record object
        }

        /// <summary>
        /// Get the type of the object
        /// </summary>
        /// <returns>in this case return c for container </returns>
        public override char getType()
        {
            return 'c';
        }

        /// <summary>
        /// Method is only used for trie node class 
        /// </summary>
        /// <param name="num">number or place of the node</param>
        /// <returns></returns>
        public override BurstNavigable GetChild(int num)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add only portion of the word into the container with the starting index
        /// </summary>
        /// <param name="word">the word itself</param>
        /// <param name="start">starting index</param>
        /// <param name="pageCount">page count of the word</param>
        public override void Add(char[] word, int start, int pageCount)
        {
            if (word.Length == start)
            {
                End = true;
                return;
            }
            string result = "";
            for (int i = start; i < word.Length; i++)
            {
                result = result + word[i];
            }
            _records.Add(new Word(result, pageCount));
            if (_records.Count >= BurstThreshold)
                ShouldBurst = true;
        }

        /// <summary>
        /// Return whether or not the number of words in the container exceeds the BurstThreshold
        /// </summary>
        /// <returns></returns>
        public bool shouldBust()
        {
            return _records.Count >= BurstThreshold;
        }

        /// <summary>
        /// Add the whole word into the container
        /// </summary>
        /// <param name="word">the word itself</param>
        /// <param name="pageCount">the page count of that word</param>
        public void Add(string word, int pageCount)
        {
            _records.Add(new Word(word, pageCount));
            if (_records.Count >= BurstThreshold)
                ShouldBurst = true;
        }

        /// <summary>
        /// Method only tobe used by the node class
        /// </summary>
        /// <returns>return null because containers don't have an array of children</returns>
        public override BurstNavigable[] GetNexts()
        {
            return null;
        }

        /// <summary>
        /// Return all the words within the container
        /// </summary>
        /// <returns>return a set of words that are in the current container</returns>
        public override SortedSet<Word> GetChildren()
        {
            return _records;
        }

        //Method will only be used by the trie node class
        public override void Add(char[] word, int pageCount)
        {
            throw new NotImplementedException();
        }
    }

}