using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ZansiHustle.API.Realtime
{
    /// <summary>
    /// PUBLIC realtime hub for the lightweight "app configs changed" signal.
    /// Anonymous on purpose: it only ever pushes a tiny { version, updatedAt }
    /// notification — never any config VALUES. Clients react by re-fetching the
    /// public source of truth (<c>GET /api/app-configs/public</c>), so no
    /// sensitive/admin-only data is ever broadcast. One-way server→client; the
    /// hub exposes no client-callable methods.
    /// </summary>
    [AllowAnonymous]
    public sealed class AppConfigHub : Hub
    {
    }
}
