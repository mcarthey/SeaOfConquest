using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfConquest
{
    public class Trinket
    {
        public string Name { get; set; }
        public int Amount { get; set; }

        public Trinket(string name, int amount)
        {
            Name = name;
            Amount = amount;
        }
    }
}
