using IrcDotNet;
using IrcDotNet.Ctcp;
using Jackett.Models.Commands;
using Jackett.Models.Commands.IRC;
using Jackett.Models.DTO;
using Jackett.Models.Irc;
using Jackett.Models.Irc.DTO;
using Jackett.Utils;
using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;

namespace Jackett.Services
{
    public interface IIRCService
    {
        List<NetworkDTO> GetSummary();
        List<Message> GetMessages(string network, string channel);
        List<User> GetUser(string network, string channel);
    }

    public class IRCService : IIRCService, INotificationHandler<AddProfileCommand>
    { 
        List<Network> networks = new List<Network>();
        IIDService idService = null;

        public IRCService(IIDService i)
        {
            idService = i;
        }

        public List<Message> GetMessages(string networkId, string channelId)
        {
            var network = networks.Where(n => n.Id == networkId).FirstOrDefault();
            if (network == null)
                return new List<Message>();
            if (string.IsNullOrEmpty(channelId))
            {
                return network.Messages.TakeLast(100).ToList();
            }

            var channel = network.Channels.Where(c => c.Id == channelId).FirstOrDefault();
            return channel.Messages.TakeLast(100).ToList();
        }

        public List<User> GetUser(string networkId, string channelId)
        {
            var network = networks.Where(n => n.Id == networkId).FirstOrDefault();
            if (network == null)
                return new List<User>();
            var channel = network.Channels.Where(c => c.Id == channelId).FirstOrDefault();
            return channel.Users.ToList();
        }

        public List<NetworkDTO> GetSummary()
        {
            var list = new List<NetworkDTO>();
            foreach(var network in networks)
            {
                var d = new NetworkDTO();
                d.Name = network.Name;
                d.Id = network.Id;
                foreach(var c in network.Channels)
                {
                    d.Channels.Add(new ChannelDTO()
                    {
                        Name = c.Name
                    });
                }
                list.Add(d);
            }

            return list;
        }

        public void Start()
        {
            networks.Add(new Network()
            {
                Address  = "chat.freenode.net:6667"
            });

         /*  foreach(var network in networks)
            {
                SetupNetwork(network);
                Connect(network);
            }*/
        }

        private void SetupNetwork(Network network)
        {
            var client =  network.Client = new StandardIrcClient();
            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            client.Registered += Client_Registered;
            client.Disconnected += Client_Disconnected;
            client.ClientInfoReceived += Client_ClientInfoReceived;
            client.Error += Client_Error;
            client.ErrorMessageReceived += Client_ErrorMessageReceived;
            client.MotdReceived += Client_MotdReceived;
            client.ProtocolError += Client_ProtocolError;
            client.ChannelListReceived += Client_ChannelListReceived;
            client.ConnectFailed += Client_ConnectFailed;
            client.NetworkInformationReceived += Client_NetworkInformationReceived;
            client.PingReceived += Client_PingReceived;
            client.PongReceived += Client_PongReceived;
            client.ServerBounce += Client_ServerBounce;
            client.ServerStatsReceived += Client_ServerStatsReceived;
            client.ServerTimeReceived += Client_ServerTimeReceived;
            client.ServerSupportedFeaturesReceived += Client_ServerSupportedFeaturesReceived;
            client.ServerVersionInfoReceived += Client_ServerVersionInfoReceived;
            client.WhoIsReplyReceived += Client_WhoIsReplyReceived;
            client.WhoReplyReceived += Client_WhoReplyReceived;
            client.WhoWasReplyReceived += Client_WhoWasReplyReceived;

            var ctcpClient = new CtcpClient(client);
            ctcpClient.ClientVersion = "Jackett " + Engine.ConfigService.GetVersion();
            ctcpClient.PingResponseReceived += CtcpClient_PingResponseReceived;
            ctcpClient.VersionResponseReceived += CtcpClient_VersionResponseReceived;
            ctcpClient.TimeResponseReceived += CtcpClient_TimeResponseReceived;
            ctcpClient.ActionReceived += CtcpClient_ActionReceived;

            networks.Add(network);
        }

        private void Client_Registered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += LocalUser_LeftChannel;
            client.LocalUser.InviteReceived += LocalUser_InviteReceived;
            client.LocalUser.IsAwayChanged += LocalUser_IsAwayChanged;
            client.LocalUser.ModesChanged += LocalUser_ModesChanged;
            client.LocalUser.NickNameChanged += LocalUser_NickNameChanged;
        }

        private void LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            var channel = GetChannelFromEvent(e);
            channel.Joined = true;
            e.Channel.UserJoined += Channel_UserJoined;
            e.Channel.MessageReceived += Channel_MessageReceived;
            e.Channel.ModesChanged += Channel_ModesChanged;
            e.Channel.NoticeReceived += Channel_NoticeReceived;
            e.Channel.TopicChanged += Channel_TopicChanged;
            e.Channel.UserInvited += Channel_UserInvited;
            e.Channel.UserKicked += Channel_UserKicked;
            e.Channel.UserLeft += Channel_UserLeft;
            e.Channel.UsersListReceived += Channel_UsersListReceived;
        }

        private Network NetworkFromIrcClient(StandardIrcClient client)
        {
            return networks.Where(n => n.Client.ToString() == client.ToString()).First();
        }

        private Network NetworkFromIrcClient(IrcLocalUser user)
        {
            return NetworkFromIrcClient((StandardIrcClient)user.Client);
        }

        private void BroadcastMessageToNetwork(Network network, Message message)
        {
            network.Messages.Add(message);
            foreach(var channel in network.Channels)
                channel.Messages.Add(message);
        }

        private void CtcpClient_ActionReceived(object sender, CtcpMessageEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = e.Source.NickName,
                Text = e.Text,
                Type = MessageType.CTCP
            });

        }

        private void CtcpClient_TimeResponseReceived(object sender, CtcpTimeResponseReceivedEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = e.User.NickName,
                Text = $"Time received: {e.DateTime}",
                Type = MessageType.CTCP
            });
        }

        private void CtcpClient_VersionResponseReceived(object sender, CtcpVersionResponseReceivedEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = e.User.NickName,
                Text = $"Version received: {e.VersionInfo}",
                Type = MessageType.CTCP
            });
        }

        private void CtcpClient_PingResponseReceived(object sender, CtcpPingResponseReceivedEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = e.User.NickName,
                Text = $"Ping time: {e.PingTime}",
                Type = MessageType.CTCP
            });
        }

        private void Client_WhoWasReplyReceived(object sender, IrcUserEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = e.User.NickName,
                Text = $"Who was: {e.User.NickName}",
                Type = MessageType.System
            });
        }

        private void Client_WhoReplyReceived(object sender, IrcNameEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = network.Address,
                Text = $"Who was: {e.Name}",
                Type = MessageType.System
            });
        }

        private void Client_WhoIsReplyReceived(object sender, IrcUserEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = network.Address,
                Text = $"Who is: {e.User.NickName}",
                Type = MessageType.System
            });
        }

        private void Client_ServerVersionInfoReceived(object sender, IrcServerVersionInfoEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = e.ServerName,
                Text = $"Server version: {e.Version}",
                Type = MessageType.System
            });
        }

        private void Client_ServerSupportedFeaturesReceived(object sender, EventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            foreach (var config in network.Client.ServerSupportedFeatures)
            {
                network.Messages.Add(new Message()
                {
                    From = network.Address,
                    Text = $"Server features: {config.Key} = {config.Value}",
                    Type = MessageType.System
                });
            }
        }

        private void Client_ServerTimeReceived(object sender, IrcServerTimeEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = e.ServerName,
                Text = $"Server time: {e.DateTime}",
                Type = MessageType.System
            });
        }

        private void Client_ServerStatsReceived(object sender, IrcServerStatsReceivedEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            foreach (var entry in e.Entries)
            {
                network.Messages.Add(new Message()
                {
                    From = network.Address,
                    Text = $"Server stats ({entry.Type}) : { string.Join(",", entry.Parameters)}",
                    Type = MessageType.System
                });
            }
        }

        private void Client_ServerBounce(object sender, IrcServerInfoEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = network.Address,
                Text = $"Server is requesting bounce to: {e.Address}:{e.Port}",
                Type = MessageType.System
            });
        }

        private void Client_PongReceived(object sender, IrcPingOrPongReceivedEventArgs e)
        {
           // Ignore
        }

        private void Client_PingReceived(object sender, IrcPingOrPongReceivedEventArgs e)
        {
            // Ignore
        }

        private void Client_NetworkInformationReceived(object sender, IrcCommentEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = network.Address,
                Text = e.Comment,
                Type = MessageType.System
            });
        }

        private void Client_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = network.Address,
                Text = $"Connect failed",
                Type = MessageType.System
            });
        }

        private void Client_ChannelListReceived(object sender, IrcChannelListReceivedEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            foreach (var channel in e.Channels)
            {
                BroadcastMessageToNetwork(network, new Message()
                {
                    From = network.Address,
                    Text = $"Channel: \"{channel.Name}\" Users: {channel.VisibleUsersCount} Topic: \"{channel.Topic}\"",
                    Type = MessageType.System
                });
            }
        }

        private void Client_ClientInfoReceived(object sender, EventArgs e)
        {
            // Ignore  ServerName/ServerVersion/ServerAvailableUserModes/ServerAvailableChannelModes  is set
        }

        private void Client_ProtocolError(object sender, IrcProtocolErrorEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = network.Address,
                Text = $"Protocol error ({e.Code}): {e.Message} { string.Join(" ", e.Parameters)}",
                Type = MessageType.System
            });
        }

        private void Client_MotdReceived(object sender, EventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = network.Address,
                Text = $"MOTD: {network.Client.MessageOfTheDay}",
                Type = MessageType.System
            });
        }

        private void Client_ErrorMessageReceived(object sender, IrcErrorMessageEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = network.Address,
                Text = $"Network Error: {e.Message}",
                Type = MessageType.System
            });
        }
    

        private void Client_Error(object sender, IrcErrorEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = network.Address,
                Text = $"Client Error: {e.Error.Message} {e.Error.StackTrace}",
                Type = MessageType.System
            });
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = network.Address,
                Text = "Disconnected",
                Type = MessageType.System
            });

            foreach(var channel in network.Channels)
            {
                channel.Joined = false;
            }
        }

        private void LocalUser_NickNameChanged(object sender, EventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = network.Address,
                Text = $"Nickname changed to {network.Client.LocalUser.NickName}",
                Type = MessageType.System
            });
        }

        private void LocalUser_ModesChanged(object sender, EventArgs e)
        {
            var user = sender as IrcLocalUser;
            var network = NetworkFromIrcClient((StandardIrcClient)user.Client);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = network.Address,
                Text = $"Your modes were set to: {String.Join(",", network.Client.LocalUser.Modes)}",
                Type = MessageType.System
            });
        }

        private void LocalUser_IsAwayChanged(object sender, EventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            network.Messages.Add(new Message()
            {
                From = network.Address,
                Text = network.Client.LocalUser.IsAway?"You were marked away": "You are no longer marked as away",
                Type = MessageType.System
            });
        }

        private void LocalUser_InviteReceived(object sender, IrcChannelInvitationEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = e.Inviter.NickName,
                Text = $"You were invited to: {e.Channel} {e.Comment}",
                Type = MessageType.System
            });
        }

        private Channel GetChannelFromEvent(IrcChannelEventArgs e)
        {
            var network = networks.Where(n => n.Client == e.Channel.Client).First();
            var channel = network.Channels.Where(c => c.Name == e.Channel.Name).FirstOrDefault();
            if(channel == null)
            {
                channel = new Channel()
                {
                    Joined = true,
                    Name = e.Channel.Name
                };
            }

            return channel;
        }

        private ChannelInfoResult GetChannelInfoFromClientChannel(IrcChannel ircchannel)
        {
            var network = networks.Where(n => n.Client == ircchannel.Client).First();
            return new ChannelInfoResult()
            {
                Network = network,
               Channel = network.Channels.Where(c => c.Name == ircchannel.Name).FirstOrDefault()
        };
        }


        private Network GetNetworkFromEvent(IrcChannelEventArgs e)
        {
            return networks.Where(n => n.Client == e.Channel.Client).First();
        }

        private void LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            e.Channel.UserJoined -= Channel_UserJoined;
            e.Channel.MessageReceived -= Channel_MessageReceived;
            e.Channel.ModesChanged -= Channel_ModesChanged;
            e.Channel.NoticeReceived -= Channel_NoticeReceived;
            e.Channel.TopicChanged -= Channel_TopicChanged;
            e.Channel.UserInvited -= Channel_UserInvited;
            e.Channel.UserKicked -= Channel_UserKicked;
            e.Channel.UserLeft -= Channel_UserLeft;
            e.Channel.UsersListReceived -= Channel_UsersListReceived;

            var network = GetNetworkFromEvent(e);
            var channel = GetChannelFromEvent(e);
            channel.Joined = false;
            channel.Messages.Add(new Message()
            {
                From = network.Address,
                Text = "You left this channel",
                Type = MessageType.System
            });
        }

        private void Channel_UsersListReceived(object sender, EventArgs e)
        {
           // event
        }

        private void Channel_UserLeft(object sender, IrcChannelUserEventArgs e)
        {
            // event
        }

        private void Channel_UserKicked(object sender, IrcChannelUserEventArgs e)
        {
            var info = GetChannelInfoFromClientChannel(sender as IrcChannel);
            info.Channel.Messages.Add(new Message()
            {
                From = info.Network.Address,
                Text =$"{e.ChannelUser.User.NickName} was kicked.",
                Type = MessageType.System
            });
        }

        private void Channel_UserInvited(object sender, IrcUserEventArgs e)
        {
            var info = GetChannelInfoFromClientChannel(sender as IrcChannel);
            info.Channel.Messages.Add(new Message()
            {
                From = info.Network.Address,
                Text = $"{e.User.NickName} was invited.",
                Type = MessageType.System
            });
        }

        private void Channel_TopicChanged(object sender, IrcUserEventArgs e)
        {
            var info = GetChannelInfoFromClientChannel(sender as IrcChannel);
            info.Channel.Messages.Add(new Message()
            {
                From = info.Network.Address,
                Text = $"{e.User.NickName} was invited.",
                Type = MessageType.System
            });
        }

        private void Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var info = GetChannelInfoFromClientChannel(sender as IrcChannel);
            info.Channel.Messages.Add(new Message()
            {
                From = e.Source.Name,
                Text = e.Text,
                Type = MessageType.Notice
            });
        }

        private void Channel_ModesChanged(object sender, IrcUserEventArgs e)
        {
            var info = GetChannelInfoFromClientChannel(sender as IrcChannel);
            info.Channel.Messages.Add(new Message()
            {
                From = e.User.NickName,
                Text = $"Channel modes changed to: {(sender as IrcChannel).Modes}",
                Type = MessageType.System
            });
        }

        private void Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var info = GetChannelInfoFromClientChannel(sender as IrcChannel);
            info.Channel.Messages.Add(new Message()
            {
                From = e.Source.Name,
                Text = e.Text,
                Type = MessageType.Message
            });
        }

        private void Channel_UserJoined(object sender, IrcChannelUserEventArgs e)
        {
            var info = GetChannelInfoFromClientChannel(sender as IrcChannel);
            info.Channel.Messages.Add(new Message()
            {
                From = info.Network.Address,
                Text = $"User joined: {e.ChannelUser.User.NickName}",
                Type = MessageType.System
            });
        }

        private void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            // TODO PM Implementation
            var network = NetworkFromIrcClient(sender as IrcLocalUser); // Confirmed
            BroadcastMessageToNetwork(network, new Message()
            {
                From = e.Source.Name,
                Text = $"Private message {e.Text}",
                Type = MessageType.Message
            });
        }

        private void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var network = NetworkFromIrcClient(sender as StandardIrcClient);
            BroadcastMessageToNetwork(network, new Message()
            {
                From = e.Source.Name,
                Text = e.Text,
                Type = MessageType.Notice
            });
        }

        private void Connect(Network network)
        {
            var port = 6667;
            var addr = string.Empty;
            var split = network.Address.Split(':');
            if (split.Length > 0)
                addr = split[0];
            if (split.Length > 1)
                port = ParseUtil.CoerceInt(split[1]);

            network.Client.Connect(addr, port, network.UseSSL, new IrcUserRegistrationInfo()
            {
                NickName = network.Username,
                UserName = network.Username,
                RealName = network.Username
            });
        }

        public List<NetworkInfo> NetworkInfo
        {
            get
            {
                var info = new List<NetworkInfo>();
                foreach(var network in networks)
                {
                    var netInfo = new NetworkInfo()
                    {
                        Address = network.Address,
                        Name = network.Name
                    };

                    foreach (var channel in network.Channels) {
                        netInfo.Channels.Add(new ChannelInfo()
                        {
                            Joined = channel.Joined,
                            Name = channel.Name
                        });
                    }

                    info.Add(netInfo);
                }
                return info;
            }
        }

        void INotificationHandler<AddProfileCommand>.Handle(AddProfileCommand notification)
        {
            var network = networks.Where(n => n.Id == notification.Profile.Id).FirstOrDefault();
            if (network == null)
            {
                network = new Network()
                {
                    Id = notification.Profile.Id,
                    Name = notification.Profile.Name,
                    Address = "adams.freenode.net",
                    Username = notification.Profile.Username
                };

                SetupNetwork(network);
                Connect(network);
            }
        }
    }
}
