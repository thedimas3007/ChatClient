using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Generation {
    internal class Model {
        public string Provider { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }

        public Model(string provider, string id, string name) {
            Provider = provider;
            Id = id;
            Name = name;
        }
    }
}
