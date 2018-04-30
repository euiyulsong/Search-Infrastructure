using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class WebPageEntity : TableEntity
    {
        public WebPageEntity(string pk, string rk)
        {
            this.PartitionKey = pk;
            this.RowKey = rk;
        }
        public WebPageEntity() { }

        private string _title;
        private string _url;
        private string _date;
        private string _text;

        public string Date
        {
            get { return _date; }
            set
            {
                _date = value;
            }
        }

        public int ID { get; set; }

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
            }
        }

        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }
    }
}
