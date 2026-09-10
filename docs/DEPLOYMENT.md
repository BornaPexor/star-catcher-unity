# Deployment

## Local development and LAN

Build `Web/` with Unity, then run `docker compose up -d --build --wait`. The default binding is `127.0.0.1:8088`, accessible only on the host. Check `docker compose ps` and `http://localhost:8088/health`.

For another device on the same LAN, copy `.env.example` to `.env`, set `GAME_BIND=0.0.0.0`, and recreate the service. Browse to `http://<server-LAN-IP>:8088`. The device must share a reachable network; host firewall rules may need a narrow allowance for this port on a trusted private network. No router or firewall settings have been changed automatically.

## Public access without a static IP

A tunnel can connect outward from the spare computer to a public HTTPS endpoint. A changing home IP, including the absence of an inbound public IP, is not inherently a blocker for an outbound tunnel. Actual connectivity to the tunnel provider still needs verification on the host network.

For a stable custom-domain deployment with Cloudflare Tunnel:

1. Configure an account/domain and named tunnel using the provider's documented process.
2. Run `cloudflared` on the server host as a managed service.
3. Route the chosen public hostname to `http://localhost:8088` on that host.
4. Keep the game container bound to loopback. Do not expose Docker administration or remote desktop through the public game route.
5. Test the public HTTPS URL from another network and check restart recovery.

If the tunnel connector itself runs in Docker, `localhost` refers to that connector container; use a shared network and the service URL `http://game:80` instead. Tunnel credentials belong outside Git. No tunnel or public hostname has been configured yet.

Tailscale Funnel is another option for a public demo endpoint. Tailscale Serve is for private tailnet access; it does not provide a public portfolio URL by itself. Check current service constraints before choosing a permanent hosting arrangement.

## Operations and growth

The first version serves static files, so upload bandwidth, availability and initial download size generally matter more than server CPU. Measure the final build instead of guessing capacity. For example, a hypothetical 20 MB download over a 10 Mbps uplink takes approximately 16 seconds for one download before overhead or contention.

Keep the host awake, enable Docker startup, maintain backups of source and release artifacts, retain at least the previous working release, and record deployment steps. `restart: unless-stopped` restarts the container when Docker runs; it does not start the operating system or restore a failed internet connection.

When a demo must remain available independently of home power/internet, move or mirror the static Web export to suitable static hosting. GitHub stores project history; it is separate from the running Docker host. GitHub Pages can serve static sites but does not run an arbitrary Docker backend. A future multiplayer design requires a separate latency, transport and server-authority decision.

## Sources

- [Cloudflare Tunnel](https://developers.cloudflare.com/tunnel/)
- [Create a Cloudflare tunnel](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/get-started/create-remote-tunnel/)
- [Tailscale Funnel](https://tailscale.com/docs/features/tailscale-funnel)
- [GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages)
- [Unity browser compatibility](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-browsercompatibility.html)
