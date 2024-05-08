using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Data.Sqlite;
using OpenAI.ObjectModels.RequestModels;
using static OpenAI.ObjectModels.SharedModels.IOpenAiModels;
using static System.Net.WebRequestMethods;
using System.Xml;
using WinRT;
using OpenAI.ObjectModels.ResponseModels;
using System.Data;

namespace ChatClient.Repositories {
    enum MessageRole {
        System,
        Assistant,
        User,
        Tool
    }

    class Chat {
        public int Id;
        public string Title;
        public DateTime CreatedAt;
        public DateTime LastAccessed;

        public List<Message> GetMessages() {
            return new List<Message>();
        }

        public List<ChatMessage> GetChatMessages() {
            return GetMessages().ConvertAll(m => m.AsChatMessage());
        }

        public Chat(int id, string title, DateTime? createdAt, DateTime? lastAccessed) {
            Id = id;
            Title = title;
            CreatedAt = createdAt ?? DateTime.Now;
            LastAccessed = lastAccessed ?? DateTime.Now;
        }

        
    }

    class Message { // TODO: image support
        public int Id;
        public int ChatId;
        public MessageRole Role;
        public string? Content;
        public List<ToolCall>? ToolCalls;
        public string? Name;
        public string? ToolCallId;
        public DateTime CreatedAt;

        public Message(int id, int chatId, MessageRole role, string content, List<ToolCall>? toolCalls = null, string? name = null, string? toolCallId = null, DateTime? createdAt = null) {
            Id = id;
            ChatId = chatId;
            Role = role;
            Content = content;
            ToolCalls = toolCalls;
            Name = name;
            ToolCallId = toolCallId;
            CreatedAt = createdAt ?? DateTime.Now;
        }

        public ChatMessage AsChatMessage() {
            return new ChatMessage(Role.ToString().ToLower(), Content, Name, ToolCalls, ToolCallId);
        }
    }

    class MessageRepository {
        private static bool _loaded = false;

        private static StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
        private static string _databasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "chatData.db");
        private static SqliteConnection _connection;

        private static void AssertLoaded() {
            if (!_loaded) throw new InvalidOperationException("The database is not loaded.");
        }

        public static async void Load() {
            if (_loaded) return;

            if (!await _localFolder.FileExistsAsync("chatData.db")) {
                await _localFolder.CreateFileAsync("chatData.db");
            }

            _connection = new SqliteConnection($"Filename={_databasePath}");
            _connection.Open();

            var createChatTableCommand = new SqliteCommand(@"CREATE TABLE IF NOT EXISTS chat (
                id INTEGER UNIQUE PRIMARY KEY AUTOINCREMENT
                title VARCHAR(255) NOT NULL,
                createdAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                lastAccessed TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            );", _connection);
            await createChatTableCommand.ExecuteNonQueryAsync();

            var createMessageTableCommand = new SqliteCommand(@"CREATE TABLE IF NOT EXISTS message (
                id INTEGER UNIQUE PRIMARY KEY AUTOINCREMENT
                chatId INTEGER NOT NULL,
                role VARCHAR(10) NOT NULL,
                content TEXT,
                toolCalls TEXT,
                name VARCHAR(64),
                toolCallId VARCHAR(48),
                createdAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            );", _connection);
            await createMessageTableCommand.ExecuteNonQueryAsync();
            _loaded = true;
        }

        #region Chats

        public static async Task<bool> ChatExists(int id) {
            AssertLoaded();
            var selectChatCommand = new SqliteCommand(@"SELECT * FROM chat WHERE `id` = @Id", _connection);
            selectChatCommand.Parameters.AddWithValue("@Id", id);
            var reader = await selectChatCommand.ExecuteReaderAsync();
            return reader.HasRows;
        }

        public static async Task<List<Chat>> GetChats() {
            AssertLoaded();
            var result = new List<Chat>();
            var getChatsCommand = new SqliteCommand(@"SELECT * FROM chat;", _connection);
            var reader = await getChatsCommand.ExecuteReaderAsync();
            do {
                result.Add(new Chat(reader.GetInt32(0), reader.GetString(1),
                    reader.GetDateTime(2), reader.GetDateTime(3)));
            } while (reader.Read());

            return result;
        }

        public static async Task<Chat?> GetChat(int id) {
            AssertLoaded();
            var selectChatCommand = new SqliteCommand(@"SELECT * FROM chat WHERE `id` = @Id", _connection);
            selectChatCommand.Parameters.AddWithValue("@Id", id);
            var reader = await selectChatCommand.ExecuteReaderAsync();
            if (!reader.HasRows) return null;

            reader.Read();
            return new Chat(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2), reader.GetDateTime(3));
        }

        public static async Task<Chat> CreateChat(string title) {
            AssertLoaded();
            var insertChatCommand =
                new SqliteCommand(@"INSERT INTO chat (title) VALUES (@Title); SELECT last_insert_rowid();",
                    _connection);
            insertChatCommand.Parameters.AddWithValue("@Title", title);

            int id = Convert.ToInt32(await insertChatCommand.ExecuteScalarAsync());
            return await GetChat(id);
        }

        public static async Task UpdateChat(int id, string key, string value) {
            AssertLoaded();
            var updateChatCommand = new SqliteCommand(@"UPDATE chat SET @Key = @Value WHERE Id = @Id;", _connection);
            updateChatCommand.Parameters.AddWithValue("@Id", id);
            updateChatCommand.Parameters.AddWithValue("@Key", key);
            updateChatCommand.Parameters.AddWithValue("@Value", value);

            await updateChatCommand.ExecuteNonQueryAsync();
        }

        public static async Task DeleteChat(int id) {
            AssertLoaded();
            var deleteChatCommand = new SqliteCommand(@"DELETE FROM chat WHERE Id = @Id;", _connection);
            deleteChatCommand.Parameters.AddWithValue("@Id", id);

            await deleteChatCommand.ExecuteNonQueryAsync();
        }

        #endregion

        #region Messages

        public static async Task<List<Message>> GetMessages(int chatId) {
            AssertLoaded();
            if (!await ChatExists(chatId)) throw new KeyNotFoundException($"Chat with ID {chatId} does not exist.");

            var getMessagesCommand = new SqliteCommand(@"SELECT * FROM message WHERE chatId = @ChatId;", _connection);
            getMessagesCommand.Parameters.AddWithValue("@ChatId", chatId);
            var reader = await getMessagesCommand.ExecuteReaderAsync();
            if (!reader.HasRows) return new List<Message>();

            List<Message> result = new List<Message>();
            do {
                MessageRole role;
                Enum.TryParse(reader.GetString(2), true, out role);
                List<ToolCall> toolCalls = await JsonSerializer.DeserializeAsync<List<ToolCall>>(reader.GetStream(4));


                result.Add(new Message(reader.GetInt32(0), reader.GetInt32(1),
                    role, reader.GetString(3),
                    toolCalls, reader.GetString(5), reader.GetString(6)));
            } while (reader.Read());

            return result;
        }

        public static async Task<Message?> GetMessage(int id) {
            AssertLoaded();

            var selectMessageCommand = new SqliteCommand(@"SELECT * FROM message WHERE `id` = @Id", _connection);
            selectMessageCommand.Parameters.AddWithValue("@Id", id);
            var reader = await selectMessageCommand.ExecuteReaderAsync();
            if (!reader.HasRows) return null;

            reader.Read();
            MessageRole role;
            Enum.TryParse(reader.GetString(2), true, out role);
            List<ToolCall> toolCalls = await JsonSerializer.DeserializeAsync<List<ToolCall>>(reader.GetStream(4));
            return new Message(reader.GetInt32(0), reader.GetInt32(1),
                role, reader.GetString(3),
                toolCalls, reader.GetString(5), reader.GetString(6));

        }

        public static async Task<Message> CreateMessage(int chatId, ChatMessage chatMessage) {
            AssertLoaded();
            var insertMessageCommand =
                new SqliteCommand(@"INSERT INTO message (role, content, toolCalls, name, toolCallId)
                    VALUES (@Role, @Content, @ToolCalls, @Name, @ToolCallId);
                    SELECT last_insert_rowid();",
                    _connection);
            insertMessageCommand.Parameters.AddWithValue("@Role", chatMessage.Role);
            insertMessageCommand.Parameters.AddWithValue("@Content", chatMessage.Content);
            if (chatMessage.ToolCalls != null) {
                var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, chatMessage.ToolCalls);
                insertMessageCommand.Parameters.AddWithValue("@ToolCalls", Encoding.UTF8.GetString(stream.ToArray()));
            } else {
                insertMessageCommand.Parameters.AddWithValue("@ToolCalls", DBNull.Value);
            }
            insertMessageCommand.Parameters.AddWithValue("@Name", chatMessage.Name);
            insertMessageCommand.Parameters.AddWithValue("@ToolCallId", chatMessage.ToolCallId);

            int id = Convert.ToInt32(await insertMessageCommand.ExecuteScalarAsync());
            return await GetMessage(id);
        }

        public static async Task DeleteMessage(int id) {
            AssertLoaded();
            var deleteMessageCommand = new SqliteCommand(@"DELETE FROM message WHERE Id = @Id;", _connection);
            deleteMessageCommand.Parameters.AddWithValue("@Id", id);

            await deleteMessageCommand.ExecuteNonQueryAsync();
        }

        #endregion
    }
}   
