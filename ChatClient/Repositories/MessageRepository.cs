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
using System.Xml;
using WinRT;
using OpenAI.ObjectModels.ResponseModels;
using System.Data;
using File = System.IO.File;

namespace ChatClient.Repositories {
    class Chat {
        public int Id;
        public string Title;
        public DateTime CreatedAt;
        public DateTime LastAccessed;
        
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
        public string Role;
        public string? Content;
        public List<ToolCall>? ToolCalls;
        public string? Name;
        public string? ToolCallId;
        public DateTime CreatedAt;

        public Message(int id, int chatId, string role, string content, List<ToolCall>? toolCalls = null, string? name = null, string? toolCallId = null, DateTime? createdAt = null) {
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
        private StorageFolder _localFolder = ApplicationData.Current.LocalFolder;
        private string _databasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "chatData.db");
        private SqliteConnection _connection;
        
        public MessageRepository() {
            if (!File.Exists(_databasePath)) {
                File.Create(_databasePath);
            }

            _connection = new SqliteConnection($"Filename={_databasePath}");
            _connection.Open();

            var createChatTableCommand = new SqliteCommand(@"CREATE TABLE IF NOT EXISTS chat (
                id INTEGER UNIQUE PRIMARY KEY AUTOINCREMENT,
                title VARCHAR(255) NOT NULL,
                createdAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                lastAccessed TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );", _connection);
            createChatTableCommand.ExecuteNonQuery();

            var createMessageTableCommand = new SqliteCommand(@"CREATE TABLE IF NOT EXISTS message (
                id INTEGER UNIQUE PRIMARY KEY AUTOINCREMENT,
                chatId INTEGER NOT NULL,
                role VARCHAR(10) NOT NULL,
                content TEXT,
                toolCalls TEXT,
                name VARCHAR(64),
                toolCallId VARCHAR(48),
                createdAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );", _connection); 
            createMessageTableCommand.ExecuteNonQuery();
        }

        #region Chats

        public async Task<bool> ChatExists(int id) {
            var selectChatCommand = new SqliteCommand(@"SELECT * FROM chat WHERE `id` = @Id", _connection);
            selectChatCommand.Parameters.AddWithValue("@Id", id);
            var reader = await selectChatCommand.ExecuteReaderAsync();
            return reader.HasRows;
        }

        public async Task<List<Chat>> GetChats() {
            var result = new List<Chat>();
            var getChatsCommand = new SqliteCommand(@"SELECT * FROM chat;", _connection);
            var reader = await getChatsCommand.ExecuteReaderAsync();
            while (reader.Read()) {
                result.Add(new Chat(reader.GetInt32(0), reader.GetString(1),
                    reader.GetDateTime(2), reader.GetDateTime(3)));
            }

            return result;
        }

        public async Task<Chat?> GetChat(int id) {
            var selectChatCommand = new SqliteCommand(@"SELECT * FROM chat WHERE `id` = @Id", _connection);
            selectChatCommand.Parameters.AddWithValue("@Id", id);
            var reader = await selectChatCommand.ExecuteReaderAsync();
            if (!reader.HasRows) return null;

            reader.Read();
            await UpdateChat(id, "lastAccessed", DateTime.Now);
            return new Chat(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2), reader.GetDateTime(3));
        }

        public async Task<Chat> CreateChat(string title) {
            var insertChatCommand =
                new SqliteCommand(@"INSERT INTO chat (title) VALUES (@Title); SELECT last_insert_rowid();",
                    _connection);
            insertChatCommand.Parameters.AddWithValue("@Title", title);

            int id = Convert.ToInt32(await insertChatCommand.ExecuteScalarAsync());
            return await GetChat(id);
        }

        public async Task UpdateChat(int id, string key, object value) {
            var updateChatCommand = new SqliteCommand($"UPDATE chat SET {key} = @Value WHERE Id = @Id;", _connection);
            updateChatCommand.Parameters.AddWithValue("@Id", id);
            updateChatCommand.Parameters.AddWithValue("@Value", value != null ? value : DBNull.Value);

            await updateChatCommand.ExecuteNonQueryAsync();
        }

        public async Task DeleteChat(int id) {
            var deleteChatCommand = new SqliteCommand(@"DELETE FROM chat WHERE Id = @Id;", _connection);
            deleteChatCommand.Parameters.AddWithValue("@Id", id);

            await deleteChatCommand.ExecuteNonQueryAsync();
        }

        #endregion

        #region Messages

        public async Task<List<Message>> GetMessages(int chatId) {
            if (!await ChatExists(chatId)) throw new KeyNotFoundException($"Chat with ID {chatId} does not exist.");

            var getMessagesCommand = new SqliteCommand(@"SELECT * FROM message WHERE chatId = @ChatId;", _connection);
            getMessagesCommand.Parameters.AddWithValue("@ChatId", chatId);
            var reader = await getMessagesCommand.ExecuteReaderAsync();
            if (!reader.HasRows) return new List<Message>();

            List<Message> result = new List<Message>();
            while (reader.Read()) {
                List<ToolCall> toolCalls = !reader.IsDBNull(4)
                    ? await JsonSerializer.DeserializeAsync<List<ToolCall>>(reader.GetStream(4))
                    : null;

                result.Add(new Message(reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    !reader.IsDBNull(3) ? reader.GetString(3) : null,
                    toolCalls,
                    !reader.IsDBNull(5) ? reader.GetString(5) : null,
                    !reader.IsDBNull(6) ? reader.GetString(6) : null,
                    reader.GetDateTime(7)));
            }
            
            await UpdateChat(chatId, "lastAccessed", DateTime.Now);
            return result;
        }

        public async Task<Message?> GetMessage(int id) {
            var selectMessageCommand = new SqliteCommand(@"SELECT * FROM message WHERE `id` = @Id", _connection);
            selectMessageCommand.Parameters.AddWithValue("@Id", id);
            var reader = await selectMessageCommand.ExecuteReaderAsync();
            if (!reader.HasRows) return null;

            reader.Read();
            List<ToolCall> toolCalls = !reader.IsDBNull(4)
                ? await JsonSerializer.DeserializeAsync<List<ToolCall>>(reader.GetStream(4))
                : null;

            return new Message(reader.GetInt32(0), 
                reader.GetInt32(1),
                reader.GetString(2), 
                !reader.IsDBNull(3) ? reader.GetString(3) : null,
                toolCalls, 
                !reader.IsDBNull(5) ? reader.GetString(5) : null,
                !reader.IsDBNull(6) ? reader.GetString(6) : null,
                reader.GetDateTime(7));
        }

        public async Task<Message> CreateMessage(int chatId, ChatMessage chatMessage) {
            var insertMessageCommand =
                new SqliteCommand(@"INSERT INTO message (role, chatId, content, toolCalls, name, toolCallId)
                    VALUES (@Role, @ChatId, @Content, @ToolCalls, @Name, @ToolCallId);
                    SELECT last_insert_rowid();",
                    _connection);
            insertMessageCommand.Parameters.AddWithValue("@Role", chatMessage.Role);
            insertMessageCommand.Parameters.AddWithValue("@ChatId", chatId);
            insertMessageCommand.Parameters.AddWithValue("@Content", chatMessage.Content != null ? chatMessage.Content : DBNull.Value);
            if (chatMessage.ToolCalls != null) {
                var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, chatMessage.ToolCalls);
                insertMessageCommand.Parameters.AddWithValue("@ToolCalls", Encoding.UTF8.GetString(stream.ToArray()));
            } else {
                insertMessageCommand.Parameters.AddWithValue("@ToolCalls", DBNull.Value);
            }
            insertMessageCommand.Parameters.AddWithValue("@Name", chatMessage.Name != null ? chatMessage.Name : DBNull.Value);
            insertMessageCommand.Parameters.AddWithValue("@ToolCallId", chatMessage.ToolCallId != null ? chatMessage.ToolCallId : DBNull.Value);

            int id = Convert.ToInt32(await insertMessageCommand.ExecuteScalarAsync());
            await UpdateChat(chatId, "lastAccessed", DateTime.Now);
            return await GetMessage(id);
        }

        public async Task DeleteMessage(int id) {
            var deleteMessageCommand = new SqliteCommand(@"DELETE FROM message WHERE Id = @Id;", _connection);
            deleteMessageCommand.Parameters.AddWithValue("@Id", id);

            await deleteMessageCommand.ExecuteNonQueryAsync();
        }

        #endregion
    }
}   
