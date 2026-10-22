# Card presentation v1

- Back: `Assets/Art/Cards/multiverse-card-back-v1.png` (832 x 1216).
- Replacement: assign a new Sprite to `Assets/CardSpriteDatabase.asset` → Card Back Sprite. Keep its `.meta` when replacing the image in place.
- GameScene now references that database. This also enables its existing four number-card artwork mappings; unset special artwork retains the text fallback.
- Front: `CardFrameView.cs` renders a separate dark/champagne double edge and color-coded type plate. `CardUI.cs` positions the number, name and art without baking them into the images.
- Suit labels deliberately retain the actual Green/Yellow/Purple/Black data names; the new faction-to-suit mapping has not been assumed.
- Opponent cards hide all front metadata, including the new frame, and render the back without the disabled overlay.
- No card rules, values or deck composition changed.

Unity Play-mode visual verification is still required.
