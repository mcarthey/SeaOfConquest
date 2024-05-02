using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfConquest
{
    public class Hero
    {
        public string Name { get; set; }
        public List<string> PreferredPositions { get; set; }
        public List<string> PreferredShips { get; set; }
        public List<string> PreferredTrinkets { get; set; }

        public Hero(string name)
        {
            Name = name;
            PreferredPositions = new List<string>();
            PreferredShips = new List<string>();
            PreferredTrinkets = new List<string>();
        }
    }
}