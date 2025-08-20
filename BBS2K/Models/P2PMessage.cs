using STUN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBS2K.Models
{
    public class P2PMessage
    {
        public MessageType MessageType { get; set; }
        public string Sender { get; set; }
        public string Content { get; set; }
    }

    public enum MessageType
    {
        Chat,
        PeerRequest,
        PeerResponse,
        Ping,
        Pong,
        Bye
    }
}
