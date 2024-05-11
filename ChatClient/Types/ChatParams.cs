using ChatClient.Repositories;

namespace ChatClient.Types;

internal class ChatParams {
    public Chat Chat;
    public MessageRepository Repository;

    public ChatParams(MessageRepository repository, Chat chat) {
        Repository = repository;
        Chat = chat;
    }
}