using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ZansiHustle.API.Realtime
{
    /// <summary>
    /// Authenticated realtime hub for user-targeted in-app events. Clients
    /// connect with their JWT (sent as the <c>access_token</c> query-string on
    /// the WebSocket — see the JwtBearer events wiring in ServiceExtensions).
    /// The server only ever pushes to <c>Clients.User(userId)</c>; private data
    /// is never broadcast. The hub itself exposes no client-callable methods —
    /// it's a one-way server→client channel fed by <see cref="SignalRRealtimeNotifier"/>.
    /// </summary>
    [Authorize]
    public sealed class RealtimeHub : Hub
    {
        public override Task OnConnectedAsync() => base.OnConnectedAsync();
    }
}
