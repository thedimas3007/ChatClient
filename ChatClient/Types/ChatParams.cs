using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatClient.Repositories;

namespace ChatClient.Types {
    class ChatParams {
        public MessageRepository Repository;
        public Chat Chat;
        public ChatParams(MessageRepository repository, Chat chat) {
            Repository = repository;
            Chat = chat;
        }
    }
}
