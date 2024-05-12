using ChatClient.Providers;
using ChatClient.Repositories;

namespace ChatClient.Types;

internal class ChatParams {
    public readonly Chat Chat;
    public readonly MessageRepository Repository;
    public readonly SettingsProvider Settings;

    public ChatParams(MessageRepository repository, SettingsProvider settings, Chat chat) {
        Repository = repository;
        Settings = settings;
        Chat = chat;
    }
}