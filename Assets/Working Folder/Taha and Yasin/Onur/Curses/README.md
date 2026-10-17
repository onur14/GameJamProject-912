# Curse assets and tuning

Created with OpenAI built-in imagegen, transparent-background mode. No API key or external stock artwork used.

- SlownessPenalty.png: chained dark iron hourglass, amber sand, deep red chains, three downward chevrons; crisp blocky pixel art, charcoal outlines, transparent background, no text.
- InformationFog.png: dark smoky eye with amber question-mark pupil, indigo-black tendrils and violet wisps; matching crisp pixel-art style, transparent background, no text.

Prompt set: standalone square HUD emblems, readable 64x64-style silhouettes, limited palette, thick near-black outlines, centered with transparent padding, no scene, border, lettering, watermark or drawn checkerboard. The hourglass prompt specifies red cursed chains and gold highlights; the fog prompt specifies an amber question-mark pupil and curling smoke.

Runtime: DispatchCurseDirector activates Slowness Penalty on call 2 and Information Fog on call 5 (Inspector settings). Slowness immediately removes up to 3 of each team, never below zero. This permanent cost is applied once regardless of speed. Both badges last two completed calls. Fog halves positive call awareness gains, rounding upward; original gains return after expiry. Preparation-scene rewards are unchanged.

Retry restores current call-start awareness, teams, decision counters and prior completed-call progress, discarding the failed attempt. Curse costs are not charged again. It does not restart preparation or the whole shift.

Audio: original Assets/SoundEffect files retained. Telefon rings during incoming calls; pickup starts low conversation noise after 0.18 s; closing stops it. NotificationBlink is a 0.19 s faded excerpt of TelefonAcma (0.085-0.275 s). OperatorTick and PresidentTick are 0.045 s excerpts of Konuşurken (0.650-0.695 s) and BaskanKonusması (0.250-0.295 s). Tick spacing is 0.09 s. There was no separate authored notification recording.

Play Mode checks: Logs/CurseChecks/result.txt. Verified costs, low-stock clamp, two-call expiry, fog rounding and expiry, checkpoint restoration, topmost retry-button raycast, audio playback state/timing and saved pixel font. Screenshots in the same directory. Audio routing was tested programmatically, not by listening.
