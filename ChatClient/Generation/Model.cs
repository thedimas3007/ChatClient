using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Generation {
    internal class Model {
        public string Id { get; set; }
        public string Name { get; set; }

        public Model(string id, string name) {
            Id = id;
            Name = name;
        }
    }
}
