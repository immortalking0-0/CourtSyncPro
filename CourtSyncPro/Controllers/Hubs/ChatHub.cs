using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using System.Collections.Concurrent;

namespace CourtSyncPro.Hubs
{
    public class ChatHub : Hub
    {
        // ── Static dictionaries to track online users ─────────
        // connectionId  →  userId
        private static readonly ConcurrentDictionary<string, int>
            _connections = new();

        // userId  →  connectionId
        private static readonly ConcurrentDictionary<int, string>
            _userConnections = new();

        private readonly ApplicationDbContext _db;

        public ChatHub(ApplicationDbContext db) => _db = db;

        // ── Called when browser connects ──────────────────────
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                _connections[Context.ConnectionId] = userId;
                _userConnections[userId] = Context.ConnectionId;

                await BroadcastOnlineUsers();
            }
            await base.OnConnectedAsync();
        }

        // ── Called when browser disconnects / closes ──────────
        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                _connections.TryRemove(Context.ConnectionId, out _);
                _userConnections.TryRemove(userId, out _);

                await BroadcastOnlineUsers();
            }
            await base.OnDisconnectedAsync(ex);
        }

        // ── Player A sends chat request to Player B ───────────
        public async Task SendChatRequest(int toUserId)
        {
            var fromUserId = GetUserId();
            if (fromUserId <= 0) return;

            // Check not already pending
            bool exists = await _db.ChatRequests.AnyAsync(r =>
                r.FromUserId == fromUserId &&
                r.ToUserId == toUserId &&
                r.Status == ChatRequestStatus.Pending);

            if (exists) return;

            // Check accepted already exists
            bool accepted = await _db.ChatRequests.AnyAsync(r =>
                ((r.FromUserId == fromUserId && r.ToUserId == toUserId) ||
                 (r.FromUserId == toUserId && r.ToUserId == fromUserId)) &&
                r.Status == ChatRequestStatus.Accepted);

            if (accepted) return;

            // Save to DB
            var request = new ChatRequest
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Status = ChatRequestStatus.Pending,
                SentAt = DateTime.UtcNow
            };
            _db.ChatRequests.Add(request);
            await _db.SaveChangesAsync();

            // Get sender's name
            var sender = await _db.Users.FindAsync(fromUserId);

            // Notify Player B if online
            if (_userConnections.TryGetValue(toUserId, out var toConn))
            {
                await Clients.Client(toConn).SendAsync("ReceiveChatRequest",
                    new
                    {
                        requestId = request.RequestId,
                        fromUserId = fromUserId,
                        fromName = sender?.Name ?? "Unknown"
                    });
            }

            // Confirm to Player A
            await Clients.Caller.SendAsync("RequestSent",
                new { toUserId, message = "Chat request sent!" });
        }

        // ── Player B accepts the request ──────────────────────
        public async Task AcceptRequest(int requestId)
        {
            var userId = GetUserId();
            if (userId <= 0) return;

            var request = await _db.ChatRequests
                .Include(r => r.FromUser)
                .Include(r => r.ToUser)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId &&
                    r.ToUserId == userId &&
                    r.Status == ChatRequestStatus.Pending);

            if (request == null) return;

            request.Status = ChatRequestStatus.Accepted;
            request.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify both players to open chat
            var accepter = await _db.Users.FindAsync(userId);

            // Tell Player A
            if (_userConnections.TryGetValue(request.FromUserId, out var fromConn))
            {
                await Clients.Client(fromConn).SendAsync("RequestAccepted",
                    new
                    {
                        chatWithUserId = userId,
                        chatWithName = accepter?.Name ?? "Unknown"
                    });
            }

            // Tell Player B (self)
            await Clients.Caller.SendAsync("RequestAccepted",
                new
                {
                    chatWithUserId = request.FromUserId,
                    chatWithName = request.FromUser?.Name ?? "Unknown"
                });
        }

        // ── Player B declines the request ─────────────────────
        public async Task DeclineRequest(int requestId)
        {
            var userId = GetUserId();
            if (userId <= 0) return;

            var request = await _db.ChatRequests
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId &&
                    r.ToUserId == userId &&
                    r.Status == ChatRequestStatus.Pending);

            if (request == null) return;

            request.Status = ChatRequestStatus.Declined;
            request.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify Player A their request was declined
            if (_userConnections.TryGetValue(request.FromUserId, out var fromConn))
            {
                var decliner = await _db.Users.FindAsync(userId);
                await Clients.Client(fromConn).SendAsync("RequestDeclined",
                    new { name = decliner?.Name ?? "Unknown" });
            }
        }

        // ── Send a chat message ───────────────────────────────
        public async Task SendMessage(int toUserId, string messageText)
        {
            var fromUserId = GetUserId();
            if (fromUserId <= 0 || string.IsNullOrWhiteSpace(messageText))
                return;

            // Verify they have an accepted request
            bool canChat = await _db.ChatRequests.AnyAsync(r =>
                ((r.FromUserId == fromUserId && r.ToUserId == toUserId) ||
                 (r.FromUserId == toUserId && r.ToUserId == fromUserId)) &&
                r.Status == ChatRequestStatus.Accepted);

            if (!canChat) return;

            // Save message
            var msg = new ChatMessage
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Message = messageText.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            var sender = await _db.Users.FindAsync(fromUserId);

            var payload = new
            {
                messageId = msg.MessageId,
                fromUserId = fromUserId,
                fromName = sender?.Name ?? "Unknown",
                toUserId = toUserId,
                message = messageText.Trim(),
                sentAt = msg.SentAt.ToString("hh:mm tt")
            };

            // Send to receiver if online
            if (_userConnections.TryGetValue(toUserId, out var toConn))
                await Clients.Client(toConn).SendAsync("ReceiveMessage", payload);

            // Echo back to sender
            await Clients.Caller.SendAsync("ReceiveMessage", payload);
        }

        // ── Public method for other code to check online status ─
        public static bool IsUserOnline(int userId)
            => _userConnections.ContainsKey(userId);

        public static List<int> GetOnlineUserIds()
            => _userConnections.Keys.ToList();

        // ── Private helper ────────────────────────────────────
        private int GetUserId()
        {
            var claim = Context.User?.FindFirst("UserId")?.Value;
            if (int.TryParse(claim, out int id)) return id;

            // Fallback: try connection context items
            if (Context.Items.TryGetValue("UserId", out var obj) &&
                obj is int uid) return uid;

            return 0;
        }

        private async Task BroadcastOnlineUsers()
        {
            var onlineIds = _userConnections.Keys.ToList();

            var users = await _db.Users
                .Where(u => onlineIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Name })
                .ToListAsync();

            await Clients.All.SendAsync("UpdateOnlineUsers", users);
        }
    }
}