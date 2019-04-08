![Header](https://github.com/emotitron/NetworkSyncTransform/blob/master/Docs/img/NST_DocumentHeader.jpg?raw=true)
# Network Sync Transform

Networking HLAPI for Photon PUN/PUN2 , UNet and Mirror. This asset incorporates a circular buffer based transform sync engine, creating very smooth and stable movement replication - even in lossy/jittery real-world network conditions.

Additionally, the circular buffer reduces data rates by packaging all outgoing data onto regular network ticks. The bitstream is accessible as well, allowing for additional data to piggyback on these packets - such as health, emotes, timers, etc - rather than uncompressed and adhoc with RPCs/Syncvars.

**Supports:**

- **EXTREMELY low network usage** compared to the UNET/PUN transform sync components, with a range of compression and culling options.
- **Smart Interpolation** with automatic handling of rigidbodies and non-rigidbodies.
- **Extrapolation options** for how buffer under-runs (network hangups) are handled.
- **Buffered frames** to reduce and eliminate hitching from network loss and jitter.
- Server initiated **teleport and auto-teleport**.
- **Bit-level control** over nearly every aspect of the packets created and the resulting packet sizes.
- **Sample components and scenes** demonstrating networked handling of health, movement, weapon fire, etc.
- **Custom Messages** that allow user data to piggyback on sync ticks, such as weapon fire or object throws.
- **Rewind Add-on available** on the Unity Store, with full recreation of colliders on gameobjects, including synced children objects.
- **Element Add-on available** on the Asset Store, for syncing child turrets, arms, heads, etc.
- **Animator Add-on available** on the Asset Store for syncing the unity Animator using NST's buffer and compression.

## Free NST Core Download
You are welcome to use this library for games, however scripts/components not explicity marked with MIT licenses may not sold be part of assets libraries.

[**NST Free 5.7.05 Release** - Unity Package Download](https://github.com/emotitron/NetworkSyncTransform/blob/master/Releases/NST_RELEASE_5705_FREE.unitypackage?raw=true)

## NST Add-ons on Unity Asset Store
These are not required, but they can be purchased on the asset store if you would like to help contribute to this project, or just desire the functionality and would rather not write it yourself.

[**NST Animator Add-on** - Unity Asset Store](https://assetstore.unity.com/packages/tools/network/network-sync-transform-nst-animator-add-on-109433)

[**NST Elements Add-on** - Unity Asset Store](https://assetstore.unity.com/packages/tools/network/network-sync-transform-nst-elements-add-on-107530)

[**NST Rewind Add-on** - Unity Asset Store](https://assetstore.unity.com/packages/tools/network/network-sync-transform-nst-rewind-add-on-109377)

## Transform Crusher PRO Upgrade on Unity Asset Store
Offers bit level control of the compression settings, rather than just the presets.

[**Transform Crusher PRO** - Unity Asset Store](https://assetstore.unity.com/packages/tools/network/transform-crusher-116587)

## Documentation

[Network Sync Transform (NST) Documentation](https://docs.google.com/document/d/1nPWGC_2xa6t4f9P0sI7wAe4osrg4UP0n_9BVYnr5dkQ/edit?usp=sharing)

## Contact
<davincarten@gmail.com>

## Donate
[![Paypal Donations](https://raw.githubusercontent.com/emotitron/NetworkSyncTransform/master/Docs/img/paypaldonate.png)](https://paypal.me/emotitron?locale.x=en_US)

[Paypal donations](https://paypal.me/emotitron?locale.x=en_US) are always welcome!
