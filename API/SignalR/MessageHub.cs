﻿using API.Data;
using API.DTO.MessageDto;
using API.Entity;
using API.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly DataContext _dataContext;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _tracker;

        public MessageHub(
            IMessageRepository messageRepository,
            DataContext dataContext, 
            IHubContext<PresenceHub> presenceHub,
            PresenceTracker tracker)
{
            _messageRepository = messageRepository;
            _dataContext = dataContext;
            _presenceHub = presenceHub;
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString().Replace('-', ' ');
            var taskId = httpContext.Request.Query["taskId"].ToString();
            var groupName = GetGroupName(Context.User.Identity.Name, otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var messages = await _messageRepository.GetMessageThread(Guid.Parse(taskId));
            await Clients.Group(groupName).SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var sender = await _dataContext.AppUser.FindAsync(createMessageDto.SenderId);
            var recipient = await _dataContext.AppUser.FindAsync(createMessageDto.RecipientId);
            var task = await _dataContext.Task.FindAsync(createMessageDto.TaskId);

            if (recipient == null) throw new HubException("Not found user");

            var message = new Message
            {
                Id = new Guid(),
                TasksId = createMessageDto.TaskId,
                SenderId = sender.Id,
                SenderUserName = sender.FirstName + " " + sender.LastName,
                Sender = sender,
                RecipientId = recipient.Id,
                RecipientUserName = recipient.FirstName + " " + recipient.LastName,
                Recipient = recipient,
                Content = createMessageDto.Content,
            };
            await _dataContext.Messages.AddAsync(message);
            await _dataContext.SaveChangesAsync();

            var notify = new Notification
            {
                Id = new Guid(),
                Content = "You have a new message in " + task.TaskName + " task",
                AppUserId = recipient.Id,
                CreateDate = DateTime.Now,
                IsRead = false,
                ProjectId = null,
                TasksId = task.Id
            };

            await _dataContext.Notifications.AddAsync(notify);
            await _dataContext.SaveChangesAsync();

            var result = new MessageForViewDto
            {
                Id = message.Id,
                TasksId = createMessageDto.TaskId,
                SenderId = sender.Id,
                SenderUserName = sender.FirstName + " " + sender.LastName,
                RecipientId = recipient.Id,
                RecipientUserName = recipient.FirstName + " " + recipient.LastName,
                Content = createMessageDto.Content,
            };
            var groupName = GetGroupName(sender.FirstName + " " + sender.LastName, recipient.FirstName + " " + recipient.LastName);

            var connections = await _tracker.GetConnectionsForUser(recipient.FirstName + " " + recipient.LastName);
            if(connections != null)
            {
                var notifies = await _dataContext.Notifications.Where(n => n.AppUserId == recipient.Id).OrderByDescending(n => n.CreateDate).ToListAsync();
                await _presenceHub.Clients.Clients(connections).SendAsync("Notification", notifies);
                await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", notify);
                var count = await _dataContext.Notifications.Where(n => n.AppUserId == recipient.Id && n.IsRead == false).CountAsync();
                await _presenceHub.Clients.Clients(connections).SendAsync("UnreadNotificationNumber", count);
            }
            await Clients.Group(groupName).SendAsync("NewMessage", result);
        }

        

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }

}
